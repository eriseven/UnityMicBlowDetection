using UnityEngine;

namespace UnityMicBlowDetection
{
    public class MicroPhoneDumpData : ScriptableObject
    {
        [SerializeField, HideInInspector]
        public float[] dumpData;
    }
}