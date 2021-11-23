using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityBufferTag : MonoBehaviour
{
    public static List<VelocityBufferTag> list = new List<VelocityBufferTag>();

    public bool IsAvailable => gameObject.activeSelf && renderer != null;

    Transform trans;
    new public Renderer renderer { get; private set; }

    private void Awake()
    {
        trans = transform;
        renderer = GetComponent<Renderer>();
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