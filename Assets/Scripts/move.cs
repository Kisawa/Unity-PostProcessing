using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class move : MonoBehaviour
{
    [Range(1, 10)]
    public float Length = 1;
    [Range(.1f, 3)]
    public float Speed = 1;
    Transform trans;
    Vector3 pos0;
    Vector3 pos1;
    Vector3 pos;

    private void Awake()
    {
        trans = transform;
        pos0 = trans.position + trans.right * Length;
        pos1 = trans.position - trans.right * Length;
        pos = pos0;
    }

    private void Update()
    {
        trans.position = Vector3.MoveTowards(trans.position, pos, Speed * Time.deltaTime);
        if (Vector3.Distance(trans.position, pos0) == 0)
            pos = pos1;
        else if (Vector3.Distance(trans.position, pos1) == 0)
            pos = pos0;
    }
}