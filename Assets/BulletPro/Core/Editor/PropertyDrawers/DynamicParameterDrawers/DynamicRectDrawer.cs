using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
    // Base class for a DynamicParameter drawer related to the root struct. Heavily relies on inheritance.
    [CustomPropertyDrawer(typeof(DynamicRect))]
    public class DynamicRectDrawer : DynamicParameterDrawer
    {
        public override float GetNumberOfLines(SerializedProperty property)
        {
            if (EditorGUIUtility.currentViewWidth < 341) return 3;
            else return 2;
        }

        public override void DrawFixed(SerializedProperty property, SerializedProperty nonDynamicValue, GUIContent label)
        {
            nonDynamicValue.rectValue = EditorGUI.RectField(mainRect, label, nonDynamicValue.rectValue);
        }
    }
}