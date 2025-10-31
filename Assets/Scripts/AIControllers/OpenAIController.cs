using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Assets.Scripts.AIControllers;


// this script is used to get responses from chatgpt.
public class OpenAIController : AIController
{
    #region Private Fields
    [SerializeField] private string m_ApiKey;
    #endregion

    private OpenAIAPI api;
    private List<ChatMessage> messages;

    [SerializeField]
    private string outputString;
    public override string OutputString
    {
        get
        {
            return outputString;
        }
        set
        {
            outputString = value;
        }
    }

    [SerializeField]
    private string[] outputLines;
    public override string[] OutputLines
    {
        get
        {
            return outputLines;
        }
        set
        {
            outputLines = value;
        }
    }

    [SerializeField]
    private SceneDirector director;
    public override SceneDirector Director
    {
        get
        {
            return director;
        }
        set
        {
            director = value;
        }
    }

    [SerializeField]
    private List<TextAsset> examples;
    public List<TextAsset> Examples
    {
        get
        {
            return examples;
        }
        set
        {
            examples = value;
        }
    }

    [SerializeField]
    private TextAsset systemMessage;
    public override TextAsset SystemMessage
    {
        get
        {
            return systemMessage;
        }
        set
        {
            systemMessage = value;
        }
    }

    public bool useChatGPT4 = false;


    public int maxTokens = 2000;




    public override void Init()
    {
        // this is the system message. its probably shit but it kinda works

        // Fix SSL/TLS certificate validation issues in Unity
        ServicePointManager.ServerCertificateValidationCallback = 
            delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
            { 
                return true; 
            };
        
        // Configure ServicePointManager for better connection handling
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        ServicePointManager.DefaultConnectionLimit = 10;
        ServicePointManager.Expect100Continue = false;

        // Using OSS models - API key is optional
        api = new OpenAIAPI(string.IsNullOrEmpty(m_ApiKey) ? "" : m_ApiKey);

        string text = systemMessage.text;

        string nextLine = " Here are some example scripts:\n";

        foreach (TextAsset example in examples)
        {
            text += nextLine;
            text += example.text;
            nextLine = "\nHere is another example: \n";
        }

        // add the system message to the messages history.
        messages = new List<ChatMessage> {
            new ChatMessage(ChatMessageRole.System, text)
        };
    }

    // because we give chatgpt the entire history of messages it is important to clear it because there is a max message length.
    // atm this is cleared after every prompt
    public override void Clear()
    {
        string text = systemMessage.text;

        string nextLine = " Here are some example scripts:\n";

        foreach (TextAsset example in examples)
        {
            text += nextLine;
            text += example.text;
            nextLine = "\nHere is another example: \n";
        }

        messages = new List<ChatMessage> {
            new ChatMessage(ChatMessageRole.System, text)
        };
    }

    // ok this is the actual interaction with chatgpt.
    public override async Task<string> EnterPromptAndGetResponse(string inputPrompt)
    {
        // Don't submit empty messages
        if (inputPrompt.Length < 1)
        {
            Debug.Log("message is empty");
            return null;
        }

        inputPrompt += ". Make sure to use light profanity like frick, shoot and crap. Scripts should have at least 30 lines of dialog.";

        ChatMessage userMessage = new ChatMessage();
        userMessage.Role = ChatMessageRole.User;
        userMessage.Content = inputPrompt;

        Debug.Log(string.Format("{0}: {1}", userMessage.rawRole, userMessage.Content));

        // Add the message to the list
        messages.Add(userMessage);

        // Retry logic with exponential backoff
        int retryCount = 0;
        const int maxRetryCount = 5;
        while (retryCount < maxRetryCount)
        {
            try
            {
                // Send the entire chat to OpenAI to get the next message
                var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
                {

                    Model = new Model("gpt-oss:120b"),
                    // Model = new Model("gpt-oss:120b"),

                    Temperature = 0.6,
                    MaxTokens = maxTokens,
                    Messages = messages
                });

                // Get the response message and store it in a response message variable 
                ChatMessage responseMessage = new ChatMessage();
                responseMessage.Role = chatResult.Choices[0].Message.Role;
                responseMessage.Content = chatResult.Choices[0].Message.Content;
                Debug.Log(string.Format("{0}: {1}", responseMessage.rawRole, responseMessage.Content));

                // Add the response to the list of messages
                messages.Add(responseMessage);

                // Exit the retry loop if the request is successful
                return responseMessage.Content;
            }
            catch (HttpRequestException ex)
            {
                retryCount++;
                int delayMs = 2000 * retryCount; // Exponential backoff: 2s, 4s, 6s, 8s, 10s
                
                if (ex.Message.Contains("429"))
                {
                    // Model overloaded, retry after a delay
                    Debug.LogWarning($"TooManyRequests error. Retrying in {delayMs / 1000} seconds... (Attempt {retryCount}/{maxRetryCount})");
                }
                else if (ex.InnerException is System.Net.WebException webEx)
                {
                    // Connection error, retry with longer delay
                    Debug.LogError($"HTTP request error: {ex.Message}");
                    Debug.LogError($"Inner exception: {webEx.Message}");
                    Debug.LogWarning($"Retrying in {delayMs / 1000} seconds... (Attempt {retryCount}/{maxRetryCount})");
                }
                else
                {
                    // Other HTTP request error occurred
                    Debug.LogError($"HTTP request error: {ex}");
                    Debug.LogWarning($"Retrying in {delayMs / 1000} seconds... (Attempt {retryCount}/{maxRetryCount})");
                }
                
                if (retryCount < maxRetryCount)
                {
                    await Task.Delay(delayMs);
                }
            }
            catch (Exception ex)
            {
                // Other exceptions occurred, log the exception
                Debug.LogError($"Unexpected error: {ex}");
                retryCount++;
                
                if (retryCount < maxRetryCount)
                {
                    int delayMs = 2000 * retryCount;
                    Debug.LogWarning($"Retrying in {delayMs / 1000} seconds... (Attempt {retryCount}/{maxRetryCount})");
                    await Task.Delay(delayMs);
                }
                else
                {
                    break;
                }
            }
        }

        // If all retries failed, return null
        Debug.LogError("All retry attempts failed.");
        return null;
    }

}