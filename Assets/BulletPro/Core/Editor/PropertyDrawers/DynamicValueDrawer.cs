using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
    // Base class for any DynamicParameter drawer besides the root struct. Only displayed in window.
    public class DynamicValueDrawer : DynamicDrawer
    {
        public override float GetNumberOfLines(SerializedProperty property) { return base.GetNumberOfLines(property); }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight + 1;
            
            // without iterator
            /* *
            SerializedProperty _settings = property.FindPropertyRelative("settings");
            SerializedProperty _valueType = _settings.FindPropertyRelative("valueType");
            if (_valueType.enumValueIndex > 0)
                return lineHeight;
            /* */

            // with iterator (slight performance gain)
            SerializedProperty spIterator = property.Copy();
            spIterator.NextVisible(true);
            spIterator.NextVisible(true);
            if (spIterator.enumValueIndex > 0)
                return lineHeight;

            return lineHeight * GetNumberOfLines(property);
        }

        public override void InitFormatting(Rect position)
        {
            base.InitFormatting(position);
        }

        public override void InitSerializedProperties(SerializedProperty property)
        {
            base.InitSerializedProperties(property);

            // with iterator (slight performance gain)
            /* */
            SerializedProperty spIterator = dynamicParameter.Copy();
            spIterator.NextVisible(true);
            valueTree = spIterator.Copy();

            // the index 0 will remain unused, as it's the "default index" marking an object hasn't been initialized.
            currentValue = property;

            spIterator = currentValue.Copy();

            spIterator.NextVisible(true);
            settings = spIterator.Copy();
            
            nonDynamicValue = spIterator.Copy();
            nonDynamicValue.NextVisible(false);

            spIterator.NextVisible(true);
            valueType = spIterator.Copy();

            spIterator.NextVisible(false);
            interpolationValue = spIterator.Copy();

            spIterator.NextVisible(false);
            spIterator.NextVisible(false);
            currentValueIndex = spIterator.Copy();
            
            spIterator.NextVisible(false);
            spIterator.NextVisible(false);
            fromIndex = spIterator.Copy();
            spIterator.NextVisible(false);
            toIndex = spIterator.Copy();

            spIterator.NextVisible(false);
            spIterator.NextVisible(false);
            spIterator.NextVisible(false);
            headerTitle = spIterator.Copy();
            /* */

            // without iterator
            /* *
            valueTree = dynamicParameter.FindPropertyRelative("valueTree");

            currentValue = property;

            settings = currentValue.FindPropertyRelative("settings");
            currentValueIndex = settings.FindPropertyRelative("index");
            headerTitle = settings.FindPropertyRelative("headerTitle");
            valueType = settings.FindPropertyRelative("valueType");            

            fromIndex = settings.FindPropertyRelative("indexOfFrom");
            toIndex = settings.FindPropertyRelative("indexOfTo");
            /* */
        }

        public override void DrawFixed(SerializedProperty property, SerializedProperty nonDynamicValue, GUIContent label)
        {
            base.DrawFixed(property, nonDynamicValue, label);
        }

        public override void DrawFromTo(GUIContent label, SerializedProperty fromValue, SerializedProperty toValue)
        {
            base.DrawFromTo(label, fromValue, toValue);
        }
    }
}