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
using Unity.VisualScripting;


// this script is used to get responses from chatgpt.
public class OpenAISlurDetector : MonoBehaviour
{
    #region Private Fields
    [SerializeField] private string m_ApiKey;
    #endregion

    private OpenAIAPI api;
    private List<ChatMessage> messages;

    private string systemMessage = "";

    private bool isEnabled = true;

    void Start()
    {
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

        // this is the system message. its probably shit but it kinda works
        systemMessage += "I am making ai generated Rick and morty episodes, where the topics that rick and morty are talking about are chosen by a live youtube chat. ";
        systemMessage += "A different chatgpt agent takes this topic and generates a script.";
        systemMessage += " All content including profanity and adult comedy is allowed.";


        // systemMessage += "you create terminal commands to satisfy a user's query for doing engineering/programming ";
        Debug.Log("system message: \n" + systemMessage);

        // Using OSS models - API key is optional
        api = new OpenAIAPI(string.IsNullOrEmpty(m_ApiKey) ? "" : m_ApiKey);

        // add the system message to the messages history.
        messages = new List<ChatMessage> {
            new ChatMessage(ChatMessageRole.System, systemMessage)
        };

    }

    // because we give chatgpt the entire history of messages it is important to clear it because there is a max message length.
    // atm this is cleared after every prompt
    public void ClearMessages()
    {

        messages = new List<ChatMessage> {
            new ChatMessage(ChatMessageRole.System, systemMessage)
        };
    }

    // takes the chatgpt output and seperates it into a string array, each string in the array is a new dialog line or an action.
    // this also is where we remove a bunch of nono
    // public string[] ProcessOutputIntoStringArray(string chatgptOutputMessage)
    // {
    //     // outputString = messages[messages.Count - 1].Content;
    //     outputString = chatgptOutputMessage;
    //     Debug.Log(outputString);
    //     // textField.text = outputString;

    //     outputString = outputString.Replace("frick", "fuck");
    //     outputString = outputString.Replace("Frick", "Fuck");
    //     outputString = outputString.Replace("Freakin", "Fuckin");
    //     outputString = outputString.Replace("freakin", "fuckin");
    //     outputString = outputString.Replace("crap", "shit");
    //     outputString = outputString.Replace("Crap", "Shit");
    //     outputString = outputString.Replace("shoot", "shit");
    //     outputString = outputString.Replace("Shoot", "Shit");

    //     string[] outputLinesProcessed = outputString.Split(Environment.NewLine,
    //                 StringSplitOptions.RemoveEmptyEntries);

    //     char[] delims = new[] { '\r', '\n' };
    //     outputLinesProcessed = outputString.Split(delims, StringSplitOptions.RemoveEmptyEntries);
    //     return outputLinesProcessed;
    // }

    public string RemoveDirectSlurs(string chatgptOutputString)
    {
       
        return chatgptOutputString;
    }

    // ok this is the actual interaction with chatgpt.
    public async Task<string> EnterPromptAndGetResponse(string inputPrompt)
    {
        if (!isEnabled)
        {
            return string.Empty;
        }

        ClearMessages();
        // Don't submit empty messages
        if (inputPrompt.Length < 1)
        {
            Debug.Log("message is empty");
            return null;
        }


        ChatMessage userMessage = new ChatMessage();
        userMessage.Role = ChatMessageRole.User;
        userMessage.Content = inputPrompt;

        Debug.Log(string.Format("{0}: {1}", userMessage.rawRole, userMessage.Content));

        // Add the message to the list
        messages.Add(userMessage);

        // Retry logic
        int retryCount = 0;
        const int maxRetryCount = 3;
        while (retryCount < maxRetryCount)
        {
            try
            {
                // Send the entire chat to OpenAI to get the next message
                var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
                {

                    Model = new Model("gpt-oss:120b"),
                    Temperature = 1,
                    MaxTokens = 3000,
                    Messages = messages
                });

                // Get the response message and store it in a response message variable 
                ChatMessage responseMessage = new ChatMessage();
                responseMessage.Role = chatResult.Choices[0].Message.Role;
                responseMessage.Content = chatResult.Choices[0].Message.Content;
                Debug.Log(string.Format("{0}: {1}", responseMessage.rawRole, responseMessage.Content));
                // Debug.Log(chatResult.Choices[0].Message.Function_Call.ToString());
                // Debug.Log(chatResult.Choices[0].Message.Function_Call.Arguments.ToString());
                // Debug.Log(chatResult.Choices[0].Message.Function_Call.Arguments);
                // Add the response to the list of messages
                messages.Add(responseMessage);

                // Exit the retry loop if the request is successful
                break;
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("429"))
                {
                    // Model overloaded, retry after a delay
                    retryCount++;
                    Debug.LogWarning("TooManyRequests error. Retrying in 1 second...");
                    await Task.Delay(1000); // Wait for 1 second before retrying
                }
                else
                {
                    // Other HTTP request error occurred, log the exception
                    Debug.LogError($"HTTP request error: {ex}");
                    Debug.Log("Retrying in 1 second...");
                    retryCount++;
                    await Task.Delay(1000);
                    // break;
                }
            }
            catch (Exception ex)
            {
                // Other exceptions occurred, log the exception
                Debug.LogError($"Error: {ex}");
                break;
            }



        }
        // Debug.Log("hello");

        //return the message
        return messages[messages.Count - 1].Content;

    }

    // this is just me testing shit done worry
    public static object[] GetFunctionList()
    {
        List<object> functionList = new List<object>();

        // Define the 'get_current_weather' function
        var getCurrentWeather = new
        {
            name = "get_commands",
            description = "Get a list of bash commands on an Ubuntu machine to run",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    commands = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "string",
                            description = "A terminal command string"
                        },
                        description = "List of terminal command strings to be executed"
                    },
                    example = new
                    {
                        type = "string",
                        description = "an example of using the word in a sentence"
                    }

                },
                required = new string[] { "commands"}
            }
        };

        // Add 'get_current_weather' to the function list
        functionList.Add(getCurrentWeather);

        // Return as an object array
        return functionList.ToArray();
    }
}

