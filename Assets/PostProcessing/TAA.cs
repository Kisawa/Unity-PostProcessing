using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class TAA : MonoBehaviour
{
    [Range(0, 10)]
    public float JitterSpread = 0.75f;

    const int k_SampleCount = 8;
    int sampleIndex;
    Vector4 jitterTexelSize;

    Camera cam;
    Material material;

    static int _JitterTexelSize = Shader.PropertyToID("_JitterTexelSize");

    void Awake()
    {
        cam = GetComponent<Camera>();
        material = new Material(Shader.Find("PostProcessing/TAA"));
    }

    void OnEnable()
    {
        cam.depthTextureMode |= DepthTextureMode.Depth;
        cam.depthTextureMode |= DepthTextureMode.MotionVectors;
    }

    void OnDisable()
    {
        cam.depthTextureMode &= ~DepthTextureMode.MotionVectors;
        cam.depthTextureMode &= ~DepthTextureMode.Depth;

        sampleIndex = 0;
        jitterTexelSize = Vector4.zero;
        cam.projectionMatrix = cam.nonJitteredProjectionMatrix;
    }

    private void OnPreCull()
    {
        Vector2 offset = new Vector2(HaltonSeq(2, sampleIndex + 1), HaltonSeq(3, sampleIndex + 1));
        offset = (offset - Vector2.one * 0.5f) * JitterSpread;
        sampleIndex++;
        sampleIndex = sampleIndex >= k_SampleCount ? 0 : sampleIndex;
        Matrix4x4 proj = CalcProjectionMatrix(cam, offset);
        jitterTexelSize.z = jitterTexelSize.x;
        jitterTexelSize.w = jitterTexelSize.y;
        jitterTexelSize.x = offset.x;
        jitterTexelSize.y = offset.y;
        cam.projectionMatrix = proj;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        material.SetVector(_JitterTexelSize, jitterTexelSize);
        Graphics.Blit(source, destination, material);
    }

    static float HaltonSeq(int refer, int index = 1/* NOT! zero-based */)
    {
        float result = 0;
        float fraction = 1;
        int i = index;
        while (i > 0)
        {
            fraction /= refer;
            result += fraction * (i % refer);
            i = (int)Mathf.Floor(i / (float)refer);
        }
        return result;
    }

    static Matrix4x4 CalcProjectionMatrix(Camera camera, Vector2 texelOffset)
    {
        Matrix4x4 projectionMatrix = new Matrix4x4();
        texelOffset.x /= .5f * camera.pixelWidth;
        texelOffset.y /= .5f * camera.pixelHeight;
        if (camera.orthographic)
        {
            float vertical = camera.orthographicSize;
            float horizontal = vertical * camera.aspect;
            texelOffset.x *= horizontal;
            texelOffset.y *= vertical;
            float right = texelOffset.x + horizontal;
            float left = texelOffset.x - horizontal;
            float top = texelOffset.y + vertical;
            float bottom = texelOffset.y - vertical;

            projectionMatrix.m00 = 2 / (right - left);
            projectionMatrix.m03 = -(right + left) / (right - left);
            projectionMatrix.m11 = 2 / (top - bottom);
            projectionMatrix.m13 = -(top + bottom) / (top - bottom);
            projectionMatrix.m22 = -2 / (camera.farClipPlane - camera.nearClipPlane);
            projectionMatrix.m23 = -(camera.farClipPlane + camera.nearClipPlane) / (camera.farClipPlane - camera.nearClipPlane);
            projectionMatrix.m33 = 1;
        }
        else
        {
            float thfov = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2);
            float frustumDepth = camera.farClipPlane - camera.nearClipPlane;
            float oneOverDepth = 1 / frustumDepth;

            projectionMatrix.m00 = 1 / thfov / camera.aspect;
            projectionMatrix.m11 = 1 / thfov;
            projectionMatrix.m22 = -(camera.farClipPlane + camera.nearClipPlane) * oneOverDepth;
            projectionMatrix.m23 = -2 * camera.nearClipPlane * camera.farClipPlane * oneOverDepth;
            projectionMatrix.m32 = -1;
            projectionMatrix.m02 = texelOffset.x;
            projectionMatrix.m12 = texelOffset.y;
        }
        return projectionMatrix;
    }
}
