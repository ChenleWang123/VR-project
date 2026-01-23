using System;
using UnityEngine;

namespace Tutorial_5
{
    public enum EffectType { None, ILD, ITD }
    
    [RequireComponent(typeof(AudioSource))]
    public class AudioController : MonoBehaviour
    {
        [SerializeField] private EffectType currentEffect = EffectType.None;
        [Tooltip("Use position of player and audio source in the scene instead of the stereo pan slider")]
        [SerializeField] private bool useScene;
        
        [Range(0, 1)]
        [SerializeField] private float volume = 1f;
        [Range(0, 1)] [Tooltip("0 is left, 1 is right")]
        [SerializeField] private float stereoPosition = 0.5f;
        [Tooltip("Maximum ITD delay in milliseconds")]
        [SerializeField] private float maxHaasDelay = 20f;
        [SerializeField] private int sampleRate = 44100;

        [Header("References")]
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private Transform player;
        
        private AudioSource audioSource;
        
        private float[] leftDelayBuffer;
        private float[] rightDelayBuffer;
        private int bufferSize;
        private int leftWriteIndex = 0;
        private int rightWriteIndex = 0;
        private int leftReadIndex = 0;
        private int rightReadIndex = 0;
        private int leftDelaySamples;
        private int rightDelaySamples;

        void Start()
        {
            // Initialize audio source
            audioSource.clip = audioClip;
            audioSource.loop = true;
            audioSource.Play();

            // Initialize delay buffers
            bufferSize = Mathf.CeilToInt((maxHaasDelay / 1000f) * sampleRate);
            leftDelayBuffer = new float[bufferSize];
            rightDelayBuffer = new float[bufferSize];
            
            // main camera is the player
            player = Camera.main.transform;
        }

        private void Update()
        {
            if (useScene)
            {
                UpdateParamsFromScene();
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (channels < 2 || leftDelayBuffer == null || rightDelayBuffer == null)
            {
                Debug.LogError("This script requires a stereo audio source with initialized delay buffers.");
                return;
            }
            
            for (var i = 0; i < data.Length; i++)
            {
                // apply volume
                data[i] *= volume;
            }

            // Apply the selected effect
            switch (currentEffect)
            {
                case EffectType.ILD:
                    ApplyILD(data, channels);
                    break;
                case EffectType.ITD:
                    ApplyITD(data, channels);
                    break;
            }
        }
    
        private void ApplyILD(float[] data, int channels)
        {
            // stereoPosition: 0 = left, 1 = right
            // 转成 [-1, 1]
            float pan = stereoPosition * 2f - 1f;

            float leftGain  = Mathf.Clamp01(1f - pan);
            float rightGain = Mathf.Clamp01(1f + pan);

            for (int i = 0; i < data.Length; i += channels)
            {
                data[i]     *= leftGain;   // Left
                data[i + 1] *= rightGain;  // Right
            }
        }


     
        private void ApplyITD(float[] data, int channels)
        {
            float pan = stereoPosition * 2f - 1f;

            leftDelaySamples  = pan > 0 ? Mathf.RoundToInt(Mathf.Abs(pan) * bufferSize) : 0;
            rightDelaySamples = pan < 0 ? Mathf.RoundToInt(Mathf.Abs(pan) * bufferSize) : 0;

            for (int i = 0; i < data.Length; i += channels)
            {
                // LEFT
                leftDelayBuffer[leftWriteIndex] = data[i];
                int leftRead = (leftWriteIndex - leftDelaySamples + bufferSize) % bufferSize;
                if (leftDelaySamples > 0)
                    data[i] = leftDelayBuffer[leftRead];

                // RIGHT
                rightDelayBuffer[rightWriteIndex] = data[i + 1];
                int rightRead = (rightWriteIndex - rightDelaySamples + bufferSize) % bufferSize;
                if (rightDelaySamples > 0)
                    data[i + 1] = rightDelayBuffer[rightRead];

                leftWriteIndex  = (leftWriteIndex + 1) % bufferSize;
                rightWriteIndex = (rightWriteIndex + 1) % bufferSize;
            }
        }


        private void UpdateParamsFromScene()
        {
            Vector3 toSource = transform.position - player.position;

            // 左右声像
            Vector3 right = player.right;
            float pan = Vector3.Dot(right, toSource.normalized);

            stereoPosition = Mathf.Clamp01((pan + 1f) * 0.5f);

            // 距离衰减
            float distance = toSource.magnitude;
            volume = 1f / Mathf.Max(distance, 1f);
        }


        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            audioSource.hideFlags = HideFlags.HideInInspector;
        }
    }
}
