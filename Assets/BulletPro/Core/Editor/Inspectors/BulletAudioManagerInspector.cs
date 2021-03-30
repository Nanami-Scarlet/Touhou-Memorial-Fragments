using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(BulletAudioManager))]
	public class BulletAudioManagerInspector : Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox("This script just needs to exist once in your scene.\nAttach a few AudioSources to this object, so a sound effect can play when bullets are shot.",MessageType.Info);
			//EditorGUILayout.LabelField("This script just needs to exist once in your scene.");
			//EditorGUILayout.LabelField("Attach a few AudioSources to this object,");
			//EditorGUILayout.LabelField("so a sound effect can play when bullets are shot.");

			base.OnInspectorGUI();
		}
	}

}