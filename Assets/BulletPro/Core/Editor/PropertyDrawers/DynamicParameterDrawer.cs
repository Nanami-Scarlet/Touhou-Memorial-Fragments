using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
    // Base class for any DynamicParameter drawer related to the root struct
    public class DynamicParameterDrawer : DynamicDrawer
    {
        public override float GetNumberOfLines(SerializedProperty property) { return base.GetNumberOfLines(property); }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight + 1;
            
            SerializedProperty _currentValue = null;
            
            // with iterator (slight performance gain)
            SerializedProperty _valueTree = property.Copy();
            _valueTree.NextVisible(true);

            // without iterator
            //SerializedProperty _valueTree = property.FindPropertyRelative("valueTree");
            
            if (_valueTree.arraySize < 2)
                return lineHeight;
            else _currentValue = _valueTree.GetArrayElementAtIndex(1);
            
            // without iterator
            /* *
            SerializedProperty _settings = _currentValue.FindPropertyRelative("settings");
            SerializedProperty _valueType = _settings.FindPropertyRelative("valueType");
            if (_valueType.enumValueIndex > 0)
                return lineHeight;
            /* */

            // with iterator (slight performance gain)
            _currentValue.NextVisible(true);
            _currentValue.NextVisible(true);
            if (_currentValue.enumValueIndex > 0) // at this point, reflects valueType and not currentValue
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
            dynamicParameter = property;

            // with iterator (slight performance gain)
            /* */
            SerializedProperty spIterator = property.Copy();
            spIterator.NextVisible(true);
            valueTree = spIterator.Copy();

            bool mustInitializeEverything = false;
            if (valueTree.arraySize < 2)
            {
                valueTree.arraySize = 2;
                mustInitializeEverything = true;
            }

            // the index 0 will remain unused, as it's the "default index" marking an object hasn't been initialized.
            currentValue = valueTree.GetArrayElementAtIndex(1);

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
    
            if (mustInitializeEverything)
            {
                currentValueIndex.intValue = valueTree.arraySize-1;
                spIterator = interpolationValue.Copy();
                spIterator.NextVisible(true);
                spIterator.NextVisible(false);
                spIterator.animationCurveValue = AnimationCurve.Linear(0,0,1,1);
                headerTitle.stringValue = property.displayName;
            }
            /* */

            // without iterator
            /* *
            valueTree = property.FindPropertyRelative("valueTree");
            bool mustInitializeEverything = false;
            if (valueTree.arraySize < 2)
            {
                valueTree.arraySize = 2;
                mustInitializeEverything = true;
            }
            currentValue = valueTree.GetArrayElementAtIndex(1);
            settings = currentValue.FindPropertyRelative("settings");
            valueType = settings.FindPropertyRelative("valueType");            
            currentValueIndex = settings.FindPropertyRelative("index");
            fromIndex = settings.FindPropertyRelative("indexOfFrom");
            toIndex = settings.FindPropertyRelative("indexOfTo");
            headerTitle = settings.FindPropertyRelative("headerTitle");            
            if (mustInitializeEverything)
            {
                currentValueIndex.intValue = valueTree.arraySize-1;
                interpolationValue.FindPropertyRelative("repartitionCurve").animationCurveValue = AnimationCurve.Linear(0,0,1,1);
                headerTitle.stringValue = property.displayName;
            }
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