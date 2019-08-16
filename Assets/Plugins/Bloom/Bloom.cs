/*
Kino Bloom v2 - Bloom filter for Unity

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

using UnityEngine;

[ExecuteInEditMode]
[DisallowMultipleComponent]
[ImageEffectAllowedInSceneView]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Kino Bloom")]
public class Bloom : MonoBehaviour {
    public static class ShaderProps {
        public static readonly int _BaseTex = Shader.PropertyToID("_BaseTex");
        public static readonly int _Cutoff = Shader.PropertyToID("_Cutoff");
        public static readonly int _Intensity = Shader.PropertyToID("_Intensity");
        public static readonly int _PrefilterOffs = Shader.PropertyToID("_PrefilterOffs");
        public static readonly int _Threshold = Shader.PropertyToID("_Threshold");
    }

    public Shader shader;
    public bool highQuality = true;
    public bool downsample = false;
    public bool antiFlicker = false;
    public float exposure = 0.5f;
    public float threshold = 0.5f;
    public float intensity = 1f;
	public int blurIterations = 6;

    private Material mat;
    private RenderTexture[] rt1;
    private RenderTexture[] rt2;
    private RenderTextureFormat rtFormat;
    private int prevRtLength;

    private void OnEnable() {
        if(shader == null || !shader.isSupported) {
            enabled = false;
            return;
        }

        if(mat == null) {
            mat = new Material(shader);
            mat.hideFlags = HideFlags.HideAndDontSave;
        }

        prevRtLength = -1;
        CreateRenderTextureBuffers();

        if(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR))
            rtFormat = RenderTextureFormat.DefaultHDR;
        else
            rtFormat = RenderTextureFormat.Default;
    }

    private void OnDisable() {
        if(mat != null)
            DestroyImmediate(mat);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if(!enabled) {
            Graphics.Blit(source, destination);
            return;
        }
            
        int tw = source.width >> 1;
        int th = source.height >> 1;

        if(downsample) {
            tw >>= 1;
            th >>= 1;
        }

        blurIterations = Mathf.Max(2, blurIterations);
        mat.SetFloat(ShaderProps._Threshold, threshold);

        float pfc = -Mathf.Log(exposure, 10f);
        mat.SetFloat(ShaderProps._Cutoff, threshold + (pfc * 10f));

        bool pfo = !highQuality && antiFlicker;
        mat.SetFloat(ShaderProps._PrefilterOffs, (pfo) ? -0.5f : 0.0f);
        mat.SetFloat(ShaderProps._Intensity, intensity);
            
        if(highQuality)
            mat.EnableKeyword("HIGH_QUALITY");
        else
            mat.DisableKeyword("HIGH_QUALITY");

        if(antiFlicker)
            mat.EnableKeyword("ANTI_FLICKER");
        else
            mat.DisableKeyword("ANTI_FLICKER");

        CreateRenderTextureBuffers();
        int rtLength = rt1.Length;

        for(int i = 0; i < rtLength; i++) {
            rt1[i] = RenderTexture.GetTemporary(tw, th, 0, rtFormat);

            if(i > 0 && i < rtLength - 1) {
                rt2[i] = RenderTexture.GetTemporary(tw, th, 0, rtFormat);
            }

            tw >>= 1;
            th >>= 1;

            if(tw == 0 || th == 0) {
                rtLength = i + 1;
                break;
            }
        }

        Graphics.Blit(source, rt1[0], mat, 0); //prefilter
        Graphics.Blit(rt1[0], rt1[1], mat, 1); //mip pyramid

        for(int i = 1; i < rtLength - 1; i++) {
            Graphics.Blit(rt1[i], rt1[i + 1], mat, 2);
        }

        // blur and combine loop
        mat.SetTexture(ShaderProps._BaseTex, rt1[rtLength - 2]);
        Graphics.Blit(rt1[rtLength - 1], rt2[rtLength - 2], mat, 3);

        for(int i = rtLength - 2; i > 1; i--) {
            mat.SetTexture(ShaderProps._BaseTex, rt1[i - 1]);
            Graphics.Blit(rt2[i], rt2[i - 1], mat, 3);
        }

        mat.SetTexture(ShaderProps._BaseTex, source);
        Graphics.Blit(rt2[1], destination, mat, 4);

        // release temporary buffers
        for(int i = 0; i < rtLength; i++) {
            RenderTexture.ReleaseTemporary(rt1[i]);
            RenderTexture.ReleaseTemporary(rt2[i]);
        }
    }

    private void CreateRenderTextureBuffers() {
        int rtLength = blurIterations + 1;

        if(rtLength != prevRtLength) {
            rt1 = new RenderTexture[rtLength];
            rt2 = new RenderTexture[rtLength];
            prevRtLength = rtLength;
        }
    }
}