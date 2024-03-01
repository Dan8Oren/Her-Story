using System;
using Newtonsoft.Json;
using OpenAI;

[Serializable]
public class Response
{
    [FunctionProperty("the story text to present to the player, must end with an event that invites the player to make a choices", true)] 
    [JsonProperty("storyText")]
    public string StoryText {get; private set;}
    
    
    [FunctionProperty("call-to-action or a hint for the player on what to do next. Use a suggestive tone (e.g. start with \"You can ...\" or \"You might ...\"). Don't suggest passive actions."
        , true)] 
    [JsonProperty("callToAction")]
    public string CallToAction {get; private set;}
    
    
    [FunctionProperty("additional story event that happens regardless of the player's input, in order to push the story forward. It might be poetic, it might be surprising, or even very dramatic."
        , true)] 
    [JsonProperty("storyEvent")]
    public string StoryEvent {get; private set;}
    
    
    [FunctionProperty("the time in the story that took the player to finish his last action, It should be in the format \"00:MM\", where 'MM' represents the minutes, a number between 5 to 30 (e.g. \"00:30\", for a 30 min long action)."
        , true)] 
    [JsonProperty("actionTime")]
    public string ActionTime {get; private set;}
    
    
    [FunctionProperty("a description of the image that should be displayed to the player, it should be a 2D image that represents the current state of the story, Mai if she apears, must look the same in all of them."
        , true)] 
    [JsonProperty("imagePrompt")]
    public string ImagePrompt {get; private set;}
    
    
    [FunctionProperty("float between 0 and 1. It represents how close is the player to reach his goal. 0 means not at all, 1 means the AI was stopped from destroying the world."
        , true)] 
    [JsonProperty("goalProgress")]
    public float GoalProgress {get; private set;}
    
    
    [FunctionProperty("describing the player's emotional state, or 'Unknown' if it's not clear enough (e.g. 'joy' | 'irritation' | 'sadness' | 'fear' | 'surprise' | 'disgust' | 'empathy' | 'neutral' | 'anger' | 'unknown' )"
        , true)] 
    [JsonProperty("playerSentiment")]
    public string PlayerSentiment {get; private set;}
    
    
    [FunctionProperty("true if 'progress >= 1' or currentTime is past 2 AM (e.g 02:00), false otherwise.", true)] 
    [JsonProperty("isGameOver")]
    public bool IsGameOver {get; private set;}
    
    [JsonConstructor]
    public Response(string storyText, string callToAction, string storyEvent, string actionTime, string imagePrompt, float goalProgress, string playerSentiment, bool isGameOver)
    {
        this.StoryText = storyText;
        this.CallToAction = callToAction;
        this.StoryEvent = storyEvent;
        this.ActionTime = actionTime;
        this.ImagePrompt = imagePrompt;
        this.GoalProgress = goalProgress;
        this.PlayerSentiment = playerSentiment;
        this.IsGameOver = isGameOver;
    }
}