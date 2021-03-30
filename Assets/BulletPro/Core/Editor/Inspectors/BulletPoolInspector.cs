using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CustomEditor(typeof(BulletPoolManager))]
	public class BulletPoolInspector : Editor
	{
		BulletPoolManager bp;

		public void OnEnable()
		{
			bp = target as BulletPoolManager;
			bp.mainTransform = bp.transform;
			bool dirty = false;

			if (bp.regularPoolRoot == null)
			{
				GameObject poolRoot = new GameObject("Regular Bullet Pool");
				poolRoot.transform.SetParent(bp.transform);
				bp.regularPoolRoot = poolRoot.transform;
				EditorUtility.SetDirty(bp.regularPoolRoot);
				dirty = true;
			}

			if (bp.meshPoolRoot == null)
			{
				GameObject poolRoot = new GameObject("Mesh-Based Bullet Pool");
				poolRoot.transform.SetParent(bp.transform);
				bp.meshPoolRoot = poolRoot.transform;
				EditorUtility.SetDirty(bp.meshPoolRoot);
				dirty = true;
			}

			if (dirty)
			{
				EditorSceneManager.MarkAllScenesDirty();
				EditorUtility.SetDirty(bp);
			}
		}

		public override void OnInspectorGUI()
		{
			//base.OnInspectorGUI();

			GUILayout.Space(12);

			bool wrap = EditorStyles.label.wordWrap;
			EditorStyles.label.wordWrap = true;
			//EditorGUILayout.HelpBox("This script just needs to exist once in your scene.", MessageType.Info);

			GameObject go = bp.gameObject;
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
				return;
			}

			if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPaused)
			{
				EditorGUILayout.HelpBox("This object cannot be edited in Play mode.", MessageType.Info);
				return;
			}

			bp.pool = new Bullet[bp.regularPoolRoot.childCount];
			bp.meshPool = new Bullet[bp.meshPoolRoot.childCount];

			#region regular pool

			if (bp.pool.Length > 0)
			{
				bool missing = false;
				for (int i = 0; i < bp.pool.Length; i++)
				{
					bp.pool[i] = bp.regularPoolRoot.GetChild(i).GetComponent<Bullet>();
					if (bp.pool[i] == null) missing = true;
					else ReserializeBullet(bp.pool[i]);
				}
				if (missing)
					EditorGUILayout.HelpBox("Some children of this object lack the Bullet Component!\nThis object should only have Bullet children.", MessageType.Error);
				else
				{
					FontStyle defF = EditorStyles.label.fontStyle;
					Color defC = EditorStyles.label.onNormal.textColor;
					EditorStyles.label.fontStyle = FontStyle.Normal;
					EditorStyles.label.normal.textColor = new Color(0, 0.5f, 0, 1);
					if (EditorGUIUtility.isProSkin) EditorStyles.label.normal.textColor = new Color(0, 1, 0, 1);
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Current pool : " + bp.pool.Length.ToString() + " bullets.");
					if (GUILayout.Button("Add 100", EditorStyles.miniButton)) ModifyPool(100);
					if (GUILayout.Button("Remove 100", EditorStyles.miniButton)) ModifyPool(-100);
					EditorGUILayout.EndHorizontal();
					EditorStyles.label.fontStyle = defF;
					EditorStyles.label.normal.textColor = defC;
				}
			}
			else
			{
				EditorGUILayout.HelpBox("This pool does not yet have any Bullet children.", MessageType.Warning);
				Color defB = GUI.color;
				GUI.color = new Color(0.8f, 1, 0.5f, 1);
				if (GUILayout.Button("Click here to create a pool of 1000 Bullets as children"))
				{
					Undo.RecordObject(bp, "Create Pool");

					List<Bullet> lb = new List<Bullet>();
					for (int i = 0; i < 1000; i++)
					{
						GameObject bullet = new GameObject("Bullet " + i.ToString());
						bullet.transform.SetParent(bp.regularPoolRoot);
						Bullet b = bullet.AddComponent<Bullet>();
						ReserializeBullet(b);
						lb.Add(b);
						Undo.RegisterCreatedObjectUndo(bullet, "Create Pool");
					}
					bp.pool = lb.ToArray();
					EditorSceneManager.MarkAllScenesDirty();
				}
				GUI.color = defB;
			}

			#endregion

			// foldout for mesh-based bullets
			bp.foldout = EditorGUILayout.Foldout(bp.foldout, "Click here if some bullets should have a MeshRenderer", true);
			if (bp.foldout)
			{
				#region mesh-based pool

				int indentDelta = 2;

				EditorGUI.indentLevel += indentDelta;

				if (bp.meshPool.Length > 0)
				{
					bool missing = false;
					for (int i = 0; i < bp.meshPool.Length; i++)
					{
						bp.meshPool[i] = bp.meshPoolRoot.GetChild(i).GetComponent<Bullet>();
						if (bp.meshPool[i] == null) missing = true;
						else ReserializeBullet(bp.meshPool[i], true);
					}
					if (missing)
						EditorGUILayout.HelpBox("Some children of this object lack the Bullet Component!\nThis object should only have Bullet children.", MessageType.Error);
					else
					{
						FontStyle defF = EditorStyles.label.fontStyle;
						Color defC = EditorStyles.label.onNormal.textColor;
						EditorStyles.label.fontStyle = FontStyle.Normal;
						EditorStyles.label.normal.textColor = new Color(0, 0.5f, 0, 1);
						if (EditorGUIUtility.isProSkin) EditorStyles.label.normal.textColor = new Color(0, 1, 0, 1);
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField("Current pool : " + bp.meshPool.Length.ToString() + " 3D bullets.");
						if (GUILayout.Button("Add 100", EditorStyles.miniButton)) ModifyMeshPool(100);
						if (GUILayout.Button("Remove 100", EditorStyles.miniButton)) ModifyMeshPool(-100);
						EditorGUILayout.EndHorizontal();
						EditorStyles.label.fontStyle = defF;
						EditorStyles.label.normal.textColor = defC;
					}
				}
				else
				{
					EditorGUILayout.HelpBox("This pool does not yet have any Bullet children.", MessageType.Warning);
					Color defB = GUI.color;
					GUI.color = new Color(0.8f, 1, 0.5f, 1);
					if (GUILayout.Button("Click here to create a pool of 300 3D Bullets as children"))
					{
						Undo.RecordObject(bp, "Created Pool");

						List<Bullet> lb = new List<Bullet>();
						for (int i = 0; i < 300; i++)
						{
							GameObject bullet = new GameObject("Mesh-Bullet " + i.ToString());
							bullet.transform.SetParent(bp.meshPoolRoot);
							Bullet b = bullet.AddComponent<Bullet>();
							ReserializeBullet(b, true);
							lb.Add(b);
							Undo.RegisterCreatedObjectUndo(bullet, "Created Pool");
						}
						bp.meshPool = lb.ToArray();
						EditorSceneManager.MarkAllScenesDirty();
					}
					GUI.color = defB;
				}

				EditorGUI.indentLevel -= indentDelta;

				#endregion
			}

			EditorStyles.label.wordWrap = wrap;
			EditorUtility.SetDirty(bp);
		}

		bool ReserializeBullet(Bullet b, bool isMesh = false) { return BulletInspector.Reserialize(b, isMesh); }

		void ModifyPool(int delta)
		{
			// don't change anything
			if (delta == 0) return;

			Undo.RecordObject(bp, "Edit Pool");

			// remove things
			if (delta < 0)
			{
				int toRemove = Mathf.Min(-delta, bp.regularPoolRoot.childCount);
				for (int i = 0; i < toRemove; i++)
					Undo.DestroyObjectImmediate(bp.pool[i].gameObject);
			}

			// add things
			else
				for (int i = 0; i < delta; i++)
				{
					GameObject bullet = new GameObject("Bullet " + i.ToString());
					bullet.transform.SetParent(bp.regularPoolRoot);
					Bullet b = bullet.AddComponent<Bullet>();
					ReserializeBullet(b);
					Undo.RegisterCreatedObjectUndo(bullet, "Add object to pool");
				}

			// recreate array
			bp.pool = new Bullet[bp.regularPoolRoot.childCount];
			if (bp.regularPoolRoot.childCount > 0)
				for (int i = 0; i < bp.pool.Length; i++)
				{
					bp.pool[i] = bp.regularPoolRoot.GetChild(i).GetComponent<Bullet>();
					bp.pool[i].gameObject.name = "Bullet " + i.ToString();
					EditorUtility.SetDirty(bp.pool[i].gameObject);
				}

			EditorSceneManager.MarkAllScenesDirty();
		}

		void ModifyMeshPool(int delta)
		{
			// don't change anything
			if (delta == 0) return;

			// remove things
			if (delta < 0)
			{
				int toRemove = Mathf.Min(-delta, bp.meshPoolRoot.childCount);
				for (int i = 0; i < toRemove; i++)
					Undo.DestroyObjectImmediate(bp.meshPool[i].gameObject);
			}

			// add things
			else
				for (int i = 0; i < delta; i++)
				{
					GameObject bullet = new GameObject("Mesh-Bullet " + i.ToString());
					bullet.transform.SetParent(bp.meshPoolRoot);
					Bullet b = bullet.AddComponent<Bullet>();
					ReserializeBullet(b, true);
					Undo.RegisterCreatedObjectUndo(bullet, "Add object to pool");
				}

			// recreate array
			bp.meshPool = new Bullet[bp.meshPoolRoot.childCount];
			if (bp.meshPoolRoot.childCount > 0)
				for (int i = 0; i < bp.meshPool.Length; i++)
				{
					bp.meshPool[i] = bp.meshPoolRoot.GetChild(i).GetComponent<Bullet>();
					bp.meshPool[i].gameObject.name = "Mesh-Bullet " + i.ToString();
					EditorUtility.SetDirty(bp.meshPool[i].gameObject);
				}

			EditorSceneManager.MarkAllScenesDirty();
		}
	}
}
