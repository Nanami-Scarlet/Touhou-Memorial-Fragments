using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CustomPropertyDrawer(typeof(CollisionTagLabels))]
	public class CollisionTagLabelsDrawer : PropertyDrawer
	{
		int textFieldWidth = 80;
		int horizontalSpace = 5;
		int verticalSpace = 5;
		int indentationValue = 32;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float lineHeight = EditorGUIUtility.singleLineHeight + 1 + verticalSpace;
			return lineHeight * Mathf.Ceil(32/GetNumberOfTagsPerLine());
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty strings = property.FindPropertyRelative("tags");
			strings.arraySize = 32;

			int tags = GetNumberOfTagsPerLine();
			float lineHeight = EditorGUIUtility.singleLineHeight + 1;
			float numberOfLines = Mathf.Ceil(32/tags);

			GUIStyle hollowStyle = new GUIStyle(EditorStyles.textField);
			Color col = hollowStyle.normal.textColor;
			hollowStyle.normal.textColor = new Color(col.r, col.g, col.b, 0.5f);

			for (int j=0; j<numberOfLines; j++)
			{
				Rect[] rects = new Rect[2*tags+1];

				float sumOfPrevWidths = indentationValue;
				float y = position.y + (lineHeight+verticalSpace) * j;

				for (int i=0; i<rects.Length; i++)
				{
					float width = i % 2 == 0 ? textFieldWidth : horizontalSpace;
					rects[i] = new Rect(position.x + sumOfPrevWidths, y, width, lineHeight);
					sumOfPrevWidths += width;

					if (i % 2 == 0)
					{
						int curIndex = j*(tags+1) + i/2;
						if (curIndex > 31) break;
						SerializedProperty prop = strings.GetArrayElementAtIndex(curIndex);

						string defaultTag = "Tag "+curIndex.ToString();
						
						Color defC = GUI.color;
						bool hollow = prop.stringValue == defaultTag;
						if (hollow) GUI.color = new Color(defC.r,defC.g,defC.b,0.8f);
						else GUI.color = new Color(0.7f, 1f, 1f, 1);
						prop.stringValue = EditorGUI.TextField(rects[i], prop.stringValue, hollow?hollowStyle:EditorStyles.textField);
						GUI.color = defC;

						if (string.IsNullOrEmpty(prop.stringValue))
						{
							if (curIndex==0) defaultTag = "Player";
							if (curIndex==1) defaultTag = "Enemy";
							prop.stringValue = defaultTag;
						}
					}
				}
			}
		}

		public int GetNumberOfTagsPerLine()
		{
			return 3;

			// Changed my mind : 8*4 display is less ugly than the responsive version.
			// (Feeling curious? uncomment this)

			/* *

			float total = EditorGUIUtility.currentViewWidth - indentationValue;
			int displayableTagsPerWidth = 0;
			float usedWidth = 0;

			while(usedWidth < total)
			{
				displayableTagsPerWidth++;
				usedWidth += textFieldWidth;
				if (usedWidth + horizontalSpace + textFieldWidth > total) break;
				usedWidth += horizontalSpace;
			}

			return displayableTagsPerWidth-1;

			/* */
		}
	}
}