using MediaPipe.BlazeFace;
using UnityEngine;

namespace Tutorial_4
{
    public class HeadTracker : MonoBehaviour
    {
        [Tooltip("Index of your webcam.")]
        [SerializeField] private int webcamIndex = 0;

        [Tooltip("Threshold of the face detector.")]
        [Range(0f, 1f)]
        [SerializeField] private float threshold = 0.5f;

        [SerializeField] private ResourceSet resources;

        [Tooltip("Focal length of your webcam in pixels.")]
        [SerializeField] private int focalLength = 492;

        [Tooltip("Distance between your eyes in meters.")]
        [SerializeField] private float ipd = 0.064f;

        [Header("Rotation Approximation (Calibrated)")]
        [Tooltip("Max yaw angle in degrees when face center moves across the image.")]
        [SerializeField] private float yawMax = 25f;

        [Tooltip("Max pitch angle in degrees when face center moves across the image.")]
        [SerializeField] private float pitchMax = 20f;

        [Tooltip("Deadzone in normalized units (0..1 of image size). Small movements inside deadzone won't rotate.")]
        [SerializeField] private float deadzone = 0.02f;

        [Tooltip("Smoothing for rotation (0=no smoothing, 1=very smooth).")]
        [Range(0f, 1f)]
        [SerializeField] private float rotationSmoothing = 0.85f;

        [Tooltip("Invert yaw direction if left/right feels reversed.")]
        [SerializeField] private bool invertYaw = true;

        [Tooltip("Invert pitch direction if up/down feels reversed.")]
        [SerializeField] private bool invertPitch = false;

        [Header("Calibration")]
        [Tooltip("Press C to re-center (calibrate) face center as neutral.")]
        [SerializeField] private KeyCode recalibrateKey = KeyCode.C;

        public Vector3 DetectedFace { get; private set; } = Vector3.zero;

        // Approximate head rotation from calibrated face center offset (NOT true 3D pose).
        public Quaternion DetectedRotation { get; private set; } = Quaternion.identity;

        private FaceDetector _detector;
        private WebCamTexture _webCamTexture;

        // Calibration center in normalized image coordinates (0..1)
        private bool _hasCenter = false;
        private Vector2 _center01 = new Vector2(0.5f, 0.5f);

        // Smoothed angles
        private float _yawSmoothed = 0f;
        private float _pitchSmoothed = 0f;

        private void Start()
        {
            _detector = new FaceDetector(resources);

            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogWarning("No webcam found.");
                return;
            }

            webcamIndex = Mathf.Clamp(webcamIndex, 0, devices.Length - 1);
            var device = devices[webcamIndex];

            _webCamTexture = new WebCamTexture(device.name);
            _webCamTexture.Play();
        }

        private void OnDestroy()
        {
            _detector?.Dispose();
        }

        private void Update()
        {
            if (_webCamTexture == null)
                return;

            if (Input.GetKeyDown(recalibrateKey))
            {
                _hasCenter = false;
                _yawSmoothed = 0f;
                _pitchSmoothed = 0f;
                DetectedRotation = Quaternion.identity;
            }

            _detector.ProcessImage(_webCamTexture, threshold);

            if (_detector.Detections.Length == 0)
            {
                DetectedFace = Vector3.zero;
                // Keep rotation stable when lost (do not reset to identity to avoid jumps)
                return;
            }

            SetPose(_detector.Detections[0]);
        }

        private void SetPose(Detection face)
        {
            int width = _webCamTexture.width;
            int height = _webCamTexture.height;

            // Convert UV (0..1) -> pixel coordinates.
            Vector2 leftEye = new Vector2(face.leftEye.x * width, face.leftEye.y * height);
            Vector2 rightEye = new Vector2(face.rightEye.x * width, face.rightEye.y * height);

            // Eye distance in image (pixels).
            float S_img = Vector2.Distance(leftEye, rightEye);
            Debug.Log("Eye Pixel Distance (S_img): " + S_img);

            if (S_img <= 1f)
                return;

            // Eye center (pixels).
            float u = (leftEye.x + rightEye.x) / 2f;
            float v = (leftEye.y + rightEye.y) / 2f;

            // Image center (pixels).
            float cx = width / 2f;
            float cy = height / 2f;

            // Depth (negative because forward is -Z here).
            float z = -(focalLength * ipd) / S_img;

            // Back-project to x,y.
            float x = (u - cx) * z / focalLength;
            float y = -(v - cy) * z / focalLength;

            DetectedFace = new Vector3(x, y, z);

            // --- Calibrated rotation ---
            // Current center in normalized image coordinates (0..1)
            Vector2 p01 = new Vector2(u / width, v / height);

            // Capture neutral center when first face appears or after recalibration
            if (!_hasCenter)
            {
                _center01 = p01;
                _hasCenter = true;
            }

            // Offset relative to calibrated center
            Vector2 d01 = p01 - _center01;

            // Deadzone
            d01.x = ApplyDeadzone(d01.x, deadzone);
            d01.y = ApplyDeadzone(d01.y, deadzone);

            // Map to angles
            float yaw = d01.x * yawMax;
            float pitch = -d01.y * pitchMax;

            if (invertYaw) yaw = -yaw;
            if (invertPitch) pitch = -pitch;

            // Smooth to prevent drift/jitter
            _yawSmoothed = Mathf.Lerp(_yawSmoothed, yaw, 1f - rotationSmoothing);
            _pitchSmoothed = Mathf.Lerp(_pitchSmoothed, pitch, 1f - rotationSmoothing);

            DetectedRotation = Quaternion.Euler(_pitchSmoothed, _yawSmoothed, 0f);
        }

        private static float ApplyDeadzone(float v, float dz)
        {
            float av = Mathf.Abs(v);
            if (av <= dz) return 0f;

            // Re-scale so output is continuous at the edge of deadzone
            float sign = Mathf.Sign(v);
            return sign * (av - dz) / (1f - dz);
        }
    }
}
