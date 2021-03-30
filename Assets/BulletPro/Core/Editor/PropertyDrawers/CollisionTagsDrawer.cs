using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CustomPropertyDrawer(typeof(CollisionTags))]
	public class CollisionTagsDrawer : PropertyDrawer
	{
		int maxButtonWidth = 80;
		int horizontalSpace = 5;
		int verticalSpace = 5;
		int indentationValue = 16;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float lineHeight = EditorGUIUtility.singleLineHeight + 1 + verticalSpace;

			// Changed my mind : 8*4 display is less ugly than the responsive version.
			// (Feeling curious? uncomment this)

			//float lineAmount = Mathf.Ceil(32/GetNumberOfTagsPerLine());
			//return lineHeight * lineAmount;

			float lines = 8;
			float extraWarning = property.hasMultipleDifferentValues ? EditorGUIUtility.singleLineHeight + 1 + verticalSpace + 5 : 0;

			return lineHeight * lines + verticalSpace + extraWarning;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			//float totalWidth = EditorGUIUtility.currentViewWidth - indentationValue;
			float totalWidth = position.width - indentationValue;
			float buttonWidth = Mathf.Min(maxButtonWidth, (totalWidth - 3*horizontalSpace)*0.25f);

			SerializedProperty tagList = property.FindPropertyRelative("tagList");

			int tags = GetNumberOfTagsPerLine();
			float lineHeight = EditorGUIUtility.singleLineHeight + 1;
			float numberOfLines = Mathf.Ceil(32/tags);

			string[] tagNames = new string[32];
			string[] defaultTagNames = new string[32];
			for (int i=0; i<32; i++) defaultTagNames[i] = "Tag "+i.ToString();
			
			BulletProSettings bcs = Resources.Load("BulletProSettings") as BulletProSettings;
			bool invalid = false;
			if (bcs != null)
			{
				tagNames = bcs.collisionTags.tags;
				// Double-check : tagNames might actually be null
				if (tagNames == null) invalid = true;
				else if (tagNames.Length != 32) invalid = true;
			}
			else invalid = true;
			if (invalid)
			{
				tagNames = defaultTagNames;
				tagNames[0] = "Player";
				tagNames[1] = "Enemy";
			}

			// Style for pressed buttons
			GUIStyle focusedButton = new GUIStyle(EditorStyles.miniButton);
			focusedButton.normal = focusedButton.active;

			// Style for unpressed buttons with default tag
			GUIStyle hollowButton = new GUIStyle(EditorStyles.miniButton);
			Color col = hollowButton.normal.textColor;
			hollowButton.normal.textColor = new Color(col.r, col.g, col.b, 0.5f);

			float heightUsed = 0;
			if (property.hasMultipleDifferentValues)
			{
				float width = 4*buttonWidth + 3*horizontalSpace;
				Rect rect = new Rect(position.x + indentationValue, verticalSpace + position.y, width, lineHeight);
				heightUsed += lineHeight + 5;
				EditorGUI.HelpBox(rect, "Not all selected objects have the same Collision Tags.", MessageType.Warning);
			}

			for (int j=0; j<numberOfLines; j++)
			{
				Rect[] rects = new Rect[2*tags+1];

				float sumOfPrevWidths = indentationValue;
				float y = heightUsed + verticalSpace + position.y + (lineHeight+verticalSpace) * j;

				for (int i=0; i<rects.Length; i++)
				{
					float width = i % 2 == 0 ? buttonWidth : horizontalSpace;
					rects[i] = new Rect(position.x + sumOfPrevWidths, y, width, lineHeight);
					sumOfPrevWidths += width;

					if (i % 2 == 0)
					{
						int curIndex = j*(tags+1) + i/2;
						if (curIndex > 31) break;
						
						uint tagListValue = (uint)tagList.longValue;

						CollisionTags ct = new CollisionTags();
						ct.tagList = tagListValue;
						
						GUIStyle styleToUse = EditorStyles.miniButton;
						if (tagNames[curIndex] == defaultTagNames[curIndex] && !ct[curIndex]) styleToUse = hollowButton;
						if (ct[curIndex]) styleToUse = focusedButton;

						#if UNITY_2019_3_OR_NEWER
						Color defC = GUI.color;
						if (ct[curIndex]) GUI.color *= new Color(0.5f, 1f, 0.5f, 1f);
						#endif
						
						EditorGUI.BeginChangeCheck();
						// commented : alternate version with tooltips (seemed too heavy, but would work)
						//if (GUI.Button(rects[i], new GUIContent(tagNames[curIndex], tagNames[curIndex]), styleToUse))
						if (GUI.Button(rects[i], tagNames[curIndex], styleToUse))
							ct[curIndex] = !ct[curIndex];

						#if UNITY_2019_3_OR_NEWER
						GUI.color = defC;
						#endif

						tagListValue = ct.tagList;
						if (EditorGUI.EndChangeCheck())
							tagList.longValue = tagListValue;
					}
				}
			}

			EditorGUI.indentLevel = oldIndent;
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
				usedWidth += maxButtonWidth;
				if (usedWidth + horizontalSpace + maxButtonWidth > total) break;
				usedWidth += horizontalSpace;
			}

			return displayableTagsPerWidth-1;

			/* */
		}
	}
}