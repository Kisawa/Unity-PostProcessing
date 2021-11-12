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
		float2 _AABBScale;
		float2 _Blend;
		float _Sharpness;

		inline float SampleDepth(float2 uv)
		{
			return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
		}

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

		inline float Luma4(float3 Color)
		{
			return Color.g * 2 + Color.r + Color.b;
		}

		inline float HdrWeight4(float3 Color, const float Exposure) 
		{
			return rcp(Luma4(Color) * Exposure + 4);
		}

		float3 ClipToAABB(float3 color, float3 minimum, float3 maximum)
		{
			// Note: only clips towards aabb center (but fast!)
			float3 center = 0.5 * (maximum + minimum);
			float3 extents = 0.5 * (maximum - minimum);

			// This is actually `distance`, however the keyword is reserved
			float3 offset = color.rgb - center;

			float3 ts = abs(extents / (offset + 0.0001));
			float t = saturate(min(ts.x, min(ts.y, ts.z)));
			color.rgb = center + offset * t;
			return color;
		}

		float4 clip_aabb(float3 aabb_min, float3 aabb_max, float4 p, float4 q)
		{
			// note: only clips towards aabb center (but fast!)
			float3 p_clip = 0.5 * (aabb_max + aabb_min);
			float3 e_clip = 0.5 * (aabb_max - aabb_min) + 0.00000001;

			float4 v_clip = q - float4(p_clip, p.w);
			float3 v_unit = v_clip.xyz / e_clip;
			float3 a_unit = abs(v_unit);
			float ma_unit = max(a_unit.x, max(a_unit.y, a_unit.z));

			if (ma_unit > 1.0)
				return float4(p_clip, p.w) + v_clip / ma_unit;
			else
				return q;// point inside aabb
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

        fixed4 frag_TAA (v2f i) : SV_Target
        {
			float2 uv = i.uv - _JitterTexelOffset.xy * _MainTex_TexelSize.xy;
			float nearestDepth;
			float2 nearestUV;
			SelectNearestDepthUV(i.uv, nearestDepth, nearestUV);
			float2 velocity = tex2D(_CameraMotionVectorsTexture, nearestUV).xy;

			float4 topLeft = tex2D(_MainTex, uv + _MainTex_TexelSize * int2(-1, 1));
			float4 topCenter = tex2D(_MainTex, uv + _MainTex_TexelSize * int2(0, 1));
			float4 topRight = tex2D(_MainTex, uv + _MainTex_TexelSize * int2(1, 1));
			float4 left = tex2D(_MainTex, uv + _MainTex_TexelSize * int2(-1, 0));
			float4 col = tex2D(_MainTex, uv);
			float4 right = tex2D(_MainTex, uv + _MainTex_TexelSize * int2(1, 0));
			float4 bottomLeft = tex2D(_MainTex, uv + _MainTex_TexelSize * int2(-1, -1));
			float4 bottomCenter = tex2D(_MainTex, uv + _MainTex_TexelSize * int2(0, -1));
			float4 bottomRight = tex2D(_MainTex, uv + _MainTex_TexelSize * int2(1, -1));
			float topLeftWeight = HdrWeight4(topLeft, 10);
			float topCenterWeight = HdrWeight4(topCenter, 10);
			float topRightWeight = HdrWeight4(topRight, 10);
			float leftWeight = HdrWeight4(left, 10);
			float colWeight = HdrWeight4(col, 10);
			float rightWeight = HdrWeight4(right, 10);
			float bottomLeftWeight = HdrWeight4(bottomLeft, 10);
			float bottomCenterWeight = HdrWeight4(bottomCenter, 10);
			float bottomRightWeight = HdrWeight4(bottomRight, 10);
			topLeft = RGBToYCoCg(topLeft);
			topCenter = RGBToYCoCg(topCenter);
			topRight = RGBToYCoCg(topRight);
			left = RGBToYCoCg(left);
			col = RGBToYCoCg(col);
			right = RGBToYCoCg(right);
			bottomLeft = RGBToYCoCg(bottomLeft);
			bottomCenter = RGBToYCoCg(bottomCenter);
			bottomRight = RGBToYCoCg(bottomRight);

			float totalWeight = topLeftWeight + topCenterWeight + topRightWeight + leftWeight + colWeight + rightWeight + bottomLeftWeight + bottomCenterWeight + bottomRightWeight;
			float4 filtered = (topLeft * topLeftWeight + topCenter * topCenterWeight + topRight * topRightWeight + left * leftWeight + col * colWeight + right * rightWeight + bottomLeft * bottomLeftWeight + bottomCenter * bottomCenterWeight + bottomRight * bottomRightWeight) / totalWeight;

			float velocityWeight = saturate(length(velocity) * 3000);
			float AABBScale = lerp(_AABBScale.x, _AABBScale.y, velocityWeight);

			float4 mean = (topLeft + topCenter + topRight + left + col + right + bottomLeft + bottomCenter + bottomRight) / 9;
			float4 stddev = sqrt((pow2(topLeft) + pow2(topCenter) + pow2(topRight) + pow2(left) + pow2(col) + pow2(right) + pow2(bottomLeft) + pow2(bottomCenter) + pow2(bottomRight)) / 9 - pow2(mean));

			float4 minCol = mean - AABBScale * stddev;
			float4 maxCol = mean + AABBScale * stddev;
			minCol = min(minCol, filtered);
			maxCol = max(maxCol, filtered);

			col = YCoCgToRGB(col);
			float2 prevUV = i.uv - velocity + (_JitterTexelOffset.zw - _JitterTexelOffset.xy) * _MainTex_TexelSize.xy;
			float4 prevCol = tex2D(_PrevTex, prevUV);
			prevCol.rgb = YCoCgToRGB(clip_aabb(minCol, maxCol, clamp(mean, minCol, maxCol), RGBToYCoCg(prevCol)));
			float historyWeight = lerp(_Blend.x, _Blend.y, velocityWeight);
			col = lerp(col, prevCol, historyWeight);
			
			return max(0, col);
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
