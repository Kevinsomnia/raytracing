using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PostProcessMaster))]
public class PostProcessMasterEditor : Editor {
    public override void OnInspectorGUI() {
        PostProcessMaster ppm = (PostProcessMaster)target;

        EditorGUIUtility.labelWidth = 160f;

        GUILayout.Space(5f);

        ppm.vignetting = EditorGUILayout.Toggle("Vignetting:", ppm.vignetting);

        if(ppm.vignetting) {
            EditorGUI.indentLevel += 1;

            ppm.vignetteIntensity = EditorGUILayout.FloatField("Intensity:", Mathf.Max(0f, ppm.vignetteIntensity));
            ppm.vignetteSmoothness = EditorGUILayout.FloatField("Smoothness:", Mathf.Max(0f, ppm.vignetteSmoothness));
            ppm.chromaticAberration = EditorGUILayout.FloatField("Chromatic Aberration:", Mathf.Max(0f, ppm.chromaticAberration));
            
            EditorGUI.indentLevel -= 1;
        }

        GUILayout.Space(8f);

        ppm.tonemapping = EditorGUILayout.Toggle("Tonemapping:", ppm.tonemapping);

        if(ppm.tonemapping) {
            EditorGUI.indentLevel += 1;

            EditorGUILayout.HelpBox("Photographic-based tonemapping. Pulled from Unity Standard Assets.", MessageType.None);

            GUILayout.Space(-1f);
            ppm.exposure = EditorGUILayout.Slider("Exposure:", ppm.exposure, 0.01f, 4f);

            EditorGUI.indentLevel -= 1;
        }

        GUILayout.Space(8f);

        ppm.colorCorrection = EditorGUILayout.Toggle("Color Correction (LUT):", ppm.colorCorrection);

        if(ppm.colorCorrection) {
            EditorGUI.indentLevel += 1;

            if(QualitySettings.activeColorSpace != ColorSpace.Linear) {
                EditorGUILayout.HelpBox("Color correction will not function properly without Linear lighting!", MessageType.Warning);
                GUILayout.Space(5f);
            }
            
            ppm.colorCorrectionLUT = (Texture3D)EditorGUILayout.ObjectField("Look-up Table:", ppm.colorCorrectionLUT, typeof(Texture3D), false);
            
            EditorGUI.indentLevel -= 1;
        }

        GUILayout.Space(8f);

        ppm.filmicGrain = EditorGUILayout.Toggle("Filmic Grain:", ppm.filmicGrain);

        if(ppm.filmicGrain) {
            EditorGUI.indentLevel += 1;

            ppm.grainIntensity = EditorGUILayout.Slider("Grain Intensity:", ppm.grainIntensity, 0f, 2f);
            ppm.luminanceContribution = EditorGUILayout.Slider("Luminance Contribution:", ppm.luminanceContribution, 0f, 1f);

            EditorGUI.indentLevel -= 1;
        }

        GUILayout.Space(8f);

        ppm.colorTint = EditorGUILayout.Vector3Field("Color Tint:", ppm.colorTint);
        ppm.brightnessShift = EditorGUILayout.Slider("Brightness Shift:", ppm.brightnessShift, 0f, 1f);
        ppm.saturation = EditorGUILayout.Slider("Saturation:", ppm.saturation, 0f, 2f);
        ppm.colorTemp = EditorGUILayout.Slider("Color Temperature:", ppm.colorTemp, -1f, 1f);
        ppm.dimming = EditorGUILayout.Slider("Dimming:", ppm.dimming, 0f, 1f);
        ppm.colorBlindType = (PostProcessMaster.ColorBlindType)EditorGUILayout.EnumPopup("Color-blind Type:", ppm.colorBlindType);

        if(GUI.changed) {
            EditorUtility.SetDirty(ppm);
        }
    }
}