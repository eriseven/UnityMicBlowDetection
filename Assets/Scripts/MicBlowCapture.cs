using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityMicBlowDetection;

[RequireComponent(typeof(AudioSource))]
public class MicBlowCapture : MonoBehaviour
{
    // Boolean flags shows if the microphone is connected   
    private bool micConnected = false;

    //The maximum and minimum available recording frequencies    
    private int minFreq;
    private int maxFreq;

    //A handle to the attached AudioSource    
    private AudioSource goAudioSource;
    private AudioMixerGroup silenceGroup;
    [SerializeField] private int captureTimeLength = 20;

    [SerializeField]
    private bool keepSilence = true;

    // Start is called before the first frame update
    void Start()
    {
        if (Microphone.devices.Length <= 0)
        {
            //Throw a warning message at the console if there isn't    
            Debug.LogWarning("Microphone not connected!");
            return;
        }

        micConnected = true;

        //Get the default microphone recording capabilities    
        Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);

        //According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...    
        if (minFreq == 0 && maxFreq == 0)
        {
            //...meaning 44100 Hz can be used as the recording sampling rate    
            maxFreq = 44100;
        }

        goAudioSource = GetComponent<AudioSource>();
        silenceGroup = goAudioSource.outputAudioMixerGroup;
        SetupSamplesBuffer();
    }

    private bool isCapturing = false;

    public void StartCapture(Action<bool> callback)
    {
        if (!isCapturing)
        {
            Debug.Log("StartCapture");
            StartCoroutine(Capture(callback));
        }
        else
        {
            Debug.LogWarning("Is isCapturing...");
        }
    }

    public void StopCapture()
    {
        Debug.Log("StopCapture");
        isCapturing = false;
    }

    IEnumerator Capture(Action<bool> callback)
    {
        if (!micConnected)
        {
            callback?.Invoke(false);
            yield break;
        }

        if (Microphone.IsRecording(null))
        {
            callback?.Invoke(false);
            yield break;
        }

        if (goAudioSource == null)
        {
            callback?.Invoke(false);
            yield break;
        }

        goAudioSource.clip = Microphone.Start(null, true, captureTimeLength, maxFreq);
        isCapturing = true;

        while (!(Microphone.GetPosition(null) > 0))
        {
        }

        if (silenceGroup != null && keepSilence)
        {
            goAudioSource.outputAudioMixerGroup = silenceGroup;
        }
        else
        {
            goAudioSource.outputAudioMixerGroup = null;
        }

        goAudioSource.Play();

        yield return null;


        while (Microphone.IsRecording(null))
        {
            UpdateCaptureData();

            yield return null;
            if (!isCapturing)
            {
                Microphone.End(null);
                break;
            }
        }


        isCapturing = false;
        goAudioSource.Stop();

        yield return null;
        callback?.Invoke(true);
    }

    [SerializeField] [Range(32, 16384)] private int sampleSize = 2048;
    private float[] spectrumSamples;
    private float[] tempSamples;

    void SetupSamplesBuffer()
    {
        if (spectrumSamples == null || spectrumSamples.Length != sampleSize)
        {
            spectrumSamples = new float[sampleSize];
            tempSamples = new float[sampleSize];
        }
    }

    [SerializeField] private SimpleSpectrumDataRender renderer;

    class Accumulator
    {
        private float[] meanVector;
        public Accumulator(int vectorSize)
        {
            meanVector = new[] { 0.0f };
        }
        
        private float m;
        // private float s;
        private int N;

        // public void addDateValue(float x)
        // {
        //     N++;
        //     s = s + 1.0f * (N - 1) / N * (x - m) * (x - m);
        //     m = m + (x - m) / N;
        // }

        public float[] addDateValue(float[] vec)
        {
            Debug.Assert(vec.Length == meanVector.Length);

            N++;
            for (int i = 0; i < vec.Length; i++)
            {
                meanVector[i] = meanVector[i] + (vec[i] - meanVector[i]) / N;
                // m = m + (x - m) / N;
            }
            
            return meanVector;
        }
        
        
        // public void addDateValue(float x)
        // {
        //     N++;
        //     s = s + 1.0f * (N - 1) / N * (x - m) * (x - m);
        //     m = m + (x - m) / N;
        // }
        
        public float mean()
        {
            return m;
        }
    }

    private Accumulator _accumulator;
    
    void UpdateCaptureData()
    {
        SetupSamplesBuffer();
        
        if (_accumulator == null)
        {
            goAudioSource.GetSpectrumData(spectrumSamples, 0, FFTWindow.BlackmanHarris);
        }
        else
        {
            goAudioSource.GetSpectrumData(tempSamples, 0, FFTWindow.BlackmanHarris);
            spectrumSamples = _accumulator.addDateValue(tempSamples);
        }


        if (renderer != null)
        {
            renderer.Draw(spectrumSamples);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}