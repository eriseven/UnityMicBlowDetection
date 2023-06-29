using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMicBlowDetection
{
    public class SimpleSpectrumDataRender : MonoBehaviour
    {
        [SerializeField] private LineRenderer _lineRenderer;

        [SerializeField] private AudioSource _audioSource;

        private float[] samples;

        [SerializeField] [Range(32, 16384)] private int sampleSize = 2048;

        [SerializeField] [Range(32, 256)] private int displayResolution = 32;

        private int LinerenderPointCount
        {
            get
            {
                if (histogram)
                {
                    return displayResolution * 3;
                }

                return displayResolution;
            }
        }

        [SerializeField] [Range(2, 100)] private float scalFactor = 10;

        [SerializeField] private bool histogram = false;

        void SetupSamplesBuffer()
        {
            if (samples == null || samples.Length != sampleSize)
            {
                samples = new float[sampleSize];
            }
        }

        private void Awake()
        {
            Application.runInBackground = true;
            SetupSamplesBuffer();
        }

        private void OnEnable()
        {
            SetupSamplesBuffer();
        }

        // Update is called once per frame
        void Update()
        {
            if (_lineRenderer != null && _audioSource != null && _audioSource.isPlaying)
            {
                _lineRenderer.positionCount = LinerenderPointCount;
                _lineRenderer.startWidth = 0.02f;
                _lineRenderer.endWidth = 0.02f;

                _audioSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);


                for (int i = 0; i < displayResolution; i++)
                {
                    var step = samples.Length / displayResolution;
                    var offset = i * step;

                    float v = 0;
                    for (int j = offset; j < offset + step && j < samples.Length; j++)
                    {
                        v += samples[j];
                    }

                    // v /= (float)displayResolution;

                    // v = Mathf.Pow(v, 5);

                    v *= scalFactor;
                    var position = _lineRenderer.transform.position;
                    // var position = Vector3.zero;

                    if (!histogram)
                    {
                        _lineRenderer.SetPosition(i,
                            new Vector3((i - displayResolution / 2) * 0.2f + position.x, v + position.y, -5));
                    }
                    else
                    {
                        _lineRenderer.SetPosition(i * 3,
                            new Vector3((i - displayResolution / 2) * 0.2f + position.x, position.y, -5));
                        _lineRenderer.SetPosition(i * 3 + 1,
                            new Vector3((i - displayResolution / 2) * 0.2f + position.x, v + position.y, -5));
                        _lineRenderer.SetPosition(i * 3 + 2,
                            new Vector3((i - displayResolution / 2) * 0.2f + position.x, position.y, -5));
                    }
                }
            }
        }
    }
}