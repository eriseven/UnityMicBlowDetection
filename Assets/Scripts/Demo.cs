using System;
using System.Collections;
using System.Collections.Generic;
using IngameDebugConsole;
using RuntimeInspectorNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityMicBlowDetection
{
    public class Demo : MonoBehaviour
    {
        [SerializeField] private MicBlowCapture capture;

        [SerializeField] private CaptureButton captureButton;

        [SerializeField] private CaptureButton recordButton;

        [SerializeField] private Button startButton;

        [SerializeField] private Button stopButton;

        [SerializeField] private TextMeshProUGUI state;

        // Start is called before the first frame update
        void Start()
        {
            // (startButton != null ? startButton.onClick : null).AddListener(() => { capture?.StartCapture(OnCaptureFinish); });
            //
            // (stopButton != null ? stopButton.onClick : null).AddListener(() => { capture?.StopCapture(); });       

            if (captureButton != null)
            {
                captureButton.onPress += () =>
                {
                    if (state != null)
                    {
                        state.text = "Capturing...";
                    }

                    capture?.StartCapture(OnCaptureFinish);
                };
                captureButton.onRelease += () =>
                {
                    capture?.StopCapture();
                    if (state != null)
                    {
                        state.text = "";
                    }
                };
            }

            if (recordButton != null)
            {
                recordButton.onPress += () =>
                {
                    if (state != null)
                    {
                        state.text = "Recording...";
                    }

                    capture?.StartCapture(OnCaptureFinish, true);
                };
                recordButton.onRelease += () =>
                {
                    capture?.StopCapture();
                    if (state != null)
                    {
                        state.text = "";
                    }
                };
            }
        }

        void OnCaptureFinish(string result)
        {
            Debug.Log($"OnCaptureFinish {result}");
        }

        private void Awake()
        {
            DebugLogConsole.AddCommand("show-ins", "show inspector", _ShowInspector);
            DebugLogConsole.AddCommand("hide-ins", "hide inspector", _HideInspector);
            if (capture != null)
            {
                DebugLogConsole.AddCommand("load", "load saved samples", capture.LoadReference);
            }
        }

        [SerializeField] private RuntimeInspector inspector;

        void _ShowInspector()
        {
            if (inspector != null)
            {
                inspector.gameObject.SetActive(true);
                inspector.Inspect(capture);
            }
        }

        void _HideInspector()
        {
            if (inspector != null)
            {
                inspector.StopInspect();
                inspector.gameObject.SetActive(false);
            }
        }
    }
}