using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMicBlowDetection
{
    [RequireComponent(typeof(RectTransform))]
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

        [SerializeField] [Range(2, 2048)] private float scalFactor = 256;

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

        public void Draw(float[] samplesData)
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = LinerenderPointCount;
                _lineRenderer.startWidth = 0.02f;
                _lineRenderer.endWidth = 0.02f;

                var with = _lineRenderer.GetComponent<RectTransform>().sizeDelta.x;
                // var with = _lineRenderer.GetComponent<RectTransform>().anchorMax.x -
                //            _lineRenderer.GetComponent<RectTransform>().anchorMin.x;
                // with *= 0.5f;
                
                for (int i = 0; i < displayResolution; i++)
                {
                    var step = samplesData.Length / displayResolution;
                    var offset = i * step;

                    float v = 0;
                    for (int j = offset; j < offset + step && j < samplesData.Length; j++)
                    {
                        v += samplesData[j];
                    }

                    // v /= (float)displayResolution;

                    // v = Mathf.Pow(v, 5);

                    v *= scalFactor;
                    // var position = _lineRenderer.transform.position;
                    var position = Vector3.zero;

                    var xFactor = with / displayResolution;

                    if (!histogram)
                    {
                        _lineRenderer.SetPosition(i,
                            new Vector3((i * xFactor - with / 2) + position.x, v + position.y, -5));
                    }
                    else
                    {
                        _lineRenderer.SetPosition(i * 3,
                            new Vector3((i * xFactor - with / 2) + position.x, position.y, -5));
                        _lineRenderer.SetPosition(i * 3 + 1,
                            new Vector3((i * xFactor - with / 2) + position.x, v + position.y, -5));
                        _lineRenderer.SetPosition(i * 3 + 2,
                            new Vector3((i * xFactor - with / 2) + position.x, position.y, -5));
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (_lineRenderer != null && _audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);
                Draw(samples);
                
            //     _lineRenderer.positionCount = LinerenderPointCount;
            //     _lineRenderer.startWidth = 0.02f;
            //     _lineRenderer.endWidth = 0.02f;
            //
            //
            //     for (int i = 0; i < displayResolution; i++)
            //     {
            //         var step = samples.Length / displayResolution;
            //         var offset = i * step;
            //
            //         float v = 0;
            //         for (int j = offset; j < offset + step && j < samples.Length; j++)
            //         {
            //             v += samples[j];
            //         }
            //
            //         // v /= (float)displayResolution;
            //
            //         // v = Mathf.Pow(v, 5);
            //
            //         v *= scalFactor;
            //         var position = _lineRenderer.transform.position;
            //         // var position = Vector3.zero;
            //
            //         if (!histogram)
            //         {
            //             _lineRenderer.SetPosition(i,
            //                 new Vector3((i - displayResolution / 2) * 0.2f + position.x, v + position.y, -5));
            //         }
            //         else
            //         {
            //             _lineRenderer.SetPosition(i * 3,
            //                 new Vector3((i - displayResolution / 2) * 0.2f + position.x, position.y, -5));
            //             _lineRenderer.SetPosition(i * 3 + 1,
            //                 new Vector3((i - displayResolution / 2) * 0.2f + position.x, v + position.y, -5));
            //             _lineRenderer.SetPosition(i * 3 + 2,
            //                 new Vector3((i - displayResolution / 2) * 0.2f + position.x, position.y, -5));
            //         }
            //     }
            }
        }
    }
}