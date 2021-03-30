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
    [CustomPropertyDrawer(typeof(DynamicVector2Value))]
    public class DynamicVector2ValueDrawer : DynamicVectorValueDrawer
    {
        public override void DrawFixed(SerializedProperty property, SerializedProperty nonDynamicValue, GUIContent label)
        {
            nonDynamicValue.vector2Value = EditorGUI.Vector2Field(mainRect, label, nonDynamicValue.vector2Value);
        }
    }
}