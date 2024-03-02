using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenAI;
using OpenAI.Chat;
using TMPro;
using UnityEngine;

public class SummaryScript : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI summaryPanelText;
    [SerializeField] private TextMeshProUGUI summaryButtonText;
    [SerializeField] private int numOfEventsToRefreshSummary = 3;
    
    
    private int _currentSummaryIndex = 0;
    private Message _behaviourInstructionsMessage;
    private StringBuilder _sb;
    private void Start()
    {
        summaryPanelText.text = "Nothing to summarize yet";
        GameManager.Instance.OnResponseReceivedEvent += UpdateSummary;
        _behaviourInstructionsMessage = new Message(Role.System, BEHAVIOR_INSTRUCTIONS);
    }

    private const string BEHAVIOR_INSTRUCTIONS =
        "You are tasked with summarizing key moments from a combined story text provided by the user. " +
        "The input text consists of multiple segments of a narrative game story, " +
        "each separated by a newline character (\"\\n\"). " +
        "Your goal is to extract and summarize the essential plot points and significant events from the entire story. " +
        "The summary should capture the main developments, character interactions, and any notable twists or revelations. " +
        "Ensure that the summary maintains coherence and captures the essence of the story's progression. " +
        "Feel free to skip story texts. " +
        "Return the summary as a single string, with each key moment seperated by a (\"\\n - \"). ";

    private async void UpdateSummary(object sender, GameManager.ResponseReceivedEventArgs e)
    {
        int storyEventSize = GameManager.Instance.storyTexts.Count;
        if (storyEventSize < _currentSummaryIndex + numOfEventsToRefreshSummary)
        {
            return;
        }
        if (_sb == null)
        {
            _sb = new StringBuilder();
            HandleFirstSummary();
        }
        else
        {
            for (int i = _currentSummaryIndex; i < storyEventSize; i++)
            {
                _sb.AppendLine(GameManager.Instance.storyTexts[i]);
            }
        }

        Message storyEvents = new Message(Role.User, _sb.ToString());
        
        var chatRequest = new ChatRequest(
            messages: new Message[] {
            _behaviourInstructionsMessage,
            storyEvents },
            model: "gpt-3.5-turbo-0125",
            responseFormat: ChatResponseFormat.Text);
        ChatResponse response = await GameManager.Instance.API.ChatEndpoint.GetCompletionAsync(chatRequest);
        summaryPanelText.text = response.FirstChoice.Message;
        _currentSummaryIndex = storyEventSize;
    }

    private void HandleFirstSummary()
    {
        foreach (string s in GameManager.Instance.storyTexts)
        {
            _sb.AppendLine(s);
        }
    }

    public void ToggleSummary()
    {
        var curSummaryState = summaryPanelText.enabled;
        curSummaryState = !curSummaryState;
        summaryPanelText.enabled = curSummaryState;
        summaryButtonText.SetText(curSummaryState ? "Hide Summary" : "Show Summary");
    }
}
