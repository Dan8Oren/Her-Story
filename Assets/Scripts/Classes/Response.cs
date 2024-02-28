using System;

[Serializable]
public class Response
{
    public readonly string StoryText;
    public readonly string CallToAction;
    public readonly string StoryEvent; 
    public readonly string ActionTime;
    public readonly string ImagePrompt;
    //public readonly string CurrentTime;
    public readonly string PlayerSentiment;
    public readonly float GoalProgress;
    // public readonly float PlayerEngagement;
    public readonly bool IsGameOver;
    
    public Response(string storyText, string callToAction, string storyEvent, string actionTime, string imagePrompt, string currentTime, float goalProgress, float playerEngagement, string playerSentiment, bool isGameOver)
    {
        this.StoryText = storyText;
        this.CallToAction = callToAction;
        this.StoryEvent = storyEvent;
        // this.PlayerEngagement = playerEngagement;
        this.ActionTime = actionTime;
        this.ImagePrompt = imagePrompt;
        //this.CurrentTime = currentTime;
        this.GoalProgress = goalProgress;
        this.PlayerSentiment = playerSentiment;
        this.IsGameOver = isGameOver;
    }
}