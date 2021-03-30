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
    [CustomPropertyDrawer(typeof(DynamicEnumValue))]
    public class DynamicEnumValueDrawer : DynamicValueDrawer
    {
        public override void DrawFixed(SerializedProperty property, SerializedProperty nonDynamicValue, GUIContent label)
        {
            SerializedProperty options = dynamicParameter.FindPropertyRelative("enumOptions");
            if (options.arraySize == 0) base.DrawFixed(property, nonDynamicValue, label);
            else
            {
                GUIContent[] optionsGC = new GUIContent[options.arraySize];
                for (int i = 0; i < options.arraySize; i++)
                    optionsGC[i] = new GUIContent(options.GetArrayElementAtIndex(i).stringValue);

                nonDynamicValue.intValue = EditorGUI.Popup(mainRect, label, nonDynamicValue.intValue, optionsGC);
            }
        }
    }
}