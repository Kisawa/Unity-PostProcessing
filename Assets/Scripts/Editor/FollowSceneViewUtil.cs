using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class FollowSceneViewUtil : EditorWindow
{
    [MenuItem("Tools/FollowSceneViewUtil")]
    static void Open()
    {
        GetWindow<FollowSceneViewUtil>("Follow Scene View Util");
    }

    bool res = true;

    private void OnEnable()
    {
        SceneView.duringSceneGui += SceneView_duringSceneGui;
    }

    private void OnGUI()
    {
        res = EditorGUILayout.Toggle("Running", res);
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= SceneView_duringSceneGui;
    }

    private void SceneView_duringSceneGui(SceneView obj)
    {
        Camera gameMainCamera = Camera.main;
        if (!res || gameMainCamera == null)
            return;
        gameMainCamera.transform.position = obj.camera.transform.position;
        gameMainCamera.transform.rotation = obj.camera.transform.rotation;
        obj.Repaint();
    }
}
