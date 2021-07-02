using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
    // Base class for any property drawer related to DynamicParameters
    public class DynamicDrawer : PropertyDrawer
    {
        public static SerializedProperty dynamicParameter;

        // formatting
        public int indent;
        public float availableWidth;
        public float extendButtonWidth;
        public float space;
        public Rect mainRect, buttonRect;

        // serialized properties
        public SerializedProperty currentValue, valueTree, interpolationValue, settings, currentValueIndex, headerTitle, valueType, fromIndex, toIndex;
        public SerializedProperty nonDynamicValue;

        public virtual float GetNumberOfLines(SerializedProperty property) { return 1f; }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }

        public virtual void InitFormatting(Rect position)
        {
            // non-tweakable
            indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            availableWidth = position.width - indent * 15;

            // tweakable
            extendButtonWidth = 21;
            space = 5;

            // rects
            mainRect = new Rect(position.x + indent * 15, position.y, availableWidth - (extendButtonWidth+space), position.height);
            buttonRect = new Rect(mainRect.x + space + mainRect.width, position.y, extendButtonWidth, 16);
            //buttonRect = new Rect(mainRect.x + space + mainRect.width, position.y, extendButtonWidth, EditorGUIUtility.singleLineHeight+1);
        }

        public virtual void InitSerializedProperties(SerializedProperty property)
        {
            
        }

        // Passes "property" so the parameter drawers don't have to fetch static values
        public virtual void DrawFixed(SerializedProperty property, SerializedProperty nonDynamicValue, GUIContent label)
        {
            EditorGUI.PropertyField(mainRect, nonDynamicValue, label);
        }

        // actually tell user what are the "from" and "to" values
        public virtual void DrawFromTo(GUIContent label, SerializedProperty fromValue, SerializedProperty toValue)
        {
            SerializedProperty baseFromValue = fromValue.Copy();//FindPropertyRelative("defaultValue");
            SerializedProperty baseToValue = toValue.Copy();//FindPropertyRelative("defaultValue");
            baseFromValue.NextVisible(true); // -> settings
            baseFromValue.NextVisible(false); // -> defaultValue
            baseToValue.NextVisible(true); // -> settings
            baseToValue.NextVisible(false); // -> defaultValue

            string fromString = "";
            string toString = "";

            bool isFloat = (baseFromValue.type == "float");
            bool isInt = (baseFromValue.type == "int");

            if (isFloat)
            {
                fromString = baseFromValue.floatValue.ToString();
                toString = baseToValue.floatValue.ToString();
            }
            else if (isInt)
            {
                fromString = baseFromValue.intValue.ToString();
                toString = baseToValue.intValue.ToString();
            }

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

        // only for colors: draws the gradient and makes it editable on the go
        public virtual void DrawFromGradient(SerializedProperty gradientProperty, GUIContent label)
        {
            string capstr = string.IsNullOrEmpty(label.text) ? "P" : " : p";
            string totalStr = label.text + capstr + "icked from a gradient.";
            
            float gradWidth = 60f;
            float gradSpace = 10f;
            float strWidth = mainRect.width - (gradWidth+gradSpace);

            Rect strRect = new Rect(mainRect.x, mainRect.y, strWidth, mainRect.height);
            Rect gradRect = new Rect(mainRect.x + strWidth + gradSpace, mainRect.y, gradWidth, mainRect.height);

            EditorGUI.LabelField(strRect, totalStr);
            EditorGUI.PropertyField(gradRect, gradientProperty, GUIContent.none);
        }

        public void DrawWindowButtonAndEndFormatting(SerializedProperty property)
        {
            // window button
            Color defC = GUI.color;
            if (valueType.enumValueIndex > 0) GUI.color = Color.green;
            GUIContent dynamicButtonGC = new GUIContent("...", "Make this parameter Dynamic: randomize it or have its value be based on various parameters.");
            if (GUI.Button(buttonRect, dynamicButtonGC, EditorStyles.miniButton))
            {
                GUI.color = defC;
                // OpenWindow(property, headerTitle, defC);
                DynamicParameterWindow dpw = EditorWindow.GetWindow(typeof(DynamicParameterWindow)) as DynamicParameterWindow;
                dpw.LoadProperty(property.serializedObject, dynamicParameter, currentValueIndex.intValue);
            }
            GUI.color = defC;

            // end
            EditorGUI.indentLevel = indent;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitFormatting(position);
            InitSerializedProperties(property);

            if (valueType.enumValueIndex == (int)DynamicParameterSorting.Fixed)
            {
                EditorGUIUtility.labelWidth -= indent*15;
                DrawFixed(property, nonDynamicValue, label);
                EditorGUIUtility.labelWidth += indent*15;
            }
            else if (valueType.enumValueIndex == (int)DynamicParameterSorting.Blend) // if blend of N different values
            {
                string capstr = string.IsNullOrEmpty(label.text) ? "M" : " : m";
                EditorGUI.LabelField(mainRect, label.text + capstr+ "ultiple possible values. Click for more.");
            }
            else if (valueType.enumValueIndex == (int)DynamicParameterSorting.FromGradient) // if color picked on a gradient
            {
                //string capstr = string.IsNullOrEmpty(label.text) ? "P" : " : p";
                //EditorGUI.LabelField(mainRect, label.text + capstr+ "icked from a gradient. Click for more.");
                DrawFromGradient(currentValue.FindPropertyRelative("gradientValue"), label);
            }
            else if (valueType.enumValueIndex == (int)DynamicParameterSorting.EqualToParameter) // if equal to Custom or Global parameter
            {
                string capstr = string.IsNullOrEmpty(label.text) ? "E" : " : e";
                EditorGUI.LabelField(mainRect, label.text + capstr+ "qual to another parameter. Click for more.");
            }
            else if (fromIndex.intValue == 0 || toIndex.intValue == 0) // if "from" and "to" values don't exist yet
            {
                EditorGUI.LabelField(mainRect, label.text + " : dynamic value. Click for more.");
            }
            else // special line display if value has been tweaked in the window
            {
                // find out if it's more than just a "from-to" value
                bool nestedIsDynamic = false;
                SerializedProperty fromValue = valueTree.GetArrayElementAtIndex(fromIndex.intValue);
                SerializedProperty toValue = valueTree.GetArrayElementAtIndex(toIndex.intValue);
                SerializedProperty fromIterator = fromValue.Copy();
                SerializedProperty toIterator = toValue.Copy();
                fromIterator.NextVisible(true); // -> settings
                fromIterator.NextVisible(true); // -> valueType
                toIterator.NextVisible(true);
                toIterator.NextVisible(true);
                if (fromIterator.enumValueIndex > 0)
                    nestedIsDynamic = true;
                else if (toIterator.enumValueIndex > 0)
                    nestedIsDynamic = true;

                // just explain it's nested dynamic
                if (nestedIsDynamic)
                {
                    string capstr = string.IsNullOrEmpty(label.text) ? "N" : " : n";
                    EditorGUI.LabelField(mainRect, label.text + capstr+"ested dynamic value. Click for more.");
                }

                // just explain it's from-to
                else DrawFromTo(label, fromValue, toValue);
            }

            DrawWindowButtonAndEndFormatting(property);
        }
    }
}