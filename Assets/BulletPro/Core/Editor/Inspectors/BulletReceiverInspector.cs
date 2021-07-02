using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(BulletReceiver))]
	public class BulletReceiverInspector : Editor
	{
		BulletReceiver[] brs;
		SerializedProperty colliderType, hitboxSize, hitboxOffset;
		SerializedProperty killBulletOnCollision, maxSimultaneousCollisionsPerFrame;
		SerializedProperty collisionTags, collisionTagsFoldout, OnHitByBullet, gizmoColor;
		SerializedProperty advancedEventsFoldout, OnHitByBulletEnter, OnHitByBulletStay, OnHitByBulletExit;

		void OnEnable()
		{
			brs = new BulletReceiver[targets.Length];
			for (int i = 0; i < brs.Length; i++)
				brs[i] = targets[i] as BulletReceiver;


			//self = serializedObject.FindProperty("self");
			colliderType = serializedObject.FindProperty("colliderType");
			hitboxSize = serializedObject.FindProperty("hitboxSize");
			hitboxOffset = serializedObject.FindProperty("hitboxOffset");
			killBulletOnCollision = serializedObject.FindProperty("killBulletOnCollision");
			maxSimultaneousCollisionsPerFrame = serializedObject.FindProperty("maxSimultaneousCollisionsPerFrame");
			collisionTags = serializedObject.FindProperty("collisionTags");
			collisionTagsFoldout = serializedObject.FindProperty("collisionTagsFoldout");
			OnHitByBullet = serializedObject.FindProperty("OnHitByBullet");
			gizmoColor = serializedObject.FindProperty("gizmoColor");

			advancedEventsFoldout = serializedObject.FindProperty("advancedEventsFoldout");
			OnHitByBulletEnter = serializedObject.FindProperty("OnHitByBulletEnter");
			OnHitByBulletStay = serializedObject.FindProperty("OnHitByBulletStay");
			OnHitByBulletExit = serializedObject.FindProperty("OnHitByBulletExit");
		}

		public override void OnInspectorGUI()
		{
			GUILayout.Space(16);
			
			// Unnecessary help that takes too much space
			//EditorGUILayout.HelpBox("This is the component you typically place on players, enemies, anything that can get hit by a bullet.", MessageType.Info);
			//GUILayout.Space(6);

			serializedObject.Update();

			//EditorGUILayout.Space();
			//EditorGUILayout.LabelField("Basic Collision Info", EditorStyles.boldLabel);
			//EditorGUILayout.PropertyField(self);
			EditorGUILayout.PropertyField(colliderType);
			EditorGUILayout.PropertyField(hitboxSize);
			EditorGUILayout.PropertyField(hitboxOffset);
			EditorGUILayout.PropertyField(killBulletOnCollision);
			EditorGUILayout.PropertyField(maxSimultaneousCollisionsPerFrame);

			collisionTagsFoldout.boolValue = EditorGUILayout.Foldout(collisionTagsFoldout.boolValue, "Collision Tags", true);
			if (collisionTagsFoldout.boolValue)
			{
				EditorGUILayout.PropertyField(collisionTags);
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUIUtility.currentViewWidth*0.4f);
				Color defC = GUI.color;
				GUI.color = new Color(0.6f, 1f, 1f, 1f);
				if (GUILayout.Button("Manage Tags", EditorStyles.miniButton))
				{
					BulletProSettings bcs = Resources.Load("BulletProSettings") as BulletProSettings;
					if (bcs == null)
						bcs = BulletProAssetCreator.CreateCollisionSettingsAsset();
					else
					{
						#if UNITY_2018_3_OR_NEWER
						SettingsService.OpenProjectSettings("Project/Bullet Pro");
						#else
						EditorGUIUtility.PingObject(bcs);
						EditorUtility.FocusProjectWindow();
						Selection.activeObject = bcs;
						#endif
					}
				}
				GUI.color = defC;
				EditorGUILayout.LabelField("");
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();

			int hasEmptyTags = 0;
			int hasCorrectTags = 0;
			for (int i = 0; i < brs.Length; i++)
			{
				if (brs[i].collisionTags.tagList == 0)
					hasEmptyTags++;
				else hasCorrectTags++;
			}
			if (hasEmptyTags > 0)
			{
				string str = "Selected object has no Collision Tags. It won't collide with anything.";
				if (brs.Length > 1)
				{
					if (hasEmptyTags == 1)
						str = "One of the selected objects has no Collision Tags. It won't collide with anything.";
					else if (hasCorrectTags > 0)
						str = "Some of the selected objects have no Collision Tags. They won't collide with anything.";
					else
						str = "Selected objects have no Collision Tags. They won't collide with anything.";
				}
				if (!collisionTagsFoldout.boolValue)
					str += "\nYou may need to unfold and activate the Collision Tags above.";
				else
					str += "\nYou may need to click on some Collision Tags above.";
				EditorGUILayout.HelpBox(str, MessageType.Warning);
			}

			EditorGUILayout.Space();

			//EditorGUILayout.LabelField("Events", EditorStyles.boldLabel); // (ugly UI, commented out)
			EditorGUILayout.PropertyField(OnHitByBullet);

			advancedEventsFoldout.boolValue = EditorGUILayout.Foldout(advancedEventsFoldout.boolValue, "Advanced Events (Enter, Stay, Exit)", true);
			if (advancedEventsFoldout.boolValue)
			{
				EditorGUILayout.PropertyField(OnHitByBulletEnter);
				EditorGUILayout.PropertyField(OnHitByBulletStay);
				EditorGUILayout.PropertyField(OnHitByBulletExit);
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Editor", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(gizmoColor);

			serializedObject.ApplyModifiedProperties();

			for (int i = 0; i < brs.Length; i++)
				if (brs[i].self == null)
				{
					brs[i].self = brs[i].transform;
					EditorUtility.SetDirty(brs[i]);
				}
		}
	}
}
