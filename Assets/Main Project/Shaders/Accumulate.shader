Shader "Hidden/Accumulate"
{
    Properties
    {
        _MainTex ("Screen (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float _SampleNum;

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

			float4 frag (v2f i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).rgb;
				return float4(col, 1.0 / (1.0 + _SampleNum));
            }
            ENDCG
        }
    }
}
