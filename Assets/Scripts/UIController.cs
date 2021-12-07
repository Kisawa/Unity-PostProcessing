using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PostProcessing;

public class UIController : MonoBehaviour
{
    public Text TxtFPS;

    private void Update()
    {
        if (TxtFPS != null)
            TxtFPS.text = $"FPS: {1.0f / Time.smoothDeltaTime * Time.timeScale}";
    }
}
