using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CustomPropertyDrawer(typeof(BulletPreview))]
	public class BulletPreviewDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			int extraLines = 1;
			float square = Mathf.Min(EditorGUIUtility.currentViewWidth * 0.5f, 128);
			return square + extraLines * (1 + EditorGUIUtility.singleLineHeight);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			Rect color1 = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(color1, property.FindPropertyRelative("gizmoColor"));

			float squareSize = Mathf.Min(EditorGUIUtility.currentViewWidth * 0.5f, 128);

			Rect square = new Rect(position.x + (position.width - squareSize) * 0.5f, position.y + EditorGUIUtility.singleLineHeight + 1, squareSize, squareSize);

			// background
			Texture2D bg = new Texture2D(1, 1);
			EditorGUI.DrawTextureTransparent(square, bg);

			// bullet sprite
			Sprite sprite = property.FindPropertyRelative("sprite").objectReferenceValue as Sprite;
			DrawSprite(square, sprite);

			// colliders
			GUI.DrawTexture(square, property.FindPropertyRelative("collidersTex").objectReferenceValue as Texture, ScaleMode.ScaleToFit, true);

			EditorGUI.EndProperty();
		}

		// Thanks to Krucho for figuring this function out on the Unity forums!
		private void DrawSprite(Rect position, Sprite sprite)
		{
			Vector2 fullSize = new Vector2(sprite.texture.width, sprite.texture.height);
			Vector2 size = new Vector2(sprite.textureRect.width, sprite.textureRect.height);

			Rect coords = sprite.textureRect;
			coords.x /= fullSize.x;
			coords.width /= fullSize.x;
			coords.y /= fullSize.y;
			coords.height /= fullSize.y;

			Vector2 ratio;
			ratio.x = position.width / size.x;
			ratio.y = position.height / size.y;
			float minRatio = Mathf.Min(ratio.x, ratio.y);

			Vector2 center = position.center;
			position.width = size.x * minRatio;
			position.height = size.y * minRatio;
			position.center = center;

			GUI.DrawTextureWithTexCoords(position, sprite.texture, coords, true);
		}
	}
}
