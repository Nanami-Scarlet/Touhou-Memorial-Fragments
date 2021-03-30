using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(BulletProSettings))]
	public class BulletProSettingsInspector : Editor
	{
		SerializedProperty tags;
		SerializedProperty defaultEmitterProfile;
		SerializedProperty maxAmountOfBullets, maxAmountOfReceivers;
		SerializedProperty collisionHandler, computeShaders, maximumAmountOfCollisionsPerFrame;

		void OnEnable()
		{
			tags = serializedObject.FindProperty("collisionTags");
			defaultEmitterProfile = serializedObject.FindProperty("defaultEmitterProfile");
			maximumAmountOfCollisionsPerFrame = serializedObject.FindProperty("maxAmountOfCollisionsPerFrame");
			computeShaders = serializedObject.FindProperty("computeShaders");
			collisionHandler = serializedObject.FindProperty("collisionHandler");
			maxAmountOfBullets = serializedObject.FindProperty("maxAmountOfBullets");
			maxAmountOfReceivers = serializedObject.FindProperty("maxAmountOfReceivers");
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(6);
			EditorGUILayout.BeginVertical();

			serializedObject.Update();

			EditorGUILayout.LabelField("Collision Tags", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("You can edit the strings used as Collision Tags here.\nOne same object can have multiple tags, thus belonging to multiple groups.", MessageType.Info);
			//EditorGUILayout.LabelField("You can edit the strings used as Collision Tags here.");
			//EditorGUILayout.LabelField("One same object can have multiple tags, thus belonging to multiple groups.");

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(tags);

			//EditorGUILayout.Space();

			EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(defaultEmitterProfile);
			EditorGUILayout.Space();
			
			EditorGUILayout.LabelField("Compute Shaders (GPU-based collisions)", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Compute Shaders provide huge performance boosts.\nThey're available for several common platforms (Windows, Android, Apple...)", MessageType.Info);
			//EditorGUILayout.LabelField("Compute Shaders provide huge performance boosts.");
			//EditorGUILayout.LabelField("They're available for several common platforms (Windows, Android, Apple...)");
			EditorGUILayout.PropertyField(collisionHandler);
			EditorGUILayout.PropertyField(computeShaders);
			serializedObject.ApplyModifiedProperties();
			int idx = computeShaders.enumValueIndex;

			EditorGUI.BeginDisabledGroup(idx == 2);
			EditorGUILayout.LabelField("Maximum amount (per frame, at once)");
			EditorGUI.indentLevel += 1;
			EditorGUILayout.PropertyField(maxAmountOfBullets, new GUIContent("of bullets :"));
			if (maxAmountOfBullets.intValue < 0) maxAmountOfBullets.intValue = 0;
			EditorGUILayout.PropertyField(maxAmountOfReceivers, new GUIContent("of receivers :"));
			if (maxAmountOfReceivers.intValue < 0) maxAmountOfReceivers.intValue = 0;
			EditorGUILayout.PropertyField(maximumAmountOfCollisionsPerFrame, new GUIContent("of collisions :"));
			EditorGUI.indentLevel -= 1;
			EditorGUI.EndDisabledGroup();

			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		}
	}
}