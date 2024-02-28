using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageManager : MonoBehaviour
{
    [SerializeField] private List<Sprite> images = new List<Sprite>();
    private Image _imageComponent;
    private int _currentImageIndex = 0;
    private void Awake()
    {
        _imageComponent = GetComponent<Image>();
        _imageComponent.sprite = images[0];
    }

    void Start()
    {
        GameManager.Instance.OnResponseReceivedEvent += UpdateImage;
    }

    private void UpdateImage(object sender, GameManager.ResponseReceivedEventArgs e)
    {
        string imageDescription = e.response.ImagePrompt;
        if (imageDescription == null)
            return;
        
        if (imageDescription.Contains("forest") && imageDescription.Contains("running") && _currentImageIndex == 0)
        {
            _imageComponent.sprite = images[1];
            _currentImageIndex = 1;
            return;
        }
        if ((imageDescription.Contains("work") || imageDescription.Contains("Cloud Cooperation")) &&
            (imageDescription.Contains("arrive") || imageDescription.Contains("enter"))&& _currentImageIndex < 2)
        {
            _imageComponent.sprite = images[2];
            _currentImageIndex = 2;
            return;
        }
    }
}
