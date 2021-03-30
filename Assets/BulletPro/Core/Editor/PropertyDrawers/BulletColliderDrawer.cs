using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CustomPropertyDrawer(typeof(BulletCollider))]
	public class BulletColliderDrawer : PropertyDrawer
	{
		int oldIndent;

		// Caching fields and methods obtained via reflection
		MethodInfo doFloatFieldMethod;
		object recycledEditor;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float result = EditorGUIUtility.singleLineHeight + 1;

			//if (EditorGUIUtility.currentViewWidth < 400)
			// if (EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth < 230) result *= 2;

			return result * 2;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (recycledEditor == null) GetRecycledEditor();

			EditorGUI.BeginProperty(position, label, property);

			oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			// if (EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth < 230) OnGUITwoLines(position, property, label);
			// else OnGUIOneLine(position, property, label);

			DrawGUI(position, property, label);

			// TODO : could be optimized by iterating through SerializedProperties instead of calling Find()

			EditorGUI.indentLevel = oldIndent;

			EditorGUI.EndProperty();
		}

		void DrawGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			float h = EditorGUIUtility.singleLineHeight;
			float w = position.width;
			float x = position.x + oldIndent * 15;
			float y = position.y;
			float w1 = 50; // enum rect
			float space = 5;
			float nextX = x + w1 + space;
			float remainingWidth = position.width - (space + w1);

			Rect enumRect = new Rect(x, y, w1, h);
			Rect upperParamRect = new Rect(nextX, y, remainingWidth, h);
			Rect lowerParamRect = new Rect(nextX, y + h + 4, remainingWidth, h);

			/* *
			EditorGUI.DrawRect(position, Color.green);
			EditorGUI.DrawRect(enumRect, Color.red);
			EditorGUI.DrawRect(upperParamRect, Color.blue);
			EditorGUI.DrawRect(lowerParamRect, Color.yellow);
			/* */

			SerializedProperty colType = property.FindPropertyRelative("colliderType");
			EditorGUI.PropertyField(enumRect, colType, GUIContent.none);

			float oldLabelWidth = EditorGUIUtility.labelWidth;
			float oldFieldWidth = EditorGUIUtility.fieldWidth;
			EditorGUIUtility.labelWidth = 70f;
			EditorGUIUtility.fieldWidth *= 2f;
				
			if (colType.enumValueIndex == (int)BulletColliderType.Circle)
			{
				EditorGUI.PropertyField(upperParamRect, property.FindPropertyRelative("size"), new GUIContent("Radius"));
				SerializedProperty offsetProp = property.FindPropertyRelative("offset");
				EditorGUI.BeginChangeCheck();
				Vector2 newOffset = EditorGUI.Vector2Field(lowerParamRect, new GUIContent("Offset"), offsetProp.vector3Value);
				if (EditorGUI.EndChangeCheck())
					offsetProp.vector3Value = newOffset;

				// offset is stored as a Vector3 to make the buffer load faster in compute shader (CollisionManager.cs)
			}
			else // line
			{
				EditorGUI.PropertyField(upperParamRect, property.FindPropertyRelative("lineStart"), new GUIContent("Start"));
				EditorGUI.PropertyField(lowerParamRect, property.FindPropertyRelative("lineEnd"), new GUIContent("End"));
			}		

			EditorGUIUtility.labelWidth = oldLabelWidth;
			EditorGUIUtility.fieldWidth = oldFieldWidth;
		}

		public void GetRecycledEditor()
		{
			// Obtain internal methods for customizing float fields later on
			System.Type editorGUIType = typeof(EditorGUI);
			System.Type RecycledTextEditorType = Assembly.GetAssembly(editorGUIType).GetType("UnityEditor.EditorGUI+RecycledTextEditor");
			System.Type[] argumentTypes = new System.Type[] { RecycledTextEditorType, typeof(Rect), typeof(Rect), typeof(int), typeof(float), typeof(string), typeof(GUIStyle), typeof(bool) };
			doFloatFieldMethod = editorGUIType.GetMethod("DoFloatField", BindingFlags.NonPublic | BindingFlags.Static, null, argumentTypes, null);
			FieldInfo fieldInfo = editorGUIType.GetField("s_RecycledEditor", BindingFlags.NonPublic | BindingFlags.Static);
			recycledEditor = fieldInfo.GetValue(null);
		}

		#region unused old versions

		public void OnGUIOneLine(Rect position, SerializedProperty property, GUIContent label)
		{
			float h = EditorGUIUtility.singleLineHeight;
			float w = position.width;
			float x = position.x;
			float y = position.y;
			float w1 = 50; // enum rect
			float w3 = 26; // label rect
			float w7 = 9; // "X" or "Y" label
			float space = 5;
			float biggerSpace = 12; // between enum and "size" label
			float w5 = w - (15*oldIndent + w1 + w3 + biggerSpace + w7 + w7 + 5*space); // float field for size
			w5 *= 0.3333f; // we've got three fields

			Rect[] rects = new Rect[16];
			int i = 0;
			float curX = x + oldIndent*15;

			rects[i] = new Rect(curX, y, w1, h); curX += rects[i++].width + biggerSpace; // enum

			rects[i] = new Rect(curX, y, w3, h); curX += rects[i++].width + space; // "size"
			rects[i] = new Rect(curX, y, w5, h); curX += rects[i++].width + space; // float field

			rects[i] = new Rect(curX, y, w7, h); curX += rects[i++].width + space; // "x"
			rects[i] = new Rect(curX, y, w5, h); curX += rects[i++].width + space; // float field

			rects[i] = new Rect(curX, y, w7, h); curX += rects[i++].width + space; // "y"
			rects[i] = new Rect(curX, y, w5, h); curX += rects[i++].width + space; // float field"

			i = 0;

			EditorGUI.PropertyField(rects[i++], property.FindPropertyRelative("colliderType"), GUIContent.none);
			EditorGUI.LabelField(rects[i++], "Size");
			
			SerializedProperty sizeProp = property.FindPropertyRelative("size");
			Rect curRect = rects[i++];
			Rect dragZone = new Rect(curRect.x - 10, curRect.y, 15, curRect.height);
			sizeProp.floatValue = CustomFloatField(curRect, dragZone, sizeProp.floatValue);
			
			EditorGUI.LabelField(rects[i++], "X");
			SerializedProperty xProp = property.FindPropertyRelative("offset").FindPropertyRelative("x");
			curRect = rects[i++];
			dragZone = new Rect(curRect.x - 10, curRect.y, 15, curRect.height);
			xProp.floatValue = CustomFloatField(curRect, dragZone, xProp.floatValue);
			
			EditorGUI.LabelField(rects[i++], "Y");
			SerializedProperty yProp = property.FindPropertyRelative("offset").FindPropertyRelative("y");
			curRect = rects[i++];
			dragZone = new Rect(curRect.x - 10, curRect.y, 15, curRect.height);
			yProp.floatValue = CustomFloatField(curRect, dragZone, yProp.floatValue);
		}

		public void OnGUITwoLines(Rect position, SerializedProperty property, GUIContent label)
		{
			float h = EditorGUIUtility.singleLineHeight;
			float w = position.width;
			float x = position.x;
			float y = position.y;
			float w1 = 60; // enum rect
			float w3 = 30; // label rect
			float w7 = 12; // "X" or "Y" label
			float space = 5;
			float biggerSpace = 16; // between enum and "size" label
			float w5Upper = w - (15*oldIndent + w1 + w3 + biggerSpace + space);
			float w5Lower = w - (15*oldIndent + w1 + biggerSpace + 2*w7 + 3*space); // float field for size
			w5Lower *= 0.5f; // there's two fields

			Rect[] rects = new Rect[16];
			int i = 0;
			float curX = x + oldIndent*15;

			rects[i] = new Rect(curX, y, w1, h); curX += rects[i++].width + biggerSpace; // enum

			rects[i] = new Rect(curX, y, w3, h); curX += rects[i++].width + space; // "size"
			rects[i] = new Rect(curX, y, w5Upper, h); curX += rects[i++].width + space; // float field

			y += h + 1; curX = x + oldIndent*15 + w1 + biggerSpace; // next line

			rects[i] = new Rect(curX, y, w7, h); curX += rects[i++].width + space; // "x"
			rects[i] = new Rect(curX, y, w5Lower, h); curX += rects[i++].width + space; // float field

			rects[i] = new Rect(curX, y, w7, h); curX += rects[i++].width + space; // "y"
			rects[i] = new Rect(curX, y, w5Lower, h); curX += rects[i++].width + space; // float field

			i = 0;

			EditorGUI.PropertyField(rects[i++], property.FindPropertyRelative("colliderType"), GUIContent.none);
			EditorGUI.LabelField(rects[i++], "Size");

			SerializedProperty sizeProp = property.FindPropertyRelative("size");
			Rect curRect = rects[i++];
			Rect dragZone = new Rect(curRect.x - 10, curRect.y, 15, curRect.height);
			sizeProp.floatValue = CustomFloatField(curRect, dragZone, sizeProp.floatValue);
			
			EditorGUI.LabelField(rects[i++], "X");
			SerializedProperty xProp = property.FindPropertyRelative("offset").FindPropertyRelative("x");
			curRect = rects[i++];
			dragZone = new Rect(curRect.x - 10, curRect.y, 15, curRect.height);
			xProp.floatValue = CustomFloatField(curRect, dragZone, xProp.floatValue);
			
			EditorGUI.LabelField(rects[i++], "Y");
			SerializedProperty yProp = property.FindPropertyRelative("offset").FindPropertyRelative("y");
			curRect = rects[i++];
			dragZone = new Rect(curRect.x - 10, curRect.y, 15, curRect.height);
			yProp.floatValue = CustomFloatField(curRect, dragZone, yProp.floatValue);
		}

		#endregion

		// Float field with draggable zone, obtained via reflection
		private float CustomFloatField(Rect position, Rect dragHotZone, float value, GUIStyle style = null)
		{
			if (style == null) style = EditorStyles.numberField;
			int controlID = GUIUtility.GetControlID("EditorTextField".GetHashCode(), FocusType.Keyboard, position);

			object[] parameters = new object[] { recycledEditor, position, dragHotZone, controlID, value, "g7", style, true };

			return (float)doFloatFieldMethod.Invoke(null, parameters);
		}
	}
}
