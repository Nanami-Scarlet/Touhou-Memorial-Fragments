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
    [CustomPropertyDrawer(typeof(DynamicVector4))]
    public class DynamicVector4Drawer : DynamicVectorDrawer
    {
        public override void DrawFixed(SerializedProperty property, SerializedProperty nonDynamicValue, GUIContent label)
        {
            nonDynamicValue.vector4Value = EditorGUI.Vector4Field(mainRect, label, nonDynamicValue.vector4Value);
        }
    }
}