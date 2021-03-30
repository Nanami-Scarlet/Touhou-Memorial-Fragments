using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	// This script embodies the editor's Start and Update function.
	[InitializeOnLoad]
	public class BulletProUpdater
	{
		// Whenever a new EmitterProfile is selected, it is enqueued by the AssetPostProcessor and treated next frame.
		public static Queue<string> lastProfilesCreated;

		// Start
		static BulletProUpdater()
		{
			lastProfilesCreated = new Queue<string>();
			EditorApplication.update += Update;
		}
		
		// The Update is kept as light as possible. It is only called when the queue isn't empty,
		// which is only the 2-3 frames following the creation of a new EmitterProfile.
		static void Update()
		{
			if (lastProfilesCreated.Count > 0)
				UpdateEmitterProfileCreationQueue();
		}

		#region EmitterProfile queue handling

		// Called by Update
		static void UpdateEmitterProfileCreationQueue()
		{
			while (lastProfilesCreated.Count > 0)
			{
				// waits for the new item to be considered as existing before treating it
				EmitterProfile newEP = AssetDatabase.LoadMainAssetAtPath(lastProfilesCreated.Peek()) as EmitterProfile;
				if (newEP == null) break;
				SelectNewProfile(newEP);
				lastProfilesCreated.Dequeue();
			}
		}

		// Called by the AssetPostProcessor to treat and select newly-created EmitterProfiles.
		public static void RegisterNewProfile(string path)
		{
			lastProfilesCreated.Enqueue(path);
		}

		// Actual profile treatment : it is cleaned, given random seeds, and finally selected.
		static void SelectNewProfile(EmitterProfile profile)
		{
			profile.currentParamsSelected = null;
			EditorUtility.SetDirty(profile);
			Selection.activeObject = profile;
		}

		#endregion
	}
}