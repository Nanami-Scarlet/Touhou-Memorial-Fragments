using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CustomEditor(typeof(BulletBehaviourManager))]
	public class BulletBehaviourManagerInspector : Editor
	{

		BulletBehaviourManager bbm;
		bool objectsHaveBeenReserialized;

		public void OnEnable()
		{
			bbm = target as BulletBehaviourManager;
			if (bbm.pools == null)
			{
				bbm.pools = new List<BulletBehaviourPool>();
				EditorUtility.SetDirty(bbm);
			}

			Undo.undoRedoPerformed += RecreateAllPoolArrays;
		}

		public void OnDisable()
		{
			Undo.undoRedoPerformed -= RecreateAllPoolArrays;
		}

		public override void OnInspectorGUI()
		{
			bool wrap = EditorStyles.label.wordWrap;
			EditorStyles.label.wordWrap = true;
			EditorGUILayout.HelpBox("This script just needs to exist once in your scene.\nIf BulletBehaviour objects are used in your game, pooling will be handled here for each BulletBehaviour.", MessageType.Info);

			GUILayout.Space(12);

			GameObject go = bbm.gameObject;
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
				return;
			}

			if (bbm.pools.Count > 0)
				for (int i = 0; i < bbm.pools.Count; i++)
					DrawBulletBehaviourPoolInspector(i);

			GUILayout.Space(8);
			GUIContent addGC = new GUIContent("Add Behaviour", "Add a pool of objects based on a Bullet Behaviour prefab.");
			if (GUILayout.Button(addGC, EditorStyles.miniButton))
			{
				Undo.RecordObject(bbm, "Updated Bullet Behaviour Manager");
				bbm.pools.Add(new BulletBehaviourPool());
				EditorUtility.SetDirty(bbm);
			}

			EditorStyles.label.wordWrap = wrap;
			EditorUtility.SetDirty(bbm);
		}

		bool ReserializeBullet(Bullet b) { return BulletInspector.Reserialize(b); }

		void ModifyPool(int index, int delta, ref BulletBehaviourPool bbp)
		{
			bbp = bbm.pools[index];
			if (bbp.prefab == null)
			{
				Debug.LogError("Error : trying to modify length of inexisting pool");
				return;
			}

			Undo.RecordObject(bbm, "Edit Pool");

			if (bbp.poolRoot == null)
			{
				GameObject poolRoot = new GameObject(bbp.prefab.name + " Pool");
				Undo.RegisterCreatedObjectUndo(poolRoot, "Create Pool");
				poolRoot.transform.SetParent(bbm.transform);
				bbp.poolRoot = poolRoot.transform;
			}

			// don't change anything
			if (delta == 0) return;

			// remove things
			if (delta < 0)
			{
				int toRemove = Mathf.Min(-delta, bbp.poolRoot.childCount);
				for (int i = 0; i < toRemove; i++)
					Undo.DestroyObjectImmediate(bbp.pool[i].gameObject);
			}

			// add things
			else
				for (int i = 0; i < delta; i++)
				{
					GameObject bb = PrefabUtility.InstantiatePrefab(bbp.prefab) as GameObject;
					bb.transform.SetParent(bbp.poolRoot);
					BaseBulletBehaviour bbb = bb.GetComponent<BaseBulletBehaviour>();
					bbb.self = bbb.transform;
					bbb.defaultParent = bbp.poolRoot;
					EditorUtility.SetDirty(bbb);
					Undo.RegisterCreatedObjectUndo(bb, "Add objects to pool");
				}

			// recreate array
			bbp.pool = new BaseBulletBehaviour[bbp.poolRoot.childCount];
			if (bbp.poolRoot.childCount > 0)
				for (int i = 0; i < bbp.pool.Length; i++)
				{
					bbp.pool[i] = bbp.poolRoot.GetChild(i).GetComponent<BaseBulletBehaviour>();
					bbp.pool[i].gameObject.name = bbp.prefab.name + " " + i.ToString();
					EditorUtility.SetDirty(bbp.pool[i].gameObject);
				}

			bbm.pools[index] = bbp;
			EditorUtility.SetDirty(bbm);
			EditorSceneManager.MarkAllScenesDirty();
		}

		// Called at Undo to ensure pool size consistency
		void RecreatePoolArray(int index)
		{
			BulletBehaviourPool bbp = bbm.pools[index];
			if (!bbp.poolRoot) return;
			bbp.pool = new BaseBulletBehaviour[bbp.poolRoot.childCount];
			if (bbp.poolRoot.childCount > 0)
				for (int i = 0; i < bbp.pool.Length; i++)
				{
					bbp.pool[i] = bbp.poolRoot.GetChild(i).GetComponent<BaseBulletBehaviour>();
					bbp.pool[i].gameObject.name = bbp.prefab.name + " " + i.ToString();
					EditorUtility.SetDirty(bbp.pool[i].gameObject);
				}

			bbm.pools[index] = bbp;
			EditorUtility.SetDirty(bbm);
		}

		// Called at Undo to ensure pool size consistency
		public void RecreateAllPoolArrays()
		{
			if (bbm.pools == null) return;
			if (bbm.pools.Count == 0) return;
			for (int i = 0; i < bbm.pools.Count; i++)
				RecreatePoolArray(i);
		}

		void DrawBulletBehaviourPoolInspector(int index)
		{
			BulletBehaviourPool bbp = bbm.pools[index];

			string foldoutStr = "";
			if (bbm.pools[index].prefab == null) foldoutStr = "New Bullet Behaviour";
			else
			{
				string prefabName = bbm.pools[index].prefab.name;
				if (bbm.pools[index].pool == null) foldoutStr = prefabName;
				else
				{
					foldoutStr = "(";
					int length = bbm.pools[index].pool.Length;
					foldoutStr += length == 0 ? "Empty" : length.ToString();
					foldoutStr += ") " + prefabName;
				}
			}

			bool shouldBeRemoved = false;
			EditorGUILayout.BeginHorizontal();
			bbp.foldout = EditorGUILayout.Foldout(bbm.pools[index].foldout, foldoutStr, true);
			GUIContent removeGC = new GUIContent("Remove Behaviour", "Remove and destroy the pool of objects holding this Behaviour.");
			if (GUILayout.Button(removeGC, EditorStyles.miniButton)) shouldBeRemoved = true;
			EditorGUILayout.EndHorizontal();

			if (shouldBeRemoved)
			{
				if (bbm.pools[index].poolRoot) Undo.DestroyObjectImmediate(bbm.pools[index].poolRoot.gameObject);
				Undo.RecordObject(bbm, "Deleted Bullet Behaviour - " + foldoutStr);
				bbm.pools.RemoveAt(index);
				EditorUtility.SetDirty(bbm);
				return;
			}

			bbm.pools[index] = bbp;
			if (!bbp.foldout) return;

			EditorGUI.indentLevel += 2;

			Undo.RecordObject(bbm, "Changed Bullet Behaviour - " + foldoutStr);

			// Prefab field : updates a Transform, child of manager, under which pooled behaviours will be created
			EditorGUI.BeginChangeCheck();
			GUIContent prefabGC = new GUIContent("Prefab", "Prefab must hold a Bullet Behaviour in order to create an object pool.");
			GameObject newPrefab = EditorGUILayout.ObjectField(prefabGC, bbp.prefab, typeof(GameObject), false) as GameObject;
			if (EditorGUI.EndChangeCheck())
			{
				bool wrongObject = false;
				if (newPrefab != null)
					if (!newPrefab.GetComponent<BaseBulletBehaviour>())
						wrongObject = true;

				if (wrongObject)
					Debug.LogError("Cannot use this prefab : it must have a BulletBehaviour Component.");
				else
				{
					// deleting old things
					if (bbp.poolRoot)
						Undo.DestroyObjectImmediate(bbp.poolRoot.gameObject);

					// adding new things
					bbp.prefab = newPrefab;

					// updating real object
					Undo.RecordObject(bbm, "Changed Bullet Behaviour - " + foldoutStr);
					bbm.pools[index] = bbp;
				}
			}

			// That's all if no prefab has been set
			if (bbp.prefab == null)
			{
				EditorGUI.indentLevel -= 2;
				return;
			}

			// Pool handling : add, remove, display, serialize

			if (bbp.poolRoot == null)
			{
				GameObject poolRoot = new GameObject(newPrefab.name + " Pool");
				poolRoot.transform.SetParent(bbm.transform);
				bbp.poolRoot = poolRoot.transform;
				bbm.pools[index] = bbp;
				ModifyPool(index, 50, ref bbp);
				Undo.RegisterCreatedObjectUndo(poolRoot, "Created New BulletBehaviour Pool");
			}

			// Updating poolRoot name in any case
			bbp.poolRoot.gameObject.name = foldoutStr + " Pool";

			// Refresh value of bbp to get the actual pool data
			bbp = bbm.pools[index];

			if (bbp.pool.Length > 0)
			{
				bool missing = false;
				for (int i = 0; i < bbp.pool.Length; i++)
					if (bbp.pool[i] == null)
						missing = true;

				if (missing)
					EditorGUILayout.HelpBox("Some children of this pool lack a BulletBehaviour Component!\nThis pool should only contain BulletBehaviour objects.", MessageType.Error);
				else
				{
					FontStyle defF = EditorStyles.label.fontStyle;
					Color defC = EditorStyles.label.onNormal.textColor;
					EditorStyles.label.fontStyle = FontStyle.Normal;
					EditorStyles.label.normal.textColor = new Color(0, 0.5f, 0, 1);
					if (EditorGUIUtility.isProSkin) EditorStyles.label.normal.textColor = new Color(0, 1, 0, 1);
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Current pool : " + bbp.pool.Length.ToString() + " objects.");
					if (GUILayout.Button("Add 50", EditorStyles.miniButton)) ModifyPool(index, 50, ref bbp);
					if (GUILayout.Button("Remove 50", EditorStyles.miniButton)) ModifyPool(index, -50, ref bbp);
					EditorGUILayout.EndHorizontal();
					EditorStyles.label.fontStyle = defF;
					EditorStyles.label.normal.textColor = defC;
				}
			}
			else
			{
				EditorGUILayout.HelpBox("For now, this pool is empty.", MessageType.Warning);
				Color defB = GUI.color;
				GUI.color = new Color(0.8f, 1, 0.5f, 1);
				if (GUILayout.Button("Click here to add 50 BulletBehaviours to pool")) ModifyPool(index, 50, ref bbp);
				GUI.color = defB;
			}

			// And we're done.
			EditorGUI.indentLevel -= 2;
			GUILayout.Space(8);

			bbm.pools[index] = bbp;
			EditorUtility.SetDirty(bbm);
		}
	}
}
