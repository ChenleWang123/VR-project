using UnityEngine;
using System;
using Apt.Unity.Projection;

public class StereoCameraController : MonoBehaviour
{
    public Camera leftCamera;
    public Camera rightCamera;

    public float ipd = 0.064f;

    public bool toe_in = false;
    public Transform convergencePoint;

    public ProjectionPlane projectionPlane;
    public bool offAxisProjection = false;

    void Update()
    {
        // 1. 设置左右眼位置（IPD）
        leftCamera.transform.localPosition  = Vector3.left  * ipd / 2f;
        rightCamera.transform.localPosition = Vector3.right * ipd / 2f;

        // 2. Toe-in 或平行
        if (toe_in)
        {
            leftCamera.transform.LookAt(convergencePoint);
            rightCamera.transform.LookAt(convergencePoint);
        }
        else
        {
            leftCamera.transform.rotation  = Quaternion.identity;
            rightCamera.transform.rotation = Quaternion.identity;
        }

        // 3. Off-axis projection
        ApplyOffAxisProjection(leftCamera);
        ApplyOffAxisProjection(rightCamera);
    }

    private void ApplyOffAxisProjection(Camera camera)
    {
        if (!offAxisProjection)
        {
            camera.ResetProjectionMatrix();
            camera.ResetWorldToCameraMatrix();
            return;
        }

        if (!projectionPlane)
            throw new Exception("No projection plane set!");

        camera.projectionMatrix = GetProjectionMatrix(projectionPlane, camera);

        // Eye translation
        var M = projectionPlane.M;   // special matrix provided by ProjectionPlane.cs

        var relativeRotation =
            Matrix4x4.Rotate(
                Quaternion.Inverse(transform.rotation) *
                projectionPlane.transform.rotation);

        var cameraTranslation =
            Matrix4x4.Translate(-camera.transform.position);

        camera.worldToCameraMatrix =
            M * relativeRotation * cameraTranslation;
    }

    private static Matrix4x4 GetProjectionMatrix(ProjectionPlane P, Camera cam)
    {
        // Screen corners in world space
        Vector3 pa = P.BottomLeft;
        Vector3 pb = P.BottomRight;
        Vector3 pc = P.TopLeft;

        // Camera center
        Vector3 pe = cam.transform.position;

        // Screen edges
        Vector3 vr = P.DirRight;   // right direction of screen
        Vector3 vu = P.DirUp;      // up direction of screen
        Vector3 vn = P.DirNormal;  // screen normal (pointing towards camera)

        // Distances from eye to screen corners
        Vector3 va = pa - pe;
        Vector3 vb = pb - pe;
        Vector3 vc = pc - pe;

        // Distance from eye to screen (along normal)
        float d = -Vector3.Dot(va, vn);

        float n = cam.nearClipPlane;
        float f = cam.farClipPlane;

        // Left, right, top, bottom on near plane
        float l = Vector3.Dot(vr, va) * n / d;
        float r = Vector3.Dot(vr, vb) * n / d;
        float b = Vector3.Dot(vu, va) * n / d;
        float t = Vector3.Dot(vu, vc) * n / d;

        // Build projection matrix
        return Matrix4x4.Frustum(l, r, b, t, n, f);
    }
}
