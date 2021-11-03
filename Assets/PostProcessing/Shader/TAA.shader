Shader "PostProcessing/TAA"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
		CGINCLUDE
        #include "UnityCG.cginc"
        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        v2f vert (appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;
		sampler2D _CameraDepthTexture;
		sampler2D _CameraMotionVectorsTexture;
		float4 _JitterTexelSize;

		inline float SampleDepth(float2 uv)
		{
			return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
		}

		inline float3 RGBToYCoCg(float3 RGB)
		{
			const float3x3 mat = float3x3(0.25, 0.5, 0.25, 0.5, 0, -0.5, -0.25, 0.5, -0.25);
			return mul(mat, RGB);
		}

		inline float4 RGBToYCoCg(float4 RGB)
		{
			return float4(RGBToYCoCg(RGB.xyz), RGB.w);
		}

		inline float3 YCoCgToRGB(float3 YCoCg)
		{
			const float3x3 mat = float3x3(1, 1, -1, 1, 0, 1, 1, -1, -1);
			return mul(mat, YCoCg);
		}

		inline float4 YCoCgToRGB(float4 YCoCg)
		{
			return float4(YCoCgToRGB(YCoCg.xyz), YCoCg.w); 
		}

		inline float Luma4(float3 Color)
		{
			return Color.g * 2 + Color.r + Color.b;
		}

		inline float HdrWeight4(float3 Color, const float Exposure) 
		{
			return rcp(Luma4(Color) * Exposure + 4);
		}

		static const int2 _Offset[8] = { int2(-1, 1), int2(0, 1), int2(-1, 1), int2(-1, 0), int2(1, 0), int2(-1, -1), int2(0, -1), int2(1, -1) };

		#if defined(UNITY_REVERSED_Z)
			#define COMPARE_DEPTH(a, b) step(b, a)
		#else
			#define COMPARE_DEPTH(a, b) step(a, b)
		#endif

		void SelectNearestDepthUV(float2 uv, out float nearestDepth, out float2 nearestUV)
		{
			float3 res = float3(0, 0, SampleDepth(uv));
			[unroll]
			for(int i = 0; i < 8; i++)
			{
				float3 _res = float3(_Offset[i], SampleDepth(uv + _Offset[i] * _MainTex_TexelSize.xy));
				res = lerp(res, _res, COMPARE_DEPTH(_res.z, res.z));
			}
			nearestDepth = res.z;
			nearestUV = uv + res.xy * _MainTex_TexelSize.xy;
		}

        fixed4 frag_TAA (v2f i) : SV_Target
        {
			float2 uv = i.uv - _JitterTexelSize.xy * _MainTex_TexelSize.xy;
			float nearestDepth;
			float2 nearestUV;
			SelectNearestDepthUV(i.uv, nearestDepth, nearestUV);
			float2 velocity = tex2D(_CameraMotionVectorsTexture, nearestUV).xy;

            fixed4 col = tex2D(_MainTex, uv);
            return col;
        }
		ENDCG

        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_TAA
            ENDCG
        }
    }
}
