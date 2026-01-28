using System;
using UnityEngine;

namespace Final
{
    public enum EffectType { None, ILD, ITD }

    [RequireComponent(typeof(AudioSource))]
    public class AudioController : MonoBehaviour
    {
        [SerializeField] private EffectType currentEffect = EffectType.None;
        [SerializeField] private bool useScene;

        [Header("Volume Control")]
        [Range(0, 3)]
        [SerializeField] private float volume = 1.0f; // 稍微小一点
        [Range(0, 1)]
        [Tooltip("0 is left, 1 is right")]
        [SerializeField] private float stereoPosition = 0.5f;

        [Tooltip("Maximum ITD delay in milliseconds")]
        [SerializeField] private float maxHaasDelay = 30f; // ITD 延迟稍微放大，让左右差更明显
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
        private int leftDelaySamples;
        private int rightDelaySamples;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = audioClip;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.spatialize = false;
            audioSource.volume = 1f;
            audioSource.Play();

            bufferSize = Mathf.CeilToInt((maxHaasDelay / 1000f) * sampleRate);
            leftDelayBuffer = new float[bufferSize];
            rightDelayBuffer = new float[bufferSize];

            if (player == null)
                player = Camera.main.transform;
        }

        private void Update()
        {
            if (useScene && player != null)
                UpdateParamsFromScene();
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (channels < 2 || leftDelayBuffer == null || rightDelayBuffer == null)
                return;

            float overallVolume = Mathf.Clamp(volume, 0.1f, 2f);

            for (int i = 0; i < data.Length; i++)
                data[i] *= overallVolume;

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
            float pan = stereoPosition * 2f - 1f;

            // 增强左右差异 1.6 倍
            float leftGain = Mathf.Clamp01(1f - pan) * 1.6f;
            float rightGain = Mathf.Clamp01(1f + pan) * 1.6f;

            for (int i = 0; i < data.Length; i += channels)
            {
                data[i] *= leftGain;
                data[i + 1] *= rightGain;
            }
        }

        private void ApplyITD(float[] data, int channels)
        {
            float pan = stereoPosition * 2f - 1f;

            // 放大延迟效果
            leftDelaySamples = pan > 0 ? Mathf.RoundToInt(Mathf.Abs(pan) * bufferSize) : 0;
            rightDelaySamples = pan < 0 ? Mathf.RoundToInt(Mathf.Abs(pan) * bufferSize) : 0;

            for (int i = 0; i < data.Length; i += channels)
            {
                leftDelayBuffer[leftWriteIndex] = data[i];
                int leftRead = (leftWriteIndex - leftDelaySamples + bufferSize) % bufferSize;
                if (leftDelaySamples > 0)
                    data[i] = leftDelayBuffer[leftRead];

                rightDelayBuffer[rightWriteIndex] = data[i + 1];
                int rightRead = (rightWriteIndex - rightDelaySamples + bufferSize) % bufferSize;
                if (rightDelaySamples > 0)
                    data[i + 1] = rightDelayBuffer[rightRead];

                leftWriteIndex = (leftWriteIndex + 1) % bufferSize;
                rightWriteIndex = (rightWriteIndex + 1) % bufferSize;
            }
        }

        private void UpdateParamsFromScene()
        {
            if (player == null) return;

            Vector3 toSource = transform.position - player.position;

            float pan = Vector3.Dot(player.right, toSource.normalized);
            stereoPosition = Mathf.Clamp01((pan + 1f) * 0.5f);

            // 轻微距离衰减，保证声音不会太小
            float distance = Mathf.Max(toSource.magnitude, 0.1f);
            volume = Mathf.Clamp(1.2f / distance, 0.5f, 1.5f);
        }

        private void OnValidate()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            audioSource.hideFlags = HideFlags.HideInInspector;
        }
    }
}
