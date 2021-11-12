using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class test : MonoBehaviour
{
    public bool RandomNoise;
    [Range(1, 32)]
    public int Count = 4;
    [Range(0.1f, 5)]
    public float Radius = 1;

    Transform trans;

    public Transform point0;
    public Transform point1;
    public Transform point2;
    public Transform plane0;
    public Transform plane1;

    private void Awake()
    {
        trans = transform;
    }
    public float num0 = 2;
    public float num1 = 3;
    private void OnDrawGizmos()
    {
        Vector3 start = trans.position;
        Gizmos.color = Color.red;
        if (point0 == null || point1 == null)
            return;
        Vector3 dir0 = Vector3.Normalize(point0.position - trans.position);
        Vector3 dir1 = Vector3.Normalize(point1.position - trans.position);
        Gizmos.DrawLine(start, start + dir0);
        Gizmos.DrawLine(start, start + dir1);
        Vector3 res = dir0 - Vector3.up * Vector3.Dot(dir0, Vector3.up);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(start, start + res * Radius);
        if (point2 != null)
            point2.position = start + res;
        if (plane0 != null)
            plane0.up = dir0;
        if (plane1 != null)
            plane1.up = dir1;

        //for (int i = 0; i < Count; i++)
        //{
        //    Vector3 randomDir = UniformSampleDisk(GetRandom3((uint)i)) * Radius;
        //    Gizmos.DrawLine(start, start + randomDir);
        //}
    }

    [Tooltip("The diameter (in texels) inside which jitter samples are spread. Smaller values result in crisper but more aliased output, while larger values result in more stable but blurrier output.")]
    [Range(0f, 1f)]
    public float JitterSpread = 0.75f;

    int sampleIndex;
    const int k_SampleCount = 8;
    Vector2 j;

    void func()
    {
        j = GenerateRandomOffset() * JitterSpread;
        j = new Vector2(j.x / Camera.main.pixelWidth, j.y / Camera.main.pixelHeight);
        //Debug.LogError(j.x + "   " + j.y);
    }

    public Matrix4x4 GetJitteredProjectionMatrix(Camera camera, ref Vector2 jitter)
    {
        Matrix4x4 cameraProj;
        jitter = GenerateRandomOffset();
        jitter *= JitterSpread;
        cameraProj = camera.orthographic
            ? GetJitteredOrthographicProjectionMatrix(camera, jitter)
            : GetJitteredPerspectiveProjectionMatrix(camera, jitter);
        jitter = new Vector2(jitter.x / camera.pixelWidth, jitter.y / camera.pixelHeight);
        return cameraProj;
    }

    public void ConfigureJitteredProjectionMatrix(Camera camera, ref Vector2 jitter)
    {
        camera.nonJitteredProjectionMatrix = camera.projectionMatrix;
        camera.projectionMatrix = GetJitteredProjectionMatrix(camera, ref jitter);
        camera.useJitteredProjectionMatrixForTransparentRendering = false;
    }

    Vector2 GenerateRandomOffset()
    {
        var offset = new Vector2(
                HaltonSeq((sampleIndex & 1023) + 1, 2) - 0.5f,
                HaltonSeq((sampleIndex & 1023) + 1, 3) - 0.5f
            );

        if (++sampleIndex >= k_SampleCount)
            sampleIndex = 0;

        return offset;
    }

    private void OnDisable()
    {

    }

    private static float HaltonSeq(int refer, int index = 1/* NOT! zero-based */)
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

    Vector3 GetRandom3(uint i)
    {
        float g = 1.32471795724474602596090885447809f;
        return new Vector3((i * 1 / g + 0.5f) % 1, (i * 1 / g / g + 0.5f) % 1, (i * 1 / g / g / g + 0.5f) % 1);
    }

    Vector3 UniformSampleDisk(Vector3 random)
    {
        float Theta = 2.0f * Mathf.PI * random.x;
        float Radius = Mathf.Sqrt(random.y);
        return new Vector3(Radius * Mathf.Cos(Theta), 0, Mathf.Sqrt(1 - random.y) * random.z);
    }

    float GTAO_Noise(Vector2 position)
    {
        return frac(52.9829189f * frac(Vector2.Dot(position, new Vector2(0.06711056f, 0.00583715f))));
    }

    Vector2 Rotate(Vector2 vec, float rotation)
    {
        return new Vector2(vec.x * Mathf.Cos(rotation) - vec.y * Mathf.Sin(rotation),
                            vec.x * Mathf.Sin(rotation) + vec.y * Mathf.Cos(rotation));
    }

    Vector2 Rotate(Vector2 vec, Vector2 rotation)
    {
        return new Vector2(vec.x * Mathf.Cos(rotation.x) - vec.y * Mathf.Sin(rotation.y),
                            vec.x * Mathf.Sin(rotation.y) + vec.y * Mathf.Cos(rotation.x));
    }

    Vector2 GetRandom2(Vector2 uv)
    {
        float x, y;
        if (RandomNoise)
        {
            x = Mathf.Clamp01(frac(Mathf.Sin(Vector2.Dot(uv, new Vector2(12.9898f, 78.233f))) * 43758.5453f)) * 2f - 1f;
            y = Mathf.Clamp01(frac(Mathf.Sin(Vector2.Dot(uv, new Vector2(12.9898f, 78.233f) * 2)) * 43758.5453f)) * 2f - 1f;
        }
        else
        {
            x = (frac(1f - uv.x * Screen.width * 0.5f) * 0.25f + frac(uv.y * Screen.height * 0.5f) * 0.75f) * 2f - 1f;
            y = (frac(1f - uv.x * Screen.width * 0.5f) * 0.75f + frac(uv.y * Screen.height * 0.5f) * 0.25f) * 2f - 1f;
        }
        return new Vector2(x, y);
    }

    float frac(float num)
    {
        return num % 1;
    }

    private void OnEnable()
    {
        //Vector2 offset = new Vector2(HaltonSeq(2, sampleIndex + 1), HaltonSeq(3, sampleIndex + 1));
        //offset -= Vector2.one * 0.5f;
        //sampleIndex++;
        //sampleIndex = sampleIndex >= k_SampleCount ? 0 : sampleIndex;
        //Debug.LogError(Camera.main.projectionMatrix);
        //Debug.LogError(CalcProjectionMatrix(Camera.main, offset));
        //float vertical = Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad / 2);
        //float horizontal = vertical * Camera.main.aspect;
        //float left = ((offset.x * horizontal / (0.5f * Camera.main.pixelWidth)) - horizontal) * Camera.main.nearClipPlane;
        //float right = ((offset.x * horizontal / (0.5f * Camera.main.pixelWidth)) + horizontal) * Camera.main.nearClipPlane;
        //float top = ((offset.y * vertical / (0.5f * Camera.main.pixelHeight)) + vertical) * Camera.main.nearClipPlane;
        //float bottom = ((offset.y * vertical / (0.5f * Camera.main.pixelHeight)) - vertical) * Camera.main.nearClipPlane;
        //Debug.LogError(GetPerspectiveProjection(left, right, bottom, top, Camera.main.nearClipPlane, Camera.main.farClipPlane));
        //Debug.LogError(GetJitteredPerspectiveProjectionMatrix(Camera.main, offset));
    }

    public static Matrix4x4 GetJitteredPerspectiveProjectionMatrix(Camera camera, Vector2 offset)
    {
        float near = camera.nearClipPlane;
        float far = camera.farClipPlane;

        float vertical = Mathf.Tan(0.5f * Mathf.Deg2Rad * camera.fieldOfView) * near;
        float horizontal = vertical * camera.aspect;

        offset.x *= horizontal / (0.5f * camera.pixelWidth);
        offset.y *= vertical / (0.5f * camera.pixelHeight);

        var matrix = camera.projectionMatrix;

        matrix[0, 2] += offset.x / horizontal;
        matrix[1, 2] += offset.y / vertical;

        return matrix;
    }

    public static Matrix4x4 GetJitteredOrthographicProjectionMatrix(Camera camera, Vector2 offset)
    {
        float vertical = camera.orthographicSize;
        float horizontal = vertical * camera.aspect;

        offset.x *= horizontal / (0.5f * camera.pixelWidth);
        offset.y *= vertical / (0.5f * camera.pixelHeight);

        float left = offset.x - horizontal;
        float right = offset.x + horizontal;
        float top = offset.y + vertical;
        float bottom = offset.y - vertical;

        return Matrix4x4.Ortho(left, right, bottom, top, camera.nearClipPlane, camera.farClipPlane);
    }

    public static Matrix4x4 GetOrthographicProjection(float left, float right, float bottom, float top, float near, float far)
    {
        float x = 2.0f / (right - left);
        float y = 2.0f / (top - bottom);
        float z = -2.0f / (far - near);
        float a = -(right + left) / (right - left);
        float b = -(top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = 1.0f;

        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x; m[0, 1] = 0; m[0, 2] = 0; m[0, 3] = a;
        m[1, 0] = 0; m[1, 1] = y; m[1, 2] = 0; m[1, 3] = b;
        m[2, 0] = 0; m[2, 1] = 0; m[2, 2] = z; m[2, 3] = c;
        m[3, 0] = 0; m[3, 1] = 0; m[3, 2] = 0; m[3, 3] = d;
        return m;
    }

    public static Matrix4x4 GetPerspectiveProjection(float left, float right, float bottom, float top, float near, float far)
    {
        float x = (2.0f * near) / (right - left);
        float y = (2.0f * near) / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0f * far * near) / (far - near);
        float e = -1.0f;

        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x; m[0, 1] = 0; m[0, 2] = a; m[0, 3] = 0;
        m[1, 0] = 0; m[1, 1] = y; m[1, 2] = b; m[1, 3] = 0;
        m[2, 0] = 0; m[2, 1] = 0; m[2, 2] = c; m[2, 3] = d;
        m[3, 0] = 0; m[3, 1] = 0; m[3, 2] = e; m[3, 3] = 0;
        return m;
    }

    Matrix4x4 CalcProjectionMatrix(Camera cam, Vector2 texelOffset)
    {
        Matrix4x4 projectionMatrix = new Matrix4x4();
        texelOffset.x /= .5f * cam.pixelWidth;
        texelOffset.y /= .5f * cam.pixelHeight;
        if (cam.orthographic)
        {
            float vertical = cam.orthographicSize;
            float horizontal = vertical * cam.aspect;
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
            projectionMatrix.m22 = -2 / (cam.farClipPlane - cam.nearClipPlane);
            projectionMatrix.m23 = -(cam.farClipPlane + cam.nearClipPlane) / (cam.farClipPlane - cam.nearClipPlane);
            projectionMatrix.m33 = 1;
        }
        else
        {
            float thfov = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad / 2);
            float frustumDepth = cam.farClipPlane - cam.nearClipPlane;
            float oneOverDepth = 1 / frustumDepth;

            projectionMatrix.m00 = 1 / thfov / cam.aspect;
            projectionMatrix.m11 = 1 / thfov;
            projectionMatrix.m22 = -(cam.farClipPlane + cam.nearClipPlane) * oneOverDepth;
            projectionMatrix.m23 = -2 * cam.nearClipPlane * cam.farClipPlane * oneOverDepth;
            projectionMatrix.m32 = -1;
            projectionMatrix.m02 = texelOffset.x;
            projectionMatrix.m12 = texelOffset.y;
        }
        return projectionMatrix;
    }
}