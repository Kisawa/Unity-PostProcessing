using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityBufferTag : MonoBehaviour
{
    public static List<VelocityBufferTag> list = new List<VelocityBufferTag>();

    public bool IsAvailable => gameObject.activeInHierarchy && enabled && renderer != null && renderer.enabled;

    public Color col;

    MeshFilter meshFilter;
    new Renderer renderer;
    SkinnedMeshRenderer SkinnedRenderer;
    public Mesh Mesh { get; private set; }
    public Matrix4x4 LocalToWorld { get; private set; }
    public Matrix4x4 PrevLocalToWorld { get; set; }

    Transform trans;

    private void Awake()
    {
        trans = transform;
        meshFilter = GetComponent<MeshFilter>();
        renderer = GetComponent<Renderer>();
        SkinnedRenderer = renderer as SkinnedMeshRenderer;
    }

    private void LateUpdate()
    {
        if (meshFilter != null)
        {
            Mesh = meshFilter.mesh;
            LocalToWorld = trans.localToWorldMatrix;
        }
        if (SkinnedRenderer != null)
        {
            if (Mesh == null)
                Mesh = new Mesh();
            else
                Mesh.Clear();
            SkinnedRenderer.BakeMesh(Mesh);
            LocalToWorld = Matrix4x4.TRS(trans.position, trans.rotation, Vector3.one);
        }
    }

    private void OnEnable()
    {
        list.Add(this);
    }

    private void OnDisable()
    {
        list.Remove(this);
    }
}