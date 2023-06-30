using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Demo : MonoBehaviour
{
    [SerializeField]
    private MicBlowCapture capture;
    
    [SerializeField]
    private CaptureButton captureButton;

    [SerializeField]
    private Button startButton;
    
    [SerializeField]
    private Button stopButton;
    // Start is called before the first frame update
    void Start()
    {
        startButton?.onClick.AddListener(() => { capture?.StartCapture(OnCaptureFinish); });

        stopButton?.onClick.AddListener(() => { capture?.StopCapture(); });       
        // captureButton.onPress += () =>
        // {
        //     capture?.StartCapture(OnCaptureFinish);
        // };
        // captureButton.onRelease += () =>
        // {
        //     capture?.StopCapture();
        // };
    }

    void OnCaptureFinish(bool result)
    {
        Debug.Log($"OnCaptureFinish {result}");
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
