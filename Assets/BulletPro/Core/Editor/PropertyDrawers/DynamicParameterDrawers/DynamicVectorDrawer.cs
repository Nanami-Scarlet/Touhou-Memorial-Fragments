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
    public class DynamicVectorDrawer : DynamicParameterDrawer
    {
        public override float GetNumberOfLines(SerializedProperty property)
        {
            if (EditorGUIUtility.currentViewWidth < 341) return 2;
            else return 1;
        }

        public override void DrawFixed(SerializedProperty property, SerializedProperty nonDynamicValue, GUIContent label)
        {
            base.DrawFixed(property, nonDynamicValue, label);
        }

        public override void DrawFromTo(GUIContent label, SerializedProperty fromValue, SerializedProperty toValue)
        {
            string capstr = string.IsNullOrEmpty(label.text) ? "R" : " : r";
            EditorGUI.LabelField(mainRect, label.text + capstr + "anges between two vectors. Click for more.");
        }
    }
}