using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class TextViewBehavior : MonoBehaviour
{
    private const int MAX_ALPHA = 255;
    public RectTransform content;
    [SerializeField] private float disabledAlpha;
    [SerializeField] private float activeAlpha = 230;
    [SerializeField] private float transitionTime = 6f;
    [SerializeField] private float timeToFade = 3.5f;
    
    private ScrollRect _scrollRect;
    private Image _backgroundImage;
    private float _lastScrollTime;
    private readonly List<bool> _firsts = new List<bool>() { false, false};
    private float _startTime = 0;
    private Coroutine _coroutine;

    private void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
        _backgroundImage = GetComponent<Image>();
        disabledAlpha = _backgroundImage.color.a*MAX_ALPHA;
    }

    // Call this method whenever you add a new item to the content
    public void ScrollToNewItem(RectTransform newItem)
    {
        // Calculate the position of the new item relative to the content
        Vector2 localPosition = content.InverseTransformPoint(newItem.position);

        // Calculate the normalized scroll position based on the item's position
        float normalizedScrollPosition = Mathf.Clamp01(1f - ((localPosition.y - _scrollRect.viewport.rect.yMin) / _scrollRect.viewport.rect.height));

        // Scroll to the new item
        _scrollRect.verticalNormalizedPosition = normalizedScrollPosition;
    }


    private void Update()
    {
        if (_lastScrollTime < Time.time - timeToFade)
        {
            if (_firsts[0] == false)
            {
                if (_coroutine != null) StopCoroutine(_coroutine);
                _firsts[1] = false;
                _firsts[0] = true;
                _coroutine = StartCoroutine(CreateAlphaTransition(disabledAlpha, _backgroundImage.color));

            }
            
        }
        else
        {
            if (_firsts[1] == false)
            {
                if (_coroutine != null) StopCoroutine(_coroutine);
                _firsts[0] = false;
                _firsts[1] = true;
                _coroutine = StartCoroutine(CreateAlphaTransition(activeAlpha, _backgroundImage.color));

            }
        }
    }

    // Function to scroll to the bottom of the Scroll View
    public void ScrollToBottom()
    {
        _lastScrollTime = Time.time;
        _scrollRect.verticalNormalizedPosition = 0f;
    }
    
    private IEnumerator CreateAlphaTransition(float targetAlpha, Color currentColor)
    {
        _startTime = Time.time;
        float currentAlpha = currentColor.a*MAX_ALPHA;
        while (Mathf.Approximately(currentAlpha, targetAlpha) == false)
        {
            // currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, transitionSpeed * Time.deltaTime);
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, (Time.time - _startTime)/transitionTime);
            _backgroundImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, currentAlpha/MAX_ALPHA);
            yield return new WaitForSeconds(0.01f);
        }
        
        _backgroundImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha/MAX_ALPHA);
        _coroutine = null;
    }

}
