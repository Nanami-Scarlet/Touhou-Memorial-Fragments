using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CustomEditor(typeof(BulletVFXManager))]
	public class BulletVFXManagerInspector : Editor
	{
		BulletVFXManager bm;
		bool bulletsHaveBeenReserialized;

		public void OnEnable()
		{
			bm = target as BulletVFXManager;
		}

		public override void OnInspectorGUI()
		{
			GUILayout.Space(12);

			bm.effectPool = new BulletVFX[bm.transform.childCount];

			bool wrap = EditorStyles.label.wordWrap;
			EditorStyles.label.wordWrap = true;
			//EditorGUILayout.HelpBox("This script just needs to exist once in your scene.", MessageType.Info);

			GameObject go = bm.gameObject;

			#if UNITY_2019_1_OR_NEWER
			bool isPrefabOriginal = ((UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null) || (Selection.GetFiltered<GameObject>(SelectionMode.ExcludePrefab).Length == 0));
			#elif UNITY_2018_3_OR_NEWER
			bool isPrefabOriginal = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null;			
			#elif UNITY_2018_2_OR_NEWER
			bool isPrefabOriginal = PrefabUtility.GetCorrespondingObjectFromSource(go) == null && PrefabUtility.GetPrefabObject(go.transform) != null;
			#else
			bool isPrefabOriginal = PrefabUtility.GetPrefabParent(go) == null && PrefabUtility.GetPrefabObject(go.transform) != null;
			#endif
			if (isPrefabOriginal)
			{
				EditorGUILayout.HelpBox("Currently inspected object is the prefab asset.\nThus, it can't contain a pool of objects.", MessageType.Info);
				//EditorGUILayout.LabelField("Currently inspected object is the prefab asset.");
				//EditorGUILayout.LabelField("Thus, it can't contain a pool of objects.");
				
				// Anyway, ask prefab reference and default FX style
				Undo.RecordObject(bm, "Modified Bullet VFX References");
				bm.vfxPrefab = EditorGUILayout.ObjectField("VFX Prefab", bm.vfxPrefab, typeof(GameObject), false) as GameObject;
				bm.defaultParticles = EditorGUILayout.ObjectField("Default VFX Style (Prefab)", bm.defaultParticles, typeof(ParticleSystem), false) as ParticleSystem;
				bm.defaultParticleRenderer = EditorGUILayout.ObjectField("Default VFX Renderer (Prefab)", bm.defaultParticleRenderer, typeof(ParticleSystemRenderer), false) as ParticleSystemRenderer;
				return;
			}

			// if there's something in the pool
			if (bm.effectPool.Length > 0)
			{
				bool missing = false;
				for (int i = 0; i < bm.effectPool.Length; i++)
				{
					bm.effectPool[i] = bm.transform.GetChild(i).GetComponent<BulletVFX>();
					if (bm.effectPool[i] == null) missing = true;
					else if (ReserializeVFX(bm.effectPool[i])) bulletsHaveBeenReserialized = true;
				}

				// if something's wrong
				if (missing)
					EditorGUILayout.HelpBox("Error: Some children of this object lack the BulletVFX Component!\nThis object should only have BulletVFX children.", MessageType.Error);
				

				// if everything's in place
				else
				{
					FontStyle defF = EditorStyles.label.fontStyle;
					Color defC = EditorStyles.label.onNormal.textColor;
					EditorStyles.label.fontStyle = FontStyle.Normal;
					EditorStyles.label.normal.textColor = new Color(0, 0.5f, 0, 1);
					if (EditorGUIUtility.isProSkin) EditorStyles.label.normal.textColor = new Color(0, 1, 0, 1);
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Current pool : " + bm.effectPool.Length.ToString() + " VFX.");
					if (GUILayout.Button("Add 100", EditorStyles.miniButton)) ModifyPool(100);
					if (GUILayout.Button("Remove 100", EditorStyles.miniButton)) ModifyPool(-100);
					EditorGUILayout.EndHorizontal();
					if (bulletsHaveBeenReserialized) EditorGUILayout.LabelField("Newly added VFX have been prepared.");
					EditorStyles.label.fontStyle = defF;
					EditorStyles.label.normal.textColor = defC;
				}
			}

			// if the pool is empty
			else
			{
				// Do we have the right prefab ?
				bool prefabNeeded = false;
				if (bm.vfxPrefab == null) prefabNeeded = true;
				else if (bm.vfxPrefab.GetComponent<BulletVFX>() == null) prefabNeeded = true;

				// text
				if (prefabNeeded) EditorGUILayout.HelpBox("You must first assign a valid BulletVFX Prefab object", MessageType.Error);
				else EditorGUILayout.HelpBox("This GameObject does not yet have any BulletVFX children.", MessageType.Warning);

				// button
				if (!prefabNeeded)
				{
					Color defB = GUI.color;
					GUI.color = new Color(0.8f, 1, 0.5f, 1);
					if (GUILayout.Button("Click here to create a pool of 1000 BulletVFX as children"))
					{
						Undo.RecordObject(bm, "Create Pool");

						List<BulletVFX> lb = new List<BulletVFX>();
						for (int i = 0; i < 1000; i++)
						{
							GameObject vfx = PrefabUtility.InstantiatePrefab(bm.vfxPrefab) as GameObject;
							vfx.transform.SetParent(bm.transform);
							BulletVFX b = vfx.GetComponent<BulletVFX>();
							ReserializeVFX(b);
							lb.Add(b);
							Undo.RegisterCreatedObjectUndo(vfx, "Create Pool");
						}
						bm.effectPool = lb.ToArray();
						EditorSceneManager.MarkAllScenesDirty();
					}
					GUI.color = defB;
				}
			}

			// We're done with all the verbose stuff
			EditorStyles.label.wordWrap = wrap;

			// Anyway, ask prefab reference and default FX anyway
			Undo.RecordObject(bm, "Modified Bullet VFX References");
			GUIContent prefabGC = new GUIContent("VFX Prefab", "The prefab object that will spawn at every bullet birth/death.");
			GUIContent particleGC = new GUIContent("Default VFX Style", "The Particle System to play as generic VFX. This should be the same object as the VFX Prefab.");
			GUIContent rendererGC = new GUIContent("Default VFX Renderer", "Particle System Renderer used to display the generic VFX. This should be the same object as the VFX Prefab.");
			bm.vfxPrefab = EditorGUILayout.ObjectField(prefabGC, bm.vfxPrefab, typeof(GameObject), false) as GameObject;
			bm.defaultParticles = EditorGUILayout.ObjectField(particleGC, bm.defaultParticles, typeof(ParticleSystem), true) as ParticleSystem;
			bm.defaultParticleRenderer = EditorGUILayout.ObjectField(rendererGC, bm.defaultParticleRenderer, typeof(ParticleSystemRenderer), true) as ParticleSystemRenderer;

			EditorUtility.SetDirty(bm);

		}

		public static bool ReserializeVFX(BulletVFX b)
		{
			bool wasNecessary = false;

			if (b.thisTransform == null)
			{
				wasNecessary = true;
				b.thisTransform = b.transform;
			}
			else if (b.thisTransform != b.transform)
			{
				wasNecessary = true;
				b.thisTransform = b.transform;
			}

			ParticleSystem curPs = b.GetComponent<ParticleSystem>();

			if (b.thisParticleSystem == null)
			{
				wasNecessary = true;
				b.thisParticleSystem = curPs;
			}
			else if (b.thisParticleSystem != curPs)
			{
				wasNecessary = true;
				b.thisParticleSystem = curPs;
			}

			ParticleSystemRenderer curPsr = b.GetComponent<ParticleSystemRenderer>();

			if (b.thisParticleRenderer == null)
			{
				wasNecessary = true;
				b.thisParticleRenderer = curPsr;
			}
			else if (b.thisParticleRenderer != curPsr)
			{
				wasNecessary = true;
				b.thisParticleRenderer = curPsr;
			}

			EditorUtility.SetDirty(b);

			return wasNecessary;
		}

		void ModifyPool(int delta)
		{
			// don't change anything
			if (delta == 0) return;

			Undo.RecordObject(bm, "Edit Pool");

			// remove things
			if (delta < 0)
			{
				int toRemove = Mathf.Min(-delta, bm.transform.childCount);
				for (int i = 0; i < toRemove; i++)
					Undo.DestroyObjectImmediate(bm.effectPool[i].gameObject);
			}

			// add things
			else
				for (int i = 0; i < delta; i++)
				{
					GameObject vfx = PrefabUtility.InstantiatePrefab(bm.vfxPrefab) as GameObject;
					vfx.transform.SetParent(bm.transform);
					BulletVFX b = vfx.GetComponent<BulletVFX>();
					ReserializeVFX(b);
					Undo.RegisterCreatedObjectUndo(vfx, "Add objects to pool");
				}

			// recreate array
			bm.effectPool = new BulletVFX[bm.transform.childCount];
			if (bm.transform.childCount > 0)
				for (int i = 0; i < bm.effectPool.Length; i++)
				{
					bm.effectPool[i] = bm.transform.GetChild(i).GetComponent<BulletVFX>();
					bm.effectPool[i].gameObject.name = "Bullet VFX " + i.ToString();
					EditorUtility.SetDirty(bm.effectPool[i].gameObject);
				}

			EditorSceneManager.MarkAllScenesDirty();
		}
	}
}

