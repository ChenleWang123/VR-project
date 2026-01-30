using UnityEngine;
using System;
using Apt.Unity.Projection;

public class StereoCameraController : MonoBehaviour
{
    public enum FilterType
    {
        None, MovingAverage, SingleExponential, DoubleExponential, OneEuro
    }

    [Header("Stereo Cameras")]
    public Camera leftCamera;
    public Camera rightCamera;

    [Tooltip("Inter-pupillary distance in meters.")]
    public float ipd = 0.064f;

    [Header("Convergence")]
    public bool toe_in = false;
    public Transform convergencePoint;

    [Header("Projection")]
    public ProjectionPlane projectionPlane;
    public bool offAxisProjection = false;

    [Header("Head Tracking")]
    public Tutorial_4.HeadTracker headTracker;

    [Tooltip("Anchor that parents all cameras. Apply head pose here.")]
    public Transform headAnchor;

    [Tooltip("Offset applied to headAnchor local position (e.g., Y=1.6 for eye height).")]
    public Vector3 headOffset = new Vector3(0f, 1.6f, 0f);

    [Tooltip("Scale factor for head tracking translation.")]
    public float positionScale = 0.3f;

    [Tooltip("If true, keep last valid head position when face is lost.")]
    public bool freezeWhenLost = true;

    [Header("Position Axis Inversion")]
    public bool invertX = false;
    public bool invertY = false;
    public bool invertZ = false;

    [Header("Rotation")]
    [Tooltip("Apply head rotation offset (from HeadTracker) to headAnchor.")]
    public bool applyHeadRotation = true;

    [Range(0f, 2f)]
    public float rotationStrength = 1.0f;

    [Header("Filtering (Position Only)")]
    public FilterType positionFilterType = FilterType.OneEuro;

    [Tooltip("Reference to Filter component in the scene.")]
    public Tutorial_4.Filter filter;

    private Vector3 _lastHeadAnchorLocalPos;
    private Quaternion _headAnchorBaseLocalRot;

    private void Start()
    {
        if (headAnchor == null)
        {
            Debug.LogError("HeadAnchor is not assigned. Please create a HeadAnchor under CameraRig and assign it.");
            headAnchor = transform; // fallback to avoid null crash
        }

        _headAnchorBaseLocalRot = headAnchor.localRotation;

        if (filter == null)
        {
            // Try to auto-find a Filter in the scene
            filter = FindAnyObjectByType<Tutorial_4.Filter>();
            if (filter == null)
                Debug.LogWarning("Filter reference is null. Position filtering will be disabled.");
        }
    }

    private void Update()
    {
        // 0) Update head anchor pose (translation + optional rotation).
        UpdateHeadAnchorFromTracker();

        // 1) IPD offsets (relative to headAnchor).
        if (leftCamera != null)
            leftCamera.transform.localPosition = Vector3.left * ipd / 2f;

        if (rightCamera != null)
            rightCamera.transform.localPosition = Vector3.right * ipd / 2f;

        // 2) Toe-in or parallel cameras.
        if (toe_in && convergencePoint != null)
        {
            if (leftCamera != null) leftCamera.transform.LookAt(convergencePoint);
            if (rightCamera != null) rightCamera.transform.LookAt(convergencePoint);
        }
        else
        {
            if (leftCamera != null) leftCamera.transform.localRotation = Quaternion.identity;
            if (rightCamera != null) rightCamera.transform.localRotation = Quaternion.identity;
        }

        // 3) Off-axis projection.
        if (leftCamera != null) ApplyOffAxisProjection(leftCamera);
        if (rightCamera != null) ApplyOffAxisProjection(rightCamera);
    }

    private void UpdateHeadAnchorFromTracker()
    {
        if (headTracker == null || headAnchor == null)
            return;

        Vector3 h = headTracker.DetectedFace;
        bool hasFace = h != Vector3.zero;

        if (!hasFace)
        {
            if (freezeWhenLost)
                headAnchor.localPosition = _lastHeadAnchorLocalPos;

            if (applyHeadRotation)
                headAnchor.localRotation = _headAnchorBaseLocalRot;

            return;
        }

        // Axis correction for translation.
        if (invertX) h.x = -h.x;
        if (invertY) h.y = -h.y;
        if (invertZ) h.z = -h.z;

        // Translation (then filter).
        Vector3 anchorLocalPos = (h * positionScale) + headOffset;
        anchorLocalPos = ApplyPositionFilter(anchorLocalPos);

        headAnchor.localPosition = anchorLocalPos;
        _lastHeadAnchorLocalPos = anchorLocalPos;

        // Rotation offset (already smoothed/calibrated inside HeadTracker).
        if (applyHeadRotation)
        {
            Quaternion headRot = headTracker.DetectedRotation;

            // Apply strength by scaling Euler angles (stable for small angles).
            Vector3 e = headRot.eulerAngles;
            e.x = NormalizeAngle(e.x) * rotationStrength;
            e.y = NormalizeAngle(e.y) * rotationStrength;
            e.z = 0f;

            Quaternion headOffsetRot = Quaternion.Euler(e);
            headAnchor.localRotation = _headAnchorBaseLocalRot * headOffsetRot;
        }
    }

    private Vector3 ApplyPositionFilter(Vector3 value)
    {
        if (filter == null)
            return value;

        switch (positionFilterType)
        {
            case FilterType.None:
                return value;
            case FilterType.MovingAverage:
                return filter.MovingAverage(value);
            case FilterType.SingleExponential:
                return filter.SingleExponential(value);
            case FilterType.DoubleExponential:
                return filter.DoubleExponential(value);
            case FilterType.OneEuro:
                return filter.OneEuro(value);
            default:
                return value;
        }
    }

    private static float NormalizeAngle(float a)
    {
        // Convert 0..360 to -180..180.
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }

    private void ApplyOffAxisProjection(Camera camera)
    {
        if (!offAxisProjection)
        {
            camera.ResetProjectionMatrix();
            camera.ResetWorldToCameraMatrix();
            return;
        }

        if (projectionPlane == null)
            throw new Exception("No projection plane set!");

        camera.projectionMatrix = GetProjectionMatrix(projectionPlane, camera);

        Matrix4x4 M = projectionPlane.M;

        Matrix4x4 relativeRotation =
            Matrix4x4.Rotate(
                Quaternion.Inverse(transform.rotation) *
                projectionPlane.transform.rotation);

        Matrix4x4 cameraTranslation =
            Matrix4x4.Translate(-camera.transform.position);

        camera.worldToCameraMatrix =
            M * relativeRotation * cameraTranslation;
    }

    private static Matrix4x4 GetProjectionMatrix(ProjectionPlane P, Camera cam)
    {
        Vector3 pa = P.BottomLeft;
        Vector3 pb = P.BottomRight;
        Vector3 pc = P.TopLeft;

        Vector3 pe = cam.transform.position;

        Vector3 vr = P.DirRight;
        Vector3 vu = P.DirUp;
        Vector3 vn = P.DirNormal;

        Vector3 va = pa - pe;
        Vector3 vb = pb - pe;
        Vector3 vc = pc - pe;

        float d = -Vector3.Dot(va, vn);

        float n = cam.nearClipPlane;
        float f = cam.farClipPlane;

        float l = Vector3.Dot(vr, va) * n / d;
        float r = Vector3.Dot(vr, vb) * n / d;
        float b = Vector3.Dot(vu, va) * n / d;
        float t = Vector3.Dot(vu, vc) * n / d;

        return Matrix4x4.Frustum(l, r, b, t, n, f);
    }
}
