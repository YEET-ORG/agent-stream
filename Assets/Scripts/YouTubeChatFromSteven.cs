using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.IO;

// this is from steven4547466 on discord, hes a legend.





// this script reads shit from chat, well it recieves messages from a python script that is reading chat
// it then populates a topic suggestions and  vote suggestions list 
public class YouTubeChatFromSteven : MonoBehaviour
{

    public class ChatMessage
    {
        public string author { get; set; }
        public string text { get; set; }

        public ChatMessage(string author, string text)
        {
            this.author = author;
            this.text = text;
        }

        public ChatMessage() { }

        public override string ToString()
        {
            return $"Author: {author} | Message: {text}";
        }
    }

    private int maxQueueSize = 1000;

    public Queue<string> topicQueue = new Queue<string>();

    public List<string> voteSuggestions = new List<string>();

    List<string> alreadyTakenTopics = new List<string>();

    List<string> wordBlacklist = new List<string> { };  // List of predefined words that topics cannot contain

    private bool connected = false;

    public HttpListener listener;
    public int port = 9999;
    public bool usingYoutubeChatStuff = true;
    private string saveFilePath = "Assets/topicSuggestions/topicSuggestions.txt";

    void Start()
    {
        if (usingYoutubeChatStuff)
        {
            // Don't load old prompts from file - only use live chat prompts
            // LoadTopicsFromFile(); // DISABLED - only use live prompts from chat
            topicQueue.Clear(); // Ensure queue starts empty
            InvokeRepeating("SaveTopicsToFile", 60f, 60f); // Save topics to file every 60 seconds (for persistence, but don't load on startup)
            Server();
        }
    }


    // Method to save the topicQueue to a file
    private void SaveTopicsToFile()
    {
        try
        {
            File.WriteAllLines(saveFilePath, topicQueue.ToArray());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving topics to file: {ex.Message}");
        }
    }

    // Method to load the topicQueue from a file
    private void LoadTopicsFromFile()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                var lines = File.ReadAllLines(saveFilePath);
                topicQueue.Clear(); // Clear existing items
                for (int i = 0; i < lines.Length; i += 2)
                {
                    if (i + 1 < lines.Length) // Ensure there's an author for every topic
                    {
                        string topicMessage = lines[i];
                        string author = lines[i + 1];
                        topicQueue.Enqueue(topicMessage + "\n" + author);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading topics from file: {ex.Message}");
            }
        }
    }
    public static byte[] responseBuffer = Encoding.UTF8.GetBytes("OK");
    public void Server()
    {
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        Task.Run(() =>
        {
            while (listener.IsListening)
            {
                HttpListenerContext ctx = listener.GetContext();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                List<ChatMessage> messages = JsonConvert.DeserializeObject<List<ChatMessage>>(new StreamReader(req.InputStream).ReadToEnd());

                foreach (ChatMessage chatMessage in messages)
                {
                    string message = chatMessage.text;
                    string author = chatMessage.author;

                    if (string.IsNullOrWhiteSpace(message))
                    {
                        continue;
                    }

                    Debug.Log("recieved: " + message);

                    // Check if it's a vote first
                    if (message.ToLower().StartsWith("vote:"))
                    {
                        string voteMessage = message.Substring("vote:".Length).Trim();

                        // Check if the message after "vote:" is not empty or only spaces
                        if (!string.IsNullOrWhiteSpace(voteMessage))
                        {
                            // Add new message text to message texts
                            voteSuggestions.Add(voteMessage);
                        }
                    }
                    // Otherwise, treat ALL messages as topic suggestions (for pump.fun chat)
                    else
                    {
                        string topicMessage = message.Trim();

                        // Check if the topicMessage contains any word from the wordBlacklist
                        bool containsBlacklistedWord = wordBlacklist.Any(blackWord => topicMessage.ToLower().Contains(blackWord.ToLower()));
                        bool haveAlreadyDoneTopic = alreadyTakenTopics.Contains(topicMessage);
                        
                        // Check if the message is not empty or only spaces
                        if (!string.IsNullOrWhiteSpace(topicMessage) && !haveAlreadyDoneTopic && !containsBlacklistedWord)
                        {
                            // Add new message to queue
                            string formattedTopic = topicMessage + "\n" + author;
                            topicQueue.Enqueue(formattedTopic);

                            Debug.Log($"✅ YouTubeChatFromSteven: Added topic to queue: '{topicMessage}' by {author} (Queue size: {topicQueue.Count})");

                            // Limit the size of queue
                            if (topicQueue.Count > maxQueueSize)
                            {
                                // Remove oldest message from queue
                                string removed = topicQueue.Dequeue();
                                Debug.Log($"YouTubeChatFromSteven: Removed oldest topic from queue (queue full): '{removed}'");
                            }
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(topicMessage))
                            {
                                Debug.Log($"⚠️ YouTubeChatFromSteven: Skipped empty topic message from {author}");
                            }
                            else if (haveAlreadyDoneTopic)
                            {
                                Debug.Log($"⚠️ YouTubeChatFromSteven: Skipped duplicate topic: '{topicMessage}'");
                            }
                            else if (containsBlacklistedWord)
                            {
                                Debug.Log($"⚠️ YouTubeChatFromSteven: Skipped blacklisted topic: '{topicMessage}'");
                            }
                        }
                    }
                }

                resp.OutputStream.Write(responseBuffer, 0, responseBuffer.Length);
                resp.Close();
            }

            listener.Stop();
        });
    }

    // adds a topic to the already taken topics list
    public void AddToBlacklist(string topic)
    {
        if (!string.IsNullOrWhiteSpace(topic) && !alreadyTakenTopics.Contains(topic))
        {
            alreadyTakenTopics.Add(topic);
        }
    }

    //read it bitch
    public void ClearVotes()
    {
        voteSuggestions = new List<string>();
    }


    // counts votes and retuns a int array, which will look like [5,12,123] this means 5 votes for topic 1 ect.
    public int[] CountVotes()
    {
        int[] counts = new int[3];  // Array to hold the counts for "1", "2", "3"

        foreach (string vote in voteSuggestions)
        {
            string cleanedVote = vote.Trim();  // Remove leading and trailing white spaces

            if (cleanedVote == "1")
            {
                counts[0]++;
            }
            else if (cleanedVote == "2")
            {
                counts[1]++;
            }
            else if (cleanedVote == "3")
            {
                counts[2]++;
            }
        }

        return counts;
    }

    // Get the next topic from the queue (FIFO)
    public string GetNextTopic()
    {
        if (topicQueue.Count > 0)
        {
            string topic = topicQueue.Dequeue();
            string[] parts = topic.Split('\n');
            if (parts.Length >= 1)
            {
                alreadyTakenTopics.Add(parts[0]); // add just the message to the already taken topics
            }
            return topic;
        }
        return null;
    }



}

