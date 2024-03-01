using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SideClockScript : MonoBehaviour
{
    public TextMeshProUGUI leftDigit;
    public TextMeshProUGUI rightDigit;

    private TextMeshProUGUI _sideText;
    private string _oldText;

    private void Start()
    {
        _sideText = GetComponent<TextMeshProUGUI>();
        _oldText = _sideText.text;
    }

    private void FixedUpdate()
    {
        if (_oldText != _sideText.text)
        {
            _oldText = _sideText.text;
            UpdateDigits(_sideText.text);
        }
    }

    private void UpdateDigits(string text)
    {
        if (text.Length == 2)
        {
            leftDigit.text = text[0].ToString();
            rightDigit.text = text[1].ToString();
        }
        else
        {
            leftDigit.text = "0";
            rightDigit.text = text[0].ToString();
        }
    }
}
