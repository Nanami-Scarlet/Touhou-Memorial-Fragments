using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	public static class BulletProAssetCreator
	{
		public static MethodInfo CreateScriptMethod;

		// thanks to johnsoncodehk on the Unity forums for finding about this internal function
		static void CreateScriptAsset(string templatePath, string destName)
		{
			#if UNITY_2019_1_OR_NEWER
			if (CreateScriptMethod == null)
				CreateScriptMethod = typeof(UnityEditor.ProjectWindowUtil).GetMethod("CreateScriptAssetFromTemplateFile", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
			#else
			if (CreateScriptMethod == null)
				CreateScriptMethod = typeof(UnityEditor.ProjectWindowUtil).GetMethod("CreateScriptAsset", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			#endif

			if (CreateScriptMethod != null)			
				CreateScriptMethod.Invoke(null, new object[] { templatePath, destName });
		}

		[MenuItem("Assets/Create/Bullet Pro/Behaviour Script", false, 112)]
		public static void CreateBulletBehaviour()
		{
			TextAsset template = Resources.Load("NewBulletBehaviourTemplate") as TextAsset;
			if (template == null)
			{
				Debug.LogError("Can\'t create a BulletBehaviour : script template not found in Resources folder. Consider reimporting the package.");
				return;
			}

			string templatePath = AssetDatabase.GetAssetPath(template);
			CreateScriptAsset(templatePath, "NewBulletBehaviour.cs");
		}

		// Called automatically if Bullet Pro Settings are detected missing.
		public static BulletProSettings CreateCollisionSettingsAsset(bool highlightWhenDone = true)
		{
			// If BulletPro isn't at project root with its intact Resources files, we make another one
			string path = "Assets/BulletPro/Resources/BulletProSettings.asset";	
			if (!AssetDatabase.IsValidFolder("Assets/BulletPro/Resources"))
			{
				if (!AssetDatabase.IsValidFolder("Assets/Resources"))
					AssetDatabase.CreateFolder("Assets", "Resources");
        			
				path = "Assets/Resources/BulletProSettings.asset";
			}
			BulletProSettings bcs = ScriptableObject.CreateInstance<BulletProSettings>();
			AssetDatabase.CreateAsset(bcs, path);			

			bcs.collisionHandler = Resources.Load("BulletProCollisionHandler") as ComputeShader;
			if (bcs.collisionHandler == null)
				Debug.LogError("BulletPro Error: file BulletProCollisionHandler.compute has not been found. Try reimporting the package.");
			
			bcs.maxAmountOfBullets = 2000;
			bcs.maxAmountOfReceivers = 200;
			bcs.maxAmountOfCollisionsPerFrame = 64;
			bcs.collisionTags = new CollisionTagLabels();
			bcs.collisionTags.tags = new string[32];
			bcs[0] = "Player";
			bcs[1] = "Enemy";
			for (int i=2; i<32; i++)
				bcs[i] = "Tag "+i.ToString();

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			if (highlightWhenDone)
			{
				#if UNITY_2018_3_OR_NEWER
				SettingsService.OpenProjectSettings("Project/Bullet Pro");
				#else
				EditorGUIUtility.PingObject(bcs);
				EditorUtility.FocusProjectWindow();
				Selection.activeObject = bcs;
				#endif
			}

			Debug.Log("BulletPro Recovery: the file BulletProSettings.asset was missing, has been created in the Resources folder.");
			Debug.Log("BulletPro Recovery: restarting the editor is recommended.");

			return bcs;
		}

		[MenuItem("Tools/BulletPro/Settings", false, 1000)]
		#if !UNITY_2018_3_OR_NEWER
		[MenuItem("Edit/Project Settings/Bullet Pro", false, 300)]
		#endif
		public static void HighlightCollisionSettings()
		{
			BulletProSettings bcs = Resources.Load("BulletProSettings") as BulletProSettings;
			if (bcs == null)
				BulletProAssetCreator.CreateCollisionSettingsAsset();
			else
			{
				EditorUtility.FocusProjectWindow();
				EditorGUIUtility.PingObject(bcs);
				Selection.activeObject = bcs;
			}
		}

		[MenuItem("GameObject/Bullet Pro/Emitter", false, 10)]
		public static void CreateEmitterGameObject()
		{
			GameObject go = new GameObject("Bullet Emitter");

			Transform tr = go.transform;
			if (Selection.activeGameObject != null)
			{
				tr.SetParent(Selection.activeGameObject.transform);
				tr.localPosition = Vector3.zero;
				tr.localEulerAngles = Vector3.zero;
			}
			else
			{
				tr.SetParent(null);
				tr.position = Vector3.zero;
				tr.eulerAngles = Vector3.zero;
			}
			tr.localScale = Vector3.one;

			BulletEmitter be = go.AddComponent<BulletEmitter>();
			be.patternOrigin = tr;
			Undo.RegisterCreatedObjectUndo(go, "Created Bullet Emitter");
			
			Selection.activeObject = go;
		}

		[MenuItem("GameObject/Bullet Pro/Receiver", false, 10)]
		public static void CreateReceiverGameObject()
		{
			GameObject go = new GameObject("Bullet Receiver");
			Transform tr = go.transform;
			if (Selection.activeGameObject != null)
			{
				tr.SetParent(Selection.activeGameObject.transform);
				tr.localPosition = Vector3.zero;
				tr.localEulerAngles = Vector3.zero;
			}
			else
			{
				tr.SetParent(null);
				tr.position = Vector3.zero;
				tr.eulerAngles = Vector3.zero;
			}
			tr.localScale = Vector3.one;
			BulletReceiver br = go.AddComponent<BulletReceiver>();
			br.self = tr;
			br.hitboxSize = 0.5f;
			br.collisionTags.tagList = 1;
			Undo.RegisterCreatedObjectUndo(go, "Created Bullet Receiver");

			Selection.activeObject = go;
		}

		//[MenuItem("GameObject/Bullet Pro/Managers/Main Manager", false, 12)]
		public static GameObject CreateManagerGameObject()
		{
			GameObject go = new GameObject("Bullet Manager");
			Transform tr = go.transform;
			tr.SetParent(null);
			tr.position = Vector3.zero;
			tr.eulerAngles = Vector3.zero;
			tr.localScale = Vector3.one;
			BulletPoolManager bp = go.AddComponent<BulletPoolManager>();
			go.AddComponent<BulletCollisionManager>();
			BulletGlobalParamManager bgpm = go.AddComponent<BulletGlobalParamManager>();
			bgpm.AddParameter(ParameterType.Slider01, "Difficulty");
			Undo.RegisterCreatedObjectUndo(go, "BulletPro Scene Setup");

			// pool roots
			GameObject regularPoolRoot = new GameObject("Regular Bullet Pool");
			Transform rpr = regularPoolRoot.transform;
			rpr.SetParent(tr);
			rpr.localPosition = Vector3.zero;
			rpr.localEulerAngles = Vector3.zero;
			rpr.localScale = Vector3.one;
			bp.regularPoolRoot = rpr;
			GameObject meshPoolRoot = new GameObject("Mesh-Based Bullet Pool");
			Transform mpr = meshPoolRoot.transform;
			mpr.SetParent(tr);
			mpr.localPosition = Vector3.zero;
			mpr.localEulerAngles = Vector3.zero;
			mpr.localScale = Vector3.one;
			bp.meshPoolRoot = mpr;
			Undo.RegisterCreatedObjectUndo(regularPoolRoot, "BulletPro Scene Setup");
			Undo.RegisterCreatedObjectUndo(meshPoolRoot, "BulletPro Scene Setup");


			// filling pool of 1000 bullets
			List<Bullet> lb = new List<Bullet>();
			for (int i = 0; i < 1000; i++)
			{
				GameObject bullet = new GameObject("Bullet " + i.ToString());
				bullet.transform.SetParent(rpr);
				Bullet b = bullet.AddComponent<Bullet>();
				BulletInspector.Reserialize(b, false);
				lb.Add(b);
				Undo.RegisterCreatedObjectUndo(bullet, "BulletPro Scene Setup");
			}
			bp.pool = lb.ToArray();

			Selection.activeObject = go;

			return go;
		}

		//[MenuItem("GameObject/Bullet Pro/Managers/VFX Manager", false, 12)]
		public static GameObject CreateVFXManagerGameObject()
		{
			GameObject go = new GameObject("Bullet VFX Manager");
			Transform tr = go.transform;
			tr.SetParent(null);
			tr.position = Vector3.zero;
			tr.eulerAngles = Vector3.zero;
			tr.localScale = Vector3.one;
			BulletVFXManager bm = go.AddComponent<BulletVFXManager>();

			bm.vfxPrefab = Resources.Load("DefaultBulletVFX") as GameObject;
			bm.defaultParticles = bm.vfxPrefab.GetComponent<ParticleSystem>();
			bm.defaultParticleRenderer = bm.vfxPrefab.GetComponent<ParticleSystemRenderer>();

			Undo.RegisterCreatedObjectUndo(go, "BulletPro Scene Setup");

			// filling pool of 1000 VFX
			List<BulletVFX> lb = new List<BulletVFX>();
			for (int i = 0; i < 1000; i++)
			{
				GameObject vfx = PrefabUtility.InstantiatePrefab(bm.vfxPrefab) as GameObject;
				vfx.name = "Bullet VFX "+i.ToString();
				Transform t = vfx.transform;
				t.SetParent(tr);
				BulletVFX b = vfx.GetComponent<BulletVFX>();
				BulletVFXManagerInspector.ReserializeVFX(b);
				lb.Add(b);
				Undo.RegisterCreatedObjectUndo(vfx, "BulletPro Scene Setup");
			}
			bm.effectPool = lb.ToArray();

			Selection.activeObject = go;

			return go;
		}

		//[MenuItem("GameObject/Bullet Pro/Managers/Behaviour Manager", false, 12)]
		public static GameObject CreateBehaviourManagerGameObject()
		{
			GameObject go = new GameObject("Bullet Behaviour Manager");
			Transform tr = go.transform;
			tr.SetParent(null);
			tr.position = Vector3.zero;
			tr.eulerAngles = Vector3.zero;
			tr.localScale = Vector3.one;
			go.AddComponent<BulletBehaviourManager>();
			Undo.RegisterCreatedObjectUndo(go, "Created Bullet Behaviour Manager");

			Selection.activeObject = go;

			return go;
		}

		//[MenuItem("GameObject/Bullet Pro/Managers/Audio Manager", false, 12)]
		public static GameObject CreateAudioManagerGameObject()
		{
			GameObject go = new GameObject("Bullet Audio Manager");
			Transform tr = go.transform;
			tr.SetParent(null);
			tr.position = Vector3.zero;
			tr.eulerAngles = Vector3.zero;
			tr.localScale = Vector3.one;
			BulletAudioManager bam = go.AddComponent<BulletAudioManager>();

			int audioAmount = 3;
			AudioSource[] aa = new AudioSource[audioAmount];
			for (int i=0; i<audioAmount; i++)
			{
				GameObject s = new GameObject("Audio Source "+i.ToString());
				Transform t = s.transform;
				t.SetParent(tr);
				t.localPosition = Vector3.zero;
				t.localEulerAngles = Vector3.zero;
				t.localScale = Vector3.one;
				AudioSource a = s.AddComponent<AudioSource>();
				a.playOnAwake = false;
				aa[i] = a;
			}
			bam.sources = aa;
			
			Undo.RegisterCreatedObjectUndo(go, "BulletPro Scene Setup");

			Selection.activeObject = go;

			return go;
		}

		[MenuItem("GameObject/Bullet Pro/Scene Setup", false, 11)]
		[MenuItem("Tools/BulletPro/Scene Setup")]
		public static void CreateAllManagerGameObjects()
		{
			BulletProSceneSetup previousSceneSetup = GameObject.FindObjectOfType(typeof(BulletProSceneSetup)) as BulletProSceneSetup;
			if (previousSceneSetup != null)
			{
				Debug.LogError("BulletPro Error: a Scene Setup is already existing in your scene. Please remove it if you want to create a new one.");
				return;
			}

			GameObject go = new GameObject("BulletPro Scene Setup");
			BulletProSceneSetup bpss = go.AddComponent<BulletProSceneSetup>();
			bpss.gizmoColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
			Transform tr = go.transform;
			tr.SetParent(null);
			tr.position = Vector3.zero;
			tr.eulerAngles = Vector3.zero;
			tr.localScale = Vector3.one;
			
			GameObject go1 = CreateManagerGameObject();
			GameObject go2 = CreateVFXManagerGameObject();
			GameObject go3 = CreateBehaviourManagerGameObject();
			GameObject go4 = CreateAudioManagerGameObject();

			go1.transform.SetParent(tr);
			go2.transform.SetParent(tr);
			go3.transform.SetParent(tr);
			go4.transform.SetParent(tr);
			
			Undo.RegisterCreatedObjectUndo(go, "BulletPro Scene Setup");
			Debug.Log("[Bullet Pro] Scene has been set up successfully!");

			Selection.activeObject = go;
		}

		#region old, unused, stays here as fallback (functional but has bad UX)
		/* *
		[MenuItem("Assets/Create/Bullet Pro/Behaviour Script", false, 112)]
		public static void CreateBulletBehaviour()
		{
			// Get code
			string templatePath = "Assets/BulletPro/Scripts/Editor/Resources/NewBulletBehaviourTemplate.txt";
			string[] template;
			try { template = File.ReadAllLines(templatePath); }
			catch
			{
				Debug.LogError("Can\'t create a BulletBehaviour : BulletPro folder is not at root of project, so template path does not match.");
				return;
			}

			// Get destination folder path
			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (path == "")
				path = "Assets";
			else if (Path.GetExtension(path) != "")
				path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");

			// Name the file
			string fileName = "NewBulletBehaviour";
			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + fileName + ".cs");
			string[] split = assetPathAndName.Split(new char[] { '/' });
			string nameAndExt = split[split.Length - 1];
			fileName = nameAndExt.Split(new char[] { '.' })[0];

			// Correct code and actually write asset
			for (int i = 0; i < template.Length; i++)
				if (template[i].Contains("NewBulletBehaviour"))
					template[i] = template[i].Replace("NewBulletBehaviour", fileName);

			File.WriteAllLines(assetPathAndName, template);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = AssetDatabase.LoadAssetAtPath(assetPathAndName, typeof(Object));
		}
		/* */
		#endregion
	}
}