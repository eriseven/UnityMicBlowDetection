using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif


using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityMicBlowDetection;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

[RequireComponent(typeof(AudioSource))]
public class MicBlowCapture : MonoBehaviour
{
    public enum CaptureStat
    {
        None,
        MicBlowDetected,
        CanNotFindMicroPhone,
        MicroPhoneIsBusy,
        AudioSourceMissing,
        PermissionDenied,
        PermissionGranted,
        PermissionDeniedAndDontAskAgain,
    }

    // Boolean flags shows if the microphone is connected   
    private bool micConnected = false;

    //The maximum and minimum available recording frequencies    
    private int minFreq;
    private int maxFreq;

    //A handle to the attached AudioSource    
    private AudioSource goAudioSource;
    private AudioMixerGroup silenceGroup;
    [SerializeField] private int captureTimeLength = 20;

    [SerializeField] private bool keepSilence = true;

    private bool isCapturing = false;

    #region Public Methods

    public void StartCapture(Action<string> callback, bool record = false)
    {
        if (!isCapturing)
        {
            Debug.Log("StartCapture");
            StartCoroutine(Capture(callback, record));
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

    public bool HasPermission()
    {
#if UNITY_IOS || UNITY_EDITOR_OSX
        return Application.HasUserAuthorization(UserAuthorization.Microphone);
#elif UNITY_ANDROID
        return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
        return true;
#endif
    }

    public void RequestMiroPhonePermission(Action<string> callback)
    {
#if UNITY_IOS || UNITY_EDITOR_OSX
        StartCoroutine(RequestIOSPermission(callback));
#elif UNITY_ANDROID
        StartCoroutine(RequestAndroidPermission(callback));
#else
        callback?.Invoke(CaptureStat.PermissionGranted.ToString());
#endif
    }
    
    private static int VOLUME_DATA_LENGTH = 128 / 2; 
    private float[] volumeDataBuff = new float[VOLUME_DATA_LENGTH];
    public float GetMicroPhoneVolume()
    {
        if (Microphone.IsRecording(null) && goAudioSource != null && goAudioSource.clip != null)
        {
            var offset = Microphone.GetPosition(null) - VOLUME_DATA_LENGTH + 1;
            Debug.Log($"Microphone record offset: {offset}");
            if (offset < 0)
            {
                return 0;
            }
            
            goAudioSource.clip.GetData(volumeDataBuff, offset);
            return volumeDataBuff.Select(Mathf.Abs).Max() * 100;
        }

        return 0;
    }
    
    #endregion //Public Methods

    [Button]
    public void LoadReference()
    {
        try
        {
            // private static readonly string dumpDataDir = "DumpedSpectrumSamples";
            var files = Directory.GetFiles(Path.Combine(Application.persistentDataPath, dumpDataDir), "*.data", SearchOption.TopDirectoryOnly);
            
            Accumulator accumulator = new Accumulator(referenceSamples.Length);
            float[] tempSamples = null;
            
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                tempSamples = accumulator.addDateValue(LoadSampleData(fileName));
            }

            if (tempSamples != null)
            {
                referenceSamples = this.tempSamples;
            }
            
            recordedRenderer?.Draw(referenceSamples);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    #region Priviate Methods

    

    // Start is called before the first frame update
    void InitMicroPhone()
    {
        if (micConnected)
        {
            return;
        }

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


    private static readonly string dumpDataDir = "DumpedSpectrumSamples";
    static void SaveSampleData(float[] data, string fileName)
    {
        if (data == null || data.Length == 0)
        {
            return;
        }

        var saveDir = Path.Combine(Application.persistentDataPath, dumpDataDir);
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }
        
        var savePath = Path.Combine(saveDir, fileName);
        
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }

        var saveObject = new MicroPhoneDumpData();
        saveObject.dumpData = data;
        
        File.WriteAllText(savePath, JsonUtility.ToJson(saveObject), Encoding.UTF8);
    }

    static float[] LoadSampleData(string fileName)
    {
        try
        {
            var content = File.ReadAllText(Path.Combine(Application.persistentDataPath, dumpDataDir, fileName), Encoding.UTF8);
            return JsonUtility.FromJson<MicroPhoneDumpData>(content).dumpData;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return Array.Empty<float>();
        }
    }

#if UNITY_ANDROID
    IEnumerator RequestAndroidPermission(Action<string> callback)
    {
        CaptureStat stat = CaptureStat.None;

        var callbacks = new PermissionCallbacks();
        callbacks.PermissionDenied += s => { stat = CaptureStat.PermissionDenied; };
        callbacks.PermissionGranted += s => { stat = CaptureStat.PermissionGranted; };
        callbacks.PermissionDeniedAndDontAskAgain += s => { stat = CaptureStat.PermissionDeniedAndDontAskAgain; };

        Permission.RequestUserPermission(Permission.Microphone, callbacks);
        while (stat == CaptureStat.None)
        {
            yield return null;
        }

        callback?.Invoke(stat.ToString());
    }
#endif


#if UNITY_IOS || UNITY_EDITOR_OSX
    IEnumerator RequestIOSPermission(Action<string> callback)
    {
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }

        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            callback?.Invoke(CaptureStat.PermissionDenied.ToString());
        }
        else
        {
            callback?.Invoke(CaptureStat.PermissionGranted.ToString());
        }
    }
#endif


    [SerializeField] private TextMeshProUGUI detectMatchCountText;

    IEnumerator Capture(Action<string> callback, bool record = false)
    {
        InitMicroPhone();
        if (!micConnected)
        {
            callback?.Invoke(CaptureStat.CanNotFindMicroPhone.ToString());
            yield break;
        }

        // Request Permission

        #region iOS

#if UNITY_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }

        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            callback?.Invoke(CaptureStat.PermissionDenied.ToString());
            yield break;
        }
#endif

        #endregion

        #region Android

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            yield return RequestAndroidPermission(null);
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            callback?.Invoke(CaptureStat.PermissionDenied.ToString());
            yield break;
        }
#endif

        #endregion


        if (Microphone.IsRecording(null))
        {
            callback?.Invoke(CaptureStat.MicroPhoneIsBusy.ToString());
            yield break;
        }

        if (goAudioSource == null)
        {
            callback?.Invoke(CaptureStat.AudioSourceMissing.ToString());
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


        if (record)
        {
            // _accumulator = new Accumulator(spectrumSamples.Length);
        }
        else
        {
            _accumulator = null;
        }

        int detectMatchCount = 0;
        bool blowDetected = false;

        if (detectMatchCountText != null)
        {
            detectMatchCountText.text = "";
        }

        if (varianceText != null)
        {
            varianceText.text = "";
        }

        while (Microphone.IsRecording(null))
        {
            UpdateCaptureData();

            if (!record)
            {
                if (DetectBlow())
                {
                    detectMatchCount++;
                }

                if (detectMatchCountText != null)
                {
                    detectMatchCountText.text = detectMatchCount.ToString();
                }

                Debug.Log($"detectMatchCount: {detectMatchCount}");
                blowDetected = detectMatchCount >= varReferenceMatchCount;
            }


            yield return null;
            if (blowDetected || !isCapturing)
            {
                Microphone.End(null);
                break;
            }
        }

        isCapturing = false;
        goAudioSource.Stop();

        if (record)
        {
            referenceSamples = spectrumSamples.ToArray();
            SaveSampleData(referenceSamples, $"dump{DateTime.UtcNow:HH_mm_ss}.data");
            recordedRenderer?.Draw(referenceSamples);
        }

        yield return null;
        callback?.Invoke(blowDetected ? CaptureStat.MicBlowDetected.ToString() : CaptureStat.None.ToString());
    }

    [SerializeField] [Range(32, 16384)] private int sampleSize = 2048;

    private float[] spectrumSamples;
    private float[] tempSamples;

    [SerializeField, HideInInspector] private float[] referenceSamples;
    public float[] ReferenceSamples => referenceSamples.ToArray();

    void SetupSamplesBuffer()
    {
        if (spectrumSamples == null || spectrumSamples.Length != sampleSize)
        {
            spectrumSamples = new float[sampleSize];
            tempSamples = new float[sampleSize];
        }

        if (referenceSamples == null || referenceSamples.Length != sampleSize)
        {
            referenceSamples = new float[sampleSize];
        }
    }

    [SerializeField] private SimpleSpectrumDataRender renderer;

    [SerializeField] private SimpleSpectrumDataRender recordedRenderer;

    class Accumulator
    {
        private float[] meanVector;

        public Accumulator(int vectorSize)
        {
            meanVector = new float[vectorSize];
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

    [SerializeField, Range(0, 100)] private float varReference = 0;

    [SerializeField, Range(0, 20)] private int varReferenceMatchCount = 1;

    [SerializeField] private TextMeshProUGUI varianceText;
    

    bool DetectBlow()
    {
        if (spectrumSamples != null && referenceSamples != null && spectrumSamples.Length == referenceSamples.Length)
        {
            for (int i = 0; i < spectrumSamples.Length; i++)
            {
                tempSamples[i] = spectrumSamples[i] - referenceSamples[i];
            }

            var m = tempSamples.Sum() / tempSamples.Length;
            // var v = tempSamples.Select(f => Mathf.Pow(f - m, 2)).Sum() / tempSamples.Length;
            var v = tempSamples.Select(f => (f - m) * (f - m)).Sum() / tempSamples.Length;
            v *= 1000000;
            Debug.Log($"variance: {v:R}");
            if (varianceText != null)
            {
                varianceText.text = v.ToString("R");
            }

            return v <= varReference;
        }

        return false;
    }
    
    #endregion
}