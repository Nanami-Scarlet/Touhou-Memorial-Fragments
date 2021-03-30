using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CustomPropertyDrawer(typeof(BulletOutputTexture))]
	public class BulletOutputTextureDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			SerializedProperty texWidth = property.FindPropertyRelative("widthInWindow");
			SerializedProperty hasCompactEditor = property.FindPropertyRelative("hasCompactEditor");

			float w = EditorGUIUtility.currentViewWidth;
			if (!hasCompactEditor.boolValue) w -= 190; // the 190 refers to EmissionProfileInspector.leftSideWidth

			return base.GetPropertyHeight(property, label) * 3 + 12 + w * texWidth.floatValue;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// GUI gets somehow drawn twice over a (0,0,1,1) rect. Is this a bug from Unity?
			if (position.width < 2) return;

			EditorGUI.BeginProperty(position, label, property);

			Rect widthRect = new Rect(position.x, position.y, position.width, 16);
			SerializedProperty texWidth = property.FindPropertyRelative("widthInWindow");
			texWidth.floatValue = EditorGUI.Slider(widthRect, "Preview Size", texWidth.floatValue, 0.1f, 1f);
			float w = texWidth.floatValue;

			#region orientation

			Rect orientationRect = new Rect(position.x, position.y + 18, position.width, 20);
			string[] options = new string[]
			{
				"Up - Vertical gameplay, character",
				"Down - Vertical gameplay, enemy",
				"Left - Horizontal gameplay, enemy",
				"Right - Horizontal gameplay, character"
			};
			
			SerializedProperty orientation = property.FindPropertyRelative("orientation");
			float ws = 5; // space
			float w2 = 220; // enum
			//float w1 = 200; // label
			Rect enumRect = new Rect(orientationRect.x+position.width-w2, orientationRect.y, w2, orientationRect.height);
			Rect labelOrientationRect = new Rect(orientationRect.x, orientationRect.y, position.width-(w2+ws), orientationRect.height);

			EditorGUI.LabelField(labelOrientationRect, "Show bullets as shot towards");
			EditorGUI.BeginChangeCheck();
			orientation.enumValueIndex = EditorGUI.Popup(enumRect, orientation.enumValueIndex, options);
			if (EditorGUI.EndChangeCheck()) property.FindPropertyRelative("justChangedOrientation").boolValue = true;

			#endregion

			Rect ingameWidthRect = new Rect(position.x, position.y + 36, position.width, 20);
			SerializedProperty ingameWidth = property.FindPropertyRelative("ingameDistance");
			string plural = ingameWidth.floatValue > 1 ? "s" : "";
			EditorGUI.LabelField(ingameWidthRect, "For scale : ingame, this preview area is " + ingameWidth.floatValue.ToString() + " unit" + plural + " long.", EditorStyles.boldLabel);

			Rect texRect = new Rect(position.x + position.width * (1 - w) * 0.5f, position.y + 56, position.width * w, position.width * w);

			SerializedProperty texProp = property.FindPropertyRelative("tex");
			Texture2D tex = texProp.objectReferenceValue as Texture2D;

			if (tex != null)
				EditorGUI.DrawPreviewTexture(texRect, tex);

			// Selection rectangles are being processed and drawn here
			ProcessInteractions(texRect, property, label);

			//property.intValue = EditorGUI.MaskField(position, label, property.intValue, property.enumNames);
			EditorGUI.EndProperty();
		}

		void ProcessInteractions(Rect texRect, SerializedProperty property, GUIContent label)
		{
			Event ev = Event.current;
			SerializedProperty orientation = property.FindPropertyRelative("orientation");
			SerializedProperty mouseDown = property.FindPropertyRelative("hasMouseDown");
			SerializedProperty holdsMultiSelect = property.FindPropertyRelative("holdsMultiSelect");
			SerializedProperty isDoneWithSelection = property.FindPropertyRelative("isDoneWithSelection");
			SerializedProperty isEditingSelection = property.FindPropertyRelative("isEditingSelection");
			SerializedProperty selectionRects = property.FindPropertyRelative("selectionRects"); // the ones currently driven by mouse
			SerializedProperty selectionRectsFromExistingMods = property.FindPropertyRelative("selectionRectsFromExistingMods"); // the preexisting ones
			SerializedProperty mouseStartPoint = property.FindPropertyRelative("mouseStartPoint");
			SerializedProperty mouseEndPoint = property.FindPropertyRelative("mouseEndPoint");
			SerializedProperty currentColor = property.FindPropertyRelative("currentColor");
			isDoneWithSelection.boolValue = false;

			bool mouseInsideRect = ev.mousePosition.x > texRect.xMin && ev.mousePosition.x < texRect.xMax && ev.mousePosition.y > texRect.yMin && ev.mousePosition.y < texRect.yMax;

			// On pressing mouse
			if (ev.type == EventType.MouseDown && mouseInsideRect)
			{
				mouseDown.boolValue = true;
				
				if (selectionRects.arraySize == 0)
					property.FindPropertyRelative("indexOfFirstUndo").intValue = Undo.GetCurrentGroup();

				// we're clicking so it's time to create a new rect. Init new color if it's the first rect of this selection.
				if (selectionRects.arraySize == 0 && !isEditingSelection.boolValue)
				{
					// exclude hue from 0.5 to 0.85 so it doesn't blend into the background
					float hue = (0.85f + Random.value * 0.65f) % 1.0f;
					currentColor.colorValue = Random.ColorHSV(hue, hue, 0.5f, 1, 0.7f, 0.9f, 0.4f, 0.4f);
				}
				
				selectionRects.arraySize++;
				// selection rects are more convenient in the range [0,1]
				mouseStartPoint.vector2Value = new Vector2(Mathf.InverseLerp(texRect.xMin, texRect.xMax, ev.mousePosition.x), Mathf.InverseLerp(texRect.yMin, texRect.yMax, ev.mousePosition.y));
			}
			
			// On moving mouse
			if (ev.type == EventType.MouseDrag && mouseDown.boolValue)
			{
				// selection rects are more convenient in the range [0,1]
				mouseEndPoint.vector2Value = new Vector2(Mathf.InverseLerp(texRect.xMin, texRect.xMax, ev.mousePosition.x), Mathf.InverseLerp(texRect.yMin, texRect.yMax, ev.mousePosition.y));

				// updating the rect
				float x = Mathf.Min(mouseStartPoint.vector2Value.x, mouseEndPoint.vector2Value.x);
				float y = Mathf.Min(mouseStartPoint.vector2Value.y, mouseEndPoint.vector2Value.y);
				float w = Mathf.Abs(mouseStartPoint.vector2Value.x - mouseEndPoint.vector2Value.x);
				float h = Mathf.Abs(mouseStartPoint.vector2Value.y - mouseEndPoint.vector2Value.y);

				// cancel right/left/down orientation before writing to serializedproperty
				Rect rect = new Rect(x, y, w, h);
				int quarters = 0;
				if (orientation.enumValueIndex == (int)ShotTextureOrientation.Left) quarters = 3;
				if (orientation.enumValueIndex == (int)ShotTextureOrientation.Down) quarters = 2;
				if (orientation.enumValueIndex == (int)ShotTextureOrientation.Right) quarters = 1;
				while (quarters > 0)
				{
					quarters--;
					rect = Rotate90CCW(rect);
				}

				selectionRects.GetArrayElementAtIndex(selectionRects.arraySize-1).rectValue = rect;
			}
			
			// On releasing mouse
			if (ev.type == EventType.MouseUp && mouseDown.boolValue)
			{
				mouseDown.boolValue = false;

				// updating the rect (one last time)
				mouseEndPoint.vector2Value = new Vector2(Mathf.InverseLerp(texRect.xMin, texRect.xMax, ev.mousePosition.x), Mathf.InverseLerp(texRect.yMin, texRect.yMax, ev.mousePosition.y));
				float x = Mathf.Min(mouseStartPoint.vector2Value.x, mouseEndPoint.vector2Value.x);
				float y = Mathf.Min(mouseStartPoint.vector2Value.y, mouseEndPoint.vector2Value.y);
				float w = Mathf.Abs(mouseStartPoint.vector2Value.x - mouseEndPoint.vector2Value.x);
				float h = Mathf.Abs(mouseStartPoint.vector2Value.y - mouseEndPoint.vector2Value.y);
				
				// cancel right/left/down orientation before writing to serializedproperty
				Rect rect = new Rect(x, y, w, h);
				int quarters = 0;
				if (orientation.enumValueIndex == (int)ShotTextureOrientation.Left) quarters = 3;
				if (orientation.enumValueIndex == (int)ShotTextureOrientation.Down) quarters = 2;
				if (orientation.enumValueIndex == (int)ShotTextureOrientation.Right) quarters = 1;
				while (quarters > 0)
				{
					quarters--;
					rect = Rotate90CCW(rect);
				}

				selectionRects.GetArrayElementAtIndex(selectionRects.arraySize-1).rectValue = rect;
				
				
				// ends selection if the multi-select key is not held
				if (!Event.current.shift)
				{
					isDoneWithSelection.boolValue = true;
					holdsMultiSelect.boolValue = false;
				}
				// else, mark it as held so that its releasing gets tracked
				else
					holdsMultiSelect.boolValue = true;		

				// flushing the array of selection rects is done in ShotParamInspector.cs after making sure it's used
			}

			// end if shift is released after the mouse
			if (!ev.shift && holdsMultiSelect.boolValue && !mouseDown.boolValue)
			{
				isDoneWithSelection.boolValue = true;
				holdsMultiSelect.boolValue = false;
			}

			// Drawing selection rects (the ones we see before releasing the mouse)
			if (selectionRects.arraySize > 0)
				for (int i = 0; i < selectionRects.arraySize; i++)
					DrawRect01(selectionRects.GetArrayElementAtIndex(i).rectValue, texRect, currentColor.colorValue, (ShotTextureOrientation)orientation.enumValueIndex);

			// Drawing previously existing selection rects
			if (selectionRectsFromExistingMods.arraySize > 0)
				for (int i = 0; i < selectionRectsFromExistingMods.arraySize; i++)
				{
					SerializedProperty prop = selectionRectsFromExistingMods.GetArrayElementAtIndex(i);
					SerializedProperty rects = prop.FindPropertyRelative("selectionRects");
					SerializedProperty color = prop.FindPropertyRelative("color");
					if (rects.arraySize > 0)
						for (int j = 0; j < rects.arraySize; j++)
							DrawRect01(rects.GetArrayElementAtIndex(j).rectValue, texRect, color.colorValue, (ShotTextureOrientation)orientation.enumValueIndex);
				}
		}

		// Draws a rectangle rect relative to texRect using x=0 for left border, x=1 for right border, y=0 for top and y=1 for bottom.
		void DrawRect01(Rect rect, Rect texRect, Color color, ShotTextureOrientation orientation)
		{
			// applying orientation
			int quarters = 0;
			if (orientation == ShotTextureOrientation.Left) quarters = 1;
			if (orientation == ShotTextureOrientation.Down) quarters = 2;
			if (orientation == ShotTextureOrientation.Right) quarters = 3;
			while (quarters > 0)
			{
				quarters--;
				rect = Rotate90CCW(rect);
			}

			//rect = new Rect(Mathf.Max(0f, rect.x), Mathf.Max(0f, rect.y), Mathf.Min(rect.width, 1f), Mathf.Min(rect.height, 1f));
			rect.xMin = Mathf.Clamp01(rect.xMin);
			rect.yMin = Mathf.Clamp01(rect.yMin);
			rect.xMax = Mathf.Clamp01(rect.xMax);
			rect.yMax = Mathf.Clamp01(rect.yMax);
			Rect finalRect = new Rect(texRect.xMin + texRect.width*rect.x, texRect.yMin + texRect.height*rect.y, texRect.width*rect.width, texRect.height*rect.height);
			EditorGUI.DrawRect(finalRect, color);
		}

		// Warning : these rects are in GUI coordinates, which means (0,0) is top-left.
		Rect Rotate90CCW(Rect rect)
		{
			return new Rect(rect.y, 1-(rect.x+rect.width), rect.height, rect.width);
		}
	}
}
