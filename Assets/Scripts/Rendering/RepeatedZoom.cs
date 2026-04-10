using UnityEngine;

public class RepeatedZoom : MonoBehaviour
{
    public Material repeatedZoom;

    [Header("Zoom")]
    public float zoom;
    public float targetZoom;
    public float zoomSmoothTime = 0.2f;
    private float zoomVelocity;

    [Header("Offset X")]
    public float x;
    public float targetX;
    public float xSmoothTime = 0.15f;
    private float xVelocity;

    [Header("Offset Y")]
    public float y;
    public float targetY;
    public float ySmoothTime = 0.15f;
    private float yVelocity;

    [Header("Rotation")]
    public float z;
    public float targetZ;
    public float zSmoothTime = 0.15f;
    private float zVelocity;

    private static readonly int ZoomId = Shader.PropertyToID("_Zoom");
    private static readonly int OffsetXId = Shader.PropertyToID("_OffsetX");
    private static readonly int OffsetYId = Shader.PropertyToID("_OffsetY");
    private static readonly int RotationId = Shader.PropertyToID("_Rotation");

    private void Update()
    {
        zoom = Mathf.SmoothDamp(zoom, targetZoom, ref zoomVelocity, zoomSmoothTime);
        x = Mathf.SmoothDamp(x, targetX, ref xVelocity, xSmoothTime);
        y = Mathf.SmoothDamp(y, targetY, ref yVelocity, ySmoothTime);
        z = Mathf.SmoothDampAngle(z, targetZ, ref zVelocity, zSmoothTime);

        if (repeatedZoom == null) return;

        repeatedZoom.SetFloat(ZoomId, zoom);
        repeatedZoom.SetFloat(OffsetXId, x);
        repeatedZoom.SetFloat(OffsetYId, y);
        repeatedZoom.SetFloat(RotationId, z);
    }
}