using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityBufferTag : MonoBehaviour
{
    public static List<VelocityBufferTag> list = new List<VelocityBufferTag>();

    public bool IsAvailable => gameObject.activeInHierarchy && enabled && renderer != null && renderer.enabled;

    public Color col;

    new public Renderer renderer { get; private set; }
    public Matrix4x4 PrevLocalToWorld { get; private set; }

    Transform trans;
    bool isAvailable;

    private void Awake()
    {
        trans = transform;
        renderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        check();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_Color", col);
        renderer.SetPropertyBlock(block);
    }

    private void OnEnable()
    {
        list.Add(this);
        check();
    }

    private void OnDisable()
    {
        list.Remove(this);
        check();
    }

    void check()
    {
        if (TAA.Self == null || isAvailable == IsAvailable)
            return;
        isAvailable = IsAvailable;
        TAA.Self.RefreshVelocityBuffer();
    }
}