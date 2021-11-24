Shader "PostProcessing/MotionVectors"
{
    SubShader
    {
		CGINCLUDE
		#include "UnityCG.cginc"

		struct appdata
		{
			float4 vertex : POSITION;
		};

		struct v2f
		{
			float4 pos : SV_POSITION;
			float4 currentPos: TEXCOORD0;
			float4 prevPos : TEXCOORD1;
		};

		float4x4 _Prev_VP;

		v2f vert (appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
			o.currentPos = ComputeScreenPos(o.pos);
			o.prevPos = ComputeScreenPos(mul(_Prev_VP, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0))));
            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
            float2 currentSS = i.currentPos.xy / i.currentPos.w;
			float2 prevSS = i.prevPos.xy / i.prevPos.w;
            return float4((currentSS - prevSS) * 0.5, 0, 0);
        }
		ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}