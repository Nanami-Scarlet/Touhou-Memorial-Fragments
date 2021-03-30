using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CustomEditor(typeof(BulletParams))]
	public class BulletParamInspector : EmissionParamsInspector
	{

		#region SerializedProperties

		SerializedProperty sprite, sortingOrder, sortingLayerName;
		SerializedProperty hasLifespan, lifespan;
		SerializedProperty playVFXOnBirth, playVFXOnDeath, vfxParticleSize;
		SerializedProperty mesh, renderMode, material;
		SerializedProperty useCustomBirthVFX, useCustomDeathVFX, customBirthVFX, customDeathVFX;
		SerializedProperty color, evolutionBlendWithBaseColor, colorEvolution;
		SerializedProperty animated, sprites, animationFramerate, animationWrapMode, animationTexture;
		SerializedProperty foldoutVFX, hideSpriteList;
		SerializedProperty forwardSpeed, angularSpeed, startScale;
		SerializedProperty speedCurve, angularSpeedCurve, scaleCurve, colorCurve, alphaCurve, homingCurve;
		SerializedProperty canMove, isVisible, canCollide;
		SerializedProperty isChildOfEmitter;
		SerializedProperty patternsShot, dieWhenAllPatternsAreDone;
		SerializedProperty collisionTags, dieOnCollision, colliders;
		SerializedProperty preview, shapeFoldout, collisionTagsFoldout;
		SerializedProperty homing, homingAngularSpeed, lookAtTargetAtSpawn, targetRefreshInterval, preferredTarget, homingAngleThreshold;
		SerializedProperty homingTags, useSameTagsAsCollision, homingTagsFoldout;
		SerializedProperty behaviourPrefabs;
		SerializedProperty delaySpawn, timeBeforeSpawn, playAudioAtSpawn, audioClip;
		SerializedProperty animFoldout, animationClip, animationMovementSpace;
		SerializedProperty xMoveFromAnim, yMoveFromAnim, rotationFromAnim, scaleFromAnim;
		SerializedProperty enableCustomParameters, customParameters;
		SerializedProperty currentlyDisplayedModule;

		#endregion

		// target
		BulletParams bp;

		// assets
		Material default2DMaterial, default3DMaterial;

		// each bullet module is enclosed into a BeginVertical() call, which uses this string
		string enabledModuleStyle, disabledModuleStyle;

		// list of patterns shot
		ReorderableList rlist;

		// list of behaviours
		ReorderableList behaviourList;

		// list of custom parameter
		ReorderableList paramList;

		// animation sprite list
		ReorderableList animSpriteList;

		// bullet collider list
		ReorderableList colliderList;

		Color baseGUIColor, boxColor, enabledButtonColor;

		FontStyle defaultLabelStyle;

		// used by every sub-inspector as temp variable
		bool newVal;

		// used for collider
		bool mustRecalcTexture;

		// header buttons
		GUIStyle normalButton, focusedButton;

		// some dynamic parameter as bools
		bool canPlayAudio, canBeChildOfEmitter;

		// tweakable
		int maxShowableSprites;

		public override void OnEnable()
		{
			if (target == null)
			{
				OnUnselected();
				DestroyImmediate(this);
				return;
			}
			
			base.OnEnable();

			#region serialized properties

			sprite = serializedObject.FindProperty("sprite");
			sortingOrder = serializedObject.FindProperty("sortingOrder");
			sortingLayerName = serializedObject.FindProperty("sortingLayerName");
			
			hasLifespan = serializedObject.FindProperty("hasLifespan");
			lifespan = serializedObject.FindProperty("lifespan");

			playVFXOnBirth = serializedObject.FindProperty("playVFXOnBirth");
			playVFXOnDeath = serializedObject.FindProperty("playVFXOnDeath");
			vfxParticleSize = serializedObject.FindProperty("vfxParticleSize");
			useCustomBirthVFX = serializedObject.FindProperty("useCustomBirthVFX");
			customBirthVFX = serializedObject.FindProperty("customBirthVFX");
			useCustomDeathVFX = serializedObject.FindProperty("useCustomDeathVFX");
			customDeathVFX = serializedObject.FindProperty("customDeathVFX");

			animated = serializedObject.FindProperty("animated");
			sprites = serializedObject.FindProperty("sprites");
			animationFramerate = serializedObject.FindProperty("animationFramerate");
			animationWrapMode = serializedObject.FindProperty("animationWrapMode");
			animationTexture = serializedObject.FindProperty("animationTexture");

			color = serializedObject.FindProperty("color");
			evolutionBlendWithBaseColor = serializedObject.FindProperty("evolutionBlendWithBaseColor");
			colorEvolution = serializedObject.FindProperty("colorEvolution");

			material = serializedObject.FindProperty("material");
			mesh = serializedObject.FindProperty("mesh");
			renderMode = serializedObject.FindProperty("renderMode");
			foldoutVFX = serializedObject.FindProperty("foldoutVFX");
			hideSpriteList = serializedObject.FindProperty("hideSpriteList");

			forwardSpeed = serializedObject.FindProperty("forwardSpeed");
			angularSpeed = serializedObject.FindProperty("angularSpeed");
			startScale = serializedObject.FindProperty("startScale");

			speedCurve = serializedObject.FindProperty("speedOverLifetime");
			angularSpeedCurve = serializedObject.FindProperty("angularSpeedOverLifetime");
			scaleCurve = serializedObject.FindProperty("scaleOverLifetime");
			colorCurve = serializedObject.FindProperty("colorOverLifetime");
			alphaCurve = serializedObject.FindProperty("alphaOverLifetime");
			homingCurve = serializedObject.FindProperty("homingOverLifetime");

			canMove = serializedObject.FindProperty("canMove");
			isVisible = serializedObject.FindProperty("isVisible");
			canCollide = serializedObject.FindProperty("canCollide");
			isChildOfEmitter = serializedObject.FindProperty("isChildOfEmitter");

			animFoldout = serializedObject.FindProperty("animFoldout");
			animationClip = serializedObject.FindProperty("animationClip");
			animationMovementSpace = serializedObject.FindProperty("animationMovementSpace");

			xMoveFromAnim = serializedObject.FindProperty("xMovementFromAnim");
			yMoveFromAnim = serializedObject.FindProperty("yMovementFromAnim");
			rotationFromAnim = serializedObject.FindProperty("rotationFromAnim");
			scaleFromAnim = serializedObject.FindProperty("scaleFromAnim");

			patternsShot = serializedObject.FindProperty("patternsShot");
			dieWhenAllPatternsAreDone = serializedObject.FindProperty("dieWhenAllPatternsAreDone");

			collisionTags = serializedObject.FindProperty("collisionTags");
			dieOnCollision = serializedObject.FindProperty("dieOnCollision");
			colliders = serializedObject.FindProperty("colliders");

			preview = serializedObject.FindProperty("preview");
			shapeFoldout = serializedObject.FindProperty("shapeFoldout");
			collisionTagsFoldout = serializedObject.FindProperty("collisionTagsFoldout");

			homing = serializedObject.FindProperty("homing");
			homingAngularSpeed = serializedObject.FindProperty("homingAngularSpeed");
			lookAtTargetAtSpawn = serializedObject.FindProperty("lookAtTargetAtSpawn");
			targetRefreshInterval = serializedObject.FindProperty("targetRefreshInterval");
			preferredTarget = serializedObject.FindProperty("preferredTarget");
			homingAngleThreshold = serializedObject.FindProperty("homingAngleThreshold");

			homingTags = serializedObject.FindProperty("homingTags");
			useSameTagsAsCollision = serializedObject.FindProperty("useSameTagsAsCollision");
			homingTagsFoldout = serializedObject.FindProperty("homingTagsFoldout");

			behaviourPrefabs = serializedObject.FindProperty("behaviourPrefabs");

			default2DMaterial = Resources.Load("Default2DBulletMaterial") as Material;
			default3DMaterial = Resources.Load("Default3DBulletMaterial") as Material;

			delaySpawn = serializedObject.FindProperty("delaySpawn");
			timeBeforeSpawn = serializedObject.FindProperty("timeBeforeSpawn");
			playAudioAtSpawn = serializedObject.FindProperty("playAudioAtSpawn");
			audioClip = serializedObject.FindProperty("audioClip");

			customParameters = serializedObject.FindProperty("customParameters");

			currentlyDisplayedModule = serializedObject.FindProperty("currentlyDisplayedModule");

			#endregion

			#region pattern list (rlist) setup

			rlist = new ReorderableList(serializedObject, patternsShot, true, true, true, true);

			rlist.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "List of Patterns owned by this bullet"); };
			rlist.drawElementCallback = PatternParamDrawer;
			rlist.onRemoveCallback += (ReorderableList list) =>
			{
				EmitterProfileUtility.SetParentOfPattern(bp.patternsShot[list.index], null, this);
				patternsShot.DeleteArrayElementAtIndex(list.index);
			};
			rlist.onAddCallback += (ReorderableList list) =>
			{
				patternsShot.arraySize++;
				DynamicParameterUtility.SetFixedObject(patternsShot.GetArrayElementAtIndex(patternsShot.arraySize-1), null, true);
			};

			#endregion

			#region behaviour list setup

			behaviourList = new ReorderableList(serializedObject, behaviourPrefabs, true, true, true, true);

			behaviourList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "BulletBehaviours attached to this bullet"); };
			behaviourList.drawElementCallback = BulletBehaviourFieldDrawer;
			behaviourList.onRemoveCallback += (ReorderableList list) =>
			{
				behaviourPrefabs.DeleteArrayElementAtIndex(list.index);
			};
			behaviourList.onAddCallback += (ReorderableList list) =>
			{
				behaviourPrefabs.arraySize++;
				serializedObject.ApplyModifiedProperties();
				bp.behaviourPrefabs[bp.behaviourPrefabs.Length-1].SetNarrowType(typeof(GameObject));
				bp.behaviourPrefabs[bp.behaviourPrefabs.Length-1].RequireComponent(typeof(BaseBulletBehaviour));
			};

			#endregion

			#region parameter list setup

			paramList = new ReorderableList(serializedObject, customParameters, true, true, true, true);

			paramList.drawHeaderCallback = (Rect rect) =>
			{
				Rect nameRect = new Rect(rect.x + 16, rect.y, rect.width, rect.height);
				Rect typeRect = new Rect(rect.x + 126, rect.y, rect.width, rect.height);
				Rect valueRect = new Rect(rect.x + 241, rect.y, rect.width, rect.height);
				EditorGUI.LabelField(nameRect, "Name");
				EditorGUI.LabelField(typeRect, "Type");
				EditorGUI.LabelField(valueRect, "Value");
			};
			paramList.drawElementCallback = CustomParameterDrawer;
			paramList.onRemoveCallback += (ReorderableList list) =>
			{
				customParameters.DeleteArrayElementAtIndex(list.index);
			};
			paramList.onAddCallback += (ReorderableList list) =>
			{
				customParameters.arraySize++;
				SerializedProperty newParam = customParameters.GetArrayElementAtIndex(customParameters.arraySize-1);
				DynamicParameterUtility.SetFixedColor(newParam.FindPropertyRelative("colorValue"), Color.black, true);
				Gradient grad = new Gradient();
				GradientAlphaKey[] gak = new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };
				GradientColorKey[] gck = new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.black, 1) };
				grad.SetKeys(gck, gak);
				DynamicParameterUtility.SetFixedGradient(newParam.FindPropertyRelative("gradientValue"), grad, true);
			};
			paramList.elementHeightCallback += (int index) =>
			{
				float lines = 1;
				int typeIndex = customParameters.GetArrayElementAtIndex(index).FindPropertyRelative("type").enumValueIndex;
				if (typeIndex == (int)ParameterType.Rect) lines++;
				else if (typeIndex == (int)ParameterType.Bounds) lines++;

				return paramList.elementHeight * lines;
			};

			#endregion

			#region anim sprite list setup

			animSpriteList = new ReorderableList(serializedObject, sprites, true, true, true, true);

			animSpriteList.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "List of Sprites used in animation:");
			};
			animSpriteList.drawElementCallback = AnimSpriteDrawer;
			animSpriteList.onRemoveCallback += (ReorderableList list) =>
			{
				sprites.DeleteArrayElementAtIndex(list.index);
				if (sprites.arraySize <= maxShowableSprites)
					hideSpriteList.boolValue = false;
				serializedObject.ApplyModifiedProperties();				
			};
			animSpriteList.onAddCallback += (ReorderableList list) =>
			{
				sprites.arraySize++;
				if (sprites.arraySize > maxShowableSprites)
					hideSpriteList.boolValue = true;
				serializedObject.ApplyModifiedProperties();
				bp.sprites[bp.sprites.Length-1].SetNarrowType(typeof(Sprite));
			};
			

			#endregion

			#region collider list setup

			colliderList = new ReorderableList(serializedObject, colliders, true, true, true, true);

			colliderList.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "List of collider shapes:");
			};
			colliderList.drawElementCallback = ColliderDrawer;
			colliderList.onRemoveCallback += (ReorderableList list) =>
			{
				if (colliders.arraySize > 1)
					colliders.DeleteArrayElementAtIndex(list.index);
			};
			colliderList.onAddCallback += (ReorderableList list) =>
			{
				colliders.arraySize++;
			};
			/* */
			colliderList.elementHeightCallback += (int index) =>
			{
				float lines = 2;
				
				//if (EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth < 230) // cf BulletColliderDrawer
				//if (colliders.GetArrayElementAtIndex(index).FindPropertyRelative("colliderType").enumValueIndex == (int)BulletColliderType.Line)
				//	lines = 2;				

				return colliderList.elementHeight * lines;
			};
			/* */

			#endregion
			
			bp = target as BulletParams;

			// set custom default values
			if (!bp.hasBeenSerializedOnce)
				bp.FirstInitialization();

			if (!EditorApplication.isPlaying)
				bp.SetUniqueIndex();

			maxShowableSprites = 20;
			disabledModuleStyle = "Label";
			enabledModuleStyle = "Box";
			baseGUIColor = GUI.color;
			enabledButtonColor = new Color(0.85f, 1f, 0.75f, 1f);
			//enabledButtonColor = baseGUIColor; // cut this one
			#if UNITY_2019_3_OR_NEWER
			boxColor = new Color(baseGUIColor.r*0.95f, baseGUIColor.g, baseGUIColor.b, baseGUIColor.a); // identical for now, might change
            #else
			boxColor = new Color(baseGUIColor.r*0.95f, baseGUIColor.g, baseGUIColor.b, baseGUIColor.a);
            #endif

			// collider preview
			UpdatePreviewParams();
			UpdatePreviewTexture();
			mustRecalcTexture = false;
		}

		public override void OnDisable()
		{
			OnUnselected();
		}

		public override void OnUnselected()
		{
			base.OnUnselected();
		}

        public override bool UseDefaultMargins() { return false; }

		public override void OnInspectorGUI()
		{
			// Debug
			//System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
			//stopwatch.Start();

			base.OnInspectorGUI();

			// can't be done in OnEnable because they're not found at this time
			defaultLabelStyle = EditorStyles.label.fontStyle;
			normalButton = new GUIStyle("button");
			focusedButton = new GUIStyle("button");
			focusedButton.normal = focusedButton.active;

			// getting useful bools
			canPlayAudio = DynamicParameterUtility.GetBool(playAudioAtSpawn);
			canBeChildOfEmitter = DynamicParameterUtility.GetBool(isChildOfEmitter, false, true);

			#region navigation upper bar

			GUIContent gotoGC = new GUIContent("Go to:", "A green button means something is enabled inside.");
			EditorGUILayout.LabelField(gotoGC);
			EditorGUILayout.BeginHorizontal();
			DrawButton(ParamInspectorPart.Renderer, isVisible.boolValue, "Graphics");
			DrawButton(ParamInspectorPart.Movement, canMove.boolValue, "Speed & Size");
			DrawButton(ParamInspectorPart.Collision, canCollide.boolValue, "Collision");
			DrawButton(ParamInspectorPart.Homing, homing.boolValue, "Homing");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			bool spawnAndLifetimeEnabled = hasLifespan.boolValue || delaySpawn.boolValue || canBeChildOfEmitter || canPlayAudio;
			DrawButton(ParamInspectorPart.SpawnAndLifetime, spawnAndLifetimeEnabled, "Spawn & Lifetime");
			DrawButton(ParamInspectorPart.Emission, bp.hasPatterns, "Routines & Patterns");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			DrawButton(ParamInspectorPart.Behaviours, (behaviourPrefabs.arraySize > 0), "Attach Scripts & Objects");
			DrawButton(ParamInspectorPart.Parameters, (customParameters.arraySize > 0), "Custom Parameters");
			EditorGUILayout.EndHorizontal();

			#endregion

			#region inspector pieces
			
			GUILayout.Space(12);

			float oldWidth = EditorGUIUtility.labelWidth;
			if (EditorGUIUtility.fieldWidth < 100 && !bp.profile.compactMode)
				EditorGUIUtility.labelWidth *= 0.75f;
			if (bp.profile.compactMode)
				EditorGUIUtility.labelWidth *= 1.2f;

			if (currentlyDisplayedModule.enumValueIndex == (int)ParamInspectorPart.Renderer)
				DrawRendererModuleInspector();

			else if (currentlyDisplayedModule.enumValueIndex == (int)ParamInspectorPart.SpawnAndLifetime)
			{
				DrawLifespanModuleInspector();
				DrawSpawnModuleInspector();
			}

			else if (currentlyDisplayedModule.enumValueIndex == (int)ParamInspectorPart.Collision)
				DrawCollisionModuleInspector();

			else if (currentlyDisplayedModule.enumValueIndex == (int)ParamInspectorPart.Homing)
				DrawHomingModuleInspector();

			else if (currentlyDisplayedModule.enumValueIndex == (int)ParamInspectorPart.Movement)
				DrawMovementModuleInspector();
	
			else if (currentlyDisplayedModule.enumValueIndex == (int)ParamInspectorPart.Emission)
				DrawPatternModuleInspector();

			else if (currentlyDisplayedModule.enumValueIndex == (int)ParamInspectorPart.Behaviours)
				DrawBehaviourModuleInspector();

			else if (currentlyDisplayedModule.enumValueIndex == (int)ParamInspectorPart.Parameters)
				DrawParameterModuleInspector();

			EditorGUIUtility.labelWidth = oldWidth;

			#endregion

			KeyboardControls();

			ApplyAll();

			// Debug
			//stopwatch.Stop();
			//EditorGUILayout.LabelField(stopwatch.ElapsedTicks.ToString());
		}

		// buttons from the upper navigation bar
		void DrawButton(ParamInspectorPart paramPart, bool containsEnabledStuff, string btnText)
		{
			GUIStyle btnStyle;
			if (currentlyDisplayedModule.enumValueIndex == (int)paramPart) btnStyle = new GUIStyle(focusedButton);
			else btnStyle = new GUIStyle(normalButton);
			
			GUI.color = containsEnabledStuff ? enabledButtonColor : baseGUIColor;
			#if UNITY_2019_3_OR_NEWER
			// compensate for the loss of EditorStyles.button.active
			if (currentlyDisplayedModule.enumValueIndex == (int)paramPart)
			{
				GUI.color *= 0.7f;
				GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 1f);
			}
			#endif
			
			if (GUILayout.Button(btnText, btnStyle, GUILayout.Height(24)))
			{
				GUI.color = baseGUIColor;

				currentlyDisplayedModule.enumValueIndex = (int)paramPart;
				// special case for collider preview
				if (paramPart == ParamInspectorPart.Collision)
				{
					mustRecalcTexture = true;
					serializedObject.ApplyModifiedProperties();
					UpdatePreviewParams();
					UpdatePreviewTexture();
				}
				else serializedObject.ApplyModifiedProperties();
			}
			else GUI.color = baseGUIColor;
		}

		void ApplyAll()
		{
			serializedObject.ApplyModifiedProperties();
			EditorUtility.SetDirty(bp);
		}

		void KeyboardControls()
		{
			int prev = currentlyDisplayedModule.intValue;

			Event e = Event.current;
			if (e.type != EventType.KeyDown) return;
			if (!e.control && !e.command) return;
			if (e.keyCode == KeyCode.Tab)
			{
				if (e.shift) currentlyDisplayedModule.intValue--;
				else currentlyDisplayedModule.intValue++;

				if (currentlyDisplayedModule.intValue < 0)
					currentlyDisplayedModule.intValue = 7;
				if (currentlyDisplayedModule.intValue == 8)
					currentlyDisplayedModule.intValue = 0;
			}

			else if (e.keyCode == KeyCode.Alpha1 || e.keyCode == KeyCode.Keypad1) currentlyDisplayedModule.intValue = 0;
			else if (e.keyCode == KeyCode.Alpha2 || e.keyCode == KeyCode.Keypad2) currentlyDisplayedModule.intValue = 1;
			else if (e.keyCode == KeyCode.Alpha3 || e.keyCode == KeyCode.Keypad3) currentlyDisplayedModule.intValue = 2;
			else if (e.keyCode == KeyCode.Alpha4 || e.keyCode == KeyCode.Keypad4) currentlyDisplayedModule.intValue = 3;
			else if (e.keyCode == KeyCode.Alpha5 || e.keyCode == KeyCode.Keypad5) currentlyDisplayedModule.intValue = 4;
			else if (e.keyCode == KeyCode.Alpha6 || e.keyCode == KeyCode.Keypad6) currentlyDisplayedModule.intValue = 5;
			else if (e.keyCode == KeyCode.Alpha7 || e.keyCode == KeyCode.Keypad7) currentlyDisplayedModule.intValue = 6;
			else if (e.keyCode == KeyCode.Alpha8 || e.keyCode == KeyCode.Keypad8) currentlyDisplayedModule.intValue = 7;

			if (currentlyDisplayedModule.intValue != prev) Repaint();
		}

		#region parts of inspector

		void DrawRendererModuleInspector()
		{
			GUILayout.Space(12);

			GUI.color = boxColor;
			EditorGUILayout.BeginVertical(isVisible.boolValue ? enabledModuleStyle : disabledModuleStyle);
			GUI.color = baseGUIColor;
			
			EditorStyles.label.fontStyle = FontStyle.Bold;
			EditorGUI.BeginChangeCheck();
			newVal = EditorGUILayout.Toggle("Bullet Is Visible", isVisible.boolValue);
			if (EditorGUI.EndChangeCheck()) isVisible.boolValue = newVal;
			EditorStyles.label.fontStyle = defaultLabelStyle;
			if (isVisible.boolValue)
			{
				EditorGUI.indentLevel += 1;
	
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(renderMode);
				bool justChanged = EditorGUI.EndChangeCheck();
				if (renderMode.enumValueIndex == 1)
				{
					if (justChanged)
						if (DynamicParameterUtility.GetFixedObject(material) == default2DMaterial)
							DynamicParameterUtility.SetFixedObject(material, default3DMaterial);

					EditorGUILayout.PropertyField(mesh);
					EditorGUILayout.PropertyField(material);
					EditorGUI.indentLevel -= 1;
					EditorGUILayout.EndVertical();					
				}
				else
				{
					// Unanimated sprite
					EditorGUILayout.PropertyField(sortingLayerName, new GUIContent("Sorting Layer", "Name of this bullet's sorting layer. Leaving this empty will set it to default."));
					EditorGUILayout.PropertyField(sortingOrder);
					EditorGUILayout.PropertyField(material);
					Object mat = DynamicParameterUtility.GetFixedObject(material);
					if (mat == default3DMaterial || mat == null)
						DynamicParameterUtility.SetFixedObject(material, default2DMaterial);
					bool drawColorField = true;
					if (colorCurve.FindPropertyRelative("enabled").boolValue)
					{
						if (DynamicParameterUtility.GetHighestValueOfInt(evolutionBlendWithBaseColor, false) == (int)ColorBlend.Replace)
						{
							EditorGUILayout.LabelField("Color: overridden by gradient, see below.");
							drawColorField = false;
						}
					}
					if (drawColorField) EditorGUILayout.PropertyField(color);
					EditorGUILayout.PropertyField(sprite, new GUIContent("Sprite"));
					EditorGUI.indentLevel -= 1;
					EditorGUILayout.EndVertical();

					#region animation block

					GUILayout.Space(12);

					bool isAnimated = DynamicParameterUtility.GetBool(animated);
					GUI.color = boxColor;
					EditorGUILayout.BeginVertical(isAnimated ? enabledModuleStyle : disabledModuleStyle);
					GUI.color = baseGUIColor;

					EditorStyles.label.fontStyle = FontStyle.Bold;
					EditorGUILayout.PropertyField(animated, new GUIContent("Animate Sprite"));
					EditorStyles.label.fontStyle = defaultLabelStyle;
					
					// Sprite array and animation settings
					if (isAnimated)
					{
						EditorGUI.indentLevel += 1;

						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField(animationFramerate);
						if (EditorGUI.EndChangeCheck())
							DynamicParameterUtility.ClampAboveZero(animationFramerate);
						EditorGUILayout.PropertyField(animationWrapMode);
						
						// Fast-load sprites from a texture
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PropertyField(animationTexture, new GUIContent("Load Sprites At Once:", "Import a Texture here then click \"Load Sprites\" to autofill the sprite list."));
						EditorGUI.BeginDisabledGroup(animationTexture.objectReferenceValue == null);
						if (GUILayout.Button(new GUIContent("Load Sprites", "Autofill Sprite list with texture content."), EditorStyles.miniButton, GUILayout.MaxWidth(80)))
						{
							string texPath = AssetDatabase.GetAssetPath(animationTexture.objectReferenceValue);
							Object[] texAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(texPath);
							List<Sprite> sprList = new List<Sprite>();
							if (texAssets != null)
								if (texAssets.Length > 0)
									for (int i = 0; i < texAssets.Length; i++)
										if (texAssets[i] is Sprite)
											sprList.Add(texAssets[i] as Sprite);

							if (sprList.Count > 0)
							{
								sprites.arraySize = sprList.Count;
								if (sprList.Count > maxShowableSprites)
									hideSpriteList.boolValue = true;
								serializedObject.ApplyModifiedProperties();
								Undo.RecordObject(bp, "Imported Sprite List");
								for (int i = 0; i < sprList.Count; i++)
								{
									bp.sprites[i] = new DynamicObjectReference(sprList[i]);
									bp.sprites[i].SetNarrowType(typeof(Sprite));
								}
							}
						}
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.EndHorizontal();
						
						if (sprites.arraySize > maxShowableSprites)
						{
							GUILayout.Space(8);
							EditorGUILayout.HelpBox("Sprite list contains a lot of sprites.\nIt might slow down the editor a bit.", MessageType.Info);
							string btnmsg = hideSpriteList.boolValue ? "Show the list anyway" : "Hide list";
							int oldIndent = EditorGUI.indentLevel;
							EditorGUI.indentLevel = 0;
							EditorGUILayout.BeginHorizontal();
							GUILayout.Space(oldIndent*16);
							if (GUILayout.Button(btnmsg, EditorStyles.miniButton))
								hideSpriteList.boolValue = !hideSpriteList.boolValue;
							EditorGUILayout.EndHorizontal();
							EditorGUI.indentLevel = oldIndent;							
						}

						// Draw sprite list
						if (!hideSpriteList.boolValue)
						{
							GUILayout.Space(8);
							int oldIndent = EditorGUI.indentLevel;
							EditorGUI.indentLevel = 0;
							EditorGUILayout.BeginHorizontal();
							GUILayout.Space(oldIndent*15);
							EditorGUILayout.BeginVertical();
							animSpriteList.DoLayoutList();
							EditorGUILayout.EndVertical();
							EditorGUILayout.EndHorizontal();
							EditorGUI.indentLevel = oldIndent;
						}
						else GUILayout.Space(4);

						EditorGUI.indentLevel -= 1;
					}

					EditorGUILayout.EndVertical();

					#endregion
				}

				#region VFX block

				GUILayout.Space(12);

				GUI.color = boxColor;
				EditorGUILayout.BeginVertical(foldoutVFX.boolValue ? enabledModuleStyle : disabledModuleStyle);
				GUI.color = baseGUIColor;

				EditorStyles.label.fontStyle = FontStyle.Bold;
				foldoutVFX.boolValue = EditorGUILayout.Toggle("Spawn/Death VFX", foldoutVFX.boolValue);
				EditorStyles.label.fontStyle = defaultLabelStyle;
				if (foldoutVFX.boolValue)
				{
					EditorGUI.indentLevel += 1;				

					bool canPlayBirth = false;
					bool canPlayDeath = false;

					// VFX birth
					EditorGUILayout.PropertyField(playVFXOnBirth);
					canPlayBirth = DynamicParameterUtility.GetBool(playVFXOnBirth);
					if (canPlayBirth)
					{
						EditorGUI.indentLevel += 1;
						EditorGUILayout.PropertyField(useCustomBirthVFX);
						if (useCustomBirthVFX.boolValue) EditorGUILayout.PropertyField(customBirthVFX, new GUIContent("Custom Birth VFX Prefab"));
						EditorGUI.indentLevel -= 1;
					}

					// VFX death
					EditorGUILayout.PropertyField(playVFXOnDeath);
					canPlayDeath = DynamicParameterUtility.GetBool(playVFXOnDeath);
					if (canPlayDeath)
					{
						EditorGUI.indentLevel += 1;
						EditorGUILayout.PropertyField(useCustomDeathVFX);
						if (useCustomDeathVFX.boolValue) EditorGUILayout.PropertyField(customDeathVFX, new GUIContent("Custom Death VFX Prefab"));
						EditorGUI.indentLevel -= 1;
					}

					// Particle size for all effects
					if (canPlayBirth || canPlayDeath)
						EditorGUILayout.PropertyField(vfxParticleSize, new GUIContent("VFX Particle Size", "Size relative to the bullet's scale."));

					EditorGUI.indentLevel -= 1;					
				}

				EditorGUILayout.EndVertical();

				#endregion

				#region color and alpha over lifetime

				// Only for sprite mode, not mesh
				if (renderMode.enumValueIndex == 0)
				{
					GUILayout.Space(12);
					GUI.color = boxColor;					
					EditorGUILayout.BeginVertical(colorCurve.FindPropertyRelative("enabled").boolValue ? enabledModuleStyle : disabledModuleStyle);
					GUI.color = baseGUIColor;
					DrawDynamicCurveInspector(colorCurve);
					EditorGUILayout.EndVertical();

					GUILayout.Space(12);
					GUI.color = boxColor;					
					EditorGUILayout.BeginVertical(alphaCurve.FindPropertyRelative("enabled").boolValue ? enabledModuleStyle : disabledModuleStyle);
					GUI.color = baseGUIColor;
					DrawDynamicCurveInspector(alphaCurve);
					EditorGUILayout.EndVertical();
				}

				#endregion
			}
			else EditorGUILayout.EndVertical();
		}

		void DrawMovementModuleInspector()
		{
			// Bullet stats : movement
			GUILayout.Space(12);

			GUI.color = boxColor;
			EditorGUILayout.BeginVertical(canMove.boolValue ? enabledModuleStyle : disabledModuleStyle);
			GUI.color = baseGUIColor;
			EditorStyles.label.fontStyle = FontStyle.Bold;
			EditorGUI.BeginChangeCheck();
			newVal = EditorGUILayout.Toggle("Bullet Movement", canMove.boolValue);
			if (EditorGUI.EndChangeCheck()) canMove.boolValue = newVal;
			EditorStyles.label.fontStyle = defaultLabelStyle;
			if (canMove.boolValue)
			{
				EditorGUI.indentLevel += 1;

				bool xAnimEnabled = xMoveFromAnim.FindPropertyRelative("enabled").boolValue;
				bool yAnimEnabled = yMoveFromAnim.FindPropertyRelative("enabled").boolValue;
				bool rotationEnabled = rotationFromAnim.FindPropertyRelative("enabled").boolValue;
				bool scaleEnabled = scaleFromAnim.FindPropertyRelative("enabled").boolValue;

				if (!xAnimEnabled && !yAnimEnabled) EditorGUILayout.PropertyField(forwardSpeed, new GUIContent("Forward Speed"));
				else EditorGUILayout.LabelField("Forward Speed is fully driven by an Animation Clip. (Disable curves to cancel)");
				if (!rotationEnabled) EditorGUILayout.PropertyField(angularSpeed);
				else EditorGUILayout.LabelField("Angular Speed is fully driven by an Animation Clip. (Disable curve to cancel)");
				if (!scaleEnabled) EditorGUILayout.PropertyField(startScale);
				else EditorGUILayout.LabelField("Scale is fully driven by an Animation Clip. (Disable curve to cancel)");

				EditorGUI.indentLevel -= 1;
				EditorGUILayout.EndVertical();

				GUILayout.Space(12);

				GUI.color = boxColor;
				EditorGUILayout.BeginVertical(animFoldout.boolValue ? enabledModuleStyle : disabledModuleStyle);
				GUI.color = baseGUIColor;

				EditorStyles.label.fontStyle = FontStyle.Bold;
				animFoldout.boolValue = EditorGUILayout.Toggle("Import Animation", animFoldout.boolValue);
				EditorStyles.label.fontStyle = defaultLabelStyle;
				if (animFoldout.boolValue)
				{
					EditorGUI.indentLevel += 1;
					EditorGUILayout.PropertyField(animationMovementSpace, new GUIContent("Movement Space"));
					EditorGUILayout.PropertyField(animationClip);
					EditorGUILayout.LabelField("Import To :");
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(15);
					if (GUILayout.Button("Movement", EditorStyles.miniButtonLeft)) ImportAnimationData(BulletAnimImportType.Position);
					if (GUILayout.Button("Rotation", EditorStyles.miniButtonMid)) ImportAnimationData(BulletAnimImportType.Rotation);
					if (GUILayout.Button("Scale", EditorStyles.miniButtonMid)) ImportAnimationData(BulletAnimImportType.Scale);
					if (GUILayout.Button("Everything", EditorStyles.miniButtonRight)) ImportAnimationData(BulletAnimImportType.All);
					EditorGUILayout.EndHorizontal();
					EditorGUI.indentLevel -= 1;
				}
				EditorGUILayout.EndVertical();

				// Curves
				if (xAnimEnabled || yAnimEnabled)
				{
					GUILayout.Space(12);
					GUI.color = boxColor;					
					EditorGUILayout.BeginVertical(xMoveFromAnim.FindPropertyRelative("enabled").boolValue ? enabledModuleStyle : disabledModuleStyle);
					GUI.color = baseGUIColor;					
					DrawCurveInspector(xMoveFromAnim);
					EditorGUILayout.EndVertical();
					ClampValueAboveZeroRelative(xMoveFromAnim, "_period");

					GUILayout.Space(12);
					GUI.color = boxColor;					
					EditorGUILayout.BeginVertical(yMoveFromAnim.FindPropertyRelative("enabled").boolValue ? enabledModuleStyle : disabledModuleStyle);
					GUI.color = baseGUIColor;					
					DrawCurveInspector(yMoveFromAnim);
					EditorGUILayout.EndVertical();
					ClampValueAboveZeroRelative(yMoveFromAnim, "_period");
				}
				else
				{
					GUILayout.Space(12);
					GUI.color = boxColor;					
					EditorGUILayout.BeginVertical(speedCurve.FindPropertyRelative("enabled").boolValue ? enabledModuleStyle : disabledModuleStyle);
					GUI.color = baseGUIColor;					
					DrawDynamicCurveInspector(speedCurve);
					EditorGUILayout.EndVertical();
				}

				GUILayout.Space(12);
				if (rotationEnabled)
				{
					GUI.color = boxColor;					
					EditorGUILayout.BeginVertical(rotationFromAnim.FindPropertyRelative("enabled").boolValue ? enabledModuleStyle : disabledModuleStyle);
					GUI.color = baseGUIColor;					
					DrawCurveInspector(rotationFromAnim);
					ClampValueAboveZeroRelative(rotationFromAnim, "_period");
				}
				else
				{
					GUI.color = boxColor;					
					EditorGUILayout.BeginVertical(angularSpeedCurve.FindPropertyRelative("enabled").boolValue ? enabledModuleStyle : disabledModuleStyle);
					GUI.color = baseGUIColor;					
					DrawDynamicCurveInspector(angularSpeedCurve);
				}
				EditorGUILayout.EndVertical();

				GUILayout.Space(12);
				if (scaleEnabled)
				{
					GUI.color = baseGUIColor;					
					EditorGUILayout.BeginVertical(scaleFromAnim.FindPropertyRelative("enabled").boolValue ? enabledModuleStyle : disabledModuleStyle);
					GUI.color = baseGUIColor;					
					DrawCurveInspector(scaleFromAnim);
					ClampValueAboveZeroRelative(scaleFromAnim, "_period");
				}
				else
				{
					GUI.color = baseGUIColor;					
					EditorGUILayout.BeginVertical(scaleCurve.FindPropertyRelative("enabled").boolValue ? enabledModuleStyle : disabledModuleStyle);
					GUI.color = baseGUIColor;					
					DrawDynamicCurveInspector(scaleCurve);
				}
				EditorGUILayout.EndVertical();
			}

			else EditorGUILayout.EndVertical();
		}

		void DrawCollisionModuleInspector()
		{
			// Bullet stats : collisions
			GUILayout.Space(12);
			GUI.color = boxColor;
			EditorGUILayout.BeginVertical(canCollide.boolValue ? enabledModuleStyle : disabledModuleStyle);
			GUI.color = baseGUIColor;
			EditorStyles.label.fontStyle = FontStyle.Bold;
			EditorGUI.BeginChangeCheck();
			newVal = EditorGUILayout.Toggle("Collisions", canCollide.boolValue);
			if (EditorGUI.EndChangeCheck()) canCollide.boolValue = newVal;
			EditorStyles.label.fontStyle = defaultLabelStyle;
			if (canCollide.boolValue)
			{
				EditorGUI.indentLevel += 1;

				if (colliders.arraySize == 0) colliders.arraySize++;
				SerializedProperty mainCol = colliders.GetArrayElementAtIndex(0);

				EditorGUI.BeginChangeCheck(); // for texture preview

				string[] opts = new string[] { "Simple", "Composite" };
				int composite = EditorGUILayout.Popup("Collider Mode", (shapeFoldout.boolValue?1:0), opts);

				shapeFoldout.boolValue = composite == 1;
				if (shapeFoldout.boolValue)
				{
					int oldIndent = EditorGUI.indentLevel;
					EditorGUI.indentLevel = 0;
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(15);
					EditorGUILayout.BeginVertical();
					colliderList.DoLayoutList();
					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
					EditorGUI.indentLevel = oldIndent;	
				}
				else
				{
					SerializedProperty colliderType = mainCol.FindPropertyRelative("colliderType");
					EditorGUI.BeginChangeCheck();
					BulletColliderType newHitType = (BulletColliderType)EditorGUILayout.EnumPopup("Shape", (BulletColliderType)mainCol.FindPropertyRelative("colliderType").enumValueIndex);
					if (EditorGUI.EndChangeCheck()) colliderType.enumValueIndex = (int)newHitType;
					
					if (colliderType.enumValueIndex == (int)BulletColliderType.Circle)
					{
						SerializedProperty colSize = mainCol.FindPropertyRelative("size");
						EditorGUILayout.PropertyField(colSize);
						if (colSize.floatValue < 0) colSize.floatValue = 0;
						EditorGUI.BeginChangeCheck();
						Vector3 rawOffset = mainCol.FindPropertyRelative("offset").vector3Value;
						Vector2 oldOffset = new Vector2(rawOffset.x, rawOffset.y);
						Vector2 newOffset = EditorGUILayout.Vector2Field("Offset", oldOffset);
						if (EditorGUI.EndChangeCheck()) mainCol.FindPropertyRelative("offset").vector3Value = new Vector3(newOffset.x, newOffset.y, 0);
					}
					else // line
					{
						SerializedProperty lineStart = mainCol.FindPropertyRelative("lineStart");
						EditorGUILayout.PropertyField(lineStart);
						SerializedProperty lineEnd = mainCol.FindPropertyRelative("lineEnd");
						EditorGUILayout.PropertyField(lineEnd);
					}

					if (colliders.arraySize > 1)
					{
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.HelpBox("You still have leftover extra colliders from a Composite setup. Click this button to clean them.", MessageType.Warning);
						EditorGUILayout.BeginVertical();
						GUILayout.Space(4);
						if (GUILayout.Button("Clean colliders", EditorStyles.miniButton))
							colliders.arraySize = 1;
						EditorGUILayout.EndVertical();
						EditorGUILayout.EndHorizontal();
						GUILayout.Space(4);
					}
				}

				UpdatePreviewParams();
				bool canDrawPreview = CanDrawPreview();
				if (canDrawPreview && mustRecalcTexture)
				{
					serializedObject.ApplyModifiedProperties();
					UpdatePreviewTexture();
					mustRecalcTexture = false;
				}

				DrawBulletPreview(canDrawPreview);
				
				if (EditorGUI.EndChangeCheck())
					mustRecalcTexture = true;

				EditorGUILayout.PropertyField(dieOnCollision);

				EditorGUILayout.BeginHorizontal();
				collisionTagsFoldout.boolValue = EditorGUILayout.Foldout(collisionTagsFoldout.boolValue, "Collision Tags", true);
				if (collisionTagsFoldout.boolValue)
				{
					GUI.color = new Color(0.6f, 1f, 1f, 1f);
					if (GUILayout.Button("Manage Tags", EditorStyles.miniButton, GUILayout.MaxWidth(90)))
					{
						BulletProSettings bcs = Resources.Load("BulletProSettings") as BulletProSettings;
						if (bcs == null)
							bcs = BulletProAssetCreator.CreateCollisionSettingsAsset();
						else
						{
							#if UNITY_2018_3_OR_NEWER
							SettingsService.OpenProjectSettings("Project/Bullet Pro");
							#else
							EditorGUIUtility.PingObject(bcs);
							EditorUtility.FocusProjectWindow();
							Selection.activeObject = bcs;
							#endif
						}
					}
					GUI.color = baseGUIColor;
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.PropertyField(collisionTags);
				}
				else EditorGUILayout.EndHorizontal();
				
				if (bp.collisionTags.tagList == 0)
				{
					string str = "Selected object has no Collision Tags. It won't collide with anything.";
					
					if (!collisionTagsFoldout.boolValue)
						str += "\nYou may need to unfold and activate the Collision Tags above.";
					else
						str += "\nYou may need to click on some Collision Tags above.";
					EditorGUILayout.HelpBox(str, MessageType.Warning);
				}

				EditorGUI.indentLevel -= 1;
			}

			EditorGUILayout.EndVertical();
		}

		void DrawHomingModuleInspector()
		{
			// Bullet stats : homing
			GUILayout.Space(12);

			GUI.color = boxColor;
			EditorGUILayout.BeginVertical(homing.boolValue ? enabledModuleStyle : disabledModuleStyle);
			GUI.color = baseGUIColor;
			EditorStyles.label.fontStyle = FontStyle.Bold;
			EditorGUI.BeginChangeCheck();
			newVal = EditorGUILayout.Toggle("Homing", homing.boolValue);
			if (EditorGUI.EndChangeCheck()) homing.boolValue = newVal;
			EditorStyles.label.fontStyle = defaultLabelStyle;
			if (homing.boolValue)
			{
				EditorGUI.indentLevel += 1;

				EditorGUILayout.PropertyField(preferredTarget);
				EditorGUILayout.PropertyField(lookAtTargetAtSpawn, new GUIContent("Spawn Towards Target", "If negative, will look away from target.\nRotation from Shot applies AFTER this rotation."));
				EditorGUILayout.PropertyField(homingAngularSpeed, new GUIContent("Homing Angular Speed", "How fast does the bullet turn to its target?"));
				
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(homingAngleThreshold, new GUIContent("Homing Angle Threshold", "Below a certain delta, bullet will stop turning to its target, to avoid ugly shaking."));
				if (EditorGUI.EndChangeCheck())
					DynamicParameterUtility.ClampAboveZero(homingAngleThreshold);

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(targetRefreshInterval, new GUIContent("Target Refresh Interval", "How often should the bullet search for a new target?"));
				if (EditorGUI.EndChangeCheck())
					DynamicParameterUtility.ClampAboveZero(targetRefreshInterval);
				
				EditorGUILayout.PropertyField(useSameTagsAsCollision, new GUIContent("Use Same Tags As Collision", "What Bullet Receivers can be tracked by this bullet?"));
				if (!useSameTagsAsCollision.boolValue)
				{
					EditorGUILayout.BeginHorizontal();
					homingTagsFoldout.boolValue = EditorGUILayout.Foldout(homingTagsFoldout.boolValue, "Homing Tags", true);
					if (homingTagsFoldout.boolValue)
					{
						GUI.color = new Color(0.6f, 1f, 1f, 1f);
						if (GUILayout.Button("Manage Tags", EditorStyles.miniButton, GUILayout.MaxWidth(90)))
						{
							BulletProSettings bcs = Resources.Load("BulletProSettings") as BulletProSettings;
							if (bcs == null)
								bcs = BulletProAssetCreator.CreateCollisionSettingsAsset();
							else
							{
								#if UNITY_2018_3_OR_NEWER
								SettingsService.OpenProjectSettings("Project/Bullet Pro");
								#else
								EditorGUIUtility.PingObject(bcs);
								EditorUtility.FocusProjectWindow();
								Selection.activeObject = bcs;
								#endif
							}
						}
						GUI.color = baseGUIColor;
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.PropertyField(homingTags);
					}
					else EditorGUILayout.EndHorizontal();

					if (bp.homingTags.tagList == 0)
					{
						string str = "Selected object has no Homing Tags. It won't target anything.";

						if (!homingTagsFoldout.boolValue)
							str += "\nYou may need to unfold and activate the Homing Tags above";
						else
							str += "\nYou may need to click on some Homing Tags above";

						if (!canCollide.boolValue) str+= ".";
						else str += ", or click \"Use same tags as Collision\".";
						EditorGUILayout.HelpBox(str, MessageType.Warning);
					}
				}
				else if (!canCollide.boolValue)
					EditorGUILayout.HelpBox("Tags from the Collision Module will be used, but you did not enable it.", MessageType.Info);

				EditorGUI.indentLevel -= 1;
				EditorGUILayout.EndVertical();

				GUILayout.Space(12);
				GUI.color = boxColor;					
				EditorGUILayout.BeginVertical(homingCurve.FindPropertyRelative("enabled").boolValue ? enabledModuleStyle : disabledModuleStyle);
				GUI.color = baseGUIColor;					
				DrawDynamicCurveInspector(homingCurve);
			}

			EditorGUILayout.EndVertical();
		}

		void DrawLifespanModuleInspector()
		{
			// Lifespan
			GUILayout.Space(12);
			GUI.color = boxColor;
			EditorGUILayout.BeginVertical(hasLifespan.boolValue ? enabledModuleStyle : disabledModuleStyle);
			GUI.color = baseGUIColor;
			EditorStyles.label.fontStyle = FontStyle.Bold;
			EditorGUI.BeginChangeCheck();
			newVal = EditorGUILayout.Toggle("Limited Lifetime", hasLifespan.boolValue);
			if (EditorGUI.EndChangeCheck()) hasLifespan.boolValue = newVal;
			EditorStyles.label.fontStyle = defaultLabelStyle;
			if (hasLifespan.boolValue)
			{
				EditorGUI.indentLevel += 1;
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(lifespan);
				if (EditorGUI.EndChangeCheck())	DynamicParameterUtility.ClampAboveZero(lifespan);
				EditorGUI.indentLevel -= 1;
			}
			EditorGUILayout.EndVertical();
		}

		void DrawSpawnModuleInspector()
		{
			// attach to emitter
			GUILayout.Space(12);
			EditorStyles.label.fontStyle = FontStyle.Bold;
			GUI.color = boxColor;
			EditorGUILayout.BeginVertical(canBeChildOfEmitter ? enabledModuleStyle : disabledModuleStyle);
			GUI.color = baseGUIColor;
			EditorGUILayout.PropertyField(isChildOfEmitter, new GUIContent("Attach to Emitter", "If true, this bullet will be set as child of whatever emitted it (BulletEmitter or Bullet)."));
			EditorStyles.label.fontStyle = defaultLabelStyle;
			EditorGUILayout.EndVertical();

			// delay spawn
			GUILayout.Space(12);
			GUI.color = boxColor;
			EditorGUILayout.BeginVertical(delaySpawn.boolValue ? enabledModuleStyle : disabledModuleStyle);
			GUI.color = baseGUIColor;
			EditorStyles.label.fontStyle = FontStyle.Bold;
			EditorGUI.BeginChangeCheck();
			newVal = EditorGUILayout.Toggle("Delayed Spawn", delaySpawn.boolValue);
			if (EditorGUI.EndChangeCheck()) delaySpawn.boolValue = newVal;
			EditorStyles.label.fontStyle = defaultLabelStyle;
			if (delaySpawn.boolValue)
			{
				EditorGUI.indentLevel += 1;
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(timeBeforeSpawn);
				if (EditorGUI.EndChangeCheck()) DynamicParameterUtility.ClampAboveZero(timeBeforeSpawn);
				EditorGUI.indentLevel -= 1;
			}
			EditorGUILayout.EndVertical();

			// audio source
			GUILayout.Space(12);
			GUI.color = boxColor;
			EditorGUILayout.BeginVertical(canPlayAudio ? enabledModuleStyle : disabledModuleStyle);
			GUI.color = baseGUIColor;
			EditorStyles.label.fontStyle = FontStyle.Bold;
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(playAudioAtSpawn, new GUIContent("Audio on Spawn"));			
			if (EditorGUI.EndChangeCheck()) delaySpawn.boolValue = newVal;
			EditorStyles.label.fontStyle = defaultLabelStyle;
			if (canPlayAudio)
			{
				EditorGUI.indentLevel += 1;
				EditorGUILayout.PropertyField(audioClip);
				string audioWarning = "For performance reasons, when possible, try playing audio from Patterns rather than Bullets.";
				EditorGUILayout.HelpBox(audioWarning, MessageType.Info);
				EditorGUI.indentLevel -= 1;
			}
			EditorGUILayout.EndVertical();
		}

		void DrawPatternModuleInspector()
		{
			GUILayout.Space(12);
			string infoStr = "Patterns are small routines executed by the bullet, like moving or shooting. All the Patterns listed below can (and will) be executed simultaneously.";
			EditorGUILayout.HelpBox(infoStr, MessageType.Info);
			GUILayout.Space(12);
			GUIContent gc = new GUIContent("Die When All Patterns Are Done", "If checked, this bullet will automatically die when all the patterns listed below are over.");
			EditorGUILayout.PropertyField(dieWhenAllPatternsAreDone, gc);
			GUILayout.Space(12);
			rlist.DoLayoutList();
		}

		void DrawParameterModuleInspector()
		{
			GUILayout.Space(12);
			string infoStr = "If you need some more parameters, you can create your own from here.";
			EditorGUILayout.HelpBox(infoStr, MessageType.Info);
			GUILayout.Space(12);
			paramList.DoLayoutList();
		}

		void DrawBehaviourModuleInspector()
		{
			GUILayout.Space(12);
			string infoStr = "The list below can be filled with Prefabs, if you want to attach GameObjects to your bullet. The prefab must carry a BulletBehaviour component!";
			EditorGUILayout.HelpBox(infoStr, MessageType.Info);
			GUILayout.Space(12);
			behaviourList.DoLayoutList();
		}

		#endregion

		#region drawers from reorderable lists
		
		void PatternParamDrawer(Rect rect, int index, bool isActive, bool isFocused)
		{
			float yOffset = 2;
			rect = new Rect(rect.x, rect.y+yOffset, rect.width, rect.height);
			fieldHandler.DynamicParamField<PatternParams>(rect, new GUIContent("Pattern Style"), patternsShot.GetArrayElementAtIndex(index), 1, 0.75f);
		}

		void BulletBehaviourFieldDrawer(Rect rect, int index, bool isActive, bool isFocused)
		{
			float yOffset = 2;
			rect = new Rect(rect.x, rect.y+yOffset, rect.width, 16);
			float labelWidth = 55;
			float space = 10;
			Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
			Rect fieldRect = new Rect(rect.x + labelWidth + space, rect.y, rect.width - (labelWidth + space), rect.height);
			SerializedProperty abProp = behaviourPrefabs.GetArrayElementAtIndex(index);
			EditorGUI.LabelField(labelRect, "Prefab " + index.ToString());
			EditorGUI.PropertyField(fieldRect, abProp, GUIContent.none);
		}
		
		void CustomParameterDrawer(Rect rect, int index, bool isActive, bool isFocused)
		{
			float yOffset = 2;
			rect = new Rect(rect.x, rect.y+yOffset, rect.width, 16);
			
			float nameWidth = 100;
			float space = 10;
			float enumWidth = 105;
			float spaceTwo = 10;
			float remainingWidth = rect.width - (nameWidth + enumWidth + space + spaceTwo);

			float curX = rect.x;
			Rect nameRect = new Rect(curX, rect.y, nameWidth, rect.height); curX += nameWidth + space;
			Rect enumRect = new Rect(curX, rect.y, enumWidth, rect.height); curX += enumWidth + spaceTwo;
			Rect valueRect = new Rect(curX, rect.y, remainingWidth, rect.height);

			SerializedProperty cp = customParameters.GetArrayElementAtIndex(index);
			SerializedProperty cpName = cp.FindPropertyRelative("name");
			SerializedProperty cpType = cp.FindPropertyRelative("type");

			EditorGUI.PropertyField(nameRect, cpName, GUIContent.none);
			EditorGUI.PropertyField(enumRect, cpType, GUIContent.none);

			int typeIndex = cpType.enumValueIndex;
			string valueNameStr = "";

			bool isQuaternion = false;

			if (typeIndex == (int)ParameterType.AnimationCurve) valueNameStr = "animationCurveValue";
			else if (typeIndex == (int)ParameterType.Bool) valueNameStr = "boolValue";
			else if (typeIndex == (int)ParameterType.Bounds) valueNameStr = "boundsValue";
			else if (typeIndex == (int)ParameterType.Color) valueNameStr = "colorValue";
			else if (typeIndex == (int)ParameterType.Double) valueNameStr = "doubleValue";
			else if (typeIndex == (int)ParameterType.Float) valueNameStr = "floatValue";
			else if (typeIndex == (int)ParameterType.Gradient) valueNameStr = "gradientValue";
			else if (typeIndex == (int)ParameterType.Integer) valueNameStr = "intValue";
			else if (typeIndex == (int)ParameterType.Long) valueNameStr = "longValue";
			else if (typeIndex == (int)ParameterType.None) return;
			else if (typeIndex == (int)ParameterType.Object) valueNameStr = "objectReferenceValue";
			else if (typeIndex == (int)ParameterType.Quaternion)
			{
				isQuaternion = true;
				valueNameStr = "quaternionValue";
			}
			else if (typeIndex == (int)ParameterType.Rect) valueNameStr = "rectValue";
			else if (typeIndex == (int)ParameterType.Slider01) valueNameStr = "sliderValue";
			else if (typeIndex == (int)ParameterType.String) valueNameStr = "stringValue";
			else if (typeIndex == (int)ParameterType.Vector2) valueNameStr = "vector2Value";
			else if (typeIndex == (int)ParameterType.Vector3) valueNameStr = "vector3Value";
			else if (typeIndex == (int)ParameterType.Vector4) valueNameStr = "vector4Value";
			SerializedProperty cpValue = cp.FindPropertyRelative(valueNameStr);

			if (isQuaternion)
			{
				Vector4 quatV4 = new Vector4(cpValue.quaternionValue.x, cpValue.quaternionValue.y, cpValue.quaternionValue.z, cpValue.quaternionValue.w);
                EditorGUI.BeginChangeCheck();
                quatV4 = EditorGUI.Vector4Field(valueRect, GUIContent.none, quatV4);
                if (EditorGUI.EndChangeCheck()) cpValue.quaternionValue = new Quaternion(quatV4.x, quatV4.y, quatV4.z, quatV4.w);
			}
			else EditorGUI.PropertyField(valueRect, cpValue, GUIContent.none);
		}

		void AnimSpriteDrawer(Rect rect, int index, bool isActive, bool isFocused)
		{
			float yOffset = 2;
			rect = new Rect(rect.x, rect.y+yOffset, rect.width, 16);
			float labelWidth = 55;
			float space = 10;
			Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
			Rect fieldRect = new Rect(rect.x + labelWidth + space, rect.y, rect.width - (labelWidth + space), rect.height);
			SerializedProperty spriteProp = sprites.GetArrayElementAtIndex(index);
			EditorGUI.LabelField(labelRect, "Sprite " + index.ToString());
			EditorGUI.PropertyField(fieldRect, spriteProp, GUIContent.none);			
		}

		void ColliderDrawer(Rect rect, int index, bool isActive, bool isFocused)
		{
			float yOffset = 2;
			rect = new Rect(rect.x, rect.y+yOffset, rect.width, 16);
			SerializedProperty colProp = colliders.GetArrayElementAtIndex(index);
			EditorGUI.PropertyField(rect, colProp, GUIContent.none);			
		}

		#endregion

		#region animation import
		
		void ClampValueAboveZeroRelative(SerializedProperty parentProp, string propName)
		{
			SerializedProperty prop = parentProp.FindPropertyRelative(propName);
			if (propName == "_period") propName = "period";
		
			if (prop.floatValue < 0) prop.floatValue = 0;
		}

		void DrawCurveInspector(SerializedProperty curve)
		{
			FontStyle defaultLabelStyle = EditorStyles.label.fontStyle;

			// Curve name and enabling
			SerializedProperty curveEnabled = curve.FindPropertyRelative("enabled");
			EditorStyles.label.fontStyle = FontStyle.Bold;
			curveEnabled.boolValue = EditorGUILayout.Toggle(curve.displayName, curveEnabled.boolValue);
			EditorStyles.label.fontStyle = defaultLabelStyle;
			if (!curveEnabled.boolValue) return;

			// Curve properties

			EditorGUI.indentLevel += 1;

			if (curve == colorCurve)
				EditorGUILayout.PropertyField(colorEvolution);				

			bool hasLS = hasLifespan.boolValue;
			EditorGUILayout.PropertyField(curve.FindPropertyRelative("wrapMode"));
			SerializedProperty periodIsLifespan = curve.FindPropertyRelative("periodIsLifespan");
			EditorGUILayout.PropertyField(periodIsLifespan);
			if (!periodIsLifespan.boolValue)
				RandomizableRelativeField(curve, "_period");
			else if (!hasLS)
				EditorGUILayout.HelpBox("This curve's period id set to match this bullet's lifespan, but it has no limited lifespan.", MessageType.Error);

			SerializedProperty actualCurve = curve.FindPropertyRelative("curve");
			EditorGUILayout.PropertyField(actualCurve);
			ApplyAll();
			bool hasError = !BulletCurveDrawer.GoesFromZeroToOne(curve);
			//if (hasError)
			{
				// (Indirectly) due to Unity's Issue #930156, we have to display an extra line.
				// The line "if (hasError)" above can be uncommented to look nicer, but Unity will occasionally throw harmless exceptions.
				EditorGUI.BeginDisabledGroup(!hasError);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(hasError?"Please ensure X-axis runs from zero to one.":"This curve has no error.", hasError?EditorStyles.boldLabel:EditorStyles.label);
				if (hasError) GUI.color = new Color(1.0f, 0.6f, 0.4f, 1f);
				if (GUILayout.Button("Click to Fix Curve", EditorStyles.miniButton)) BulletCurveDrawer.RepairCurveFromZeroToOne(curve);
				GUI.color = baseGUIColor;
				EditorGUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
			}

			EditorGUI.indentLevel -= 1;
		}

		// For curve.period only, since it needs .FindPropertyRelative(). Made static so PatternParamInspector.cs can use it too.
		public static void RandomizableRelativeField(SerializedProperty parentProp, string propName)
		{
			SerializedProperty prop = parentProp.FindPropertyRelative(propName);
			if (propName == "_period") propName = "period";
			EditorGUILayout.PropertyField(prop);
		}

		// Imports animation curve data to movement, rotation or scale
		void ImportAnimationData(BulletAnimImportType importType)
		{
			#region preparation

			if (animationClip.objectReferenceValue == null)
			{
				Debug.LogWarning("Cannot import values : please provide an AnimationClip first.");
				return;
			}

			AnimationClip clip = animationClip.objectReferenceValue as AnimationClip;

			EditorCurveBinding[] ecbs = AnimationUtility.GetCurveBindings(clip);

			bool error = false;
			if (ecbs == null) error = true;
			else if (ecbs.Length == 0) error = true;
			if (error)
			{
				Debug.Log("Cannot import values : AnimationClip provided is empty.");
				return;
			}

			EditorCurveBinding xBinding = ecbs[0];
			EditorCurveBinding yBinding = ecbs[0];
			EditorCurveBinding zBinding = ecbs[0];
			EditorCurveBinding scaleBinding = ecbs[0];
			bool xExists = false;
			bool yExists = false;
			bool zExists = false;
			bool scaleExists = false;
			for (int i = 0; i < ecbs.Length; i++)
			{
				if (ecbs[i].propertyName == ("m_LocalPosition.x")) { xBinding = ecbs[i]; xExists = true; }
				if (ecbs[i].propertyName == ("m_LocalPosition.y")) { yBinding = ecbs[i]; yExists = true; }
				if (ecbs[i].propertyName == ("localEulerAnglesRaw.z")) { zBinding = ecbs[i]; zExists = true; }
				if (ecbs[i].propertyName.Contains("m_LocalScale.")) { scaleBinding = ecbs[i]; scaleExists = true; }
			}

			#endregion

			#region error checking

			if (importType == BulletAnimImportType.Position && !xExists && !yExists)
			{
				Debug.LogError("Cannot import Movement : provided AnimationClip does not contain X motion nor Y motion.");
				return;
			}
			if (importType == BulletAnimImportType.Rotation && !zExists)
			{
				Debug.LogError("Cannot import Rotation : provided AnimationClip does not contain this motion.");
				return;
			}
			if (importType == BulletAnimImportType.Scale && !scaleExists)
			{
				Debug.LogError("Cannot import Scale : provided AnimationClip does not contain this motion.");
				return;
			}
			if (importType == BulletAnimImportType.All && !xExists && !yExists && !zExists && !scaleExists)
			{
				Debug.LogError("Cannot import data : provided AnimationClip does not contain any available motion.");
				return;
			}

			#endregion

			#region X Curve

			if (xExists && (importType == BulletAnimImportType.Position || importType == BulletAnimImportType.All))
			{
				// Simple properties
				xMoveFromAnim.FindPropertyRelative("enabled").boolValue = true;
				xMoveFromAnim.FindPropertyRelative("wrapMode").enumValueIndex = 3; // Loop
				xMoveFromAnim.FindPropertyRelative("periodIsLifespan").boolValue = false;

				// Curve
				AnimationCurve curveX = AnimationUtility.GetEditorCurve(clip, xBinding);
				SerializedProperty curve = xMoveFromAnim.FindPropertyRelative("curve");
				curve.animationCurveValue = curveX;

				AnimationCurve curveY = AnimationUtility.GetEditorCurve(clip, yBinding);
				float lastY = curveY.keys[curveY.keys.Length - 1].time;

				// Period
				SerializedProperty period = xMoveFromAnim.FindPropertyRelative("_period");
				if (importType == BulletAnimImportType.Position)
				{
					period.floatValue = curveX.keys[curveX.keys.Length - 1].time;
					if (yExists) period.floatValue = Mathf.Max(period.floatValue, lastY);
				}
				else if (importType == BulletAnimImportType.All)
				{
					AnimationCurve curveZ = AnimationUtility.GetEditorCurve(clip, zBinding);
					AnimationCurve curveS = AnimationUtility.GetEditorCurve(clip, scaleBinding);
					float lastX = curveX.keys[curveX.keys.Length - 1].time;
					float lastZ = curveZ.keys[curveZ.keys.Length - 1].time;
					float lastS = curveS.keys[curveS.keys.Length - 1].time;
					period.floatValue = lastX;
					if (yExists) period.floatValue = Mathf.Max(period.floatValue, lastY);
					if (zExists) period.floatValue = Mathf.Max(period.floatValue, lastZ);
					if (scaleExists) period.floatValue = Mathf.Max(period.floatValue, lastS);
				}

				// Curve polish : ensure it's from 0 to period
				List<Keyframe> kfs = new List<Keyframe>();
				bool hasZero = false;
				bool hasMax = false;
				for (int i = 0; i < curve.animationCurveValue.keys.Length; i++)
				{
					if (curve.animationCurveValue.keys[i].time == 0) hasZero = true;
					if (curve.animationCurveValue.keys[i].time == period.floatValue) hasMax = true;
				}
				if (!hasZero) kfs.Add(new Keyframe(0, curve.animationCurveValue.keys[0].value));
				for (int i = 0; i < curve.animationCurveValue.keys.Length; i++)
					kfs.Add(curve.animationCurveValue.keys[i]);
				if (!hasMax) kfs.Add(new Keyframe(period.floatValue, curve.animationCurveValue.keys[curve.animationCurveValue.keys.Length - 1].value));

				// Make from 0 to 1, with 0 as first value
				float firstValue = kfs[0].value;
				for (int i = 0; i < kfs.Count; i++)
				{
					float curVal = kfs[i].value;
					float curTime = kfs[i].time;
					kfs[i] = new Keyframe(curTime / period.floatValue, curVal - firstValue);
				}

				curve.animationCurveValue = new AnimationCurve(kfs.ToArray());
			}

			#endregion

			#region Y Curve

			if (yExists && (importType == BulletAnimImportType.Position || importType == BulletAnimImportType.All))
			{
				// Simple properties
				yMoveFromAnim.FindPropertyRelative("enabled").boolValue = true;
				yMoveFromAnim.FindPropertyRelative("wrapMode").enumValueIndex = 3; // Loop
				yMoveFromAnim.FindPropertyRelative("periodIsLifespan").boolValue = false;

				// Curve
				AnimationCurve curveY = AnimationUtility.GetEditorCurve(clip, yBinding);
				SerializedProperty curve = yMoveFromAnim.FindPropertyRelative("curve");
				curve.animationCurveValue = curveY;

				AnimationCurve curveX = AnimationUtility.GetEditorCurve(clip, xBinding);
				float lastX = curveX.keys[curveX.keys.Length - 1].time;

				// Period
				SerializedProperty period = yMoveFromAnim.FindPropertyRelative("_period");
				if (importType == BulletAnimImportType.Position)
				{
					period.floatValue = curveY.keys[curveY.keys.Length - 1].time;
					if (xExists) period.floatValue = Mathf.Max(period.floatValue, lastX);
				}
				else if (importType == BulletAnimImportType.All)
				{
					AnimationCurve curveZ = AnimationUtility.GetEditorCurve(clip, zBinding);
					AnimationCurve curveS = AnimationUtility.GetEditorCurve(clip, scaleBinding);
					float lastY = curveY.keys[curveY.keys.Length - 1].time;
					float lastZ = curveZ.keys[curveZ.keys.Length - 1].time;
					float lastS = curveS.keys[curveS.keys.Length - 1].time;
					period.floatValue = lastY;
					if (xExists) period.floatValue = Mathf.Max(period.floatValue, lastX);
					if (zExists) period.floatValue = Mathf.Max(period.floatValue, lastZ);
					if (scaleExists) period.floatValue = Mathf.Max(period.floatValue, lastS);
				}

				// Curve polish : ensure it's from 0 to period
				List<Keyframe> kfs = new List<Keyframe>();
				bool hasZero = false;
				bool hasMax = false;
				for (int i = 0; i < curve.animationCurveValue.keys.Length; i++)
				{
					if (curve.animationCurveValue.keys[i].time == 0) hasZero = true;
					if (curve.animationCurveValue.keys[i].time == period.floatValue) hasMax = true;
				}
				if (!hasZero) kfs.Add(new Keyframe(0, curve.animationCurveValue.keys[0].value));
				for (int i = 0; i < curve.animationCurveValue.keys.Length; i++)
					kfs.Add(curve.animationCurveValue.keys[i]);
				if (!hasMax) kfs.Add(new Keyframe(period.floatValue, curve.animationCurveValue.keys[curve.animationCurveValue.keys.Length - 1].value));

				// Make from 0 to 1, with 0 as first value
				float firstValue = kfs[0].value;
				for (int i = 0; i < kfs.Count; i++)
				{
					float curVal = kfs[i].value;
					float curTime = kfs[i].time;
					kfs[i] = new Keyframe(curTime / period.floatValue, curVal - firstValue);
				}

				curve.animationCurveValue = new AnimationCurve(kfs.ToArray());
			}

			#endregion

			#region Z Curve

			if (zExists && (importType == BulletAnimImportType.Rotation || importType == BulletAnimImportType.All))
			{
				// Simple properties
				rotationFromAnim.FindPropertyRelative("enabled").boolValue = true;
				rotationFromAnim.FindPropertyRelative("wrapMode").enumValueIndex = 3; // Loop
				rotationFromAnim.FindPropertyRelative("periodIsLifespan").boolValue = false;

				// Curve
				AnimationCurve curveZ = AnimationUtility.GetEditorCurve(clip, zBinding);
				SerializedProperty curve = rotationFromAnim.FindPropertyRelative("curve");
				curve.animationCurveValue = curveZ;

				// Period
				SerializedProperty period = rotationFromAnim.FindPropertyRelative("_period");
				if (importType == BulletAnimImportType.Rotation)
				{
					period.floatValue = curveZ.keys[curveZ.keys.Length - 1].time;
				}
				else if (importType == BulletAnimImportType.All)
				{
					AnimationCurve curveX = AnimationUtility.GetEditorCurve(clip, xBinding);
					AnimationCurve curveY = AnimationUtility.GetEditorCurve(clip, yBinding);
					AnimationCurve curveS = AnimationUtility.GetEditorCurve(clip, scaleBinding);
					float lastX = curveX.keys[curveX.keys.Length - 1].time;
					float lastY = curveY.keys[curveY.keys.Length - 1].time;
					float lastZ = curveZ.keys[curveZ.keys.Length - 1].time;
					float lastS = curveS.keys[curveS.keys.Length - 1].time;
					period.floatValue = lastZ;
					if (xExists) period.floatValue = Mathf.Max(period.floatValue, lastX);
					if (yExists) period.floatValue = Mathf.Max(period.floatValue, lastY);
					if (scaleExists) period.floatValue = Mathf.Max(period.floatValue, lastS);
				}

				// Curve polish : ensure it's from 0 to period
				List<Keyframe> kfs = new List<Keyframe>();
				bool hasZero = false;
				bool hasMax = false;
				for (int i = 0; i < curve.animationCurveValue.keys.Length; i++)
				{
					if (curve.animationCurveValue.keys[i].time == 0) hasZero = true;
					if (curve.animationCurveValue.keys[i].time == period.floatValue) hasMax = true;
				}
				if (!hasZero) kfs.Add(new Keyframe(0, curve.animationCurveValue.keys[0].value));
				for (int i = 0; i < curve.animationCurveValue.keys.Length; i++)
					kfs.Add(curve.animationCurveValue.keys[i]);
				if (!hasMax) kfs.Add(new Keyframe(period.floatValue, curve.animationCurveValue.keys[curve.animationCurveValue.keys.Length - 1].value));

				// Make from 0 to 1, with 0 as first value
				float firstValue = kfs[0].value;
				for (int i = 0; i < kfs.Count; i++)
				{
					float curVal = kfs[i].value;
					float curTime = kfs[i].time;
					kfs[i] = new Keyframe(curTime / period.floatValue, curVal - firstValue);
				}

				curve.animationCurveValue = new AnimationCurve(kfs.ToArray());
			}

			#endregion

			#region Scale Curve

			if (scaleExists && (importType == BulletAnimImportType.Scale || importType == BulletAnimImportType.All))
			{
				// Simple properties
				scaleFromAnim.FindPropertyRelative("enabled").boolValue = true;
				scaleFromAnim.FindPropertyRelative("wrapMode").enumValueIndex = 3; // Loop
				scaleFromAnim.FindPropertyRelative("periodIsLifespan").boolValue = false;

				// Curve
				AnimationCurve curveS = AnimationUtility.GetEditorCurve(clip, scaleBinding);
				SerializedProperty curve = scaleFromAnim.FindPropertyRelative("curve");
				curve.animationCurveValue = curveS;

				// Period
				SerializedProperty period = scaleFromAnim.FindPropertyRelative("_period");
				if (importType == BulletAnimImportType.Scale)
				{
					period.floatValue = curveS.keys[curveS.keys.Length - 1].time;
				}
				else if (importType == BulletAnimImportType.All)
				{
					AnimationCurve curveX = AnimationUtility.GetEditorCurve(clip, xBinding);
					AnimationCurve curveY = AnimationUtility.GetEditorCurve(clip, yBinding);
					AnimationCurve curveZ = AnimationUtility.GetEditorCurve(clip, zBinding);
					float lastX = curveX.keys[curveX.keys.Length - 1].time;
					float lastY = curveY.keys[curveY.keys.Length - 1].time;
					float lastZ = curveZ.keys[curveZ.keys.Length - 1].time;
					float lastS = curveS.keys[curveS.keys.Length - 1].time;
					period.floatValue = lastS;
					if (xExists) period.floatValue = Mathf.Max(period.floatValue, lastX);
					if (yExists) period.floatValue = Mathf.Max(period.floatValue, lastY);
					if (zExists) period.floatValue = Mathf.Max(period.floatValue, lastZ);
				}

				// Curve polish : ensure it's from 0 to period
				List<Keyframe> kfs = new List<Keyframe>();
				bool hasZero = false;
				bool hasMax = false;
				for (int i = 0; i < curve.animationCurveValue.keys.Length; i++)
				{
					if (curve.animationCurveValue.keys[i].time == 0) hasZero = true;
					if (curve.animationCurveValue.keys[i].time == period.floatValue) hasMax = true;
				}
				if (!hasZero) kfs.Add(new Keyframe(0, curve.animationCurveValue.keys[0].value));
				for (int i = 0; i < curve.animationCurveValue.keys.Length; i++)
					kfs.Add(curve.animationCurveValue.keys[i]);
				if (!hasMax) kfs.Add(new Keyframe(period.floatValue, curve.animationCurveValue.keys[curve.animationCurveValue.keys.Length - 1].value));

				// Make from 0 to 1, but don't touch scale value
				for (int i = 0; i < kfs.Count; i++)
				{
					float curVal = kfs[i].value;
					float curTime = kfs[i].time;
					kfs[i] = new Keyframe(curTime / period.floatValue, curVal);
				}

				curve.animationCurveValue = new AnimationCurve(kfs.ToArray());
			}

			#endregion
		}

		#endregion

		#region dynamic-related

		void DrawDynamicCurveInspector(SerializedProperty curve)
		{
			// with iterator
			/* */
			// Curve name and enabling
			FontStyle defaultLabelStyle = EditorStyles.label.fontStyle;
			SerializedProperty spIterator = curve.Copy();
			spIterator.NextVisible(true); // enabled
			EditorStyles.label.fontStyle = FontStyle.Bold;
			spIterator.boolValue = EditorGUILayout.Toggle(curve.displayName, spIterator.boolValue);
			EditorStyles.label.fontStyle = defaultLabelStyle;
			if (!spIterator.boolValue) return;

			// Curve properties

			EditorGUI.indentLevel += 1;

			if (curve == colorCurve)
			{
				EditorGUILayout.PropertyField(colorEvolution);				
				EditorGUILayout.PropertyField(evolutionBlendWithBaseColor, new GUIContent("Blend Base Color", "What the gradient should do to this bullet's initial Color."));
			}

			bool hasLS = hasLifespan.boolValue;
			spIterator.NextVisible(false); // wrapmode

			EditorGUILayout.PropertyField(spIterator);
			spIterator.NextVisible(false); // period is lifespan
			EditorGUILayout.PropertyField(spIterator);
			if (!DynamicParameterUtility.GetBool(spIterator, false, false))
			{
				spIterator.NextVisible(false); // period
				SerializedProperty spPeriod = spIterator.Copy();
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(spPeriod);
				if (EditorGUI.EndChangeCheck())
					DynamicParameterUtility.ClampAboveZero(spPeriod);
			}
			else
			{
				spIterator.NextVisible(false); // period
				if (!hasLS)
					EditorGUILayout.HelpBox("This curve's period id set to match this bullet's lifespan, but it has no limited lifespan.", MessageType.Error);
			}

			spIterator.NextVisible(false); // curve
			EditorGUILayout.PropertyField(spIterator);
			ApplyAll();

			EditorGUI.indentLevel -= 1;
			/* */

			// without iterator
			/* *
			// Curve name and enabling
			FontStyle defaultLabelStyle = EditorStyles.label.fontStyle;
			SerializedProperty curveEnabled = curve.FindPropertyRelative("enabled");
			EditorStyles.label.fontStyle = FontStyle.Bold;
			curveEnabled.boolValue = EditorGUILayout.Toggle(curve.displayName, curveEnabled.boolValue);
			EditorStyles.label.fontStyle = defaultLabelStyle;
			if (!curveEnabled.boolValue) return;
			
			// Curve properties

			EditorGUI.indentLevel += 1;

			if (curve == colorCurve)
				EditorGUILayout.PropertyField(endColor);				

			bool hasLS = hasLifespan.boolValue;
			EditorGUILayout.PropertyField(curve.FindPropertyRelative("wrapMode"));
			SerializedProperty periodIsLifespan = curve.FindPropertyRelative("periodIsLifespan");
			EditorGUILayout.PropertyField(periodIsLifespan);
			if (!DynamicParameterUtility.GetBool(periodIsLifespan, false, false))
				EditorGUILayout.PropertyField(curve.FindPropertyRelative("period"));
			else if (!hasLS)
				EditorGUILayout.HelpBox("This curve's period id set to match this bullet's lifespan, but it has no limited lifespan.", MessageType.Error);

			SerializedProperty actualCurve = curve.FindPropertyRelative("curve");
			EditorGUILayout.PropertyField(actualCurve);
			ApplyAll();

			EditorGUI.indentLevel -= 1;
			/* */
		}

		#endregion

		#region Bullet preview (sprite + hitbox)

		// In the Collision module, draws the bullet sprite with hitboxes
		void DrawBulletPreview(bool canDraw)
		{
			// first, we check if the preview is drawable.
			if (!canDraw)
			{
				EditorGUILayout.HelpBox("Bullet preview cannot be drawn.\nSelected bullet needs to be visible and have a sprite. (In the Graphics section)", MessageType.Warning);
				return;
			}
			EditorGUILayout.PropertyField(preview);
		}

		// Is the bullet preview drawable ? (same sprite, same colliders)
		bool CanDrawPreview()
		{
			if (isVisible.boolValue)
				if (renderMode.enumValueIndex == 0)
				{
					bool spriteExists = false;
					if (!DynamicParameterUtility.GetBool(animated))
						spriteExists = DynamicParameterUtility.GetFixedObject(sprite) != null;
					else
						spriteExists = DynamicParameterUtility.GetFixedObject(sprites.GetArrayElementAtIndex(0)) != null;

					return spriteExists;
				}
			
			return false;
		}

		// Stores sprite into a drawable texture for the inspector
		void UpdatePreviewParams()
		{
			bool canBeAnimated = DynamicParameterUtility.GetBool(animated);
			Object defaultSprite = DynamicParameterUtility.GetFixedObject(sprite);
			Object firstAnimSprite = DynamicParameterUtility.GetFixedObject(sprites.GetArrayElementAtIndex(0));
			Sprite spriteToUse = (canBeAnimated ? (firstAnimSprite ?? defaultSprite) : defaultSprite) as Sprite;
			preview.FindPropertyRelative("sprite").objectReferenceValue = spriteToUse;
		}

		// Redraws the colliders onto a texture for the preview.
		void UpdatePreviewTexture()
		{
			Sprite sprite = preview.FindPropertyRelative("sprite").objectReferenceValue as Sprite;

			if (sprite == null) return;

			Texture2D tex = new Texture2D(128, 128);
			Color emptyColor = new Color(0, 0, 0, 0);
			for (int i = 0; i < tex.width; i++)
				for (int j = 0; j < tex.height; j++)
					tex.SetPixel(i, j, emptyColor);

			// figure out what distance do these 128 pixels cover
			Vector3 totalSize = sprite.bounds.size;
			float maxDistCoveredByPreview = Mathf.Max(totalSize.x, totalSize.y);

			// constants
			float worldToPixel = tex.width / maxDistCoveredByPreview;
			Vector2 spritePivot = new Vector2(sprite.pivot.x * tex.width / sprite.rect.width, sprite.pivot.y * tex.height / sprite.rect.height);

			Color gizmoColor = preview.FindPropertyRelative("gizmoColor").colorValue;

			// per-collider data
			int length = colliders.arraySize;
			int[] colTypes = new int[length];
			int[] pixelSizes = new int[length];
			Vector2[] pixelCenters = new Vector2[length];
			if (colliders.arraySize > 0)
				for (int i = 0; i < colliders.arraySize; i++)
				{
					SerializedProperty col = colliders.GetArrayElementAtIndex(i);

					// per-type calculations
					SerializedProperty colType = col.FindPropertyRelative("colliderType");
					colTypes[i] = colType.enumValueIndex;

					// lines only
					if (colType.enumValueIndex == (int)BulletColliderType.Line)
					{
						Vector2 lineStart = col.FindPropertyRelative("lineStart").vector2Value * worldToPixel + spritePivot;
						Vector2 lineEnd = col.FindPropertyRelative("lineEnd").vector2Value * worldToPixel + spritePivot;

						if (lineStart != lineEnd)
							DrawLineOnTex(tex, lineStart, lineEnd, gizmoColor);

						continue;
					}

					// circles only
					Vector2 worldCenter = col.FindPropertyRelative("offset").vector3Value;
					float worldSize = col.FindPropertyRelative("size").floatValue;
					
					// hitbox pivot is at this pixel
					Vector2 pixelCenter = spritePivot + worldCenter * worldToPixel;
					pixelCenters[i] = pixelCenter;

					// hitbox size is this int
					int pixelSize = Mathf.FloorToInt(worldSize * worldToPixel);
					pixelSizes[i] = pixelSize;
				}

			// actual texture drawing
			for (int i = 0; i < tex.width; i++)
				for (int j = 0; j < tex.height; j++)
					tex.SetPixel(i, j, GetPreviewTexColor(tex, i, j, colTypes, pixelCenters, pixelSizes, gizmoColor));

			tex.Apply();

			// assign this new texture
			bp.preview.collidersTex = tex;
			EditorUtility.SetDirty(bp);
		}

		// Draws a Line Collider on the texture.
		void DrawLineOnTex(Texture2D tex, Vector2 lineStart, Vector2 lineEnd, Color gizmoColor)
		{
			// determine how we will count pixels
			Vector2 line = lineEnd - lineStart;

			float pixelCount = Mathf.Max(Mathf.Abs(line.x), Mathf.Abs(line.y));
			float invPc = 1 / pixelCount;
			int reso = tex.width;

			// actual drawing
			for (int i = 0; i < pixelCount; i++)
			{
				float px = Mathf.Lerp(lineStart.x, lineEnd.x, i * invPc);
				float py = Mathf.Lerp(lineStart.y, lineEnd.y, i * invPc);

				// if we went out of bounds, stop here
				if (px < 0) continue;
				if (py < 0) continue;
				if (px >= reso) continue;
				if (py >= reso) continue;

				Color cur = tex.GetPixel((int)px, (int)py);
				Color newCol = AlphaBlend(gizmoColor, cur);
				tex.SetPixel((int)px, (int)py, newCol);
			}
		}
 

		// Finds if a pixel should be painted - for circles only
		Color GetPreviewTexColor(Texture2D tex, int x, int y, int[] colTypes, Vector2[] pixelCenters, int[] pixelSizes, Color gizmoColor)
		{
			Color gizmoTranspColor = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 0.1f);

			Color cur = tex.GetPixel(x, y);
			// initialize color only if a Line Collider hasn't yet passed here
			//Color defaultColor = new Color(0, 0, 0, 0);
			//if (cur != gizmoColor) cur = defaultColor;

			// does a collider gizmo overlap this pixel?
			if (colliders.arraySize > 0)
				for (int i = 0; i < colliders.arraySize; i++)
				{
					/* *
					if (colTypes[i] == (int)BulletColliderType.Line)
					{
						if (LineContainsPoint(lineStarts[i], lineEnds[i], (float)x, (float)y))
						//if (Mathf.Abs(x - (int)pixelCenters[i].x) < 2 && y >= pixelCenters[i].y && y < pixelCenters[i].y + pixelSizes[i])
							cur = AlphaBlend(gizmoColor, cur);
					}

					else /* */ if (colTypes[i] == (int)BulletColliderType.Circle)
					{
						int dx = x - (int)pixelCenters[i].x;
						int dy = y - (int)pixelCenters[i].y;

						float dist2 = dx * dx + dy * dy;
						float rad2 = pixelSizes[i] * pixelSizes[i];

						if (dist2 < rad2)
						{
							if (dist2 > (rad2 - 1.5f * tex.width))
								cur = AlphaBlend(gizmoColor, cur);
							else
								cur = AlphaBlend(gizmoTranspColor, cur);
						}
					}

				}

			return cur;
		}

		// Is the point at (x, y) located on the [start, end] segment?
		bool LineContainsPoint(Vector2 start, Vector2 end, float x, float y)
		{
			if (end == start) return false;

			float cross = (y - start.y) * (end.x - start.x) - (x - start.x) * (end.y - start.y);
			return Mathf.Abs(cross) < 1f;
		}

		Color AlphaBlend(Color src, Color dst)
		{
			Color col = src*src.a + dst*(1-src.a);
			col.a = Mathf.Lerp(src.a, 1.0f, dst.a);

			return col;
		}

		#endregion

	}

	public enum BulletAnimImportType { Position, Rotation, Scale, All }
}

