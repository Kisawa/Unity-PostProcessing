using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PostProcessing
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class GTAO : MonoBehaviour
    {
        public static GTAO Self;

        public ViewType ViewType = ViewType.Combine;
        [Range(3, 12)]
        public int SampleDirectionCount = 3;
        [Range(.1f, 5)]
        public float SampleRadius = 1;
        [Range(3, 32)]
        public int SampleStep = 12;
        [Range(.1f, 10)]
        public float AOPower = 2;
        [Range(0, 1)]
        public float AOThickness = 1;
        [Range(1, 5)]
        public float AOCompactness = 2;
        public bool MultiBounce = true;

        [Space(10)]
        [Range(0, 5)]
        public int BlurIterations = 3;
        [Range(.1f, 5)]
        public float BlurSpread = 1.6f;
        [Range(.001f, 5)]
        public float BlurThreshold = .1f;

        Camera cam;
        Material material;

        static int _TexelSize = Shader.PropertyToID("_TexelSize");
        static int _UVToView = Shader.PropertyToID("_UVToView");
        static int _ProjScale = Shader.PropertyToID("_ProjScale");
        static int _ViewType = Shader.PropertyToID("_ViewType");
        static int _SampleDirectionCount = Shader.PropertyToID("_SampleDirectionCount");
        static int _SampleRadius = Shader.PropertyToID("_SampleRadius");
        static int _SampleStep = Shader.PropertyToID("_SampleStep");
        static int _AOPower = Shader.PropertyToID("_AOPower");
        static int _AOThickness = Shader.PropertyToID("_AOThickness");
        static int _AOCompactness = Shader.PropertyToID("_AOCompactness");
        static int _MultiBounce = Shader.PropertyToID("_MultiBounce");
        static int _AOMap = Shader.PropertyToID("_AOMap");

        static int _BlurIterations = Shader.PropertyToID("_BlurIterations");
        static int _BlurSpread = Shader.PropertyToID("_BlurSpread");
        static int _BlurThreshold = Shader.PropertyToID("_BlurThreshold");

        void Awake()
        {
            Self = this;
            cam = GetComponent<Camera>();
            material = new Material(Shader.Find("PostProcessing/GTAO"));
            material.hideFlags = HideFlags.HideAndDontSave;
        }

        void OnEnable()
        {
            cam.depthTextureMode |= DepthTextureMode.DepthNormals;
        }

        void OnDisable()
        {
            cam.depthTextureMode &= ~DepthTextureMode.DepthNormals;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            float tanFov = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f);
            Vector2 invFocalLen = new Vector2(tanFov * cam.aspect, tanFov);
            material.SetVector(_UVToView, invFocalLen);
            float projScale = cam.pixelHeight / 2 / tanFov;
            material.SetFloat(_ProjScale, projScale);
            material.SetInt(_ViewType, (int)ViewType);
            material.SetInt(_SampleDirectionCount, SampleDirectionCount);
            material.SetFloat(_SampleRadius, SampleRadius);
            material.SetInt(_SampleStep, SampleStep);
            material.SetFloat(_AOPower, AOPower);
            material.SetFloat(_AOThickness, AOThickness);
            material.SetFloat(_AOCompactness, AOCompactness);
            material.SetFloat(_MultiBounce, MultiBounce ? 1 : 0);
            int width = source.width;
            int height = source.height;
            material.SetVector(_TexelSize, new Vector4(1f / width, 1f / height, width, height));
            material.SetFloat(_BlurIterations, BlurIterations);
            material.SetFloat(_BlurSpread, BlurSpread);
            material.SetFloat(_BlurThreshold, BlurThreshold);

            RenderTexture AOMap = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RG32);
            Graphics.Blit(source, AOMap, material, 0);

            for (int i = 0; i < BlurIterations; i++)
            {
                RenderTexture blurTex = RenderTexture.GetTemporary(AOMap.descriptor);
                Graphics.Blit(AOMap, blurTex, material, 1);
                RenderTexture.ReleaseTemporary(AOMap);
                AOMap = blurTex;
            }

            material.SetTexture(_AOMap, AOMap);
            Graphics.Blit(source, destination, material, 2);
            RenderTexture.ReleaseTemporary(AOMap);
        }
    }

    public enum ViewType { Origin, AO, Combine }
}