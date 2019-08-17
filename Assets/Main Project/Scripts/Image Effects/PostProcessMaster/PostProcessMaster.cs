using UnityEngine;

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
[RequireComponent(typeof(Camera))]
public class PostProcessMaster : MonoBehaviour {
    public static class ShaderProps {
        public static readonly int _BrightnessShift = Shader.PropertyToID("_BrightnessShift");
        public static readonly int _CCParams = Shader.PropertyToID("_CCParams");
        public static readonly int _ChromaticAberration = Shader.PropertyToID("_ChromaticAberration");
        public static readonly int _ColorTint = Shader.PropertyToID("_ColorTint");
        public static readonly int _ColorBlindType = Shader.PropertyToID("_ColorBlindType");
        public static readonly int _Dimming = Shader.PropertyToID("_Dimming");
        public static readonly int _Exposure = Shader.PropertyToID("_Exposure");
        public static readonly int _GrainParams = Shader.PropertyToID("_GrainParams");
        public static readonly int _LutTex = Shader.PropertyToID("_LutTex");
        public static readonly int _Temperature = Shader.PropertyToID("_Temperature");
        public static readonly int _VignetteIntensity = Shader.PropertyToID("_VignetteIntensity");
        public static readonly int _VignetteSmoothness = Shader.PropertyToID("_VignetteSmoothness");
        public static readonly int offsets = Shader.PropertyToID("offsets");
    }

    public enum ColorBlindType { None, Protanopia, Deuteranopia, Tritanopia };

    public Shader shader;
    
    public bool vignetting = false;
    public float vignetteIntensity = 1.5f;
    public float vignetteSmoothness = 2f;
    public float chromaticAberration = 0f;

    public bool tonemapping = true;
    public float exposure = 2f;

    public bool colorCorrection = false;
    public Texture3D colorCorrectionLUT;

    public bool filmicGrain = false;
    public float grainIntensity = 0.25f;
    public float luminanceContribution = 1f;

    public Vector3 colorTint = Vector3.zero;
    public float brightnessShift = 0f;
    public float saturation = 1f;
    public float colorTemp = 0f;
    public float dimming = 0f;
    public ColorBlindType colorBlindType = ColorBlindType.None;

    private Material mat;

    private void OnEnable() {
        if(shader == null || !shader.isSupported) {
            enabled = false;
            return;
        }
        
        mat = new Material(shader);
        mat.hideFlags = HideFlags.HideAndDontSave;
    }

    private void OnDisable() {
        if(mat != null) {
            DestroyImmediate(mat);
            mat = null;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        int rtW = source.width;
        int rtH = source.height;
        
        if(vignetting) {
            mat.EnableKeyword("VIGNETTING_ON");
            mat.SetFloat(ShaderProps._VignetteIntensity, Mathf.Max(0f, vignetteIntensity));
            mat.SetFloat(ShaderProps._VignetteSmoothness, Mathf.Max(0f, vignetteSmoothness));

            if(chromaticAberration > 0f) {
                mat.EnableKeyword("CHROMATIC_ABERRATION_ON");
                mat.SetFloat(ShaderProps._ChromaticAberration, chromaticAberration);
            }
            else {
                mat.DisableKeyword("CHROMATIC_ABERRATION_ON");
            }
        }
        else {
            mat.DisableKeyword("VIGNETTING_ON");
        }

        if(tonemapping) {
            mat.EnableKeyword("TONEMAPPING_ON");
            mat.SetFloat(ShaderProps._Exposure, Mathf.Max(0.001f, exposure));
        }
        else {
            mat.DisableKeyword("TONEMAPPING_ON");
        }

        if(colorCorrection && SystemInfo.supports3DTextures && colorCorrectionLUT != null) {
            mat.EnableKeyword("LUT_CC_ON");
            
            int lutWidth = colorCorrectionLUT.width;
            colorCorrectionLUT.wrapMode = TextureWrapMode.Clamp;

            // x = scale, y = offset, z = blend
            mat.SetVector(ShaderProps._CCParams, new Vector3((lutWidth - 1) / (float)lutWidth, 1f / (lutWidth * 2f)));
            mat.SetTexture(ShaderProps._LutTex, colorCorrectionLUT);
        }
        else {
            mat.DisableKeyword("LUT_CC_ON");
            mat.SetTexture(ShaderProps._LutTex, null);
        }

        if(filmicGrain) {
            mat.EnableKeyword("FILM_GRAIN_ON");
            mat.SetVector(ShaderProps._GrainParams, new Vector4(grainIntensity, luminanceContribution, 0f, 0f));
        }
        else {
            mat.DisableKeyword("FILM_GRAIN_ON");
        }
        
        mat.SetVector(ShaderProps._ColorTint, new Vector4(colorTint.x, colorTint.y, colorTint.z, saturation));
        mat.SetVector(ShaderProps._Temperature, CalculateTemperature());
        mat.SetFloat(ShaderProps._BrightnessShift, brightnessShift);
        mat.SetFloat(ShaderProps._Dimming, dimming);
        mat.SetInt(ShaderProps._ColorBlindType, (int)colorBlindType);

        Graphics.Blit(source, destination, mat);
    }

    public static bool DimensionsAreValidLUT(Texture2D lut) {
        if(lut != null && lut.height == (int)Mathf.Sqrt(lut.width)) {
            return true;
        }

        return false;
    }

    private float StandardIlluminantY(float x) {
        return (2.87f * x) - (3f * x * x) - 0.275095f;
    }

    private Vector3 CIEtoLMS(float x, float y) {
        float X = x / y;
        float Y = (1.0f - x - y) / y;

        float L = (0.7328f * X) + 0.4296f - (0.1624f * Y);
        float M = (-0.7036f * X) + 1.6975f + (0.0061f * Y);
        float S = (0.0030f * X) + 0.0136f + (0.9834f * Y);

        return new Vector3(L, M, S);
    }

    private Vector3 CalculateTemperature() {
        if(Mathf.Abs(colorTemp) < 0.001f)
            return Vector3.one;

        float x = 0.31271f - (colorTemp * 0.075f);
        float y = StandardIlluminantY(x);

        return Divide(new Vector3(0.949237f, 1.03542f, 1.08728f), CIEtoLMS(x, y));
    }

    private Vector3 Divide(Vector3 a, Vector3 b) {
        return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    }
}