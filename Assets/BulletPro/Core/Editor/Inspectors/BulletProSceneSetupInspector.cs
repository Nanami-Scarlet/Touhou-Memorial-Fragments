using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CustomEditor(typeof(BulletProSceneSetup))]
	public class BulletProSceneSetupInspector : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.HelpBox("This object just needs to exist once in your scene.\n"+
            "Your whole gameplay is contained in this object's XY plane.\n"+
            "You can rotate this object to rotate your gameplay.", MessageType.Info);
		}
	}
}

