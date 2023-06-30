using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CaptureButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public event Action onPress;
    public event Action onRelease;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        onPress?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        onRelease?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onRelease?.Invoke();
    }
}
