using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SliderScript : MonoBehaviour
{
    [SerializeField]
    PresentationLayer PresLayer;
    UnityAction<float> sliderListener;    
    void Awake()
    {
        
        Slider slider = GetComponent<Slider>();
        sliderListener = PresLayer.ChangeWaitTime;
        slider.onValueChanged.AddListener(sliderListener);
    }
}
