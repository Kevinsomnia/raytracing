// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/*
Kino/Bloom v2 - Bloom filter for Unity

Copyright (C) 2015, 2016 Keijiro Takahashi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

Shader "Hidden/Kino/Bloom" {
	Properties {
		_MainTex ("Screen (RGB)", 2D) = "" {}
		_BaseTex ("Bloom", 2D) = "" {}
	}
	
	CGINCLUDE
	
	#include "UnityCG.cginc"
			
	sampler2D _MainTex;
	sampler2D _BaseTex;
	float2 _MainTex_TexelSize;
	float2 _BaseTex_TexelSize;
	
	float _PrefilterOffs;
	half _Threshold;
	half _Cutoff;
	half _Intensity;
	
	//Brightness function
	float4 Brightness(float4 c) {
		return c.r * 0.299 + c.g * 0.587 + c.b * 0.114;
	}
	
	float4 Median(float4 a, float4 b, float4 c) {
		return a + b + c - min(min(a, b), c) - max(max(a, b), c);
	}
	
	float4 DownsampleFilter(float2 uv) {
		float4 d = _MainTex_TexelSize.xyxy * float4(-1, -1, 1, 1);
		
		float4 s = tex2D(_MainTex, uv + d.xy);
		s += tex2D(_MainTex, uv + d.zy);
		s += tex2D(_MainTex, uv + d.xw);
		s += tex2D(_MainTex, uv + d.zw);
		
		return s / 4.0;
	}
	
	#if ANTI_FLICKER
	
	//downsample with a 4x4 box filter + anti-flicker filter
	float4 DownsampleAntiFlickerFilter(float2 uv) {
		float4 d = _MainTex_TexelSize.xyxy * float4(-1, -1, 1, 1);
		
		float4 s1 = tex2D(_MainTex, uv + d.xy);
		float4 s2 = tex2D(_MainTex, uv + d.zy);
		float4 s3 = tex2D(_MainTex, uv + d.xw);
		float4 s4 = tex2D(_MainTex, uv + d.zw);
		
		// Karis' luma weighted average (using brightness instead of luma)
		float s1w = 1.0 / (Brightness(s1) + 1.0);
		float s2w = 1.0 / (Brightness(s2) + 1.0);
		float s3w = 1.0 / (Brightness(s3) + 1.0);
		float s4w = 1.0 / (Brightness(s4) + 1.0);
		
		return (s1 * s1w + s2 * s2w + s3 * s3w + s4 * s4w) / (s1w + s2w + s3w + s4w);
	}
	
	#endif
	
	float4 UpsampleFilter(float2 uv) {
	#if HIGH_QUALITY
		// 9-tap bilinear upsample (tent filter)
		float4 d = _MainTex_TexelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0);
		
		float4 s = tex2D(_MainTex, uv - d.xy);
		s += tex2D(_MainTex, uv - d.wy) * 2;
		s += tex2D(_MainTex, uv - d.zy);
		
		s += tex2D(_MainTex, uv + d.zw) * 2;
		s += tex2D(_MainTex, uv) * 4;
		s += tex2D(_MainTex, uv + d.xw) * 2;
		
		s += tex2D(_MainTex, uv + d.zy);
		s += tex2D(_MainTex, uv + d.wy) * 2;
		s += tex2D(_MainTex, uv + d.xy);
		
		return s * 0.0625;
	#else
		// 4-tap bilinear upsample
		float4 d = _MainTex_TexelSize.xyxy * float4(-0.5, -0.5, 0.5, 0.5);
	
		float4 s = tex2D(_MainTex, uv + d.xy);
		s += tex2D(_MainTex, uv + d.zy);
		s += tex2D(_MainTex, uv + d.xw);
		s += tex2D(_MainTex, uv + d.zw);
		
		return s * 0.25;
	#endif
	}
	
	// VERTEX SHADER
	struct v2f_multitex {
		float4 pos : SV_POSITION;
		float2 uvMain : TEXCOORD0;
		float2 uvBase : TEXCOORD1;
	};
	
	v2f_multitex vert_multitex(appdata_full v) {
		v2f_multitex o;
		
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uvMain = v.texcoord.xy;
		o.uvBase = v.texcoord.xy;
		
	#if UNITY_UV_STARTS_AT_TOP
		if(_BaseTex_TexelSize.y < 0.0) {
			o.uvBase.y = 1.0 - v.texcoord.y;
		}
	#endif
		
		return o;
	}
	
	// FRAGMENT SHADER
	
	float4 frag_prefilter(v2f_img i) : SV_Target {
		float2 uv = i.uv + (_MainTex_TexelSize.xy * _PrefilterOffs);
		
	#if ANTI_FLICKER
		float3 d = _MainTex_TexelSize.xyx * float3(1, 1, 0);
		float4 s0 = tex2D(_MainTex, uv);
		float4 s1 = tex2D(_MainTex, uv - d.xz);
		float4 s2 = tex2D(_MainTex, uv + d.xz);
		float4 s3 = tex2D(_MainTex, uv - d.zy);
		float4 s4 = tex2D(_MainTex, uv + d.zy);
		
		s0 = Median(Median(s0, s1, s2), s3, s4);
	#else
		float4 s0 = tex2D(_MainTex, uv);
	#endif
		
		s0 *= saturate((Brightness(s0) - _Threshold) / _Cutoff);
		
		return s0;
	}
	
	float4 frag_downsample1(v2f_img i) : SV_Target {
	#if ANTI_FLICKER
		return DownsampleAntiFlickerFilter(i.uv);
	#else	
		return DownsampleFilter(i.uv);
	#endif
	}
	
	float4 frag_downsample2(v2f_img i) : SV_Target {
		return DownsampleFilter(i.uv);
	}
	
	float4 frag_upsample(v2f_multitex i) : SV_Target {
		float4 base = tex2D(_BaseTex, i.uvBase);
		base += UpsampleFilter(i.uvMain);
		
		return base;
	}
	
	float4 frag_upsample_final(v2f_multitex i) : SV_Target {
		float4 base = tex2D(_BaseTex, i.uvBase);
		base += UpsampleFilter(i.uvMain) * _Intensity;
		return base;
	}
	ENDCG
	
	SubShader {
		Pass {
			ZTest Always Cull Off ZWrite Off
		
			CGPROGRAM
			#pragma multi_compile _ ANTI_FLICKER
			#pragma vertex vert_img
			#pragma fragment frag_prefilter
			ENDCG
		}
		
		Pass {
			ZTest Always Cull Off ZWrite Off
		
			CGPROGRAM
			#pragma multi_compile _ ANTI_FLICKER
			#pragma vertex vert_img
			#pragma fragment frag_downsample1
			ENDCG
		}
		
		Pass {
			ZTest Always Cull Off ZWrite Off
		
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag_downsample2
			ENDCG
		}
		
		Pass {
			ZTest Always Cull Off ZWrite Off
		
			CGPROGRAM
			#pragma multi_compile _ HIGH_QUALITY
			#pragma vertex vert_multitex
			#pragma fragment frag_upsample
			ENDCG
		}
		
		Pass {
			ZTest Always Cull Off ZWrite Off
		
			CGPROGRAM
			#pragma multi_compile _ HIGH_QUALITY
			#pragma vertex vert_multitex
			#pragma fragment frag_upsample_final
			ENDCG
		}
	}
}