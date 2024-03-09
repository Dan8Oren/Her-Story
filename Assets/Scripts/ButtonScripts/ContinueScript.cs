using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ContinueScript : Button, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject popupWindow;
    private bool _isfirst = true;
    
    
    private void FixedUpdate()
    {
        if (_isfirst && base.interactable) 
        {
            _isfirst = false;
            popupWindow.SetActive(true);
            Debug.Log("First time");
        }
    }
    
    
    
    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (base.interactable)
        {
            popupWindow.SetActive(true);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        popupWindow.SetActive(false);
    }
}
