using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class AutoScroll : MonoBehaviour
{
    public RectTransform content;

    private ScrollRect _scrollRect;

    private void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
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
    
    // Function to scroll to the bottom of the Scroll View
    public void ScrollToBottom()
    {
        _scrollRect.verticalNormalizedPosition = 0f;
    }
}
