Shader "Hidden/Post-Process Master" {
	Properties {
		_MainTex ("", 2D) = "white" {}
		_BloomTex ("", 2D) = "black" {}
	}

	CGINCLUDE

	#pragma multi_compile __ VIGNETTING_ON
	#pragma multi_compile __ CHROMATIC_ABERRATION_ON
	#pragma multi_compile __ TONEMAPPING_ON
	#pragma multi_compile __ LUT_CC_ON
	#pragma multi_compile __ FILM_GRAIN_ON

	#include "UnityCG.cginc"

	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
	};

	sampler2D _MainTex;
	float2 _MainTex_TexelSize;
	
	half4 _Temperature;
	half4 _ColorTint; // rgb = tint, a = saturation.
	half _Exposure;
	half _Dimming;
	half _BrightnessShift;
	int _ColorBlindType;

	static float3x3 colorblindMatrices[4] = {
		// Normal vision - identity matrix
		float3x3(
			1.0, 0.0, 0.0,
			0.0, 1.0, 0.0,
			0.0, 0.0, 1.0),
		// Protanopia - blindness to long wavelengths
		float3x3(
			0.567, 0.433, 0.0,
			0.558, 0.442, 0.0,
			0.0, 0.242, 0.758),
		// Deuteranopia - blindness to medium wavelengths
		float3x3(
			0.625, 0.375, 0.0,
			0.7, 0.3, 0.0,
			0.0, 0.3, 0.7),
		// Tritanopia - blindness to short wavelengths
		float3x3(
			0.95, 0.05, 0.0, 
			0.0, 0.433, 0.567, 
			0.0, 0.475, 0.525)
	};

	float3 LRGB_to_LMS(fixed3 c) {
        float3x3 m = {
            3.90405e-1f, 5.49941e-1f, 8.92632e-3f,
            7.08416e-2f, 9.63172e-1f, 1.35775e-3f,
            2.31082e-2f, 1.28021e-1f, 9.36245e-1f
        };
		
        return mul(m, c);
    }

    float3 LMS_to_LRGB(fixed3 c) {
        float3x3 m = {
             2.85847e+0f, -1.62879e+0f, -2.48910e-2f,
            -2.10182e-1f,  1.15820e+0f,  3.24281e-4f,
            -4.18120e-2f, -1.18169e-1f,  1.06867e+0f
        };
		
        return mul(m, c);
    }
	
	float3 ApplyBalance(fixed3 c) {
        c = LMS_to_LRGB(LRGB_to_LMS(c) * _Temperature);
        return max(c, 0.0);
    }

	float Noise(float2 uv) {
		return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.54531) - 0.5;
	}

#if VIGNETTING_ON
	half _VignetteIntensity;
	half _VignetteSmoothness;
	half _ChromaticAberration;
#endif

#if LUT_CC_ON
	sampler3D _LutTex;
	half4 _CCParams;
#endif

#if FILM_GRAIN_ON
	half4 _GrainParams;
#endif

	v2f vert(appdata_img v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		return o;
	}

	half4 frag(v2f i) : SV_Target {
		half4 screen = tex2D(_MainTex, i.uv);
		
		half3 luminanceCoeff = float3(0.2125, 0.7154, 0.0721);
		float noise = Noise(i.uv + frac(_Time.y * 10.0));

	#if VIGNETTING_ON
		float2 coords = (i.uv - 0.5) * _VignetteIntensity;
		float coordDot = dot(coords, coords);
		float2 chrom = (i.uv - 0.5) * 2.0;
		float chromDot = dot(chrom, chrom);

	#if CHROMATIC_ABERRATION_ON
		float2 uvG = i.uv - 0.002 * _ChromaticAberration * chrom * chromDot;
		screen.g = tex2D(_MainTex, uvG).g;
	#endif

		// Vignetting.
		half vig = pow(saturate(1.0 - coordDot), _VignetteSmoothness);
		half lum = dot(screen.rgb, luminanceCoeff);
		vig += noise * 0.042 * saturate(coordDot * 64.0) * saturate(1.0 - (lum * 4.0));
		screen.rgb *= vig;
	#endif

	#if TONEMAPPING_ON
		screen.rgb = 1.0 - exp2(-_Exposure * screen.rgb);
	#endif
		
		// Recalculate luminance after tonemapping.
		half avgLum = dot(screen.rgb, luminanceCoeff);

		// Simulate desaturation of darker colors.
		const half DESATURATE_START = 11.0 / 255.0;
		const half DARK_DESATURATE_FACTOR = 0.3;
		half darkDesaturate = max(0.0, 1.0 - (avgLum / DESATURATE_START));
		
		screen.rgb = lerp(avgLum, screen.rgb, _ColorTint.a * (1.0 - (darkDesaturate * DARK_DESATURATE_FACTOR)));
		screen.rgb += _ColorTint * avgLum.r;
		screen.rgb = ApplyBalance(screen.rgb);
		
	#if LUT_CC_ON
		screen.rgb = sqrt(screen.rgb);
		screen.rgb = tex3D(_LutTex, (screen.rgb * _CCParams.x) + _CCParams.y).rgb;
		screen.rgb *= screen.rgb;
	#endif

	#if FILM_GRAIN_ON
		screen.rgb += noise * _GrainParams.x * avgLum * (1.0 - sqrt(avgLum * _GrainParams.y));
		screen.rgb = saturate(screen.rgb);
	#endif
		
		half brightnessShift = saturate(_BrightnessShift) * 0.0008;
		half3 shifted = screen.rgb + brightnessShift;
		screen.rgb = lerp(screen.rgb, shifted, saturate(1.0 - (avgLum * 25.0)));
		screen.rgb = mul(screen.rgb, colorblindMatrices[_ColorBlindType]);
		screen.rgb *= saturate(1.0 - _Dimming);
		return screen;
	}

	ENDCG

	SubShader {
		ZTest Always Cull Off ZWrite Off

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}

	Fallback Off
}