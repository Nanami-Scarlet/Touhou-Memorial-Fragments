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
    [CustomPropertyDrawer(typeof(DynamicAnimationCurve))]
    public class DynamicAnimationCurveDrawer : DynamicParameterDrawer
    {
        public override float GetNumberOfLines(SerializedProperty property)
        {
            if (property.FindPropertyRelative("forceZeroToOne").boolValue)
                return 2;
            else return 1;
        }

        public override void DrawFixed(SerializedProperty property, SerializedProperty nonDynamicValue, GUIContent label)
        {
            if (!property.FindPropertyRelative("forceZeroToOne").boolValue) base.DrawFixed(property, nonDynamicValue, label);
            else
            {
                mainRect.height = EditorGUIUtility.singleLineHeight+1;
                EditorGUI.PropertyField(mainRect, nonDynamicValue, label);
                Rect bottomRect = new Rect(mainRect.x, mainRect.y + EditorGUIUtility.singleLineHeight+3, availableWidth, EditorGUIUtility.singleLineHeight+1);
                float fixCurveWidth = 70;
                Rect fixCurveRect = new Rect(bottomRect.x+availableWidth-fixCurveWidth, bottomRect.y, fixCurveWidth, bottomRect.height-2);
                float buttonSpace = 5;
                Rect warningRect = new Rect(bottomRect.x, bottomRect.y, bottomRect.width - (fixCurveWidth+buttonSpace), bottomRect.height);

                if (!BulletCurveDrawer.GoesFromZeroToOne(nonDynamicValue, true))
                {
                    string errorStr = (warningRect.width>185f?"X-axis must run from 0 to 1.":"X-axis error.");
                    EditorGUI.LabelField(warningRect, errorStr, EditorStyles.boldLabel);
                    Color oldColor = GUI.color;
                    GUI.color = new Color(1.0f, 0.6f, 0.4f, 1f);
                    if (GUI.Button(fixCurveRect, "Fix Curve", EditorStyles.miniButton))
                        BulletCurveDrawer.RepairCurveFromZeroToOne(nonDynamicValue, true);
                    GUI.color = oldColor;
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.LabelField(warningRect, (warningRect.width>140f?"This curve has no error.":"No error."));
                    if (GUI.Button(fixCurveRect, "Fix Curve", EditorStyles.miniButton))
                        Debug.Log("If you can see this, congratulations! I thought nobody would read the whole source code. Are you enjoying BulletPro so far?");
                    EditorGUI.EndDisabledGroup();
                }
            }
        }
    }
}