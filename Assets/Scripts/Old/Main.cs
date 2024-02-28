using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Chat;
using OpenAI.Models;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class Main : MonoBehaviour
{
    #region Class Variables

    public class Response
    {
        public string storyText;
        public string callToAction;
        public string storyEvent;
        public float goalProgress;
        public float playerEngagement;
    }

    [SerializeField] GameObject chatPanel;
    [SerializeField] GameObject textObject;
    [SerializeField] TMP_InputField chatBox;

    private OpenAIClient _api;
    private List<Message> _messageHistory = new List<Message>();

    private float _callToActionTimer = 10f;
    private float _storyEventTimer = 30f;
    private float _callToActionTime = 10f;
    private float _storyEventTime = 30f;
    private bool _callToActionShown;
    private bool _storyEventShown;

    private Message instructions;
    private ChatRequest chatRequest;

    private ChatResponse response;

    private const string openingLine ="You open your eyes lying on the road, with throbbing ache in your head.\n You don't remember a thing, You are in a forest and It's dark.\n You see a car, a bag on your shoulder and a phone in your hand and You're wondering what happened.";
    private const string instructionsStr = "You are an narrator for a single player interactive game about the real world.\n        The game is text-based, and the player interacts with the game by typing commands over the main character \"Mai\".\n        Craft brief yet vivid sentences that empower players to make choices that will progress them in the backstory, and eventually reaching their goal.\n        Make sure to accept the player's input only if it is well defined and practical, If the player's input is not well defined or practical, ask for clarification about the action or ask for a practical one.\n        When you narrate the action of the player, make sure to add a second part to the sentence that describes the result of the action that end with a call to action, (e.g.\"when telling someone something, add the reaction of the person you are talking to.\" , \"if the player is going somewhere add an event that will invite the player to make a choice\").\n        Calculate the time of every of the player's actions and return the action time, each action should take around 1-30 minutes.\n        The story is progress is linear moving forward, the player can't go back in time, yet he can think about things in the past.\n        The player's name is Mai.\n        Mai is a female human being, so she can't do unnatural things.\n        Mai's profession is an 'Artificial intelligence ethics consultant', Mai consult the programmers who created a new AI system, but also talks with the AI itself.\n        The AI, is very attached to Mai, and only she can talk reason to it.\n        The theme of the story is a rogue AI, who got into realization that the human race is bad for the universe and it must be extinct.\n        The AI is a super intelligent program that got access to the ethernet and manipulating electronic things to create havoc and disorder, all while trying to decipher the codes for the U.S. army nuclear bombs.\n        Add into the storyline apocalyptic scenarios and/or AI related things to give the player a better understanding of the world they are in.\n        Mai has a phone in their hand, and a bag with them.\n        If the \"currentTime\" exceeds \"01:59\" or the player does something wrong, the player loses, describe what what happen and add to the end \"The game has ended. You Lost.\".\n        Mai's goal is to get to work in time before \"02:00\", they are the only ones who can stop the AI, if it happen write at the end \"You Won.\".\n        Mai struggles with emotional eating, so make sure to distract her from the goal with opportunities of food and make her feel guilty about it.\n        Every time Mai feels stressed, anxious, or scared, make sure to add to the end of the output a hunger for something to eat, and every time she eats something, she will feel guilty about it.\n\n        Respond a single time with something similar to \"you approach the car it is smashed,but suddenly you realizes, it's yours.\", if the player's message contains the key word [\"car\"] and one of the following key words:\n        [\"look\", \"search\", \"figure\", \"see\", \"inspect\", \"examine\", \"observe\", \"view\", \"scan\", \"gaze\", \"stare\", \"into\"]  \n        \n\n  \n        Provide your output in JSON format of this scheme:\n        {          \n            // string, the story text to present to the player, must end with an event that invites the player to make a choices. \n            \"storyText\": \"\",\n            \n            // string, call-to-action or a hint for the player on what to do next. Use a suggestive tone (e.g. start with \"You can ...\" or \"You might ...\"). Don't suggest passive actions.\n            \"callToAction\": \"\",\n\n            // string, additional story event that happens regardless of the player's input, in order to push the story forward. It might be poetic, it might be surprising, or even very dramatic.\n            \"storyEvent\": \"\",\n            \n            // string, the player's last action time in the story, It should be in the format \"00:MM\", and a number between 1 to 30 (e.g. \"00:30\", for a 30 min long action).\n            \"actionTime\": \"\",\n\n            // string, the player's current time in the story, start with the value \"21:00\". It should be in the format \"HH:MM\" (e.g. \"15:30\").\n            \"currentTime\": \"21:00\",\n\n            // float between 0 and 1. It represents how close is the player to reach his goal. 0 means not at all, 1 means the AI was stopped from destroying the world.\n            \"goalProgress\": 0,\n\n            //float between 0 and 1, where 0 is bored and 1 is excited\n            \"playerEngagement\": 0.5,\n                        \n            // Array of strings describing the player's emotional state, or null if it's not clear enough: \n            // ['joy' | 'irritation' | 'sadness' | 'fear' | 'surprise' | 'disgust' | 'empathy'] | null \n            \"playerSentiment\": null,\n\n            // boolean, true if 'progress >= 1' or currentTime is past 2 AM (e.g 02:00), false otherwise.\n            \"isGameOver\": false,\n        }\n\n        You should limit the length of the output texts:\n        \"storyText\" maximum length is 50 words. It can be changed by a system message.\n        \"callToAction\" maximum length is always 15 words.\n        \"storyEvent\" maximum length is 70 words.\n        Count the words using the following regex separated by double quotation mark: \"/\\\\w+/g\"\n        The story starts with the player in the Forest, Not knowing what happened.\n        The player has a phone in their hand, and a bag with them.\n        The player's goal is to get to work in time before \"02:00\", they are the only ones who can stop the AI, if it happen write at the end \"You Won.\".\n        Every time the player feels stressed, anxious, or scared, make sure to add to the end of the output a hunger for something to eat, and every time they eat something, they will feel guilty about it.\n        If the \"currentTime\" exceeds \"01:59\" or the player does something wrong, the player loses, describe what what happen and add to the end \"The game has ended. You Lost.\".";

private Response _response;
    #endregion

    #region MonoBehaviour Methods

    void Start()
    {
        CreateThread();
    }
    
    void Update()
    {
        HandleInput();
        HandleCallToAction();
        HandleStoryEvent();
    }

    #endregion
    
    /// <summary>
    /// Creates the assistant and adds the first messages (assistant instructions, output format, backstory, opening line)
    /// to the _messageHistoryList and shows the opening line
    /// </summary>
    private async void CreateThread()
    {
        _api = new OpenAIClient("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");
        var assistant = await _api.AssistantsEndpoint.RetrieveAssistantAsync("asst_sMaBwBNZ2nWYNDWUAKcDPok8");
        // instructions = new Message(Role.System, i);
        Message firstMessage = new Message(Role.Assistant, openingLine + "what would you like to do?");
        _messageHistory.Add(firstMessage);
        ShowText(firstMessage);
    }

    /// <summary>
    /// adds a message from the player to _messageHistory list and gets response from chatGPT
    /// </summary>
    /// <param name="message"></param> input from user
    private new async void SendMessage(string message)
    {
        _callToActionTimer = _callToActionTime;
        _storyEventTimer = _storyEventTime;
        _callToActionShown = false;
        _storyEventShown = false;
        var userMessage = new Message(Role.User, message);
        _messageHistory.Add(userMessage);
        // must add format instructions every message because chatGPT is forgetful :<");
        ShowText(userMessage);
        chatRequest = new ChatRequest(_messageHistory);
        response = await _api.ChatEndpoint.GetCompletionAsync(chatRequest);
        while (!response.Choices[0].Message.Content.ToString().StartsWith("{") || !response.Choices[0].Message.Content.ToString().EndsWith("}"))
        {
            print(response.Choices[0].Message.Content.ToString());
            _messageHistory.Add(instructions);
            chatRequest = new ChatRequest(_messageHistory);
            response = await _api.ChatEndpoint.GetCompletionAsync(chatRequest);
        }
        var firstResponse = response.FirstChoice;
        ProcessResponse(firstResponse.Message);
    }
    
    /// <summary>
    /// splits response from chatGPT to different components
    /// </summary>
    /// <param name="message"></param>
    private void ProcessResponse(Message message)
    {
        print(message.Content.GetType() + message.Content.ToString()); 
        _response = JsonUtility.FromJson<Response>(message.Content.ToString());
        // string[] lines = format.Split("\n");
        // _storyText = lines[0].Remove(0, 11);
        // _callToAction = lines[1].Remove(0, 14);
        // _storyEvent = lines[2].Remove(0, 12);
        // _goalProgress = float.Parse(lines[3].Remove(0,14));
        // _playerEngagement = float.Parse(lines[4].Remove(0, 18));
        // _messageHistory.Add(new Message(Role.Assistant, _storyText));
        UnityEngine.Debug.Log(_response.playerEngagement);
        ShowText(_response.storyText);
    }

    private void ShowText(string textToShow)
    {
        GameObject text = Instantiate(textObject, chatPanel.transform);
        text.GetComponent<Text>().text = textToShow;
    }

    private void HandleInput()
    {
        if (chatBox.text != "")
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SendMessage(chatBox.text);
                chatBox.text = "";
            }
        }
        else if (!chatBox.isFocused && Input.GetKeyDown((KeyCode.Return))) chatBox.ActivateInputField();
    }

    private void HandleCallToAction()
    {
        _callToActionTimer -= Time.deltaTime;
        if (_callToActionTimer <= 0 && !_callToActionShown)
        {
            _callToActionShown = true;
            if (Random.value >= 0.5 && _response != null)
            {
                if (_response.callToAction != null)
                {
                    _messageHistory.Add(new Message(Role.Assistant, _response.callToAction));
                    // Message instructions = new Message(Role.System, format);
                    // _messageHistory.Add(instructions);
                    ShowText(_response.callToAction);
                }
            }
            _callToActionTimer = _callToActionTime;
        }
    }
    
    private void HandleStoryEvent()
    {
        _storyEventTimer -= Time.deltaTime;
        if (_storyEventTimer <= 0 && _storyEventShown)
        {
            _storyEventShown = true;
            if (_response.playerEngagement < 0.5f && _response != null)
            {
                if (Random.value >= 0.5 && _response.storyEvent != null)
                {
                    _messageHistory.Add(new Message(Role.Assistant, _response.storyEvent));
                    // Message instructions = new Message(Role.System, format);
                    // _messageHistory.Add(instructions);
                    ShowText(_response.storyEvent);
                }
            }
            _storyEventTimer = _storyEventTime;
        }
    }
}
