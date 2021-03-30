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
    [CustomPropertyDrawer(typeof(DynamicFloat))]
    public class DynamicFloatDrawer : DynamicParameterDrawer
    {
        // Custom float field : caching fields and methods obtained via reflection
		MethodInfo doFloatFieldMethod;
		object recycledEditor;

        public override void InitFormatting(Rect position)
        {
            base.InitFormatting(position);

            // basically an OnEnable. Obtain internal methods for customizing float fields later on
			if (recycledEditor == null)
            {
                System.Type editorGUIType = typeof(EditorGUI);
                System.Type RecycledTextEditorType = Assembly.GetAssembly(editorGUIType).GetType("UnityEditor.EditorGUI+RecycledTextEditor");
                System.Type[] argumentTypes = new System.Type[] { RecycledTextEditorType, typeof(Rect), typeof(Rect), typeof(int), typeof(float), typeof(string), typeof(GUIStyle), typeof(bool) };
                doFloatFieldMethod = editorGUIType.GetMethod("DoFloatField", BindingFlags.NonPublic | BindingFlags.Static, null, argumentTypes, null);
                FieldInfo fieldInfo = editorGUIType.GetField("s_RecycledEditor", BindingFlags.NonPublic | BindingFlags.Static);
                recycledEditor = fieldInfo.GetValue(null);
            }
        }

        public override void DrawFixed(SerializedProperty property, SerializedProperty nonDynamicValue, GUIContent label)
        {
            if (property.FindPropertyRelative("useSlider").boolValue)
                nonDynamicValue.floatValue = EditorGUI.Slider(mainRect, label, nonDynamicValue.floatValue, property.FindPropertyRelative("sliderMin").floatValue, property.FindPropertyRelative("sliderMax").floatValue);
            else if (string.IsNullOrEmpty(label.text))
            {
                Rect dragRect = new Rect(mainRect.x - 8f, mainRect.y, 20f, mainRect.height);
                nonDynamicValue.floatValue = CustomFloatField(mainRect, dragRect, nonDynamicValue.floatValue);
            }
            else base.DrawFixed(property, nonDynamicValue, label);
        }

        public override void DrawFromTo(GUIContent label, SerializedProperty fromValue, SerializedProperty toValue)
        {
            SerializedProperty baseFromValue = fromValue.FindPropertyRelative("defaultValue");
            SerializedProperty baseToValue = toValue.FindPropertyRelative("defaultValue");

            string fromString = baseFromValue.floatValue.ToString();
            string toString = baseToValue.floatValue.ToString();

            List<float> widths = new List<float>();
            widths.Add(120); // full display text
            
            float w1 = (32); // "between" or "from"
            float w2 = (fromString.Length*6); // value
            float w3 = (14); // "and" or "to"
            float w4 = (toString.Length*6); // value
            widths.Add (w1+w2+w3+w4+3*space);
            
            //else if (isVector) widths.Add(isInWindow ? 195 : 160); // old values used for vectors
            widths[0] = Mathf.Min(widths[0], mainRect.width - (widths[1]+10)); // avoid overlap

            float startX = mainRect.x;
            float sum = 0;
            for (int i=0; i < widths.Count; i++) sum += widths[i] + space;
            sum -= space;
            startX = mainRect.x + mainRect.width - sum;
            
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
            string str = "From "+fromString+" to "+toString;
            EditorGUI.LabelField(rects[curRect++], str);
        }

        // Use reflection to access internal functions and customize float fields
        float CustomFloatField(Rect position, Rect dragHotZone, float value, GUIStyle style = null)
		{
			if (style == null) style = EditorStyles.numberField;
			int controlID = GUIUtility.GetControlID("EditorTextField".GetHashCode(), FocusType.Keyboard, position);

			object[] parameters = new object[] { recycledEditor, position, dragHotZone, controlID, value, "g7", style, true };

			return (float)doFloatFieldMethod.Invoke(null, parameters);
		}
    }
}