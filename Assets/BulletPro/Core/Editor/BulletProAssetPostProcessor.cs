using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BulletPro;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	public class BulletProAssetPostProcessor : AssetPostprocessor
	{
		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			BulletProSettings bps = Resources.Load("BulletProSettings") as BulletProSettings;
			if (bps == null) return;
				//bps = BulletProAssetCreator.CreateCollisionSettingsAsset(false);

			foreach (string str in importedAssets)
			{
				// only retain ScriptableObjects
				if (!str.EndsWith(".asset")) continue;
				
				// Only retain EmitterProfiles
				System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(str);
				if (assetType.ToString() != "BulletPro.EmitterProfile") continue;
				
				// Get newly created asset. If flag is already set to true, it's a duplicated one, not a created one
				EmitterProfile ep = AssetDatabase.LoadMainAssetAtPath(str) as EmitterProfile;
				if (ep.hasBeenProcessed) continue;

				ep.hasBeenProcessed = true;

				// No default profile specified ? this new instance can exist as is.
				if (bps.defaultEmitterProfile == null)
				{
					EditorUtility.SetDirty(ep);
					continue;
				}

				// make this newly created profile a copy of bps.defaultEmitterProfile
				string referencePath = AssetDatabase.GetAssetPath(bps.defaultEmitterProfile);
				AssetDatabase.CopyAsset(referencePath, str);
				BulletProUpdater.RegisterNewProfile(str);

				/* *
				Debug.Log(result);
				Debug.Log(str);
				EmitterProfile newEP = AssetDatabase.LoadMainAssetAtPath(str) as EmitterProfile;
				newEP.name = oldName;
				EditorUtility.SetDirty(newEP);
				Selection.activeObject = newEP;
				/* */
					
				
			}
		}
	}
}