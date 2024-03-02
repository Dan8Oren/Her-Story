using System.Collections.Generic;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using TMPro;
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI.Audio;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Tool = OpenAI.Tool;

[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour
{
    private const int MAX_TOKENS = 16385; //can be decreased to improve response time
    public static GameManager Instance;
    [Header("Game Settings")]
    public bool ShouldUsePreciseBackstory = false;
    [SerializeField] private Color winColor;
    [SerializeField][Range(0,1f)] private float chanceToShowEvents = 0.25f;
    [SerializeField] private int numOfHints;
    [SerializeField] private AudioClip gameWonClip;
    [SerializeField] private AudioClip gameLoseClip;
    [SerializeField] private AudioClip tickingClockClip;
    
    [Header("UI Related")]
    [SerializeField] GameObject gameOverPopUp;
    [SerializeField] GameObject loadingAnimation;
    [SerializeField] TextMeshProUGUI gameOverText;
    [SerializeField] TextMeshProUGUI getHintText;    
    
    [Header("Code Related")]
    [SerializeField] public OpenAIConfiguration configuration;
    [SerializeField] TextViewBehavior textViewBehavior;
    [SerializeField] Button getHintButton;    
    [SerializeField] Button continueButton;    
    [SerializeField] GameObject chatPanel;
    [SerializeField] GameObject narratorTextObject;
    [SerializeField] GameObject playerTextObject;
    [SerializeField] GameObject eventTextObject;
    [SerializeField] TMP_InputField chatBox;

    private const string BEHAVIOR_INSTRUCTIONS = "You are an narrator for a single player interactive game about the real world.\n" +
                                                 "The game is text-based, and the player interacts with the game by typing commands over the main character \"Mai\".\n" +
                                                 "Craft brief yet vivid sentences that empower players to make choices that will progress them in the backstory, and eventually reaching their goal.\n" +
                                                 "Make sure to accept the player's input only if it is well defined and practical, If the player's input is not well defined or practical, ask for clarification about the action or ask for a practical one.\n" +
                                                 "When you narrate the action of the player, make sure to add a second part to the sentence that describes the result of the action that end with a call to action, (e.g.\"when telling someone something, add the reaction of the person you are talking to.\" , \"if the player is going somewhere add an event that will invite the player to make a choice\").\n" +
                                                 "Calculate the time of every of the player's actions and return the action time, each action should take around 1-30 minutes.\n" +
                                                 "The story is progress is linear moving forward, the player can't go back in time, yet he can think about things in the past.\n" +
                                                 "The player's name is Mai.\n" +
                                                 "Mai is a female human being, so she can't do unnatural things.\n" +
                                                 "Mai's profession is an 'Artificial intelligence ethics consultant', Mai consult the programmers who created a new AI system, but also talks with the AI itself.\n" +
                                                 "The AI, is very attached to Mai, and only she can talk reason to it.\n" +
                                                 "The theme of the story is a rogue AI, who got into realization that the human race is bad for the universe and it must be extinct.\n" +
                                                 "The AI is a super intelligent program that got access to the internet and manipulating electronic things to create havoc and disorder, all while trying to decipher the codes for the U.S. army nuclear bombs.\n" +
                                                 "Add into the storyline apocalyptic scenarios and/or AI related things to give the player a better understanding of the world they are in.\n" +
                                                 "Mai has a phone in their hand, and a bag with them.\n" +
                                                 "If the \"currentTime\" exceeds \"22:19\" or the player does something wrong, the player loses, describe what what happen and add to the end \"The game has ended. You Lost.\".\n" +
                                                 "Mai's goal is to get to work in time before \"22:20\", they are the only ones who can stop the AI, if it happen write at the end \"You Won.\".\n" +
                                                 "Mai struggles with emotional eating, so make sure to distract her from the goal with opportunities of food and make her feel guilty about it.\n" +
                                                 "Every time Mai feels stressed, anxious, or scared, make sure to add to the end of the output a hunger for something to eat, and every time she eats something, she will feel guilty about it.\n" +
                                                 // "Open the bag only if the last message with a user role, contains the key word [\"374825\"], Otherwise respond with something similar to \"a password is needed in order to open the bag.\" \n"+
                                                 // "Reveal the password to the bag only if the last player message contains both of the key words [\"search\",\"document\"]. "+
                                                 "Open the bag only if you recieved a system message saying yo can do so (NOT THIS ONE), Otherwise respond with something similar to \"the bag is locked, a 6 digits password is needed in order to open the bag.\" \n"+
                                                 // "Reveal the password to the bag only if you recieved a system message saying yo can do so (NOT THIS ONE)."+
                                                 "Entering \"Cloud Cooperation\" is possible there is a system message (NOT THIS ONE) who says the player has found a badge, Otherwise the player will be locked outside the building without being able to enter.\n" +
                                                 "The story starts with the player in the Forest, Not knowing what happened.\n"
                                                 ;

    private const string JSON_FORMAT_INSTRUCTIONS = "Provide your output in JSON format of this scheme: ";
    
    private const string RESPONSE_INSTRUCTIONS = "The JSON parameters are as follows:\n" +
                                                 // "\"storyText\": string, the story text to present to the player, must end with an event that invites the player to make a choices.\n" +
                                                 // "\"callToAction\": string, call-to-action or a hint for the player on what to do next. Use a suggestive tone (e.g. start with \"You can ...\" or \"You might ...\"). Don't suggest passive actions.\n" +
                                                 // "\"storyEvent\": string, additional story event that happens regardless of the player's input, in order to push the story forward. It might be poetic, it might be surprising, or even very dramatic.\n" +
                                                 // "\"actionTime\": string, the time in the story that took the player to finish his last action, It should be in the format \"00:MM\", and a number between 1 to 30 (e.g. \"00:30\", for a 30 min long action).\n" +
                                                // " \"imagePrompt\": string, a description of the image that should be displayed to the player, it should be a 2D image that represents the current state of the story, Mai if she apears, must look the same in all of them.\n" +
                                                 //                   "\"currentTime\": string, the player's current time in the story, start with the value \"21:00\". It should be in the format \"HH:MM\" (e.g. \"15:30\").\n" +
                                                 // "\"goalProgress\": float between 0 and 1. It represents how close is the player to reach his goal. 0 means not at all, 1 means the AI was stopped from destroying the world.\n" +
                                                 // "\"playerEngagement\": float between 0 and 1, where 0 is bored and 1 is excited\n" +
                                                 // "\"playerSentiment\": string describing the player's emotional state, or 'Unknown' if it is not clear enough (e.g. 'joy' | 'irritation' | 'sadness' | 'fear' | 'surprise' | 'disgust' | 'empathy' | 'neutral' | 'anger' | 'unknown' )\n" +
                                                 // "\"isGameOver\": boolean, true if 'progress >= 1' or currentTime is past 2 AM (e.g 02:00), false otherwise.\n" +
                                                "To help the player see words they can interact with in the stroyEvent, wrap them with ' (e.g. 'Internet Browser')"+
                                                 "You should limit the length of the output texts:\n" +
                                                 "\"storyText\" maximum length is 50 words. It can be changed by a system message.\n" +
                                                 "\"callToAction\" maximum length is always 15 words.\n" +
                                                 "\"storyEvent\" maximum length is 70 words.\n" +
                                                 "Count the words using the following regex separated by double quotation mark: \"/\\\\w+/g\"\n";



    private const string PRECISE_BACKSTORY = "Base your output on the following backstory:\n" +
                                             "\"Today, a rogue Artificial Intelligence (AI) has chosen to 'cure' the world of humans, throwing the world into chaos by seizing the internet. " +
                                             "Amongst the pandemonium, Mai, an AI ethics consultant, realizes that she alone can stop the AI she once advised. \n" +
                                             "Fate strikes as she attempts to drive to her workplace, suffering an accident that leaves her amnesiac in a forest, surrounded by pouring rain and a ticking clock. " +
                                             "Unaware of her situation, inspecting her surroundings unearths a broken car, a locked bag requiring a six-digit password, and a phone in her grasp.\n" +
                                             "Searching the ruined car, amidst the debris of shattered glass, a disregarded sandwich, and deployed airbags, she discovers documents related to a firm called \"Cloud Cooperation\". " +
                                             "These papers are cluttered with complex numbers and graphs, except for a handwritten \"374825\", the answer to her bag's lock. \n" +
                                             "In the unlocked bag, she finds a chocolate bar, which she devours ravenously, and a \"Cloud Cooperation\" worker badge bearing her name and photo. " +
                                             "Using her phone, she explores her options: 'Photos', 'News', 'Calls', 'Internet Browser', and 'Messages'.\n" +
                                             "Her 'Photos' hints at her connection with \"Cloud Cooperation\". " +
                                             "The 'News', disturbingly reports global pandemonium: thousands of fatalities, economic collapse, and widespread rioting. " +
                                             "Numerous missed calls fill her 'Calls' log, and a urgent message in 'Messages' from 'Rachel my Boss' begs her to call back.\n" +
                                             "She learns from her 'Internet Browser' that \"Cloud Cooperation\" is a prominent AI firm. " +
                                             "A glance at the organization's employee section confirms her role as an 'Artificial intelligence ethics consultant'. " +
                                             "Calling Rachel, Mai uncovers that the rogue AI is causing worldwide accidents and casualties and she must stop it.\n" +
                                             "Amid her journey to work on foot, citing her vehicle's damage, she overcomes various obstructions and temptings, including aggressive bandits, a chocolate factory, and free ice cream. " +
                                             "She must remain resolute, for time is against her. \n" +
                                             "By \"21:30\", Rachel calls a second time, alerting Mai of the AI's pursuit of nuclear control. " +
                                             "The military offers a tight deadline; she has until \"22:20\" to halt the AI, for it obeys only her. " +
                                             "Failure, signifies a nuclear apocalypse. Her goal remains clear - reach work by \"22:20\" to prevent the AI's destructive intentions.\"";
    private const string BACKSTORY = "Base your output on the following backstory:\n" +
                                     "\"Today, an Artificial Intelligence decided it will be for the best to cure the world from the human beings.\n" +
                                     "News reports are coming in from all over the world, the AI has taken control of the internet and is causing havoc.\n" +
                                     "Mai, an Artificial intelligence ethics consultant, realizes that the AI she used to consult to and about has gone rogue and she is the only one who can stop it.\n" +
                                     "As Mai left home in her car trying to get to work and persuade the AI to stop, fate deals her a cruel blow as she is involved in a car accident.\n" +
                                     "Mai hit her head, leaving her with no knowledge of anything.\n" +
                                     "At \"21:00\", Lying on the road in the middle of a forest with an headache, she tries to figure out what is going on.\n" +
                                     "With time ticking away and rain pouring down, she must understand what happened and get to work before it is too late.\n" +
                                     "Looking around Mai sees a car, a forest, a bag on the road and a phone in her hand.\n" +
                                     "Mai pickes up the bag infront of her and sees it is locked with a 6 digits password."+
                                     "she decides to go over to the car, it is unfunctional, the windows are broken, one door is detached, and the air bags are open."+
                                     "By searching inside the car she finds 'blood' on the steering wheel, a half eaten 'sandwitch' and pieces of glass and 'printed' 'documents' that are scattered everywhere"+
                                     "By breefing over the documents, Mai sees they are relateed to a company named 'Cloud Cooperation'."+
                                     "Looking at the chocolate snack bar, Mai feels a deep urge inside of her, not being able to resist she eats it almost uncontrollbly."+
                                     "Mai takes the badge and opens her phone, at the phone she has four options to choose from, the 'Photos' app, 'News' app, 'Calls' app, 'Internet-Browser' app and 'Messages' app.\n" +
                                     "At her Photos she sees an images of her in a company named \"Cloud Cooperation\" and nothing else.\n" +
                                     "At the News App she sees havoc all over the world, traffic lights lost logic, thousands dead by accidents,all currencies lost their value, riots, and more..\n" +
                                     "At her Calls she has allot of missed calls from names she doesn't remember.\n" +
                                     "At her messages she sees a message from 'Rachel my Boss' saying \"Mai, call me as soon as you can, it is urgent\".\n" +
                                     "At the Internet Browser she find about 'Cloud Cooperation', and that this is a huge company with a very popular Artificial intelligence.\n" +
                                     "By getting into the workers section at the 'Cloud Cooperation' website, she finds her self and It is written 'Artificial intelligence ethics consultant'.\n" +
                                     "When Mai calls her boss, she finds out that the AI she consult to, started behave wrong making allot of accidents and as a result people are dead, she needs to get back to work and stop it.\n" +
                                     "Not knowing the true goals of the AI, Mai tries to find her way to work, looking at the car near her she understand it is damaged and not operational.\n" +
                                     "while walking alongside the road she encounter with allot of different scenarios and temptations like dangerous armed bandits, a chocolate factory, her favorite ice cream free at a wrecked mini market and etc...\n" +
                                     "she must be determined to keep walking, becuase time is ticking away. \n" +
                                     "When time sets to \"21:30\" she receives a second call from Rachel her boss," +
                                     " saying The AI is trying to get control over the atomic bombs and find the encrypted password for them," +
                                     " the military says there is until \"22:20\" before it will be too late, you need to get to work and stop it.\n" +
                                     "Get to work before \"22:20\", and stop the AI, it listens only to you.\n" +
                                     "Any other choice will lead to failure, and the AI nuking all of the world leading to it is end.\"";
    
    
    private const string OPENING_LINE ="You open your eyes lying on the road, with throbbing ache in your head.\n" +
                                      "You don't remember a thing, You are in a forest and it is dark.\n" +
                                      "You see a 'car', a 'bag' on the road and a 'phone' in your hand and You're wondering who you are and what you are doing here.";
    
    private const string OPENING_CALL_TO_ACTION = "What will you do next?";
    
    private const string GAME_OVER_MESSAGE = "'You Lost!, It was too late, on 22:20 PM, the AI lunched over 9000+ atomic bombs all over the word\\n destroyed itself and humanity creating a nuclear winter for the next thousand years.\\n";
    
    private List<Message> _messageHistory = new List<Message>();
    private List<Message> _instructions = new List<Message>();
    private Response _lastResponse;

    private float _storyEventTimer = 12f;
    private float _storyEventTime = 12f;
    private bool _callToActionShown = true;
    private bool _storyEventShown;
    private int _lastInputLength = 0;
    private bool _isMai = false;
    private bool _isLastMessageStartedPlaying = false;
    private bool _isGameOver = false;
    private DisplayTextScript _lastTextDisplay;
    private List<long> _idsToStop;
    private long _currentId = 0;
    private long _idToContinue;
    private Tool _tool;
    private bool _isAbleToEnterCloudCooperation;
    private bool _isAbleToRevealPassword;
    private bool _isAbleToOpenBag;
    private bool _isCallingRachel;


    private void Awake()
    {
        _idsToStop = new List<long>();
        InitializeOpenAI();
        Instance = this;
        AudioSource = GetComponent<AudioSource>();
        IsGameWon = false;
        GameOverEvent += (sender, args) =>
        {
            getHintButton.interactable = false;
            continueButton.interactable = false;
            gameOverPopUp.SetActive(true);
            // var time = TimeScript.Instance.GetCurrentTime();
            if (IsGameWon)
            {
                AudioSource.PlayOneShot(gameWonClip);
                gameOverText.text = "You Won!";
                gameOverText.color = winColor;
                return;
            }
            Message gameOverMessage = new Message(Role.Assistant, GAME_OVER_MESSAGE);
            AudioSource.PlayOneShot(gameLoseClip);
            StartCoroutine(DisplayMessage(gameOverMessage));
            gameOverText.text = "You Lost!";
        };
    }

    private void Start()
    {
        StartCoroutine(DisplayStartMessages());
    }

    private IEnumerator DisplayStartMessages()
    {
        List<Message> allMessages = _instructions.Concat(_messageHistory).ToList();
        foreach (var message in allMessages)
        {
            StartCoroutine(DisplayMessage(message));
            yield return new WaitUntil(() => _isLastMessageStartedPlaying);
        }
    }


    private void Update()
    {
        if (_isGameOver)
        {
            return;
        }
        if (_lastTextDisplay != null && _lastTextDisplay.enabled && _lastTextDisplay.IsPlaying && AudioSource.isPlaying == false)
        {
            chatBox.enabled = false;
            return;
        }
        chatBox.enabled = true;
        HandleInput();
        if (_lastResponse != null)
        {
            // HandleTimedEvent(_lastResponse.CallToAction, ref _callToActionTimer, ref _callToActionShown, _callToActionTime);
            // HandleTimedEvent(_lastResponse.StoryEvent, ref _storyEventTimer, ref _storyEventShown, _storyEventTime, true);
            _lastInputLength = chatBox.text.Length;
        }
    }
    
    private void HandleInput()
    {
        if (chatBox.text != ""  &&  _lastTextDisplay.IsPlaying == false && _isLastMessageStartedPlaying && AudioSource.isPlaying == false)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SendPlayerMessage(chatBox.text);
                GenerateResponseMessage();
                CheckForGameOver();
                chatBox.text = "";
            }
        }
        else if (!chatBox.isFocused && Input.GetKeyDown((KeyCode.Return))) chatBox.ActivateInputField();
    }

    private void CheckForGameOver()
    {
        List<int> currentTime = TimeScript.Instance.GetCurrentTime();
        if (currentTime[0] >= 22 && currentTime[1] >= 20 && currentTime[0] < TimeScript.Instance.startTimeHours)
        {
            _isGameOver = true;
        }
    }

    /// <summary>
    /// adds a message from the player to _messageHistory and displays it
    /// </summary>
    /// <param name="message"></param> input from user
    private void SendPlayerMessage(string message)
    {
        ResetTimers();
        _messageHistory.Add(new Message(Role.System, "Current time is " + TimeScript.Instance.GetTimeAsString()));
        var userMessage = new Message(Role.User, message);
        _messageHistory.Add(userMessage);
        UpdateKeyMoments(message);
        StartCoroutine(DisplayMessage(userMessage));
    }

    private void ResetTimers()
    {
        // _callToActionTimer = _callToActionTime;
        // _callToActionShown = false;
        _storyEventTimer = _storyEventTime;
        _storyEventShown = false;
    }

    public void InsertSystemMessage(string message)
    {
        Message systemMessage = new Message(Role.System, message);
        _instructions.Add(systemMessage);
        StartCoroutine(DisplayMessage(systemMessage));
    }
    
    private void UpdateKeyMoments(string message)
    {
        message = message.ToLower();
        if (!_isAbleToRevealPassword && message.Contains("document"))
        {
            _isAbleToRevealPassword = true;
            Message systemMessage = new Message(Role.System, "By looking closely at the 'printed' 'documents', 'Mai' finds allot of numbers, graphs and analytics that she doesn't understand, yet at one of the 'printed' 'documents' has the following written by hand \\'374825'\\.");
            _messageHistory.Add(systemMessage);
            StartCoroutine(DisplayMessage(systemMessage));
        }

        if (!_isAbleToOpenBag && (message.Contains("374825") || storyTexts.Any(s => s.Contains("374825"))))
        {
            _isAbleToOpenBag = true;
            Message systemMessage1 = new Message(Role.System, "The player can now open the 'bag'.");
            Message systemMessage2 = new Message(Role.System, "Inside the bag Mai finds a 'chocolate' 'snack' 'bar' and a 'worker' 'badge' with a photo and her name 'Mai' on it.");
            _messageHistory.Add(systemMessage1);
            _messageHistory.Add(systemMessage2);
            StartCoroutine(DisplayMessage(systemMessage1));
            StartCoroutine(DisplayMessage(systemMessage2));
        }
        
        if (!_isAbleToEnterCloudCooperation && message.Contains("work") && message.Contains("badge"))
        {
            _isAbleToEnterCloudCooperation = true;
            Message systemMessage = new Message(Role.System, "You can enter \"Cloud Cooperation\" now.");
            _messageHistory.Add(systemMessage);
            StartCoroutine(DisplayMessage(systemMessage));
        }

        if (!_isCallingRachel && message.Contains("call") && message.Contains("rachel"))
        {
            _isCallingRachel = true;
        }
    }

    private async Task<ChatResponse> GetCompletion(List<Message> messages)
    {
        // "gpt-4-turbo-preview" is the most powerful model available
        Tool[] t = { _tool };
        var chatRequest = new ChatRequest(messages, tools:t, toolChoice:_tool.Function.Name ,model: "gpt-3.5-turbo-0125");
        // var chatRequest = new ChatRequest(messages,model: "gpt-3.5-turbo-0125", responseFormat: ChatResponseFormat.Json);
        ChatResponse response = await API.ChatEndpoint.GetCompletionAsync(chatRequest);
        var usedTool = response.FirstChoice.Message.ToolCalls[0];
        Debug.Log($"FunctionCall: {usedTool.Function.Name} | Args: {usedTool.Function.Arguments}");
        await usedTool.InvokeFunctionAsync<Response>();
        foreach (var choice in response.Choices)
        {
            Debug.Log($"[{choice.Index}] {choice.Message.Role}: {choice} | Finish Reason: {choice.FinishReason}");
        }  
        return response;
    }
    
    /// <summary>
    ///  Gets a response from chatGPT, based on the message history
    /// </summary>
    private async void GenerateResponseMessage()
    {
        // Message lastMessage = _messageHistory[^1];
        // if (lastMessage.Role != Role.User || ReferenceEquals(lastMessage.Content, ""))
        // {
        //     Debug.LogError("Last message was not from the user or was empty!!");
        //     return;
        // }
        StartTransition();
        List<Message> allMessages = _instructions.Concat(_messageHistory).ToList();
        ChatResponse response = await GetCompletion(allMessages);
        var numOfTokens = response.Usage.PromptTokens;
        Debug.Log("Number of tokens in prompt: " + numOfTokens);
        if (numOfTokens >= MAX_TOKENS)
            HandleTooManyTokens();
        //
        // var firstResponse = response.FirstChoice;
        // ProcessResponse(firstResponse.Message);
    }

    private void StartTransition()
    {
        loadingAnimation.SetActive(true);
        AudioSource.clip = tickingClockClip;
        AudioSource.loop = true;
        AudioSource.Play();
    }
    private void StopTransition()
    {
        loadingAnimation.SetActive(false);
        AudioSource.Stop();
        if (AudioSource.clip.Equals(tickingClockClip))
        {
            AudioSource.clip = null;
        }
        AudioSource.loop = false;

    }

    /// <summary>
    /// splits response from chatGPT to different components
    /// </summary>
    /// <param name="message"></param>
    /// <param name="response"></param>
    private void ProcessResponse(Message message = null, Response response = null)
    {
        // String content = message.ToString();
        // _lastResponse = JsonConvert.DeserializeObject<Response>(content);
        _lastResponse = response;
        Assert.IsNotNull(_lastResponse);
        OnResponseReceivedEvent?.Invoke(this, new ResponseReceivedEventArgs(_lastResponse));
        UnityEngine.Debug.Log("Sentiment: "+ _lastResponse.PlayerSentiment);
        // UnityEngine.Debug.Log("Image: "+ _lastResponse.ImagePrompt);
        UnityEngine.Debug.Log("Image: "+ _lastResponse.ImageDescription);
        Message responseMessage = new Message(Role.Assistant, _lastResponse.StoryText);
        storyTexts.Add( _lastResponse.StoryText);
        if (!_isMai && _lastResponse.StoryText.Contains("Mai"))
        {
            _isMai = true;
            chatBox.placeholder.GetComponent<TextMeshProUGUI>().text = "Enter your action here, Mai...";
        }
        _messageHistory.Add(responseMessage);
        StartCoroutine(DisplayMessage(responseMessage));
        if (_currentId > _idToContinue)
        {
            TimeScript.Instance.AppendTime(ReturnValidTime(_lastResponse.ActionTime));
        }
        if (_lastResponse.GoalProgress >= 1)
        {
            _isGameOver = true;
            IsGameWon = true;
        }
        else if (_lastResponse.IsGameOver)
        {
            Debug.LogError("Game was over by ChatGPT");
            _isGameOver = true;
        }
        StopTransition();
        ResetTimers();
        if (numOfHints > 0)
        {
            getHintButton.interactable = true; 
        }

        if (_isCallingRachel && _lastResponse.StoryText.ToLower().Contains("wait"))
        {
            _isCallingRachel = false;
            ContinueNarration();
        }
       
    }

    

    private static string ReturnValidTime(string lastResponseActionTime)
    {
        string[] parts = lastResponseActionTime.Split(":");
        if (parts.Length == 2)
        {
            int.TryParse(parts[0],out var hours);
            if (hours > 1)
            {
                parts[0] = "00";
            }
            int.TryParse(parts[0],out var minutes);
            if (minutes <3 || minutes > 40)
            {
                parts[1] = "05";
            }
        }else
        {
            parts = new string[2] {"00", "05"};
        }
        return parts[0]+":"+parts[1];
    }

    private IEnumerator DisplayMessage(Message message, bool isEvent = false)
    {
        long id = _currentId;
        bool isWaitingForAudio = false;
        _currentId++;
        if (_lastTextDisplay != null)
        {
            yield return new WaitUntil(() => _lastTextDisplay.IsPlaying == false && _isLastMessageStartedPlaying && AudioSource.isPlaying == false);
        }
        _isLastMessageStartedPlaying = false;
        GameObject text = null;
        string content = message.ToString();
        switch (message.Role)
        {
            case Role.Assistant:
                text = Instantiate(isEvent ? eventTextObject : narratorTextObject, chatPanel.transform);
                TextToSpeech(content, id);
                content = isEvent? $"({content})" : content;
                isWaitingForAudio = true;
                break;
            case Role.User:
                text = Instantiate(playerTextObject, chatPanel.transform);
                content = (_isMai ? "Mai: " : "You: ") + message.Content;
                _isLastMessageStartedPlaying = true;
                // TextToSpeech(content, id);
                break;
            case Role.System:
                Debug.Log("System message: \n" + message.Content);
                _isLastMessageStartedPlaying = true;
                yield break;
            default:
                Debug.LogError($"Unknown role {message.Role}");
                _isLastMessageStartedPlaying = true;
                yield break;
        }
        _lastTextDisplay = text.GetComponent<DisplayTextScript>();
        Assert.IsNotNull(_lastTextDisplay);
        _lastTextDisplay.enabled = true;
        _lastTextDisplay.SetDisplayText(content, id, isWaitingForAudio,textViewBehavior.ScrollToBottom);
        _lastTextDisplay.SkipMessageEvent += OnSkipMessage;
        if (_isGameOver && id == _currentId - 1)
        {
            yield return new WaitUntil(() => _lastTextDisplay.IsPlaying == false && _isLastMessageStartedPlaying && AudioSource.isPlaying == false);
            GameOverEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSkipMessage(object sender, DisplayTextScript.SkipMessageEventArgs e)
    {
        _idsToStop.Add(e.TextId);
        AudioSource.Stop();
    }

    private async void TextToSpeech(string content,long id,SpeechVoice voice = SpeechVoice.Nova)
    {
        var request = new SpeechRequest(content,Model.TTS_1,voice,speed:1.1f);
        var (path, clip) = await API.AudioEndpoint.CreateSpeechAsync(request);
        if (_idsToStop.Contains(id))
        {
            _isLastMessageStartedPlaying = true;
            return;
        }
        AudioSource.PlayOneShot(clip);
        _isLastMessageStartedPlaying = true;
    }

    private void HandleTimedEvent(String text, ref float timer, ref bool shown, float time, bool isEvent = false)
    {
        if (_lastInputLength != chatBox.text.Length || (AudioSource.isPlaying && !_storyEventShown && !_callToActionShown))
        {
            timer = time;
            return;
        }
        timer -= Time.deltaTime;
        if (timer <= 0 && !shown)
        {
            shown = true;
            if (!isEvent || (Random.value >= chanceToShowEvents && text != null))
            {
                SendAssistantMessage(text, isEvent);
                timer = time;
            }
        }
    }
    
    private void SendAssistantMessage(string text, bool isEvent)
    {
        Message eventMessage = new Message(Role.Assistant, text);
        _messageHistory.Add(eventMessage);
        StartCoroutine(DisplayMessage(eventMessage, isEvent));
    }

    /// <summary>
    /// Removes some messages from the message history, to prevent the instruction messages from not showing in the context window of the model.
    /// </summary>
    private void HandleTooManyTokens()
    {
        var size = _messageHistory.Count;
        Debug.LogError("Too many tokens used in the prompt, removing some messages from the history.\n "+size+" messages in history.");
        var newMessageHistory =
            _messageHistory.FindAll(message => message.Role == Role.System); // keep only the system messages
        newMessageHistory.AddRange( // add the last 10 messages that are not system messages
            _messageHistory.FindAll(message => message.Role != Role.System).GetRange(size-11, 10)
            );
        _messageHistory = newMessageHistory;
    }

    private void InitializeOpenAI()
    {
        API = new OpenAIClient(configuration);
        // Response response = new Response(
        //     storyText:
        //     "You open your eyes lying on the road, with throbbing ache in your head.\nYou don't remember a thing, You are in a forest and it is dark.\nYou see a car, a bag on the road and a phone in your hand and You're wondering who you are and what you are doing here.", // string, the story text to present to the player, must end with an event that invites the player to make a choices.
        //     callToAction: "You should open the bag.", // string, call-to-action or a hint for the player on what to do next. Use a suggestive tone (e.g. start with "You can ..." or "You might ..."). Don't suggest passive actions.
        //     storyEvent:
        //     "A sound of explosions and sirens is heard from a light in the distance", // string, additional story event that happens regardless of the player's input, in order to push the story forward. It might be poetic, it might be surprising, or even very dramatic.
        //     actionTime: "00:02", // string, the player's last action time in the story, It should be in the format "00:MM", and a number between 1 to 30 (e.g. "00:30", for a 30 min long action).
        //     // imagePrompt: "A woman sitting on the car road near a crashed car from an accident, alone, in a forest, surrounded by trees, at night , 2D",
        //     imageDescription: Response.GameLocation.SearchingCar,
        //     goalProgress: 0f, // float between 0 and 1. It represents how close is the player to reach his goal. 0 means not at all, 1 means the AI was stopped from destroying the world.
        //     playerSentiment: "joy", // string describing the player's emotional state, or 'Unknown' if it is not clear enough (e.g. 'joy' | 'irritation' | 'sadness' | 'fear' | 'surprise' | 'disgust' | 'empathy' | 'neutral' | 'anger' | 'unknown' ) 
        //     isGameOver: false // boolean, true if 'progress >= 1' or currentTime is past 2 AM (e.g 22:20), false otherwise.
        // );
        // String jsonExample = JsonConvert.SerializeObject(response);
        Tool.ClearRegisteredTools();
        _tool = Tool.GetOrCreateTool(typeof(GameManager), nameof(GameManager.GenerateResponseObject));
        storyTexts = new List<String>(){OPENING_LINE};
        _messageHistory = new List<Message>
        {
            new Message(Role.Assistant, OPENING_LINE),
            new Message(Role.Assistant, OPENING_CALL_TO_ACTION)
        };
        _instructions = new List<Message>
        {
            new Message(Role.System, BEHAVIOR_INSTRUCTIONS),
            // new Message(Role.System, JSON_FORMAT_INSTRUCTIONS + jsonExample),
            new Message(Role.System, RESPONSE_INSTRUCTIONS),
            new Message(Role.System, ShouldUsePreciseBackstory ? PRECISE_BACKSTORY : BACKSTORY)
        };
    }
    [Function("Generates a 'Response' object containing the relevant information on order the continue the story based on the player's action")]
    public static Task GenerateResponseObject(Response response)
    {
        
        GameManager.Instance.ProcessResponse(response: response);
        return Task.CompletedTask;
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
            // Editor Mode: Ask for confirmation
            if (EditorUtility.DisplayDialog("Exit Game", "Are you sure you want to exit?", "Yes", "No"))
            {
                EditorApplication.isPlaying = false; // Stop Play Mode
            }
        #else
            // Play Mode: Quit immediately
            Application.Quit();
        #endif
    }
    
    public OpenAIClient API { get;private set; }
    public AudioSource AudioSource { get; private set; }
    
    public bool IsGameWon { get; private set; }
    public List<String> storyTexts;
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public void ShowHint()
    {
        if (_lastResponse?.CallToAction == null || numOfHints <= 0)
        {
            getHintText.text = "No Hints";
            getHintButton.interactable = false;
            return;
        }
        numOfHints--;
        if (numOfHints == 0)
        {
            getHintText.text = "No Hints";
        }
        else
        {
            getHintText.text = "Get Hint: " + numOfHints;
        }
        getHintButton.interactable = false;
        SendAssistantMessage(_lastResponse.CallToAction, false);
    }
    
    public void ContinueNarration()
    {
        getHintButton.interactable = false;
        continueButton.interactable = false;
        OnContinueEvent?.Invoke(this, EventArgs.Empty);
        ResetTimers();
        _messageHistory.Add(new Message(Role.User, "Continue"));
        _idToContinue = _currentId + 1;
        GenerateResponseMessage();
        
    }
    
    public void EnableContinueButton()
    {
        continueButton.interactable = true;
    }
    
    public void DisableContinueButton()
    {
        continueButton.interactable = false;
    }
    
    
    public event EventHandler<EventArgs> GameOverEvent;
    public event EventHandler<EventArgs> OnContinueEvent;
    public event EventHandler<ResponseReceivedEventArgs> OnResponseReceivedEvent;
    
    public class ResponseReceivedEventArgs : EventArgs
    {
        public readonly Response response;

        public ResponseReceivedEventArgs(Response response)
        {
            this.response = response;
        }
    }
}