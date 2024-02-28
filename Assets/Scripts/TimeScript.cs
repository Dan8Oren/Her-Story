using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

public class TimeScript : MonoBehaviour
{
    public static TimeScript Instance;
    public int startTimeHours = 21;
    
    [SerializeField] private float timeToBlink = 0.8f;
    [SerializeField] private TextMeshProUGUI separatorTextMeshProUGUI;
    [SerializeField] private TextMeshProUGUI hoursTextMeshProUGUI;
    [SerializeField] private TextMeshProUGUI minutesTextMeshProUGUI;
    
    private Coroutine _blinkCoroutine;
    private List<int> _currentTime;
    private WaitForSeconds _timer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        _timer = new WaitForSeconds(timeToBlink);
        _currentTime = new List<int> {startTimeHours, 0};
        _blinkCoroutine = StartCoroutine(Blink());
        SetHours();
        SetMinutes();
        GameManager.Instance.GameOverEvent += GameOverBehaviour;
    }

    private void GameOverBehaviour(object sender, EventArgs e)
    {
        if (GameManager.Instance.IsGameWon)
        {
            StopCoroutine(_blinkCoroutine);
            separatorTextMeshProUGUI.SetText(":");
            return;
        }
        StartCoroutine(RedTimeBlink());
    }

    private IEnumerator RedTimeBlink()
    {
        // Color lastColor = hoursTextMeshProUGUI.color;
        while (true)
        {
            hoursTextMeshProUGUI.color = Color.red;
            minutesTextMeshProUGUI.color = Color.red;
            separatorTextMeshProUGUI.color = Color.red;
            yield return _timer;
            hoursTextMeshProUGUI.color = Color.clear;
            minutesTextMeshProUGUI.color = Color.clear;
            separatorTextMeshProUGUI.color = Color.clear;
            yield return _timer;
        }
    }

    private IEnumerator Blink()
    {
        while (true)
        {
            separatorTextMeshProUGUI.SetText("");
            yield return _timer;
            separatorTextMeshProUGUI.SetText(":");
            yield return _timer;
        }
    }
    

    public void SetTime(string time)
    {
        string[] parts = time.Split(":");
        Assert.IsTrue(parts.Length == 2, "Time format is not correct");
        int.TryParse($"Houres format is not correct - {parts[0]}",out var hours);
        int.TryParse($"Houres format is not correct - {parts[0]}",out var minutes);
        _currentTime = new List<int> {hours, minutes};
        string minutesString = _currentTime[1] < 10 ? $"0{_currentTime[1]}" : _currentTime[1].ToString();
        string hoursString = _currentTime[0] < 10 ? $"0{_currentTime[0]}" : _currentTime[0].ToString();
        hoursTextMeshProUGUI.SetText(hoursString);
        minutesTextMeshProUGUI.SetText(minutesString);
        separatorTextMeshProUGUI.SetText(":");
    }
    
    public void AppendTime(string time)
    {
        if (_currentTime == null)
        {
            SetTime(time);
            return;
        }
        string[] parts = time.Split(":");
        Assert.IsTrue(parts.Length == 2, $"Time format is incorrect {time}");
        
        if (int.TryParse(parts[0],out var hours) ) IncreaseHours(hours);
        else Debug.LogError("Hours format is not correct - " + parts[0]);
        
        if (int.TryParse(parts[1],out var minutes)) IncreaseMinutes(minutes);
        else Debug.LogError("Hours format is not correct - " + parts[1]);
        
        SetHours();
        SetMinutes();
    }
    
    private void SetHours()
    {
        Assert.IsTrue(_currentTime[0] is < 24 and >= 0, "Hours value is invalid");
        string hoursString = _currentTime[0] < 10 ? $"0{_currentTime[0]}" : _currentTime[0].ToString();
        hoursTextMeshProUGUI.SetText(hoursString);
    }
    
    private void SetMinutes()
    {
        Assert.IsTrue(_currentTime[1] is < 60 and >= 0, "Minutes value is invalid");
        string minutesString = _currentTime[1] < 10 ? $"0{_currentTime[1]}" : _currentTime[1].ToString();
        minutesTextMeshProUGUI.SetText(minutesString);
    }
    
    private void IncreaseMinutes(int minutes)
    {
        Assert.IsTrue(minutes is < 60 and >= 0, "Minutes value is invalid");
        _currentTime[1] += minutes;
        if (_currentTime[1] > 59)
        {
            _currentTime[1] += -60;
            IncreaseHours(1);
        }
    }

    private void IncreaseHours(int hours)
    {
        Assert.IsTrue(hours is < 23 and >= 0, "Hours value is invalid");
        _currentTime[0] += hours;
        if (_currentTime[0] > 23)
        {
            _currentTime[0] += -24;
        }
    }

    public string GetTimeAsString()
    {
        return hoursTextMeshProUGUI.text + ":" + minutesTextMeshProUGUI.text;
    }
    
    public List<int> GetCurrentTime()
    {
        return _currentTime;
    }
    
}
