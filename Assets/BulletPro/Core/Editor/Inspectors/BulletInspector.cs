using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	enum FoldoutType { Speed, AngularSpeed, Scale }

	[CustomEditor(typeof(Bullet))]
	[CanEditMultipleObjects]
	public class BulletInspector : Editor
	{
		// inidividual referencese
		SerializedProperty gizmoColor, selfTr, selfRdr;

		// emission
		SerializedProperty poolParent;

		Bullet[] b;

		public void OnEnable()
		{
			b = new Bullet[targets.Length];

			// auto-serializing transform and renderer whenever inspector gets drawn
			if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPaused)
				for (int i = 0; i < targets.Length; i++)
				{
					b[i] = targets[i] as Bullet;
					Reserialize(b[i], b[i].renderMode == BulletRenderMode.Mesh);
				}

			gizmoColor = serializedObject.FindProperty("gizmoColor");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(gizmoColor);

			if (EditorApplication.isPlaying)
			{
				EditorGUILayout.HelpBox("This object cannot be edited in Play mode.", MessageType.Info);
				if (targets.Length == 1)
					EditorGUILayout.LabelField("Bullet ID : ", (target as Bullet).uniqueBulletID.ToString());
			}

			if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPaused)
				return;

			for (int i = 0; i < b.Length; i++)
				if (b[i] != null)
					Reserialize(b[i], b[i].renderMode == BulletRenderMode.Mesh);

			serializedObject.ApplyModifiedProperties();
		}

		public static bool Reserialize(Bullet b, bool isMesh = false)
		{
			bool thatWasNeeded = false;

			// Take care of transform
			if (b.self != b.transform) { b.self = b.transform; thatWasNeeded = true; }

			// Take care of renderer / filter
			if (!isMesh)
			{
				SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
				if (sr)
					if (b.spriteRenderer != sr) { b.spriteRenderer = sr; thatWasNeeded = true; }
				if (b.spriteRenderer == null) { b.spriteRenderer = b.gameObject.AddComponent<SpriteRenderer>(); thatWasNeeded = true; }
				if (b.spriteRenderer.enabled) { b.spriteRenderer.enabled = false; thatWasNeeded = true; }
			}
			else
			{
				MeshRenderer mr = b.GetComponent<MeshRenderer>();
				if (mr)
					if (b.meshRenderer != mr) { b.meshRenderer = mr; thatWasNeeded = true; }
				if (b.meshRenderer == null) { b.meshRenderer = b.gameObject.AddComponent<MeshRenderer>(); thatWasNeeded = true; }
				if (b.meshRenderer.enabled) { b.meshRenderer.enabled = false; thatWasNeeded = true; }
				b.meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				b.meshRenderer.receiveShadows = false;

				MeshFilter mf = b.GetComponent<MeshFilter>();
				if (mf)
					if (b.meshFilter != mf) { b.meshFilter = mf; thatWasNeeded = true; }
				if (b.meshFilter == null) { b.meshFilter = b.gameObject.AddComponent<MeshFilter>(); thatWasNeeded = true; }
			}

			// Bullet render type
			b.renderMode = isMesh ? BulletRenderMode.Mesh : BulletRenderMode.Sprite;

			EditorUtility.SetDirty(b);
			if (b.spriteRenderer) EditorUtility.SetDirty(b.spriteRenderer);
			if (b.meshRenderer) EditorUtility.SetDirty(b.meshRenderer);

			return thatWasNeeded;
		}
	}
}
