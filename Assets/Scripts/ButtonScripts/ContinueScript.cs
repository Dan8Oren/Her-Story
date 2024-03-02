using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ContinueScript : MonoBehaviour
{
    public GameObject popupWindow;
    
    private Button _button;
    private bool _isHovering = false;
    private void Start()
    {
        _button = GetComponent<Button>();
    }
    
    private void FixedUpdate()
    {
        if (_button.interactable)
        {
            popupWindow.SetActive(true);
        }else if (!_isHovering)
        {
            popupWindow.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        popupWindow.SetActive(true);
        _isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        popupWindow.SetActive(false);
        _isHovering = false;
    }
}
