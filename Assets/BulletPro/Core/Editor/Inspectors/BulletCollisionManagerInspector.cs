using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(BulletCollisionManager))]
	public class BulletCollisionManagerInspector : Editor
	{
		BulletCollisionManager[] bcm;

		public void OnEnable()
		{
			bcm = new BulletCollisionManager[targets.Length];
			for (int i = 0; i < bcm.Length; i++)
			{
				bcm[i] = targets[i] as BulletCollisionManager;
				//EditorUtility.SetDirty(bcm[i]);
			}
		}

		public override void OnInspectorGUI()
		{
			GUILayout.Space(12);

			BulletProSettings bcs = Resources.Load("BulletProSettings") as BulletProSettings;
			if (bcs == null)
			{
				EditorGUILayout.HelpBox("Error : Collision Tags should be stored in BulletProSettings.asset.\nThe file has not been found in your Resources folder.", MessageType.Error);
				Color defB = GUI.color;
				GUI.color = new Color(0.6f, 0.9f, 1f, 1f);
				if (GUILayout.Button("Manage BulletPro Tags (this will solve the error by creating the file)"))
					bcs = BulletProAssetCreator.CreateCollisionSettingsAsset();
				GUI.color = defB;
			}
			else
			{
				//EditorGUILayout.HelpBox("This script just needs to exist once in your scene.\nCollision Tags are stored in BulletProSettings.asset.\nClick this blue button to edit them.",MessageType.Info);
				Color defB = GUI.color;
				GUI.color = new Color(0.6f, 0.9f, 1f, 1f);
				if (GUILayout.Button("Manage Collision Tags", EditorStyles.miniButton))
				{
					#if UNITY_2018_3_OR_NEWER
					SettingsService.OpenProjectSettings("Project/Bullet Pro");
					#else
					EditorGUIUtility.PingObject(bcs);
					EditorUtility.FocusProjectWindow();
					Selection.activeObject = bcs;
					#endif
				}
				GUI.color = defB;
			}
		}
	}
}