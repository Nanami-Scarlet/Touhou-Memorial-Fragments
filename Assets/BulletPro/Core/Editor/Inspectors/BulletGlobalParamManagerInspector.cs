using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(BulletGlobalParamManager))]
	public class BulletGlobalParamManagerInspector : Editor
    {
        SerializedProperty parameters;
        ReorderableList paramList;

        void OnEnable()
        {
            parameters = serializedObject.FindProperty("parameters");

			paramList = new ReorderableList(serializedObject, parameters, true, true, true, true);

			paramList.drawHeaderCallback = (Rect rect) =>
            {
                Rect nameRect = new Rect(rect.x + 16, rect.y, rect.width, rect.height);
				Rect typeRect = new Rect(rect.x + 126, rect.y, rect.width, rect.height);
				Rect valueRect = new Rect(rect.x + 241, rect.y, rect.width, rect.height);
				EditorGUI.LabelField(nameRect, "Name");
				EditorGUI.LabelField(typeRect, "Type");
				EditorGUI.LabelField(valueRect, "Value");
            };
			paramList.drawElementCallback = CustomParameterDrawer;
			paramList.onRemoveCallback += (ReorderableList list) =>
			{
				parameters.DeleteArrayElementAtIndex(list.index);
			};
			paramList.onAddCallback += (ReorderableList list) =>
			{
				parameters.arraySize++;
			};
			paramList.elementHeightCallback += (int index) =>
			{
				float lines = 1;
				int typeIndex = parameters.GetArrayElementAtIndex(index).FindPropertyRelative("type").enumValueIndex;
				if (typeIndex == (int)ParameterType.Rect) lines++;
				else if (typeIndex == (int)ParameterType.Bounds) lines++;

				return paramList.elementHeight * lines;
			};
        }   

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(12);

            paramList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        void CustomParameterDrawer(Rect rect, int index, bool isActive, bool isFocused)
		{
			float yOffset = 2;
			rect = new Rect(rect.x, rect.y+yOffset, rect.width, 16);
			
			float nameWidth = 100;
			float space = 10;
			float enumWidth = 105;
			float spaceTwo = 10;
			float remainingWidth = rect.width - (nameWidth + enumWidth + space + spaceTwo);

			float curX = rect.x;
			Rect nameRect = new Rect(curX, rect.y, nameWidth, rect.height); curX += nameWidth + space;
			Rect enumRect = new Rect(curX, rect.y, enumWidth, rect.height); curX += enumWidth + spaceTwo;
			Rect valueRect = new Rect(curX, rect.y, remainingWidth, rect.height);

			SerializedProperty cp = parameters.GetArrayElementAtIndex(index);
			SerializedProperty cpName = cp.FindPropertyRelative("name");
			SerializedProperty cpType = cp.FindPropertyRelative("type");

			EditorGUI.PropertyField(nameRect, cpName, GUIContent.none);
			EditorGUI.PropertyField(enumRect, cpType, GUIContent.none);

			int typeIndex = cpType.enumValueIndex;
			string valueNameStr = "";

			bool isQuaternion = false;
            bool isSlider = false;

			if (typeIndex == (int)ParameterType.AnimationCurve) valueNameStr = "animationCurveValue";
			else if (typeIndex == (int)ParameterType.Bool) valueNameStr = "boolValue";
			else if (typeIndex == (int)ParameterType.Bounds) valueNameStr = "boundsValue";
			else if (typeIndex == (int)ParameterType.Color) valueNameStr = "colorValue";
			else if (typeIndex == (int)ParameterType.Double) valueNameStr = "doubleValue";
			else if (typeIndex == (int)ParameterType.Float) valueNameStr = "floatValue";
			else if (typeIndex == (int)ParameterType.Integer) valueNameStr = "intValue";
			else if (typeIndex == (int)ParameterType.Long) valueNameStr = "longValue";
			else if (typeIndex == (int)ParameterType.None) return;
			else if (typeIndex == (int)ParameterType.Object) valueNameStr = "objectReferenceValue";
			else if (typeIndex == (int)ParameterType.Quaternion)
			{
				isQuaternion = true;
				valueNameStr = "quaternionValue";
			}
			else if (typeIndex == (int)ParameterType.Rect) valueNameStr = "rectValue";
			else if (typeIndex == (int)ParameterType.Slider01)
            {
                isSlider = true;
                valueNameStr = "floatValue";
            }
			else if (typeIndex == (int)ParameterType.String) valueNameStr = "stringValue";
			else if (typeIndex == (int)ParameterType.Vector2) valueNameStr = "vector2Value";
			else if (typeIndex == (int)ParameterType.Vector3) valueNameStr = "vector3Value";
			else if (typeIndex == (int)ParameterType.Vector4) valueNameStr = "vector4Value";
			SerializedProperty cpValue = cp.FindPropertyRelative(valueNameStr);

			if (isQuaternion)
			{
				Vector4 quatV4 = new Vector4(cpValue.quaternionValue.x, cpValue.quaternionValue.y, cpValue.quaternionValue.z, cpValue.quaternionValue.w);
                EditorGUI.BeginChangeCheck();
                quatV4 = EditorGUI.Vector4Field(valueRect, GUIContent.none, quatV4);
                if (EditorGUI.EndChangeCheck()) cpValue.quaternionValue = new Quaternion(quatV4.x, quatV4.y, quatV4.z, quatV4.w);
			}
            else if (isSlider) cpValue.floatValue = EditorGUI.Slider(valueRect, GUIContent.none, cpValue.floatValue, 0, 1);
			else EditorGUI.PropertyField(valueRect, cpValue, GUIContent.none);
		}
    }
}