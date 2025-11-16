using Assets.Scripts.AIControllers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Text;


// this is the big daddy script that controls everything
// basically has a async function that continuously collects suggestions from chat, creates scenes and plays scenes. 
public class WholeThingManager : MonoBehaviour
{
    public static WholeThingManager Singleton;
    public AIController AIController;
    public OpenAICameraDirector openAICameraDirector;
    public SceneDirector sceneDirector;
    public CuaiTTSAPIManager cuaiTTSAPIManager;
    public YouTubeChatFromSteven youTubeChat;
    public ReplicateAPI replicateAPI;

    public bool usingVoiceActing = true;

    public float wordsPerMinute = 250;

    public bool currentlyRunningScene = false;

    public TMP_Text textField;
    public TMP_Text dialogBox;


    private RickAndMortyScene nextScene = null;
    private bool stillGeneratingScene = false;


    public TMP_Text topicOption1;
    public TMP_Text topic1Votes;
    public BarWidthController topic1Bar;
    public TMP_Text topicOption2;
    public TMP_Text topic2Votes;
    public BarWidthController topic2Bar;
    public TMP_Text topicOption3;
    public TMP_Text topic3Votes;
    public BarWidthController topic3Bar;
    public TMP_Text topicTitleThing;
    public TMP_Text titleText;

    public GameObject bottomBarVotingInfoText;
    public GameObject topBarDiscordPluf;
    public string firstPrompt = ""; // No default prompt - only use chat prompts
    public bool runMainLoop = true;

    [Header("Manual Input Mode")]
    public bool useManualInputMode = false;
    public TMP_InputField manualInputField;
    public GameObject manualInputPanel;
    private string manualPromptInput = "";
    private bool manualPromptSubmitted = false;

    public bool usingChatGptCameraShots = true;

    public bool useAiArt = true;

    public CharacterInfo defaultGuy;
    public AiArtDimensionController aiArtDimension;
    public AiArtCharacterController aiArtCharacter;

    public bool useDefaultScript = false;
    public TextAsset defaultScript;


    public bool waitForVoting = true;

    public bool justDoOneScene = false;

    public bool runningTestTopicList = false;
    public List<string> testTopicList;

    public RandomCameraDance danceFloorManager;
    void Start()
    {
        Singleton = this;
        ToggleDiscordPlugEvery10Seconds();
        titleText.gameObject.SetActive(false);
        enableOrDisableVotingUI(false);

        AIController.Init();
        openAICameraDirector.Init();
        
        // Setup manual input mode
        if (useManualInputMode)
        {
            // Hide all voting UI in manual input mode
            enableOrDisableVotingUI(false);
            if (bottomBarVotingInfoText != null)
            {
                bottomBarVotingInfoText.SetActive(false);
            }
            
            if (manualInputField != null)
            {
                manualInputField.onSubmit.AddListener(OnManualInputSubmit);
            }
            if (manualInputPanel != null)
            {
                manualInputPanel.SetActive(true);
            }
        }
        else
        {
            if (manualInputPanel != null)
            {
                manualInputPanel.SetActive(false);
            }
        }
        
        if (runMainLoop)
        {
            MainLoop();
        }
        // TestingShit();	

    }

    private async Task TestingShit()
    {
        // chill for a bit to give time to setup everything	
        await Task.Delay(2000);
        // Testing code removed
    }
    async void ToggleDiscordPlugEvery10Seconds()
    {
        while (true)
        {
            await Task.Delay(10000);
            topBarDiscordPluf.SetActive(!topBarDiscordPluf.activeSelf);
        }
    }

    void OnDestroy()
    {
        Singleton = null;
        
        // Cleanup manual input listener
        if (manualInputField != null)
        {
            manualInputField.onSubmit.RemoveListener(OnManualInputSubmit);
        }
    }

    private void OnManualInputSubmit(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            manualPromptInput = input;
            manualPromptSubmitted = true;
            Debug.Log("Manual prompt submitted: " + input);
        }
    }

    // turns on or off all the voting ui 
    private void enableOrDisableVotingUI(bool enable)
    {


        // topicTitleThing.enabled = enable;

        topic1Bar.gameObject.SetActive(enable);
        topic1Votes.enabled = enable;
        topicOption1.enabled = enable;

        topic2Bar.gameObject.SetActive(enable);
        topic2Votes.enabled = enable;
        topicOption2.enabled = enable;

        topic3Bar.gameObject.SetActive(enable);
        topic3Votes.enabled = enable;
        topicOption3.enabled = enable;

        bottomBarVotingInfoText.SetActive(enable);
    }


    // this is the big daddy
    private async Task MainLoop()
    {

        // chill for a bit to give time to setup everything
        await Task.Delay(2000);

        RickAndMortyScene currentScene = null;
        bool firstRunThrough = true;

        // if (runningTestTopicList && testTopicList.Count > 0)
        // {
        //     CreateScene(testTopicList[0], "me", "banana", "me", usingVoiceActing);
        //     testTopicList.RemoveAt(0);

        // }
        // else
        // {
        //     CreateScene(firstPrompt, "me", "banana", "me", usingVoiceActing);
        // }

        for (int i = 0; i < 1000; i++)
        {


            float waitingCounter = 0;
            // if we're currently running a scene wait 
            while (currentlyRunningScene)
            {
                await Task.Delay(1000);

                // if we have waited for more that 8 minutes restart everything.
                waitingCounter += 1;
                // if (waitingCounter > 8 * 60)
                // {
                //     SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                //     return;
                // }
            }

            // so scene has finished playing
            Debug.Log("scene done");
            dialogBox.text = "";

            string chosenTopicText = "";
            string chosenTopicAuthor = "";
            string backupTopicText = "";
            string backupTopicAuthor = "";

            // Check if we're using manual input mode
            if (useManualInputMode)
            {
                // Manual input mode - check for pump.fun chat messages first, then wait for user input
                // Keep voting UI hidden
                enableOrDisableVotingUI(false);
                
                // Check if there are any pump.fun chat messages available
                string chatPrompt = "";
                string chatAuthor = "";
                
                // Debug: Log current state
                if (youTubeChat == null)
                {
                    Debug.LogWarning("WholeThingManager: youTubeChat is null! Make sure it's assigned in Inspector.");
                }
                else if (youTubeChat.topicQueue == null)
                {
                    Debug.LogWarning("WholeThingManager: youTubeChat.topicQueue is null!");
                }
                else
                {
                    Debug.Log($"WholeThingManager: Checking for chat prompts. Current topicQueue count: {youTubeChat.topicQueue.Count}");
                }
                
                // Get the next topic from the queue (FIFO)
                // Check multiple times with small delay to catch messages that arrive just after loop starts
                for (int checkAttempt = 0; checkAttempt < 3; checkAttempt++)
                {
                    if (youTubeChat != null && youTubeChat.topicQueue != null && youTubeChat.topicQueue.Count > 0)
                    {
                        string nextTopic = youTubeChat.GetNextTopic();
                        
                        if (!string.IsNullOrEmpty(nextTopic))
                        {
                            Debug.Log($"WholeThingManager: Found next topic from queue: '{nextTopic}'");
                            
                            string[] parts = nextTopic.Split('\n');
                            if (parts.Length >= 2)
                            {
                                chatPrompt = parts[0].Trim();
                                chatAuthor = parts[1].Trim();
                                
                                // Validate the prompt is not empty
                                if (!string.IsNullOrWhiteSpace(chatPrompt))
                                {
                                    Debug.Log($"prompt: '{chatPrompt}' by {chatAuthor}");
                                    Debug.Log($"✅ WholeThingManager: Detected and using pump.fun chat prompt: '{chatPrompt}' by {chatAuthor}");
                                    break; // Found valid prompt, exit check loop
                                }
                                else
                                {
                                    Debug.LogWarning($"WholeThingManager: Chat prompt is empty, skipping. Full topic: '{nextTopic}'");
                                    chatPrompt = ""; // Clear it so we don't use empty prompt
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"WholeThingManager: Topic format invalid (expected 'prompt\\nauthor', got {parts.Length} parts). Full topic: '{nextTopic}'");
                            }
                        }
                    }
                    
                    // If no prompt found and not last attempt, wait a bit before checking again
                    if (string.IsNullOrEmpty(chatPrompt) && checkAttempt < 2)
                    {
                        await Task.Delay(200); // Wait 200ms before checking again
                    }
                }
                
                if (string.IsNullOrEmpty(chatPrompt))
                {
                    Debug.Log("WholeThingManager: No chat prompts available in queue after checking, will show manual input UI");
                    chatPrompt = ""; // Ensure it's empty to trigger manual input
                }
                
                if (!string.IsNullOrEmpty(chatPrompt))
                {
                    // Use the pump.fun chat message directly
                    // Hide manual input UI since we're using chat
                    if (manualInputPanel != null)
                    {
                        manualInputPanel.SetActive(false);
                    }
                    
                    chosenTopicText = chatPrompt;
                    chosenTopicAuthor = chatAuthor;
                    backupTopicText = ""; // No backup prompt
                    backupTopicAuthor = "";
                    
                    Debug.Log($"✅ USING CHAT PROMPT: '{chatPrompt}' by {chatAuthor}");
                    
                    // Start dance floor camera while scene is being generated
                    danceFloorManager.DanceCameraStart();
                    
                    // Generate the scene immediately
                    CreateScene(chosenTopicText, chosenTopicAuthor, backupTopicText, backupTopicAuthor, usingVoiceActing);
                    
                    // Wait for scene generation to complete
                    while (stillGeneratingScene)
                    {
                        await Task.Delay(500);
                    }
                    
                    // Stop dance floor camera when scene is ready
                    danceFloorManager.DanceCameraStop();
                    
                    // Play the generated scene immediately
                    if (nextScene != null)
                    {
                        currentScene = nextScene;
                        nextScene = null; // Clear it so we don't play it again
                        RunScene(currentScene);
                    }
                    
                    // Skip the rest of the loop
                    if (justDoOneScene) { return; }
                    continue;
                }
                
                // No chat message available, show manual input UI
                if (manualInputPanel != null)
                {
                    manualInputPanel.SetActive(true);
                }
                
                manualPromptSubmitted = false;
                manualPromptInput = "";
                
                // Wait for manual input submission OR check for new chat prompts
                while (!manualPromptSubmitted)
                {
                    // Check if a new chat prompt arrived while waiting
                    if (youTubeChat != null && youTubeChat.topicQueue != null && youTubeChat.topicQueue.Count > 0)
                    {
                        string nextTopic = youTubeChat.GetNextTopic();
                        if (!string.IsNullOrEmpty(nextTopic))
                        {
                            string[] parts = nextTopic.Split('\n');
                            if (parts.Length >= 2)
                            {
                                string newChatPrompt = parts[0].Trim();
                                string newChatAuthor = parts[1].Trim();
                                
                                if (!string.IsNullOrWhiteSpace(newChatPrompt))
                                {
                                    Debug.Log($"✅ New chat prompt arrived while waiting: '{newChatPrompt}' by {newChatAuthor}");
                                    
                                    // Hide manual input and use the chat prompt
                                    if (manualInputPanel != null)
                                    {
                                        manualInputPanel.SetActive(false);
                                    }
                                    
                                    chosenTopicText = newChatPrompt;
                                    chosenTopicAuthor = newChatAuthor;
                                    backupTopicText = "";
                                    backupTopicAuthor = "";
                                    
                                    Debug.Log($"✅ USING CHAT PROMPT: '{newChatPrompt}' by {newChatAuthor}");
                                    
                                    // Start dance floor camera while scene is being generated
                                    danceFloorManager.DanceCameraStart();
                                    
                                    // Generate the scene immediately
                                    CreateScene(chosenTopicText, chosenTopicAuthor, backupTopicText, backupTopicAuthor, usingVoiceActing);
                                    
                                    // Wait for scene generation to complete
                                    while (stillGeneratingScene)
                                    {
                                        await Task.Delay(500);
                                    }
                                    
                                    // Stop dance floor camera when scene is ready
                                    danceFloorManager.DanceCameraStop();
                                    
                                    // Play the generated scene immediately
                                    if (nextScene != null)
                                    {
                                        currentScene = nextScene;
                                        nextScene = null; // Clear it so we don't play it again
                                        RunScene(currentScene);
                                    }
                                    
                                    // Skip the rest of the loop
                                    if (justDoOneScene) { return; }
                                    continue; // Go back to start of main loop
                                }
                            }
                        }
                    }
                    
                    await Task.Delay(100);
                }
                
                chosenTopicText = manualPromptInput;
                chosenTopicAuthor = "Manual Input";
                backupTopicText = ""; // No backup prompt
                backupTopicAuthor = "";
                
                Debug.Log($"✅ USING MANUAL INPUT PROMPT: '{manualPromptInput}'");
                
                // Clear the input field
                if (manualInputField != null)
                {
                    manualInputField.text = "";
                }
                
                // Hide the prompt box and start dance floor
                if (manualInputPanel != null)
                {
                    manualInputPanel.SetActive(false);
                }
                
                // Start dance floor camera while scene is being generated
                danceFloorManager.DanceCameraStart();
                
                // Generate the scene immediately in manual mode
                CreateScene(chosenTopicText, chosenTopicAuthor, backupTopicText, backupTopicAuthor, usingVoiceActing);
                
                // Wait for scene generation to complete
                while (stillGeneratingScene)
                {
                    await Task.Delay(500);
                }
                
                // Stop dance floor camera when scene is ready
                danceFloorManager.DanceCameraStop();
                
                // Play the generated scene immediately
                if (nextScene != null)
                {
                    currentScene = nextScene;
                    nextScene = null; // Clear it so we don't play it again
                    RunScene(currentScene);
                }
                
                // Skip the rest of the loop for manual mode
                if (justDoOneScene) { return; }
                continue;
            }
            else
            {
                // Original voting mode - using queue now (FIFO)
                enableOrDisableVotingUI(true);
                
                // Get 3 topics from queue for voting
                List<string> randomTopics = new List<string>();
                for (int j = 0; j < 3; j++)
                {
                    if (youTubeChat != null && youTubeChat.topicQueue != null && youTubeChat.topicQueue.Count > 0)
                    {
                        string topic = youTubeChat.GetNextTopic();
                        if (!string.IsNullOrEmpty(topic))
                        {
                            randomTopics.Add(topic);
                            string[] parts = topic.Split('\n');
                            if (parts.Length >= 2)
                            {
                                Debug.Log($"prompt: '{parts[0].Trim()}' by {parts[1].Trim()}");
                            }
                        }
                    }
                }

                if (randomTopics.Count == 0)
                {
                    // No prompts available - can't proceed with voting mode
                    Debug.Log("No prompts available in queue for voting mode. Waiting for chat prompts...");
                    await Task.Delay(2000);
                    continue;
                }

                // Need at least 3 topics for voting - if we have less, wait for more
                if (randomTopics.Count < 3)
                {
                    Debug.Log($"Only {randomTopics.Count} prompt(s) available, need 3 for voting. Waiting for more chat prompts...");
                    // Put the topics back in the queue
                    foreach (string topic in randomTopics)
                    {
                        youTubeChat.topicQueue.Enqueue(topic);
                    }
                    await Task.Delay(2000);
                    continue;
                }

                // the topics are stored like "name of topic \nauthor name \n"
                // so lets extract the topic and author 
                List<string> randomTopicAuthors = new List<string>();

                for (int j = 0; j < randomTopics.Count; j++)
                {
                    string topic = randomTopics[j].Split("\n")[0];
                    string author = randomTopics[j].Split("\n")[1];
                    randomTopics[j] = topic;
                    randomTopicAuthors.Add(author);
                }

                //display the topics
                topicOption1.text = randomTopics[0];
                topicOption2.text = randomTopics[1];
                topicOption3.text = randomTopics[2];

                // lets start the voting
                youTubeChat.ClearVotes();
                float voteTime = 0;
                int[] voteNumbers = youTubeChat.CountVotes();

                topic1Bar.ResetBar();
                topic2Bar.ResetBar();
                topic3Bar.ResetBar();
                targetTopic1Votes = 0;
                targetTopic2Votes = 0;
                targetTopic3Votes = 0;

                // since we are generating a scene in the background while we play a scene, the generating scene needs to finish generating before we finish voting
                // and we also wait a minimum of 30 seconds

                danceFloorManager.DanceCameraStart();


                while (stillGeneratingScene || (voteTime < 30f && waitForVoting))
                {
                    //get the votes
                    voteNumbers = youTubeChat.CountVotes();


                    // this is for testing
                    // voteNumbers[0] = UnityEngine.Random.Range(1, 101);
                    // voteNumbers[1] = UnityEngine.Random.Range(1, 101);
                    // voteNumbers[2] = UnityEngine.Random.Range(1, 101);


                    // all this shit is for having the vote text move smoothly, dont worry about it
                    initialTopic1Votes = targetTopic1Votes;
                    initialTopic2Votes = targetTopic2Votes;
                    initialTopic3Votes = targetTopic3Votes;
                    targetTopic1Votes = voteNumbers[0];
                    targetTopic2Votes = voteNumbers[1];
                    targetTopic3Votes = voteNumbers[2];
                    StopCoroutine(UpdateVotesTextOverTime(topic1Votes, initialTopic1Votes, targetTopic1Votes));
                    StartCoroutine(UpdateVotesTextOverTime(topic1Votes, initialTopic1Votes, targetTopic1Votes));
                    StopCoroutine(UpdateVotesTextOverTime(topic2Votes, initialTopic2Votes, targetTopic2Votes));
                    StartCoroutine(UpdateVotesTextOverTime(topic2Votes, initialTopic2Votes, targetTopic2Votes));
                    StopCoroutine(UpdateVotesTextOverTime(topic3Votes, initialTopic3Votes, targetTopic3Votes));
                    StartCoroutine(UpdateVotesTextOverTime(topic3Votes, initialTopic3Votes, targetTopic3Votes));


                    //calculate the highest votes so we can fill the vote bars relative to it.
                    int maxvotes = 0;
                    foreach (int voteNumber in voteNumbers)
                    {
                        if (maxvotes < voteNumber)
                        {
                            maxvotes = voteNumber;
                        }
                    }
                    if (maxvotes == 0)
                    {
                        topic1Bar.SetFillPercentage(0);
                        topic2Bar.SetFillPercentage(0);
                        topic3Bar.SetFillPercentage(0);
                    }
                    else
                    {
                        topic1Bar.SetFillPercentage((float)voteNumbers[0] / (float)maxvotes);
                        topic2Bar.SetFillPercentage((float)voteNumbers[1] / (float)maxvotes);
                        topic3Bar.SetFillPercentage((float)voteNumbers[2] / (float)maxvotes);
                    }

                    // wait for a little bit.
                    await Task.Delay(500);
                    voteTime += 0.5f;

                    // if we have been voting for more than 5 minutes (10 minutes if this is the first loop) then restart everything 
                //     float timeBeforeRestarting = 5 * 60;
                //     if (firstRunThrough) timeBeforeRestarting += 5 * 60;
                //     if (voteTime > timeBeforeRestarting)
                //     {
                //         SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                //         return;
                //     }
                }

                // ok voting is done 

                //get the chosen topic, in the ugliest way possible, wtf is this shit.
                int chosenTopic = 0;
                if (voteNumbers[0] > voteNumbers[1] && voteNumbers[0] > voteNumbers[2]) chosenTopic = 0;
                if (voteNumbers[1] > voteNumbers[0] && voteNumbers[1] > voteNumbers[2]) chosenTopic = 1;
                if (voteNumbers[2] > voteNumbers[1] && voteNumbers[2] > voteNumbers[0]) chosenTopic = 2;

                // No backup topic - only use the chosen one
                chosenTopicText = randomTopics[chosenTopic];
                chosenTopicAuthor = randomTopicAuthors[chosenTopic];
                backupTopicText = ""; // No backup prompt
                backupTopicAuthor = "";
                
                Debug.Log($"✅ USING VOTING PROMPT: '{chosenTopicText}' by {chosenTopicAuthor} (votes: {voteNumbers[chosenTopic]})");

                youTubeChat.AddToBlacklist(randomTopics[chosenTopic]);
                enableOrDisableVotingUI(false);

                danceFloorManager.DanceCameraStop();
            }

            // add the chosen topic to the blacklist so it doesnt play again

            // This section is only for voting mode
            if (nextScene != null)
            {
                currentScene = nextScene;
                RunScene(currentScene);

            }

            // both of these are async functions, so they will run in the backgound, this means we are running a scene and generating a scene at the same time. 

            if (justDoOneScene) { return; }

            // Test topic list disabled - only use chat or manual input
            // if (runningTestTopicList && testTopicList.Count > 0)
            // {
            //     CreateScene(testTopicList[0], "me", "banana", "me", usingVoiceActing);
            //     testTopicList.RemoveAt(0);
            // }
            // else 
            if (!useManualInputMode) // Only generate in background for voting mode
            {
                // Only create scene if we have a valid prompt from voting
                if (!string.IsNullOrEmpty(chosenTopicText))
                {
                    Debug.Log($"Creating scene with prompt from voting: '{chosenTopicText}' by {chosenTopicAuthor}");
                    CreateScene(chosenTopicText, chosenTopicAuthor, backupTopicText, backupTopicAuthor, usingVoiceActing);
                }
                else
                {
                    Debug.LogWarning("No valid prompt available for scene creation. Skipping.");
                }
            }

            firstRunThrough = false;
        }

    }

    // this bad boy displays the title then runs the scene 
    public async Task RunScene(RickAndMortyScene scene)
    {
        currentlyRunningScene = true;

        // display title

        aiArtDimension.UpdateTextureForNextScene();
        aiArtCharacter.UpdateTextureForNextScene();
        //run the scene
        await sceneDirector.PlayScene(scene.chatGPTOutputLines, scene.ttsVoiceActingLines);

        // scene done
        currentlyRunningScene = false;
    }


    // this is the main bitch of the program. a bunch of calling other scripts to get each element of the scene.
    // basically this turns an input prompt into a list of lines of dialog + stage directions, and a list of audio files for the tts.
    public async Task CreateScene(string prompt, string promptAuthor, string backupPrompt, string backupPromptAuthor, bool isThisSceneUsingVoiceActing)
    {
        Debug.Log($"🎬 CreateScene called with prompt: '{prompt}' by {promptAuthor}");
        
        if (string.IsNullOrEmpty(prompt))
        {
            Debug.LogError("❌ CreateScene called with EMPTY prompt! This should not happen.");
            return;
        }
        
        string initialPrompt = "";
        string chatGPTOutput = "";
        string[] chatGPTOutputLines = null;
        string[] chatGPTOutputLinesWithSwearing = null;
        string creatingScene = "";
        bool foundGoodPrompt = false;

        while (!foundGoodPrompt)
        {

            stillGeneratingScene = true;
            initialPrompt = prompt;
            creatingScene = "Currently Creating: " + prompt;
            textField.text = creatingScene + " --- " + "Generating script...";

            // add some shit to the prompt
            //prompt += ". Make sure to use light profanity like frick, shoot and crap. Scripts should have at least 30 lines of dialog.";
            // prompt += ". Rick and Morty are currently in " + sceneDirector.currentDimension.name + ". Make sure to use light profanity like frick, shoot and crap. Scripts should have at least 30 lines of dialog.";

            if (!useDefaultScript)
            {
                // chuck the prompt into chatgpt

                chatGPTOutput = await AIController.EnterPromptAndGetResponse(prompt);
            }
            else
            {

                chatGPTOutput = defaultScript.text;
                await Task.Delay(1000);
            }



            // add the title and author to the scene so the narrator speaks them
            chatGPTOutput = "Narrator: " + initialPrompt + "\n" +
                            "Narrator: Prompt By: " + promptAuthor + "\n" + chatGPTOutput;


            // this errases chatgpts memorty so it doesnt overload the max tokens cap
            // dont worry about it
            AIController.Clear();


            textField.text = creatingScene + " --- " + "Processing script...";

            // process the message into indavidual lines
            string str = AIController.OutputString;
            chatGPTOutputLines = Utils.ProcessOutputIntoStringArray(chatGPTOutput, ref str);
            
            AIController.OutputString = str;
            // if (!useDefaultScript)
            // {
            //     // save a text file
            //     try
            //     {
            //         // Get the current date and time
            //         string dateTimeString = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            //         // Create the full path for the file
            //         string path = $"{Environment.CurrentDirectory}\\Assets\\Example Scripts\\OutputScripts\\{dateTimeString}_{initialPrompt}.txt";

            //         // Create an empty file and close it immediately
            //         using (FileStream fs = File.Create(path))
            //         {
            //             // Close the file immediately to allow subsequent write operations
            //         }

            //         // Write the string to the file
            //         File.WriteAllText(path, chatGPTOutput);

            //         // Log success
            //         Debug.Log("Data saved successfully to: " + path);
            //     }
            //     catch (System.Exception e)
            //     {
            //         // Log any exceptions that occur
            //         Debug.LogError("An error occurred while saving data: " + e.Message);
            //     }
            // }


            // if the number of lines is less that 1 this means that chatgpt was like "WAAAAAA i cant do that"
            if (chatGPTOutputLines.Length < 1)
            {
                Debug.Log("ChatGPT failed to generate script. Skipping this prompt and waiting for new one.");
                // Skip this prompt and go back to waiting for chat/manual input
                continue;
            }
            else
            {
                // // add camera angles
                // if (usingChatGptCameraShots)
                // {
                //     textField.text = creatingScene + " --- " + "Adding Camera Angles";

                //     chatGPTOutput = await openAICameraDirector.EnterPromptAndGetResponse(chatGPTOutput);
                //     openAICameraDirector.Clear();


                //     // chatGPTOutput = "Narrator: " + initialPrompt + "\n" + "Narrator: Prompt By: " + promptAuthor + "\n" + chatGPTOutput;

                //     string str2 = AIController.OutputString;
                //     chatGPTOutputLines = Utils.ProcessOutputIntoStringArray(chatGPTOutput, ref str2);
                //     AIController.OutputString = str2;

                // }


                foundGoodPrompt = true;
            }
        }

        try
        {
            // Get the current date and time	
            string dateTimeString = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // remove the special characters because this fucks with file saving
            string initialPromptWithNoSpecialCharacters = Regex.Replace(initialPrompt, @"[^a-zA-Z0-9\s]", "");
            // Create the full path for the file	
            string path = $"{Environment.CurrentDirectory}\\Assets\\Example Scripts\\OutputScripts\\{dateTimeString}_.txt";
            // Create an empty file and close it immediately	
            using (FileStream fs = File.Create(path))
            {
                // Close the file immediately to allow subsequent write operations	
            }
            // Write the string to the file	
            File.WriteAllText(path, chatGPTOutput);
            // Log success	
            Debug.Log("Data saved successfully to: " + path);
        }
        catch (System.Exception e)
        {
            // Log any exceptions that occur	
            Debug.LogError("An error occurred while saving data: " + e.Message);
        }

        textField.text = creatingScene + " --- " + "Detecting Dialog...";
        Debug.Log("Starting dialog detection...");
        Debug.Log("chatGPTOutputLines length: " + chatGPTOutputLines.Length);
        Debug.Log("First 3 lines of script:");
        for (int debugIdx = 0; debugIdx < Mathf.Min(3, chatGPTOutputLines.Length); debugIdx++)
        {
            Debug.Log($"Line {debugIdx}: {chatGPTOutputLines[debugIdx]}");
        }

        chatGPTOutputLinesWithSwearing = Utils.AddSwearing(chatGPTOutputLines);
        Debug.Log("Added swearing, lines count: " + chatGPTOutputLinesWithSwearing.Length);

        string nameOfAiGeneratedCharacter = null;
        string nameOfAiGeneratedDimension = null;
        // extract the dialog info from the output lines this includes the voiceModelUUIDs, the character names, and the text that they speak.	

        Debug.Log("About to call ProcessDialogFromLines (first call)...");
        Debug.Log("SceneDirector is null? " + (sceneDirector == null));
        
        List<string>[] dialogInfo = sceneDirector.ProcessDialogFromLines(ref chatGPTOutputLines, ref nameOfAiGeneratedCharacter, ref nameOfAiGeneratedDimension);
        Debug.Log("First ProcessDialogFromLines completed");
        
        Debug.Log("About to call ProcessDialogFromLines (second call)...");
        List<string>[] dialogInfoWithSwearing = sceneDirector.ProcessDialogFromLines(ref chatGPTOutputLinesWithSwearing, ref nameOfAiGeneratedCharacter, ref nameOfAiGeneratedDimension);
        Debug.Log("Second ProcessDialogFromLines completed");
        
        List<string> voiceModelUUIDs = dialogInfoWithSwearing[0];
        List<string> characterNames = dialogInfoWithSwearing[1];
        List<string> textsToSpeak = dialogInfoWithSwearing[2];
        
        Debug.Log($"Dialog extracted - Texts to speak: {textsToSpeak.Count}, Characters: {characterNames.Count}");



        List<Task> allConcurrentTasks = new List<Task>();
        Debug.Log("Creating concurrent tasks...");

        // Start both tasks in parallel
        // var aiArtTask = replicateAPI.GenerateAndSetTexturesForCharacter(defaultGuy, nameOfAiGeneratedCharacter);
        Task aiArtTask = null;
        if (useAiArt && (nameOfAiGeneratedCharacter != null || nameOfAiGeneratedDimension != null))
        {
            Debug.Log("Creating AI Art task...");
            aiArtTask = replicateAPI.DoAllTheAiArtStuffForAScene(aiArtCharacter, nameOfAiGeneratedCharacter, aiArtDimension, nameOfAiGeneratedDimension);
            allConcurrentTasks.Add(aiArtTask);
            Debug.Log("AI Art task added");
        }

        textField.text = creatingScene + " --- " + "Generating TTS...";

        Task<List<AudioClip>> ttsVoiceActingTask = null;
        if (isThisSceneUsingVoiceActing)
        {
            Debug.Log("Creating TTS task...");
            ttsVoiceActingTask = cuaiTTSAPIManager.GenerateTTS(textsToSpeak, voiceModelUUIDs, characterNames, textField, creatingScene);
            allConcurrentTasks.Add(ttsVoiceActingTask);
            Debug.Log("TTS task added");
        }

        Task<string> CameraShotsChatGPTTask = null;
        if (usingChatGptCameraShots)
        {
            Debug.Log("Creating Camera Shots task...");
            string outputLinesReMerged = string.Join("\n", chatGPTOutputLines);
            CameraShotsChatGPTTask = openAICameraDirector.EnterPromptAndGetResponse(outputLinesReMerged);
            // string cameraChatGPTOutput = await openAICameraDirector.EnterPromptAndGetResponse(outputLinesReMerged);
            // // string cameraChatGPTOutput = CameraShotsChatGPTTask.Result;
            // char[] delims = new[] { '\r', '\n' };
            // string[] outputLinesProcessedWithCameraShots = cameraChatGPTOutput.Split(delims, StringSplitOptions.RemoveEmptyEntries);
            // chatGPTOutputLines = outputLinesProcessedWithCameraShots;
            allConcurrentTasks.Add(CameraShotsChatGPTTask);
            Debug.Log("Camera Shots task added");
        }




        Debug.Log("start await");
        if (allConcurrentTasks.Count > 0)
        {
            await Task.WhenAll(allConcurrentTasks);
        }


        // if (aiArtTask != null && ttsVoiceActingTask != null)
        // {
        //     await Task.WhenAll(aiArtTask, ttsVoiceActingTask);
        // }
        // else if (aiArtTask != null)
        // {
        //     await Task.WhenAll(aiArtTask);
        // }
        // else if (ttsVoiceActingTask != null)
        // {
        //     await Task.WhenAll(ttsVoiceActingTask);
        // }

        Debug.Log("finish await");
        //retrieve the result of ttsVoiceActingTask after awaiting it

        if (usingChatGptCameraShots)
        {

            string cameraChatGPTOutput = CameraShotsChatGPTTask.Result;
            char[] delims = new[] { '\r', '\n' };
            string[] outputLinesProcessedWithCameraShots = cameraChatGPTOutput.Split(delims, StringSplitOptions.RemoveEmptyEntries);

            // ok so now we have 2 scripts 1 with camera angles and 1 without, sometimes the one without removes lines and shit, so we cant
            // just use that we have to merge them


            List<string> combinedList = chatGPTOutputLines.ToList();

            int checkedIndexOnCombinedList = 0;
            //length -1 because we dont care if a camera instruciton is at the end of the list
            for (int i = 0; i < outputLinesProcessedWithCameraShots.Length - 1; i++)
            {
                string lineWeChecking = outputLinesProcessedWithCameraShots[i];
                //if this bitch is a camera command
                if (lineWeChecking.Contains("{") && !lineWeChecking.Contains(":"))
                {

                    // then we get the instuction after this one and find it in the original array.
                    string nextInstruction = outputLinesProcessedWithCameraShots[i + 1];

                    // dont start at 0 so if 2 lines are the same we dont insert it again
                    for (int j = checkedIndexOnCombinedList; j < combinedList.Count; j++)
                    {
                        // Clean strings by removing special characters, spaces, and converting to lowercase

                        string cleanedString1 = Regex.Replace(combinedList[j], "[^a-zA-Z0-9]", "").ToLower();
                        string cleanedString2 = Regex.Replace(nextInstruction, "[^a-zA-Z0-9]", "").ToLower();
                        //match found
                        if (cleanedString1 == cleanedString2)
                        {
                            // add the instruction in before j
                            combinedList.Insert(j, lineWeChecking);

                            // move the checked index forward so we dont add another line before this.
                            // its +2 becauses we inserted an item which increases the index by 1 and then we want to move the pointer to the next instuction
                            checkedIndexOnCombinedList = j + 2;
                            break;
                        }
                    }
                }

            }


            // ok now check for entering portals, only 2 shots actually look good so change it to either wide shot, or tracking shot behind.
            for (int i = 1; i < combinedList.Count; i++)
            {
                string lineWeChecking = combinedList[i];
                if (lineWeChecking.Contains("[") && lineWeChecking.ToLower().Contains("portal to"))
                {
                    string previousLine = combinedList[i - 1];
                    if (!previousLine.Contains("{"))
                    {
                        combinedList.Insert(i, "{Wide Shot}");
                        i += 1;
                        continue;
                    }
                    else if (!previousLine.ToLower().Contains("wide shot"))
                    {
                        //if the previous shot isnt a wide shot then add a tracking shot behind.
                        combinedList[i - 1] = "{Tracking shot, Morty, behind}";
                        continue;
                    }
                }
            }




            string outputLinesReMerged = string.Join("\n", chatGPTOutputLines);

            Debug.Log("original: \n " + string.Join("\n", chatGPTOutputLines));
            Debug.Log("Chatgpt camer angles: \n " + string.Join("\n", outputLinesProcessedWithCameraShots));
            Debug.Log("Combined: \n " + string.Join("\n", combinedList.ToArray()));



            chatGPTOutputLines = combinedList.ToArray();
            chatGPTOutputLinesWithSwearing = Utils.AddSwearing(chatGPTOutputLines);

            // chatGPTOutputLines = outputLinesProcessedWithCameraShots;
        }


        List<AudioClip> ttsVoiceActingOrdered = null;
        if (isThisSceneUsingVoiceActing)
        {
            ttsVoiceActingOrdered = ttsVoiceActingTask.Result;
        }

        //-------------------------------------------------

        // await replicateAPI.GenerateAndSetTexturesForCharacter(defaultGuy, nameOfAiGeneratedCharacter);



        // textField.text = creatingScene + " --- " + "Generating FakeYou TTS...";
        // // generate text to speech voice acting based on dialog	
        // List<AudioClip> ttsVoiceActingOrdered = null;
        // if (isThisSceneUsingVoiceActing)
        // {
        //     ttsVoiceActingOrdered = await fakeYouAPIManager.GenerateTTS(textsToSpeak, voiceModelUUIDs, characterNames, textField, creatingScene);
        // }
        textField.text = creatingScene + " --- " + "Done :)";
        // we done	
        stillGeneratingScene = false;
        nextScene = new RickAndMortyScene(initialPrompt, promptAuthor, chatGPTOutputLinesWithSwearing, ttsVoiceActingOrdered);
    }
    bool AreStringsEqual(string s1, string s2)
    {
        // Clean strings by removing special characters, spaces, and converting to lowercase
        s1 = Regex.Replace(s1, "[^a-zA-Z0-9]", "").ToLower();
        s2 = Regex.Replace(s2, "[^a-zA-Z0-9]", "").ToLower();

        return s1 == s2;
    }

    // this shit is so the vote text changes smoothly over time
    private int initialTopic1Votes = 0;
    private int initialTopic2Votes = 0;
    private int initialTopic3Votes = 0;

    private int targetTopic1Votes = 0;
    private int targetTopic2Votes = 0;
    private int targetTopic3Votes = 0;
    IEnumerator UpdateVotesTextOverTime(TMP_Text targetTextObject, int initialVotes, int targetVotes)
    {
        float elapsedTime = 0;
        float timeToChange = 0.5f; // The time over which to change the text
        while (elapsedTime < timeToChange)
        {
            elapsedTime += Time.deltaTime;

            // Interpolate between initial and target votes
            float interpolatedVotes = Mathf.Lerp(initialVotes, targetVotes, elapsedTime / timeToChange);

            // Update the text object
            targetTextObject.text = "VOTES: " + Mathf.RoundToInt(interpolatedVotes).ToString();

            yield return null;
        }

        // Make sure the final value is set accurately
        targetTextObject.text = "VOTES: " + targetVotes.ToString();
    }



}


public class RickAndMortyScene
{
    public string titleString;
    public string author;
    public string[] chatGPTOutputLines;
    public List<AudioClip> ttsVoiceActingLines;

    public RickAndMortyScene(string initialPrompt, string promptAuthor, string[] outputLines, List<AudioClip> voiceActing)
    {
        titleString = initialPrompt;
        author = promptAuthor;
        chatGPTOutputLines = outputLines;
        ttsVoiceActingLines = voiceActing;
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(WholeThingManager))]
public class RandomScript_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        WholeThingManager script = (WholeThingManager)target;
        EditorGUI.BeginChangeCheck();
        serializedObject.UpdateIfRequiredOrScript();
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
            {
                if (iterator.name == "wordsPerMinute")
                {
                    if (!script.usingVoiceActing) script.wordsPerMinute = EditorGUILayout.FloatField("Words Per Minute", script.wordsPerMinute);
                }
                else
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            enterChildren = false;
        }

        serializedObject.ApplyModifiedProperties();
        EditorGUI.EndChangeCheck();
    }
}
#endif
