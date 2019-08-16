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
using UnityEditor;

[CustomEditor(typeof(SMAA))]
public class SMAAEditor : Editor {
    SerializedProperty m_DebugPass;
    SerializedProperty m_LuminosityAdaptation;
    SerializedProperty m_Threshold;
    SerializedProperty m_DepthThreshold;
    SerializedProperty m_MaxSearchSteps;
    SerializedProperty m_MaxSearchStepsDiag;
    SerializedProperty m_CornerRounding;
    SerializedProperty m_LocalContrastAdaptationFactor;

    private void OnEnable() {
        m_DebugPass = serializedObject.FindProperty("debugPass");

        m_LuminosityAdaptation = serializedObject.FindProperty("luminosityAdaptation");
        m_Threshold = serializedObject.FindProperty("threshold");
        m_DepthThreshold = serializedObject.FindProperty("depthThreshold");
        m_MaxSearchSteps = serializedObject.FindProperty("maxSearchSteps");
        m_MaxSearchStepsDiag = serializedObject.FindProperty("maxSearchStepsDiag");
        m_CornerRounding = serializedObject.FindProperty("cornerRounding");
        m_LocalContrastAdaptationFactor = serializedObject.FindProperty("localContrastAdaptationFactor");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.PropertyField(m_DebugPass, new GUIContent("Debug Pass"));
        
        GUILayout.Space(5f);

        EditorGUIUtility.labelWidth = 200f;
        EditorGUILayout.PropertyField(m_LuminosityAdaptation, new GUIContent("Luminosity Adaptation"));
        EditorGUILayout.PropertyField(m_Threshold, new GUIContent("Threshold"));
        EditorGUILayout.PropertyField(m_DepthThreshold, new GUIContent("Depth Threshold"));
        EditorGUILayout.PropertyField(m_MaxSearchSteps, new GUIContent("Max Search Steps"));
        EditorGUILayout.PropertyField(m_MaxSearchStepsDiag, new GUIContent("Max Search Steps (Diagonal)"));
        EditorGUILayout.PropertyField(m_CornerRounding, new GUIContent("Corner Rounding"));
        EditorGUILayout.PropertyField(m_LocalContrastAdaptationFactor, new GUIContent("Local Contrast Adapt Factor"));
        EditorGUIUtility.labelWidth = 0f;

        serializedObject.ApplyModifiedProperties();
    }
}