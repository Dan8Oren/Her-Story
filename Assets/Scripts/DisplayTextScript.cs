using System;
using System.Collections;
using System.Collections.Generic;
using OpenAI.Audio;
using OpenAI.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(TextMeshProUGUI))]
public class DisplayTextScript : MonoBehaviour
{
    private const string HTML_ALPHA = "<color=#00000000>";

    public bool enableSkip;
    [SerializeField] private float timeBetweenChars;
    [SerializeField] string leadingChar = "";
    [SerializeField] bool leadingCharBeforeDelay = false;

    private string _textToDisplay;
    private TextMeshProUGUI _textMeshProUGUI;
    private Coroutine _activeDialog;
    private long _textId;
    private bool _isWaitingForAudio;
    private Action _callback;
    public bool IsPlaying { get; private set; }
    private void Start()
    {
        _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
        _textMeshProUGUI.SetText("");
        if (_activeDialog == null)
        {
            // _activeDialog = StartCoroutine(AnimateDialog(_textToDisplay)); //option 1
            _activeDialog = StartCoroutine(TypeWriterTMP(_textToDisplay)); //option 2
        }
    }

    private void Update()
    {
        if (enableSkip && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            if (_activeDialog != null) StopCoroutine(_activeDialog);
            IsPlaying = false;
            _textMeshProUGUI.SetText(_textToDisplay);
            SkipMessageEvent?.Invoke(this,new SkipMessageEventArgs(_textId));
            this.enabled = false;
        }
    }
    
    private IEnumerator TypeWriterTMP(string s, float timeBetweenMessages = 0f)
    {
        yield return new WaitForSeconds(timeBetweenMessages);
        _textMeshProUGUI.text = leadingCharBeforeDelay ? leadingChar : "";
		
        string originalText = s;
        string displayedText = "";
        int alphaIndex = 0;
        while (alphaIndex < originalText.Length)
        {
            if(originalText[alphaIndex] == '<') 
            {
                while (alphaIndex < originalText.Length && originalText[alphaIndex] != '>')
                {
                    alphaIndex++;
                }
                alphaIndex++;
            }
            _textMeshProUGUI.text = originalText;
            displayedText = _textMeshProUGUI.text.Insert(alphaIndex, leadingCharBeforeDelay?leadingChar + HTML_ALPHA : HTML_ALPHA);
            _textMeshProUGUI.text = displayedText;
            yield return new WaitForSeconds(timeBetweenChars);
            alphaIndex++;
            _callback?.Invoke();
        }
        _textMeshProUGUI.text = originalText;
    }
    
    
    /**
     * Shows the dialogs to the screen character character, with a delay.
     * (Skips html code.)
     */
    private IEnumerator AnimateDialog(string dialog)
    {
        if (_isWaitingForAudio)
        {
            yield return new WaitUntil(() => GameManager.Instance.AudioSource.isPlaying);
        }
        IsPlaying = true;
        var chars = dialog.Replace("\\n", "\n").Replace("\\t", "\t").ToCharArray();
        var tempToShow = new string("");
        var isOnHtml = false;
        var closing = 0;
        foreach (var c in chars)
        {
            if (closing == 2) isOnHtml = false;
            tempToShow += c;
            _textMeshProUGUI.SetText(tempToShow);
            if (!isOnHtml) yield return new WaitForSeconds(timeBetweenChars);
            isOnHtml = HandleHtmlText(c, isOnHtml, ref closing);
        }
        
        IsPlaying = false;
    }

    /**
     * Used to skip the display of html code (<>)
     * as a character character.
     */
    private static bool HandleHtmlText(char c, bool isOnHtml, ref int closing)
    {
        if (c == '<' && !isOnHtml)
        {
            closing = 0;
            isOnHtml = true;
        }

        else
        {
            if (c == '>') closing++;
        }

        return isOnHtml;
    }
    /// <summary>
    /// Sets the parameters for DisplayTest object in order to display the text.
    /// </summary>
    /// <param name="text"> String, the text to display </param>
    /// <param name="textId"> String, necessary inorder to stop the audio and text from being played</param>
    /// <param name="isWaitingForAudio"> Boolean, saying if the text need to wait for the current audio played to stop </param>
    /// <param name="callback">Action, a callback that will run each iteration of the text display, default to null</param>
    public void SetDisplayText(String text,long textId, bool isWaitingForAudio, Action callback = null)
    {
        _isWaitingForAudio = isWaitingForAudio;
        _textId = textId;
        _textToDisplay = text.Replace("\\n", "\n").Replace("\\t", "\t");
        _callback = callback;
    }
    
    public event EventHandler<SkipMessageEventArgs> SkipMessageEvent;
    public class SkipMessageEventArgs : EventArgs
    {
        public readonly long TextId;

        public SkipMessageEventArgs(long id)
        {
            TextId = id;
        }
    }


}