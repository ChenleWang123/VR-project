using System.Collections.Generic;
using UnityEngine;

namespace Tutorial_4
{
    public class Filter : MonoBehaviour
    {
        [Header("Moving average")]
        [Range(1, 200)] public int samples = 30;
        public bool logMovingAverage = false;

        [Header("Single Exponential")]
        [Range(0.01f, 1.0f)] public float seAlpha = 0.03f;

        [Header("Double Exponential")]
        [Range(0.0f, 1.0f)] public float deAlpha = 0.04f;
        [Range(0.0f, 1.0f)] public float deBeta = 0.5f;

        [Header("One Euro")]
        public float frequency = 60f;

        // Buffers / states
        private readonly Queue<Vector3> _movingAverageBuffer = new();

        private bool _seInitialized = false;
        private Vector3 _singleExponential;

        private bool _deInitialized = false;
        private Vector3 _doubleExponential;   // s_t
        private Vector3 _trend;               // d_t

        private OneEuroFilter<Vector3> _oneEuro;
        private float _lastFrequency = -1f;

        private void Awake()
        {
            EnsureOneEuro();
        }

        private void OnValidate()
        {
            // Called when inspector values change (Editor)
            EnsureOneEuro();
        }

        private void EnsureOneEuro()
        {
            if (_oneEuro == null || !Mathf.Approximately(_lastFrequency, frequency))
            {
                _oneEuro = new OneEuroFilter<Vector3>(frequency);
                _lastFrequency = frequency;
            }
        }

        // 1) Moving Average
        public Vector3 MovingAverage(Vector3 value)
        {
            if (logMovingAverage) Debug.Log($"[MA] Input = {value}");

            _movingAverageBuffer.Enqueue(value);

            if (_movingAverageBuffer.Count > samples)
                _movingAverageBuffer.Dequeue();

            Vector3 sum = Vector3.zero;
            foreach (var v in _movingAverageBuffer)
                sum += v;

            Vector3 output = sum / _movingAverageBuffer.Count;

            if (logMovingAverage) Debug.Log($"[MA] Output = {output}");
            return output;
        }

        // 2) Single Exponential
        public Vector3 SingleExponential(Vector3 value)
        {
            if (!_seInitialized)
            {
                _singleExponential = value;
                _seInitialized = true;
                return value;
            }

            _singleExponential = seAlpha * value + (1 - seAlpha) * _singleExponential;
            return _singleExponential;
        }

        // 3) Double Exponential (Holt)
        public Vector3 DoubleExponential(Vector3 value)
        {
            if (!_deInitialized)
            {
                _doubleExponential = value;  // s0
                _trend = Vector3.zero;       // d0
                _deInitialized = true;
                return value;
            }

            Vector3 s_prev = _doubleExponential;
            Vector3 d_prev = _trend;

            Vector3 s = deAlpha * value + (1 - deAlpha) * (s_prev + d_prev);
            Vector3 d = deBeta * (s - s_prev) + (1 - deBeta) * d_prev;

            _doubleExponential = s;
            _trend = d;

            return s;
        }

        // 4) One Euro
        public Vector3 OneEuro(Vector3 value)
        {
            EnsureOneEuro();
            return _oneEuro.Filter(value);
        }

        // Optional: reset all filter states (useful when switching modes)
        public void ResetState(Vector3 value)
        {
            _movingAverageBuffer.Clear();

            _seInitialized = true;
            _singleExponential = value;

            _deInitialized = true;
            _doubleExponential = value;
            _trend = Vector3.zero;

            _oneEuro = new OneEuroFilter<Vector3>(frequency);
            _lastFrequency = frequency;
        }
    }
}
