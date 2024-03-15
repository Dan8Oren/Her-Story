using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageManager : MonoBehaviour
{
    [SerializeField] private List<Sprite> images = new List<Sprite>();
    [SerializeField] private string carSystemMessage = "Inside the car there are scattered 'printed' 'documents', at one of the pages there is a the following written by hand \"374825\". the player can use that to unlock the bag";
    private Image _imageComponent;
    private int _currentImageIndex = 0;
    private int _currentImageContinues = 0;
    private List<bool> _isFirsts;
    private void Awake()
    {
        _imageComponent = GetComponent<Image>();
        _imageComponent.sprite = images[0];
        _isFirsts = new List<bool>();
        for (int i = 0; i < images.Count; i++)
        {
            _isFirsts.Add(true);
        }
    }

    void Start()
    {
        GameManager.Instance.OnResponseReceivedEvent += UpdateImageWithEnum;
        GameManager.Instance.OnContinueEvent += UpdateContinuesCountInCurrentImage;
    }

    private void UpdateContinuesCountInCurrentImage(object sender, EventArgs e)
    {
        _currentImageContinues++;
        if (_isFirsts[3] && _currentImageContinues >= 2 && _currentImageIndex == 1)
        {
            _isFirsts[3] = false;
            GameManager.Instance.InsertSystemMessage("Mai Sees 'Cloud Cooperation' Building, trough the trees. she arrives at the building entrance");
        }
    }

    private void UpdateImageWithEnum(object sender, GameManager.ResponseReceivedEventArgs e)
    {
        
        Response.GameLocation location = e.response.ImageDescription;
        if (_currentImageIndex == 2 && !location.Equals(Response.GameLocation.CloudCooperationBuilding))
        {
            GameManager.Instance.InsertSystemMessage("Don't let the player go to the building, respond to the player's action with a message that says 'You Don't know the way back.' or something similar");
            return;
        }
        switch (location)
        {
            case Response.GameLocation.ForestCrashSite:
                _currentImageContinues = 0;
                GameManager.Instance.DisableContinueButton();
                _imageComponent.sprite = images[0];
                return;
            case Response.GameLocation.SearchingCar:
                _currentImageContinues = 0;
                GameManager.Instance.DisableContinueButton();
                _imageComponent.sprite = images[1];
                if (_isFirsts[1]) GameManager.Instance.InsertSystemMessage(carSystemMessage);
                _isFirsts[1] = false;
                _currentImageIndex = 0;
                return;
            case Response.GameLocation.LookingAtPhone:
                GameManager.Instance.DisableContinueButton();
                if (!_imageComponent.sprite.Equals(images[2]))
                {
                    _currentImageContinues = 0;
                }
                _imageComponent.sprite = images[2];
                _currentImageIndex = 0;
                return;
            case Response.GameLocation.ThroughTheForest:
                GameManager.Instance.EnableContinueButton();
                if (!_imageComponent.sprite.Equals(images[3]))
                {
                    _currentImageContinues = 0;
                }
                _imageComponent.sprite = images[3];
                _currentImageIndex = 1;
                return;
            case Response.GameLocation.CloudCooperationBuilding:
                GameManager.Instance.EnableContinueButton();
                if (!_imageComponent.sprite.Equals(images[3])) //can't get to the building without going through the forest
                {
                    return;
                }
                if (!_imageComponent.sprite.Equals(images[4]))
                {
                    _currentImageContinues = 0;
                }
                _imageComponent.sprite = images[4];
                _currentImageIndex = 2;
                return;
            default:
                HandleUnknownLocation();
                return;
        }
    }

    private void HandleUnknownLocation()
    {
        switch (_currentImageIndex)
        {
            case 1:
            case 2:
                GameManager.Instance.EnableContinueButton();
                return;
        }
    }


    private void UpdateImage(object sender, GameManager.ResponseReceivedEventArgs e)
    {
        // string imageDescription = e.response.ImagePrompt;
        string imageDescription = e.response.StoryEvent;
        if (imageDescription == null)
            return;
        imageDescription = imageDescription.ToLower();
        if (imageDescription.Contains("car") &&
            (imageDescription.Contains("search") || imageDescription.Contains("inspect")|| imageDescription.Contains("look") || imageDescription.Contains("investigat") || imageDescription.Contains("check"))
            && _currentImageIndex == 0)
        {
            _imageComponent.sprite = images[1];
            if (_isFirsts[1]) GameManager.Instance.InsertSystemMessage(carSystemMessage);
            _isFirsts[1] = false;
            _currentImageIndex = 0;
            return;
        }
        if (imageDescription.Contains("phone") &&
            (imageDescription.Contains("open") || imageDescription.Contains("look") || imageDescription.Contains("unlock") || imageDescription.Contains("use")|| imageDescription.Contains("check"))
            && _currentImageIndex == 0)
        {
            _imageComponent.sprite = images[2];
            _currentImageIndex = 0;
            return;
        }
        if (imageDescription.Contains("forest") &&
            (imageDescription.Contains("run") || imageDescription.Contains("through")|| imageDescription.Contains("walk") || imageDescription.Contains("hike") || imageDescription.Contains("jog") || imageDescription.Contains("sprint"))
            && _currentImageIndex == 0)
        {
            _imageComponent.sprite = images[3];
            _currentImageIndex = 1;
            return;
        }
        if ((imageDescription.Contains("work") || imageDescription.Contains("cloud cooperation") || imageDescription.Contains("building")) &&
            (imageDescription.Contains("arriv") || imageDescription.Contains("stand") || imageDescription.Contains("in front of") || imageDescription.Contains("enter") || imageDescription.Contains("entrance"))
            && _currentImageIndex < 2)
        {
            _imageComponent.sprite = images[4];
            _currentImageIndex = 2;
            return;
        }
    }
    
    public int GetCurrentImageIndex()
    {
        return _currentImageIndex;
    }
}
