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
		sampler2D _PrevTex;
		float4 _JitterTexelOffset;
		float2 _Blend;

		inline float4 pow2(float4 i)
		{
			return i * i;
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

		static const float A = 0.15;
		static const float B = 0.50;
		static const float C = 0.10;
		static const float D = 0.20;
		static const float E = 0.02;
		static const float F = 0.30;
		static const float W = 11.2;

		inline float3 Uncharted2Tonemap(float3 x)
		{
		   return ((x*(A*x+C*B)+D*E)/(x*(A*x+B)+D*F))-E/F;
		}

		inline float4 Tonemapping(float4 col)
		{
			float3 _col = col.rgb;
			_col *= 16;
			const float ExposureBias = 2.0;
			_col = Uncharted2Tonemap(ExposureBias * _col);
			float3 whiteScale = 1 / Uncharted2Tonemap(W);
			_col *= whiteScale;
			_col = pow(_col, 0.454545);
			return float4(_col, col.a);
		}

		inline float SampleDepth(float2 uv)
		{
			return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
		}

		inline float4 SampleYCoCg(float2 uv)
		{
			float4 col = tex2D(_MainTex, uv);
			//col = Tonemapping(col);
			return RGBToYCoCg(col);
		}

		static const int2 _Offset[8] = { int2(-1, 1), int2(0, 1), int2(1, 1), int2(-1, 0), int2(1, 0), int2(-1, -1), int2(0, -1), int2(1, -1) };

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

		float3 Clip_AABB(float3 center, float3 extent, float3 col)
		{
			float3 v_clip = col - center;
			float3 v_unit = v_clip / extent;
			float3 a_unit = abs(v_unit);
			float ma_unit = max(a_unit.x, max(a_unit.y, a_unit.z));
			[flatten]
			if (ma_unit > 1.0)
				return center + v_clip / ma_unit;
			else
				return col;
		}

		fixed4 frag_TAA (v2f i) : SV_Target
        {
			float nearestDepth;
			float2 nearestUV;
			SelectNearestDepthUV(i.uv, nearestDepth, nearestUV);
			float2 velocity = tex2D(_CameraMotionVectorsTexture, nearestUV).xy;
			float2 uv = i.uv - _JitterTexelOffset.xy * _MainTex_TexelSize.xy;
			float2 prevUV = i.uv - velocity + (_JitterTexelOffset.zw - _JitterTexelOffset.xy) * _MainTex_TexelSize.xy;

			float4 topLeft = SampleYCoCg(uv + _MainTex_TexelSize * int2(-1, 1));
			float4 topCenter = SampleYCoCg(uv + _MainTex_TexelSize * int2(0, 1));
			float4 topRight = SampleYCoCg(uv + _MainTex_TexelSize * int2(1, 1));
			float4 left = SampleYCoCg(uv + _MainTex_TexelSize * int2(-1, 0));
			float4 col = SampleYCoCg(uv);
			float4 right = SampleYCoCg(uv + _MainTex_TexelSize * int2(1, 0));
			float4 bottomLeft = SampleYCoCg(uv + _MainTex_TexelSize * int2(-1, -1));
			float4 bottomCenter = SampleYCoCg(uv + _MainTex_TexelSize * int2(0, -1));
			float4 bottomRight = SampleYCoCg(uv + _MainTex_TexelSize * int2(1, -1));
			
			float4 mean = (topLeft + topCenter + topRight + left + col + right + bottomLeft + bottomCenter + bottomRight) / 9;
			float4 stddev = sqrt((pow2(topLeft) + pow2(topCenter) + pow2(topRight) + pow2(left) + pow2(col) + pow2(right) + pow2(bottomLeft) + pow2(bottomCenter) + pow2(bottomRight)) / 9 - pow2(mean));

			float4 prevCol = RGBToYCoCg(tex2D(_PrevTex, prevUV));
			prevCol.xyz = Clip_AABB(mean, stddev, prevCol);

			float DIF = 1 - abs(col.r - prevCol.r) / max(col.r, max(prevCol.r, 0.1));
			float feedback = lerp(_Blend.x, _Blend.y, pow2(DIF));
			float4 color_temporal = YCoCgToRGB(lerp(col, prevCol, feedback));
			return color_temporal;
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
