using UnityEngine;
using System;
using Apt.Unity.Projection;

public class StereoCameraController : MonoBehaviour
{
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

    [Tooltip("Where to apply head rotation offset. Recommended: Main Camera transform (child of CameraRig).")]
    public Transform rotationTarget;

    [Tooltip("Offset applied to rig local position (e.g., Y=1.6 for eye height).")]
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
    [Tooltip("Enable head rotation offset.")]
    public bool applyHeadRotation = true;

    [Tooltip("Extra multiplier for rotation strength.")]
    [Range(0f, 2f)]
    public float rotationStrength = 1.0f;

    private Vector3 _lastHeadRigLocalPos;
    private Quaternion _rotationTargetBaseLocalRot;

    private void Start()
    {
        // Cache the base local rotation of the rotation target so we can add head offset on top.
        if (rotationTarget != null)
            _rotationTargetBaseLocalRot = rotationTarget.localRotation;
    }

    private void Update()
    {
        // 0) Update head tracking pose (without touching CameraRig rotation to avoid conflicts).
        UpdateFromHeadTracker();

        // 1) Set left/right eye local positions (IPD).
        if (leftCamera != null)
            leftCamera.transform.localPosition = Vector3.left * ipd / 2f;

        if (rightCamera != null)
            rightCamera.transform.localPosition = Vector3.right * ipd / 2f;

        // 2) Toe-in or parallel.
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

    private void UpdateFromHeadTracker()
    {
        if (headTracker == null)
            return;

        Vector3 h = headTracker.DetectedFace;
        bool hasFace = h != Vector3.zero;

        // Position
        if (!hasFace)
        {
            if (freezeWhenLost)
                transform.localPosition = _lastHeadRigLocalPos;

            // Reset rotation target to base if face lost
            if (rotationTarget != null && applyHeadRotation)
                rotationTarget.localRotation = _rotationTargetBaseLocalRot;

            return;
        }

        if (invertX) h.x = -h.x;
        if (invertY) h.y = -h.y;
        if (invertZ) h.z = -h.z;

        Vector3 rigLocalPos = (h * positionScale) + headOffset;
        transform.localPosition = rigLocalPos;
        _lastHeadRigLocalPos = rigLocalPos;

        // Rotation offset (apply to rotationTarget, not to CameraRig, to avoid "spinning" with MouseLookRig)
        if (rotationTarget != null && applyHeadRotation)
        {
            Quaternion headRot = headTracker.DetectedRotation;

            // Apply strength by scaling Euler angles (simple and stable for small angles)
            Vector3 e = headRot.eulerAngles;
            e.x = NormalizeAngle(e.x) * rotationStrength;
            e.y = NormalizeAngle(e.y) * rotationStrength;
            e.z = 0f;

            Quaternion headOffsetRot = Quaternion.Euler(e);
            rotationTarget.localRotation = _rotationTargetBaseLocalRot * headOffsetRot;
        }
    }

    private static float NormalizeAngle(float a)
    {
        // Convert 0..360 to -180..180
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
