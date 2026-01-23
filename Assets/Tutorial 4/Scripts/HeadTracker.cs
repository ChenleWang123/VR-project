using MediaPipe.BlazeFace;
using UnityEngine;

namespace Tutorial_4
{
    public class HeadTracker : MonoBehaviour
    {
        [Tooltip("Index of your webcam.")]
        [SerializeField] private int webcamIndex = 0;
        [Tooltip("Threshold of the face detector")]
        [Range(0f, 1f)] 
        [SerializeField] private float threshold = 0.5f;
        [SerializeField] private ResourceSet resources;
        [Tooltip("Focal length of your webcam in pixels")]
        [SerializeField] private int focalLength = 492;
        [Tooltip("Distance between your eyes in meters.")]
        [SerializeField] private float ipd = 0.064f;

        public Vector3 DetectedFace { get; private set; }

        private FaceDetector _detector;
        private WebCamTexture _webCamTexture;

        private void Start()
        {
            _detector = new FaceDetector(resources);
            
            // Source - https://stackoverflow.com/a
            // Posted by S.Richmond
            // Retrieved 2025-11-19, License - CC BY-SA 3.0

            var devices = WebCamTexture.devices;
            /*
            foreach (var device in devices)
            {
                Debug.Log(device.name);
            }
            */
            if (devices.Length == 0)
            {
                Debug.LogWarning("No webcam found");
                return;
            }
            
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
            {
                return;
            }
            
            _detector.ProcessImage(_webCamTexture, threshold);
            if (_detector.Detections.Length == 0)
            {
                DetectedFace = Vector3.zero;
                return;
            }
            
            SetCameraPosition(_detector.Detections[0]);
        }

        private void SetCameraPosition(Detection face)
        {
            Debug.Log(">>> SetCameraPosition CALLED! Face detected.");
            int width = _webCamTexture.width;
            int height = _webCamTexture.height;

            // Convert UV (0..1) → pixel coordinates
            Vector2 leftEye = new Vector2(face.leftEye.x * width, face.leftEye.y * height);
            Vector2 rightEye = new Vector2(face.rightEye.x * width, face.rightEye.y * height);

            // Eye distance in image (S_img)
            float S_img = Vector2.Distance(leftEye, rightEye);
            Debug.Log("Eye Pixel Distance (S_img): " + S_img);

            if (S_img <= 1f)
                return;

            // Center point between eyes
            float u = (leftEye.x + rightEye.x) / 2f;
            float v = (leftEye.y + rightEye.y) / 2f;

            // Image center (c_x, c_y)
            float cx = width / 2f;
            float cy = height / 2f;

            // z distance
            float z = -(focalLength * ipd) / S_img;

            // x, y from formula
            float x = (u - cx) * z / focalLength;
            float y = -(v - cy) * z / focalLength;

            // Apply to camera
            Camera.main.transform.localPosition = new Vector3(x, y, z);

            // Save useful data if needed
            DetectedFace = new Vector3(x, y, z);
        }

    }
}