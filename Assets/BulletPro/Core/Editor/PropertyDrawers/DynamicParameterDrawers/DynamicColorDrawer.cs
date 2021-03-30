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
    [CustomPropertyDrawer(typeof(DynamicColor))]
    public class DynamicColorDrawer : DynamicParameterDrawer
    {
        public override void DrawFromTo(GUIContent label, SerializedProperty fromValue, SerializedProperty toValue)
        {
            SerializedProperty baseFromValue = fromValue.FindPropertyRelative("defaultValue");
            SerializedProperty baseToValue = toValue.FindPropertyRelative("defaultValue");

            Color fromColor = baseFromValue.colorValue;
            Color toColor = baseToValue.colorValue;
            //Texture2D fromTex = new Texture2D(1,1); fromTex.SetPixel(0,0, fromColor); fromTex.Apply();
            //Texture2D toTex = new Texture2D(1,1); toTex.SetPixel(0,0, toColor); toTex.Apply();
            
            List<float> widths = new List<float>();
            bool isInWindow = false;//(displayStr == "From" || displayStr == "To"); // TODO : clean this
            widths.Add(isInWindow ? 50 : 120); // full display text
            widths.Add(isInWindow ? 50 : 32); // "between" or "from"
            widths.Add(60); // color rect
            widths.Add(isInWindow ? 24 : 14); // "and" or "to"
            widths.Add(60); // color rect
            
            float startX = mainRect.x;
            float sum = 0;
            for (int i=0; i < widths.Count; i++) sum += widths[i] + space;
            sum -= space;
            startX = mainRect.x + mainRect.width - sum;
            //widths[0] = Mathf.Min(widths[0], mainRect.width - (sum+10)); // avoid overlap
            
            Rect[] rects = new Rect[widths.Count];
            float usedWidth = 0;
            
            for (int i=0; i<rects.Length; i++)
            {
                if (i==0) rects[i] = new Rect(mainRect.x + usedWidth, mainRect.y, widths[i], mainRect.height);
                else rects[i] = new Rect(startX + usedWidth, mainRect.y, widths[i], mainRect.height);
                usedWidth += widths[i];
                usedWidth += space;
            }

            int curRect = 0;
            EditorGUI.LabelField(rects[curRect++], label);
            EditorGUI.LabelField(rects[curRect++], isInWindow?"Between":"From");
            EditorGUI.PropertyField(rects[curRect++], baseFromValue, GUIContent.none);
            //EditorGUI.DrawTextureTransparent(rects[curRect++], fromTex, ScaleMode.StretchToFill);
            EditorGUI.LabelField(rects[curRect++], isInWindow?"and":"to");
            EditorGUI.PropertyField(rects[curRect++], baseToValue, GUIContent.none);
            //EditorGUI.DrawTextureTransparent(rects[curRect++], toTex, ScaleMode.StretchToFill);
        }
    }
}