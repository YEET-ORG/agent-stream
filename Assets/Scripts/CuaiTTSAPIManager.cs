using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cinemachine;
using DialogueAI;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Net;

// New TTS API Manager for https://dev1.cuai.space/docs
// This replaces the FakeYouAPIManager with a more modern TTS service
public class CuaiTTSAPIManager : MonoBehaviour
{
    #region Constants
    private const string c_ApiBaseUrl = "https://audio.yeetlabs.fun";
    private const int c_MaxRetryAttempts = 5;
    private const int c_RequestTimeoutSeconds = 60;
    // For parallelism control (tune to your server, e.g. 8 or more; server has its own queue)
    private const int c_MaxParallelRequests = 5; 
    #endregion

    #region Private Fields
    [SerializeField] private AudioSource m_AudioSource;
    [SerializeField] private List<AudioClip> m_Clips;
    // [SerializeField] private Burpifier m_Burpifier; // Disabled
    private HttpClient m_Client = new();
    #endregion

    #region Public Fields
    public VideoClip clipToPlay;
    public TextAsset proxyTextFile;
    public SceneDirector sceneDirector;
    public bool usingProxies = true;
    #endregion

    #region Public Properties
    public List<AudioClip> generatedAudioClips = new List<AudioClip>();
    public List<int> failedAudioClips = new List<int>();
    public AudioClip defaultSound;
    public TMP_Text statisText;
    public string statisStatingText = "";
    #endregion

    #region Private Fields
    private int m_NumberOfClipsCompleted = 0;
    private int m_ProxyIndex = 0;
    [SerializeField] private string[] m_ProxyArray;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        // Get proxy servers if using proxies
        if (usingProxies && proxyTextFile != null)
        {
            m_ProxyArray = proxyTextFile.text.Split('\n');
        }
        
        InitializeClient();
    }
    #endregion

    #region Private Methods
    private void InitializeClient()
    {
        var handler = new HttpClientHandler();
        
        if (m_ProxyArray != null && m_ProxyArray.Length > 0)
        {
            // Set proxy for HttpClientHandler if proxies are available
            string[] proxyParts = m_ProxyArray[m_ProxyIndex].Split(':');
            if (proxyParts.Length >= 4)
            {
                var proxy = new WebProxy(proxyParts[0] + ":" + proxyParts[1]);
                proxy.Credentials = new NetworkCredential(proxyParts[2], proxyParts[3]);
                handler.UseProxy = true;
                handler.Proxy = proxy;
            }
        }

        m_Client = new HttpClient(handler);
        m_Client.Timeout = TimeSpan.FromSeconds(c_RequestTimeoutSeconds);
    }

    /// <summary>
    /// Normalize text by replacing unicode characters with ASCII equivalents
    /// </summary>
    private string NormalizeText(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Replace common unicode characters with ASCII equivalents
        var replacements = new Dictionary<string, string>
        {
            // Smart quotes
            { "\u2018", "'" },  // Left single quotation mark
            { "\u2019", "'" },  // Right single quotation mark (apostrophe)
            { "\u201A", "'" },  // Single low-9 quotation mark
            { "\u201B", "'" },  // Single high-reversed-9 quotation mark
            { "\u201C", "\"" }, // Left double quotation mark
            { "\u201D", "\"" }, // Right double quotation mark
            { "\u201E", "\"" }, // Double low-9 quotation mark
            { "\u201F", "\"" }, // Double high-reversed-9 quotation mark
            
            // Dashes
            { "\u2013", "-" },  // En dash
            { "\u2014", "-" },  // Em dash
            { "\u2015", "-" },  // Horizontal bar
            
            // Other common unicode
            { "\u2026", "..." }, // Ellipsis
            { "\u00A0", " " },   // Non-breaking space
            { "\u2022", "*" },   // Bullet
            { "\u2032", "'" },   // Prime (feet/minutes)
            { "\u2033", "\"" },  // Double prime (inches/seconds)
        };

        foreach (var replacement in replacements)
        {
            input = input.Replace(replacement.Key, replacement.Value);
        }

        // Remove any remaining non-ASCII characters
        input = System.Text.RegularExpressions.Regex.Replace(input, @"[^\x00-\x7F]", "");

        return input;
    }

    private string RepeatLastNonSpaceCharacter(string input)
    {
        int nonSpaceCount = 0;
        char lastNonSpaceChar = ' ';

        // Count non-space characters and remember the last non-space character
        foreach (char c in input)
        {
            if (c != ' ')
            {
                nonSpaceCount++;
                lastNonSpaceChar = c;
            }
        }

        // If fewer than 3 non-space characters, repeat the last non-space character
        if (nonSpaceCount < 3 && lastNonSpaceChar != ' ')
        {
            int charactersToAdd = 3 - nonSpaceCount;
            for (int i = 0; i < charactersToAdd; i++)
            {
                input += new string(lastNonSpaceChar, 1);
            }
        }
        
        Debug.Log(input);
        return input;
    }

    private void UpdateStatusText()
    {
        if (statisText != null)
        {
            statisText.text = statisStatingText + " --- " + "Generating Cuai TTS " + m_NumberOfClipsCompleted + "/" + generatedAudioClips.Count;
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Main method to generate TTS audio clips from text lines
    /// </summary>
    public async Task<List<AudioClip>> GenerateTTS(List<string> _linesToSay, List<string> _voiceModels, List<string> _characterNames, TMP_Text updateText, string updateTextStart)
    {
        statisText = updateText;
        statisStatingText = updateTextStart;
        m_NumberOfClipsCompleted = 0;
        
        // Reset the audioclips list and populate it with default sounds
        generatedAudioClips = new List<AudioClip>();
        for (int i = 0; i < _linesToSay.Count; i++)
        {
            bool foundCharacter = false;
            // Add the default sound based on the character that is speaking
            foreach (CharacterInfo character in sceneDirector.characterList)
            {
                if (character.name == _characterNames[i] && character.defaultSound != null)
                {
                    generatedAudioClips.Add(character.defaultSound);
                    foundCharacter = true;
                    break;
                }
            }

            if (!foundCharacter)
            {
                generatedAudioClips.Add(defaultSound);
            }
        }

        // Process TTS requests concurrently with semaphore for rate limiting
        var semaphore = new SemaphoreSlim(c_MaxParallelRequests, c_MaxParallelRequests);
        
        // Create tasks for all TTS generation (concurrent execution)
        var tasks = new List<Task>();
        for (int i = 0; i < _linesToSay.Count; i++)
        {
            int index = i; // Capture index for closure
            tasks.Add(ProcessSingleTTSWithConcurrency(semaphore, index, _linesToSay[index], _voiceModels[index], _characterNames[index], updateText, updateTextStart, _linesToSay.Count));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        return generatedAudioClips;
    }

    /// <summary>
    /// Process a single TTS request with concurrency control
    /// </summary>
    private async Task ProcessSingleTTSWithConcurrency(SemaphoreSlim semaphore, int index, string text, string voiceModel, string characterName, TMP_Text updateText, string updateTextStart, int totalCount)
    {
        await semaphore.WaitAsync();
        try
        {
            if (updateText != null)
            {
                int completed = m_NumberOfClipsCompleted;
                updateText.text = updateTextStart + " --- " + "Generating Cuai TTS " + (completed + 1) + "/" + totalCount;
            }

            AudioClip audioClip = await GenerateSingleTTS(text, voiceModel, characterName);
            if (audioClip != null)
            {
                // Apply burpifier for Rick character - DISABLED
                // if (m_Burpifier != null && characterName.ToLower().Contains("rick"))
                // {
                //     audioClip = m_Burpifier.Burpify(audioClip);
                //     Debug.Log($"Applied burpifier to {characterName}'s line");
                // }
                
                generatedAudioClips[index] = audioClip;
                m_NumberOfClipsCompleted++;
                UpdateStatusText();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating TTS for line {index}: {ex.Message}");
            failedAudioClips.Add(index);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Generate TTS for a single text line
    /// </summary>
    private async Task<AudioClip> GenerateSingleTTS(string text, string voiceModel, string characterName)
    {
        // Ensure text is at least 3 characters
        text = RepeatLastNonSpaceCharacter(text);
        
        // Clean the text - replace unicode characters with ASCII equivalents
        text = NormalizeText(text);
        
        // Remove any control characters that might cause header issues
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[\r\n\t]", " ");
        text = text.Trim();

        // Create form data for the request (application/x-www-form-urlencoded)
        var formData = new Dictionary<string, string>
        {
            { "text", text },
            { "voice_name", voiceModel },
            { "output_format", "wav" },
            { "language", "en" },
            { "exaggeration", "1.0" }
        };

        var content = new FormUrlEncodedContent(formData);
        
        Debug.Log($"Generating TTS - Voice: {voiceModel}, Text: {text.Substring(0, Math.Min(50, text.Length))}...");

        int retryCount = 0;
        while (retryCount < c_MaxRetryAttempts)
        {
            try
            {
                // Create a new client for each request to handle proxy rotation
                using (var requestClient = CreateHttpClient())
                {
                    var response = await requestClient.PostAsync($"{c_ApiBaseUrl}/generate-speech", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        // The API returns the WAV audio data directly
                        byte[] audioData = await response.Content.ReadAsByteArrayAsync();
                        Debug.Log($"TTS Response: Received {audioData.Length} bytes of audio data");
                        
                        // Convert to AudioClip
                        AudioClip audioClip = await ConvertBytesToAudioClip(audioData);
                        return audioClip;
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Debug.LogWarning($"TTS request failed with status: {response.StatusCode}, Content: {errorContent}");
                        retryCount++;
                        await Task.Delay(1000 * retryCount); // Exponential backoff
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"TTS request error (attempt {retryCount + 1}): {ex.Message}");
                retryCount++;
                await Task.Delay(1000 * retryCount);
            }
        }

        Debug.LogError($"Failed to generate TTS after {c_MaxRetryAttempts} attempts for text: {text}");
        return null;
    }

    /// <summary>
    /// Create a new HttpClient with proper configuration
    /// </summary>
    private HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();
        
        if (m_ProxyArray != null && m_ProxyArray.Length > 0)
        {
            m_ProxyIndex = (m_ProxyIndex + 1) % m_ProxyArray.Length;
            string[] proxyParts = m_ProxyArray[m_ProxyIndex].Split(':');
            if (proxyParts.Length >= 4)
            {
                var proxy = new WebProxy(proxyParts[0] + ":" + proxyParts[1]);
                proxy.Credentials = new NetworkCredential(proxyParts[2], proxyParts[3]);
                handler.UseProxy = true;
                handler.Proxy = proxy;
            }
        }

        var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(c_RequestTimeoutSeconds);
        
        return client;
    }

    /// <summary>
    /// Convert byte array to AudioClip
    /// </summary>
    private async Task<AudioClip> ConvertBytesToAudioClip(byte[] audioData)
    {
        // Create a temporary file to store the audio data
        string tempPath = Path.Combine(Application.temporaryCachePath, $"temp_audio_{Guid.NewGuid()}.wav");
        
        try
        {
            // Write the audio data to a temporary file
            await File.WriteAllBytesAsync(tempPath, audioData);
            
            // Use UnityWebRequest to load the audio clip
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip($"file://{tempPath}", AudioType.WAV))
            {
                var operation = www.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Delay(100);
                }
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    return DownloadHandlerAudioClip.GetContent(www);
                }
                else
                {
                    Debug.LogError($"Failed to load audio clip: {www.error}");
                    return null;
                }
            }
        }
        finally
        {
            // Clean up the temporary file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
    #endregion

    #region Cleanup
    void OnDestroy()
    {
        m_Client?.Dispose();
    }
    #endregion
}
