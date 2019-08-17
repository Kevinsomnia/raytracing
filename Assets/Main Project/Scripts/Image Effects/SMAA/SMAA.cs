/*
 * Copyright (c) 2015 Thomas Hourdel
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 *    1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 
 *    2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 
 *    3. This notice may not be removed or altered from any source
 *    distribution.
 */

using UnityEngine;

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
[RequireComponent(typeof(Camera))]
public class SMAA : MonoBehaviour {
    public enum DebugPass {
        Off,
        Edges,
        Weights
    }
        
    public DebugPass debugPass = DebugPass.Off;
    public bool luminosityAdaptation = false;
    public float threshold = 0.1f;
    public float depthThreshold = 0.01f;
    public int maxSearchSteps = 16;
    public int maxSearchStepsDiag = 8;
    public int cornerRounding = 25;
    public float localContrastAdaptationFactor = 2f;

    public Shader shader;
    public Texture2D areaTex;
    public Texture2D searchTex;

    private Camera m_Camera;
    private Material m_Material;
    private float lumFactor;

    public Material curMaterial {
        get {
            if(m_Material == null && shader != null && shader.isSupported) {
                m_Material = new Material(shader);
                m_Material.hideFlags = HideFlags.HideAndDontSave;
            }

            return m_Material;
        }
    }

    void OnEnable() {
        m_Camera = GetComponent<Camera>();
    }

    void OnDisable() {
        if(m_Material != null) {
            DestroyImmediate(m_Material);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        int width = m_Camera.pixelWidth;
        int height = m_Camera.pixelHeight;

        lumFactor = (luminosityAdaptation) ? GetLuminosity(RenderSettings.ambientLight) : 1f; //Darker areas wil have lower threshold.
        maxSearchSteps = Mathf.Min(maxSearchSteps, 128);
        maxSearchStepsDiag = Mathf.Min(maxSearchStepsDiag, 128);
        cornerRounding = Mathf.Min(cornerRounding, 128);

        RenderTextureFormat renderFormat = (m_Camera.allowHDR && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;

        curMaterial.SetTexture("_AreaTex", areaTex);
        curMaterial.SetTexture("_SearchTex", searchTex);

        curMaterial.SetVector("_Metrics", new Vector4(1f / width, 1f / height, width, height));
        curMaterial.SetVector("_Params1", new Vector4(threshold * lumFactor * lumFactor, depthThreshold, maxSearchSteps, maxSearchStepsDiag));
        curMaterial.SetVector("_Params2", new Vector2(cornerRounding, localContrastAdaptationFactor));

        RenderTexture rt1 = RenderTexture.GetTemporary(width, height, 0, renderFormat, RenderTextureReadWrite.Linear);
        RenderTexture rt2 = RenderTexture.GetTemporary(width, height, 0, renderFormat, RenderTextureReadWrite.Linear);

        Graphics.Blit(rt1, rt1, curMaterial, 0);
        Graphics.Blit(rt2, rt2, curMaterial, 0);

        Graphics.Blit(source, rt1, curMaterial, 1);

        if(debugPass == DebugPass.Edges) {
            Graphics.Blit(rt1, destination);
        }
        else {
            Graphics.Blit(rt1, rt2, curMaterial, 2);

            if(debugPass == DebugPass.Weights) {
                Graphics.Blit(rt2, destination);
            }
            else {
                curMaterial.SetTexture("_BlendTex", rt2);
                Graphics.Blit(source, destination, curMaterial, 3);
            }
        }

        RenderTexture.ReleaseTemporary(rt1);
        RenderTexture.ReleaseTemporary(rt2);
    }

    public float GetLuminosity(Color col) {
        float lum = (col.r * 0.299f);
        lum += (col.g * 0.587f);
        lum += (col.b * 0.114f);
        return lum;
    }
}