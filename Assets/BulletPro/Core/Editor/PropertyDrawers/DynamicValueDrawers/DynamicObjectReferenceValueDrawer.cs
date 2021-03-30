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
    [CustomPropertyDrawer(typeof(DynamicObjectReferenceValue))]
    public class DynamicObjectReferenceValueDrawer : DynamicValueDrawer
    {
        public override float GetNumberOfLines(SerializedProperty property)
        {
            if (property.FindPropertyRelative("narrowType").boolValue)
            {
                string typeName = property.FindPropertyRelative("typeName").stringValue;
                if (typeName.Contains("Texture") || typeName.Contains("Sprite"))
                    return 4;
                else return 1;
            }
            else return 1;
        }

        public override void DrawFixed(SerializedProperty property, SerializedProperty nonDynamicValue, GUIContent label)
        {
            if (dynamicParameter.FindPropertyRelative("narrowType").boolValue)
            {
                System.Type specificType = System.Type.GetType(dynamicParameter.FindPropertyRelative("typeName").stringValue);
                nonDynamicValue.objectReferenceValue = EditorGUI.ObjectField(mainRect, label, nonDynamicValue.objectReferenceValue, specificType, true);
                if (dynamicParameter.FindPropertyRelative("requireComponent").boolValue)
                {
                    GameObject go = nonDynamicValue.objectReferenceValue as GameObject;
                    if (go)
                    {
                        specificType = System.Type.GetType(dynamicParameter.FindPropertyRelative("requiredComponentName").stringValue);
                        if (!go.GetComponent(specificType))
                            nonDynamicValue.objectReferenceValue = null;
                    }
                }
            }
            else base.DrawFixed(property, nonDynamicValue, label);
        }
    }
}