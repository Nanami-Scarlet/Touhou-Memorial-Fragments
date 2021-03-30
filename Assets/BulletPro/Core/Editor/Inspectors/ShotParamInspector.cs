using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	[CustomEditor(typeof(ShotParams))]
	public class ShotParamInspector : EmissionParamsInspector
	{
		#region helper vars

		// Bullet spawn place
		SerializedProperty simultaneousBulletsPerFrame, bulletSpawnArray;

		// Default bullet params
		SerializedProperty bulletParams;

		// Texture-related
		SerializedProperty textureSliceRate, ingameSizeOfTexture, inputTexture, outputTexture, hasCompactEditor;

		// Layout modifier list
		SerializedProperty modifiers;

		// Editor foldouts
		SerializedProperty[] foldouts;

		// targets
		ShotParams sp;

		// For texture reading error messages
		bool textureIsNotReadable;

		// Modifiers as reorderable list and callback helpers, so that values + texture recalculation is done *after* the list updating
		ReorderableList rlist;
		bool shouldRecalcLast;

		// at the end of the frame, if set to true, refreshes layout, texture and extreme values
		bool shouldRecalcAll, bulletAmountChangedThisFrame;

		// Caching fields and methods obtained via reflection
		MethodInfo doFloatFieldMethod;
		object recycledEditor;

		// Button textures
		Texture2D eyeOn, eyeOff;

		// Depends on dynamic parameters
		int maxNumberOfBullets;

		// for modifier enum
		GUIContent[] modOptions;

		// output texture style
		Color outputTextureBgColor, outputTextureBulletColor, outputTextureBorderColor, outputTextureGridColor;
		int borderWidth;
		Vector2Int gridOffset;

		#endregion

		#region inherited methods
		
		public override void OnEnable()
		{
			if (target == null)
			{
				OnUnselected();
				DestroyImmediate(this);
				return;
			}
			
			base.OnEnable();

			eyeOn = EditorGUIUtility.FindTexture("animationvisibilitytoggleon");
			eyeOff = EditorGUIUtility.FindTexture("animationvisibilitytoggleoff");

			outputTextureBgColor = new Color(0.2f, 0.3f, 0.7f);
			outputTextureBulletColor = Color.white;
			outputTextureBorderColor = Color.white;
			outputTextureGridColor = Color.Lerp(outputTextureBgColor, Color.white, 0.1f);
			borderWidth = 3;
			gridOffset = new Vector2Int(3, 3);

			sp = target as ShotParams;
			// set custom default values

			if (!sp.hasBeenSerializedOnce)
				sp.FirstInitialization();

			if (!EditorApplication.isPlaying)
				sp.SetUniqueIndex();

			#region serialized properties

			bulletParams = serializedObject.FindProperty("bulletParams");
			simultaneousBulletsPerFrame = serializedObject.FindProperty("simultaneousBulletsPerFrame");
			bulletSpawnArray = serializedObject.FindProperty("bulletSpawns");

			modifiers = serializedObject.FindProperty("modifiers");

			textureSliceRate = serializedObject.FindProperty("textureSliceRate");
			ingameSizeOfTexture = serializedObject.FindProperty("ingameSizeOfTexture");
			inputTexture = serializedObject.FindProperty("inputTexture");
			outputTexture = serializedObject.FindProperty("outputTexture");
			hasCompactEditor = outputTexture.FindPropertyRelative("hasCompactEditor");

			#endregion

			#region reorderable list setup

			rlist = new ReorderableList(serializedObject, modifiers, true, true, true, true);

			rlist.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Layout Modifier Stack (from top to bottom)"); };
			rlist.drawElementCallback = LayoutModifierDrawer;
			rlist.onReorderCallback += (ReorderableList list) => { shouldRecalcAll = true; };
			rlist.onRemoveCallback += (ReorderableList list) =>
			{
				ShotModifier sm = sp.modifiers[list.index];
				if (sm.modifierType == ShotModifierType.SetBulletParams)
					EmitterProfileUtility.SetParentOfBullet(sm.bulletParams, null, this);
				//BulletParams bp = modifiers.GetArrayElementAtIndex(list.index).FindPropertyRelative("bulletParams").objectReferenceValue as BulletParams;
				//if (bp != null) SetParent(bp, null);
				modifiers.DeleteArrayElementAtIndex(list.index);
				shouldRecalcAll = true;
			};
			rlist.onAddCallback += (ReorderableList list) =>
			{
				modifiers.arraySize++;
				shouldRecalcLast = true;
				SerializedProperty mod = modifiers.GetArrayElementAtIndex(modifiers.arraySize-1);
				SerializedProperty selectCol = mod.FindPropertyRelative("selectionColor");
				if (selectCol.colorValue.a < 0.1f)
				{
					Color col = Random.ColorHSV(0, 1, 0.5f, 1, 0.7f, 0.9f, 0.3f, 0.3f);
					selectCol.colorValue = col;
				}
				mod.FindPropertyRelative("selectionRectsVisible").boolValue = true;
				DynamicParameterUtility.SetFixedObject(mod.FindPropertyRelative("bulletParams"), null, true);
				//mod.FindPropertyRelative("bulletParams").objectReferenceValue = null;
				shouldRecalcAll = true;
			};
			//rlist.elementHeightCallback += (int index) => { return EditorGUIUtility.singleLineHeight * 2; };

			#endregion

			// foldout indexes are all messy because of various inspector tests,could be cleaned up later
			// foldout 7 is used for bulletspawn list
			foldouts = new SerializedProperty[9];

			for (int i = 0; i < foldouts.Length; i++)
				foldouts[i] = serializedObject.FindProperty("foldout" + i.ToString());

			textureIsNotReadable = false;

			Undo.undoRedoPerformed += OnUndoRedo;
			EditorApplication.update += Update;

			shouldRecalcAll = true;

			// Obtain internal methods for customizing float fields later on
			System.Type editorGUIType = typeof(EditorGUI);
			System.Type RecycledTextEditorType = Assembly.GetAssembly(editorGUIType).GetType("UnityEditor.EditorGUI+RecycledTextEditor");
			System.Type[] argumentTypes = new System.Type[] { RecycledTextEditorType, typeof(Rect), typeof(Rect), typeof(int), typeof(float), typeof(string), typeof(GUIStyle), typeof(bool) };
			doFloatFieldMethod = editorGUIType.GetMethod("DoFloatField", BindingFlags.NonPublic | BindingFlags.Static, null, argumentTypes, null);
			FieldInfo fieldInfo = editorGUIType.GetField("s_RecycledEditor", BindingFlags.NonPublic | BindingFlags.Static);
			recycledEditor = fieldInfo.GetValue(null);

			modOptions = new GUIContent[]
			{
				new GUIContent("Spread (per bullet)"),
				new GUIContent("Spread (total)"),
				new GUIContent("Global Translation"),
				new GUIContent("Local Translation"),
				new GUIContent("Rotation"),
				new GUIContent("Set Pivot"),
				new GUIContent("Rotate Around Pivot"),
				new GUIContent("Scale Layout"),
				new GUIContent("Reset Coordinates"),
				new GUIContent("X Spacing (per bullet)"),
				new GUIContent("X Spacing (total)"),
				new GUIContent("Y Spacing (per bullet)"),
				new GUIContent("Y Spacing (total)"),
				new GUIContent("Flip Orientation"),
				new GUIContent("Look At Point"),
				new GUIContent("Look Away From Point"),
				new GUIContent("Only Some Bullets"),
				new GUIContent("Set Bullet Params")
			};
		}

		void OnUndoRedo()
		{
			shouldRecalcAll = true;
		}

		public override void OnDisable()
		{
			OnUnselected();
		}

		public override void OnUnselected()
		{
			Undo.undoRedoPerformed -= OnUndoRedo;
			EditorApplication.update -= Update;

			base.OnUnselected();
		}

		void Update()
		{
			//Debug.Log(Undo.GetCurrentGroup());
		}

        public override bool UseDefaultMargins() { return false; }

		public override void OnInspectorGUI()
		{
			// Debug
			//System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
			//stopwatch.Start();

			base.OnInspectorGUI();

			#region overall parameters, bullet type // does not yield undo or apply

			// BulletParams used here : they're randomizable among an array
			fieldHandler.LayoutDynamicParamField<BulletParams>(new GUIContent("Default Bullet used in this Shot"), bulletParams);

			#endregion

			#region total number of bullets, and layout modifier list

			// Prepare the undo
			string undoGroupName = "Editing bullets from " + sp.name;

			// Number of bullets spawned per wave : will determine array sizes later
			GUILayout.Space(12);
			
			maxNumberOfBullets = DynamicParameterUtility.GetHighestValueOfInt(simultaneousBulletsPerFrame, true);
			if (maxNumberOfBullets != bulletSpawnArray.arraySize) // must be refreshed every frame
			{
				// if some bullet spawns are to be removed and they had a unique alternate bullet style, unparent this style
				if (maxNumberOfBullets < bulletSpawnArray.arraySize)
				{
					// list all alternate styles being removed
					List<BulletParams> removedStyles = new List<BulletParams>();
					for (int i = 0; i < (bulletSpawnArray.arraySize-maxNumberOfBullets); i++)
					{
						SerializedProperty removedSpawn = bulletSpawnArray.GetArrayElementAtIndex(bulletSpawnArray.arraySize-(i+1));
						if (!removedSpawn.FindPropertyRelative("useDifferentBullet").boolValue) continue;
						Object removedBP = removedSpawn.FindPropertyRelative("bulletParams").objectReferenceValue;
						if (removedBP == null) continue;
						BulletParams removedBullet = removedBP as BulletParams;
						if (!removedStyles.Contains(removedBullet)) removedStyles.Add(removedBullet);
					}
					
					// among all these styles, only remove the ones not used in any other bullet spawn.
					if (removedStyles.Count > 0)
					{
						// there's no "elsewhere" if no bullet spawn remains at all
						if (maxNumberOfBullets > 0)
						{
							// being used elsewhere gets the bullet excluded from the list
							for (int i = 0; i < maxNumberOfBullets; i++)
							{
								SerializedProperty remainingSpawn = bulletSpawnArray.GetArrayElementAtIndex(i);
								if (!remainingSpawn.FindPropertyRelative("useDifferentBullet").boolValue) continue;
								Object remainingBP = remainingSpawn.FindPropertyRelative("bulletParams").objectReferenceValue;
								if (remainingBP == null) continue;
								BulletParams remainingBullet = remainingBP as BulletParams;
								if (removedStyles.Contains(remainingBullet))
								{
									removedStyles.Remove(remainingBullet);
									continue;
								}
							}
						}
						
						// retesting because if could've been emptied by previous operations
						if (removedStyles.Count > 0)
							for (int i = 0; i > removedStyles.Count; i++)
								SetParent(removedStyles[i], null);
					}
				}

				bulletSpawnArray.arraySize = maxNumberOfBullets;
				bulletAmountChangedThisFrame = true;
				foldouts[7].boolValue = false;
				shouldRecalcAll = true;
				Repaint();
			}

			string potentialMax = DynamicParameterUtility.IsFixed(simultaneousBulletsPerFrame) ? "" : "up to ";
			string plural = maxNumberOfBullets > 1 ? "s" : "";
			EditorGUILayout.LabelField("Edit Layout : " + potentialMax + maxNumberOfBullets.ToString() + " bullet" + plural, EditorStyles.boldLabel);
			GUILayout.Space(6);
			EditorGUILayout.PropertyField(simultaneousBulletsPerFrame);

			GUILayout.Space(6);

			rlist.DoLayoutList();
			
			// Called if an item has been just added to the r-list
			if (shouldRecalcLast) RecalculateLastModifier();
			
			#endregion
			
			#region Preview interaction handling

			SerializedProperty rects = outputTexture.FindPropertyRelative("selectionRects");
			SerializedProperty texIsEditing = outputTexture.FindPropertyRelative("isEditingSelection");

			// Repaint UI and property drawer as the user moves the mouse
			SerializedProperty mouseDown = outputTexture.FindPropertyRelative("hasMouseDown");
			SerializedProperty multiSelect = outputTexture.FindPropertyRelative("holdsMultiSelect");
			SerializedProperty isDoneWithSelection = outputTexture.FindPropertyRelative("isDoneWithSelection");
			if (mouseDown.boolValue || multiSelect.boolValue || isDoneWithSelection.boolValue)
				Repaint();

			// if a modifier changes type or is deleted while it's in "edit selection" mode, cancel current action from texture
			if (texIsEditing.boolValue)
			{
				bool stillEditing = false;
				if (modifiers.arraySize > 0)
					for (int i = 0; i < modifiers.arraySize; i++)
					{
						SerializedProperty mod = modifiers.GetArrayElementAtIndex(i);
						if (mod.FindPropertyRelative("modifierType").enumValueIndex == (int)ShotModifierType.OnlySomeBullets)
							if (mod.FindPropertyRelative("isEditingSelection").boolValue)
								stillEditing = true;
					}

				if (!stillEditing)
				{
					rects.arraySize = 0;
					texIsEditing.boolValue = false;
					Repaint();
				}
			}

			// for what follows, we exclude simple clicks that aren't selection rectangles
			bool texHasSomeSelectionRects = rects.arraySize > 0;
			if (texHasSomeSelectionRects)
			{
				texHasSomeSelectionRects = false;
				for (int i = 0; i < rects.arraySize; i++)
				{
					Rect rect = rects.GetArrayElementAtIndex(i).rectValue;
					if (rect.width > 0.01f || rect.height > 0.01f)
						texHasSomeSelectionRects = true;
				}
			}

			// update stuff when a selection is just finished
			bool selectionIsDone = isDoneWithSelection.boolValue && texHasSomeSelectionRects;
			if (selectionIsDone)
			{
				bool oneModifierIsEditing = false;

				// saving newly edited selection from existing modifiers
				if (modifiers.arraySize > 0)
					for (int i = 0; i < modifiers.arraySize; i++)
					{
						SerializedProperty mod = modifiers.GetArrayElementAtIndex(i);
						if (mod.FindPropertyRelative("modifierType").enumValueIndex == (int)ShotModifierType.OnlySomeBullets)
							if (mod.FindPropertyRelative("isEditingSelection").boolValue)
							{
								oneModifierIsEditing = true;
								mod.FindPropertyRelative("isEditingSelection").boolValue = false;
								SerializedProperty modRects = mod.FindPropertyRelative("selectionRects");
								modRects.arraySize = rects.arraySize;
								if (modRects.arraySize > 0)
									for (int j = 0; j < modRects.arraySize; j++)
										modRects.GetArrayElementAtIndex(j).rectValue = rects.GetArrayElementAtIndex(j).rectValue;

								texIsEditing.boolValue = false;
							}
					}

				// if no modifier is currently in edit mode, then the user just made a new selection rect, so we'll store it into a new modifier.
				if (!oneModifierIsEditing)
				{
					rlist.onAddCallback.Invoke(rlist);
					SerializedProperty mod = modifiers.GetArrayElementAtIndex(modifiers.arraySize-1);
					mod.FindPropertyRelative("enabled").boolValue = true;
					mod.FindPropertyRelative("selectionColor").colorValue = outputTexture.FindPropertyRelative("currentColor").colorValue;
					mod.FindPropertyRelative("modifierType").enumValueIndex = (int)ShotModifierType.OnlySomeBullets;
					mod.FindPropertyRelative("numberOfModifiersAffected").intValue = 1;
					mod.FindPropertyRelative("isEditingSelection").boolValue = false;
					SerializedProperty modRects = mod.FindPropertyRelative("selectionRects");
					modRects.arraySize = rects.arraySize;
					if (modRects.arraySize > 0)
						for (int i = 0; i < modRects.arraySize; i++)
							modRects.GetArrayElementAtIndex(i).rectValue = rects.GetArrayElementAtIndex(i).rectValue;					

					// add one empty mod, ready to be filled by user
					rlist.onAddCallback.Invoke(rlist);
					SerializedProperty mod2 = modifiers.GetArrayElementAtIndex(modifiers.arraySize-1);
					mod2.FindPropertyRelative("modifierType").enumValueIndex = (int)ShotModifierType.GlobalTranslation;
				}
				rects.arraySize = 0;

				// rebuild to save references to the bulletSpawns (with an index?) into the modifier
				shouldRecalcAll = true;

				Repaint();
			}
		
			// And, finally, preview the result in a texture.
			GUILayout.Space(6);
			RefreshTextureSavedSelectionRects();
			SerializedProperty justChangedOrientation = outputTexture.FindPropertyRelative("justChangedOrientation");
			if (justChangedOrientation.boolValue)
			{
				justChangedOrientation.boolValue = false;
				RefreshOutputTexture();
			}
			hasCompactEditor.boolValue = sp.profile.compactMode;
			EditorGUILayout.PropertyField(outputTexture); // See BulletOutputTextureDrawer.cs

			#endregion

			#region advanced stuff : load from texture + bullet spawns, one by one

			// Header
			GUILayout.Space(12);
			EditorGUILayout.LabelField("Advanced: see and edit base bullet coordinates (before any modifiers)", EditorStyles.boldLabel);
			GUILayout.Space(6);

			int mainIndent = 2;

			#region foldout 6 : load bullet positions from a texture

			foldouts[6].boolValue = EditorGUILayout.Foldout(foldouts[6].boolValue, "Load bullet positions from a texture", true);
			if (foldouts[6].boolValue)
			{
				EditorGUI.indentLevel += mainIndent;
				EditorGUILayout.PropertyField(textureSliceRate);
				EditorGUILayout.PropertyField(ingameSizeOfTexture);
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(inputTexture);
				if (EditorGUI.EndChangeCheck()) textureIsNotReadable = false;
				if (inputTexture.objectReferenceValue != null)
				{
					if (textureIsNotReadable)
						EditorGUILayout.HelpBox("Your Texture does not have \"Read/Write\" enabled!\nPlease enable it in your Texture\'s inspector, in \"Advanced\".", MessageType.Error);
					else if (GUILayout.Button("Load bullets from texture"))
					{
						try
						{
							BulletSpawn[] bs = GetBulletsFromTexture(inputTexture.objectReferenceValue as Texture2D, textureSliceRate.intValue, ingameSizeOfTexture.vector2Value);
							if (bs != null)
								if (bs.Length > 0)
								{
									Undo.RecordObject(sp, undoGroupName);
									
									sp.simultaneousBulletsPerFrame = new DynamicInt(bs.Length);
									sp.simultaneousBulletsPerFrame.SetButtons(new int[] { -5, -1, 1, 5 });
									sp.bulletSpawns = bs;
									if (bs.Length > 100) sp.foldout7 = false;
									shouldRecalcAll = true;
									bulletAmountChangedThisFrame = true;
									Debug.Log("Texture successfully loaded into "+sp.name+" !");
								}
						}
						catch { textureIsNotReadable = true; }
					}
				}

				EditorGUI.indentLevel -= mainIndent;
			}

			#endregion

			#region foldout 0 : collapse modifiers

			foldouts[0].boolValue = EditorGUILayout.Foldout(foldouts[0].boolValue, "Collapse first modifier into Bullet coords", true);
			if (foldouts[0].boolValue)
			{
				EditorGUI.indentLevel += mainIndent;

				int numberOfModstoRemove = 1;
				bool containsSetPivot = false;
				bool containsSetBulletStyle = false;

				// don't check for first modifiers if there's no modifier
				if (sp.modifiers != null)
					if (sp.modifiers.Count > 0)
					{
						if (sp.modifiers[0].modifierType == ShotModifierType.OnlySomeBullets && sp.modifiers[0].enabled)
						{
							if (sp.modifiers.Count > 1)
							{
								int nextModsAffected = sp.modifiers[0].numberOfModifiersAffected;
								for (int i = 1; i < sp.modifiers.Count; i++)
								{
									if (sp.modifiers[i].enabled)
									{
										if (sp.modifiers[i].modifierType == ShotModifierType.OnlySomeBullets) break;
										if (sp.modifiers[i].modifierType == ShotModifierType.SetPivot) containsSetPivot = true;
										if (sp.modifiers[i].modifierType == ShotModifierType.SetBulletParams)
											if (sp.modifiers[i].bulletParams.root.settings.valueType != DynamicParameterSorting.Fixed)
												containsSetBulletStyle = true;
									}
									numberOfModstoRemove++;
									nextModsAffected--;
									if (nextModsAffected < 1) break;
								}
							}
							string str = "Your top modifier is \"Only Some Bullets\".\nAll subsequent affected modifiers ("+(numberOfModstoRemove-1).ToString()+") will be collapsed along with it.";
							EditorGUILayout.HelpBox(str, MessageType.Info);
						}
						else if (sp.modifiers[0].enabled)
						{
							if (sp.modifiers[0].modifierType == ShotModifierType.SetPivot)
								containsSetPivot = true;
							else if (sp.modifiers[0].modifierType == ShotModifierType.SetBulletParams)
								if (sp.modifiers[0].bulletParams.root.settings.valueType != DynamicParameterSorting.Fixed)					
									containsSetBulletStyle = true;				
						}
					}

				if (containsSetPivot)
					EditorGUILayout.HelpBox("You're about to collapse a \"Set Pivot\" modifier.\nThis is unsupported and will mess up with your layout.", MessageType.Warning);
				if (containsSetBulletStyle)
					EditorGUILayout.HelpBox("You're about to collapse a \"Set Bullet Params\" modifier that contains multiple Bullet styles.\nThis is unsupported and will just delete the modifier.", MessageType.Warning);

				EditorGUILayout.LabelField("Default Bullet values will change according to the top modifier.");
				GUILayout.BeginHorizontal();
				bool doable = true;
				if (sp.modifiers == null) doable = false;
				else if (sp.modifiers.Count == 0) doable = false;
				if (doable) EditorGUILayout.LabelField("Top modifier will be deleted afterwards.");
				else EditorGUILayout.LabelField("Your modifier list is currently empty.");
				EditorGUI.BeginDisabledGroup(!doable);
				if (GUILayout.Button("Collapse", EditorStyles.miniButton))
				{
					Undo.RecordObject(sp, "Collapsed shot modifier");
					for (int i = 0; i < numberOfModstoRemove; i++)
					{
						sp.bulletSpawns = sp.modifiers[0].postEffectBulletSpawns;
						if (sp.modifiers[0].modifierType == ShotModifierType.SetBulletParams && sp.modifiers[0].enabled)
							if (sp.modifiers[0].bulletParams.root.settings.valueType != DynamicParameterSorting.Fixed)
								EmitterProfileUtility.SetParentOfBullet(sp.modifiers[0].bulletParams, null, this);
						sp.modifiers.RemoveAt(0);
					}
				}
				EditorGUI.EndDisabledGroup();
				GUILayout.EndHorizontal();

				EditorGUI.indentLevel -= mainIndent;
			}

			#endregion

			#region foldout 7 : actual displaying of bulletSpawn fields, only if total amount did not just change

			string fstr = "Base Bullet Positions (XY) / Orientations (Z)";
			bool dangerousAmount = maxNumberOfBullets > 100 && !foldouts[7].boolValue;
			EditorGUI.BeginChangeCheck();
			bool foldoutResult = EditorGUILayout.Foldout(foldouts[7].boolValue, fstr, true);
			if (EditorGUI.EndChangeCheck())
			{
				if (dangerousAmount)
				{
					string areYouSure = "There are several bullets in this shot, inspector may get laggy! Do you really want to unfold this?";
					if (EditorUtility.DisplayDialog("Unfold bullets?", areYouSure, "Unfold anyway", "Cancel"))
						foldouts[7].boolValue = foldoutResult;	
				}
				else foldouts[7].boolValue = foldoutResult;
			}
			if (bulletAmountChangedThisFrame) bulletAmountChangedThisFrame = false;
			else
			{
				if (foldouts[7].boolValue)
				{
					EditorGUI.indentLevel += mainIndent;

					EditorGUI.BeginChangeCheck();
					for (int i = 0; i < maxNumberOfBullets; i++)
						BulletSpawnField(i);
					if (EditorGUI.EndChangeCheck())
						shouldRecalcAll = true;

					EditorGUI.indentLevel -= mainIndent;
				}
			}

			#endregion
		
			#endregion

			// And we're done.
			serializedObject.ApplyModifiedProperties();
			
			// If a selection rect group has ended, collapse undo substeps here
			if (selectionIsDone)
				Undo.CollapseUndoOperations(outputTexture.FindPropertyRelative("indexOfFirstUndo").intValue);

			// Adjustments are done whenever needed, but they're fully automatized and not in the Undo stack
			if (shouldRecalcAll)
			{
				RecalculateLayoutFromModifier(0); // on remove modifier, or reorder modifier list
				RefreshOutputTexture();
				ReplaceExtremeValues();
				shouldRecalcAll = false;
			}

			// Bind repaint and redraw texture
			if (Event.current.type == EventType.Repaint)
				shouldRecalcAll = true;

			// Debug
			//stopwatch.Stop();
			//EditorGUILayout.LabelField(stopwatch.ElapsedTicks.ToString());
		}

		#endregion

		#region toolbox

		// Field for editing a single bullet spawn
		void BulletSpawnField(int index)
		{
			SerializedProperty curBulletSpawn = bulletSpawnArray.GetArrayElementAtIndex(index);

			SerializedProperty changeBullet = curBulletSpawn.FindPropertyRelative("useDifferentBullet");

			EditorGUILayout.BeginHorizontal();

			SerializedProperty bulletOrigin = curBulletSpawn.FindPropertyRelative("bulletOrigin");
			//EditorGUILayout.LabelField("Bullet Origin");
			EditorGUILayout.PropertyField(bulletOrigin, GUIContent.none);

			string str = changeBullet.boolValue ? "Default Style" : "Change Style";
			if (GUILayout.Button(str, EditorStyles.miniButton))
				changeBullet.boolValue = !changeBullet.boolValue;

			EditorGUILayout.EndHorizontal();

			if (changeBullet.boolValue)
			{
				EditorGUI.indentLevel+=2;
				fieldHandler.LayoutParamField<BulletParams>(new GUIContent("Bullet Style"), curBulletSpawn.FindPropertyRelative("bulletParams"), 0.75f);
				EditorGUI.indentLevel-=2;
			}
		}

		// Draws the field for one modifier into the reorderable list.
		void LayoutModifierDrawer(Rect rect, int index, bool isActive, bool isFocused)
		{
			#region GUI color feedbacks bullet selection
			
			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			float oldLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 55;
			float oldFieldWidth = EditorGUIUtility.fieldWidth;

			Color guiDefaultColor = GUI.color;
			SerializedProperty mod = modifiers.GetArrayElementAtIndex(index);
			SerializedProperty modType = mod.FindPropertyRelative("modifierType");

			Color colorToAdopt = GUI.color;
			if (index == 0)
			{
				if (modType.enumValueIndex == (int)ShotModifierType.OnlySomeBullets)
					colorToAdopt = MakeGUIFriendly(mod.FindPropertyRelative("selectionColor").colorValue);
			}
			else
			{
				// tracks how many more modifiers should be impacted by selector
				int coloredModsLeft = 0;
				
				// browse all modifiers from first to current (current mod included, hence the +1)
				// TODO : this browsing could/should be done in OnInspectorGUI() to save some efficiency
				for (int i = 0; i < index+1; i++)
				{
					SerializedProperty curMod = modifiers.GetArrayElementAtIndex(i);
					if (curMod.FindPropertyRelative("modifierType").enumValueIndex == (int)ShotModifierType.OnlySomeBullets)
					{
						if (curMod.FindPropertyRelative("enabled").boolValue)
						{
							colorToAdopt = MakeGUIFriendly(curMod.FindPropertyRelative("selectionColor").colorValue);
							coloredModsLeft = curMod.FindPropertyRelative("numberOfModifiersAffected").intValue;
						}
						else
						{
							coloredModsLeft = 0;
							colorToAdopt = guiDefaultColor;
						}
					}
					else
					{
						coloredModsLeft--;
						if (coloredModsLeft < 0)
							colorToAdopt = guiDefaultColor;
					}
				}
			}
			if (colorToAdopt != guiDefaultColor)
			{
				Rect extendedRect = new Rect(rect.x-18, rect.y, rect.width+5+18, rect.height);
				GUI.DrawTexture(extendedRect, GradientTex(colorToAdopt), ScaleMode.StretchToFill, true);
			}
			#endregion

			#region global stuff
			rect.y += 2;
			float h = EditorGUIUtility.singleLineHeight;
			float w = EditorGUIUtility.currentViewWidth;
			float w1 = 25;
			float w2 = 115;
			float w3 = 20; // space between 2nd and 3rd rects - will change based on modifier type

			Rect enableRect = new Rect(rect.x, rect.y, w1, h);
			Rect enumRect = new Rect(rect.x + w1, rect.y, w2, h);
			Rect usableSpace = new Rect(rect.x + w1 + w2 + w3, rect.y, rect.width - (w1 + w2 + w3), h);

			SerializedProperty isEnabled = mod.FindPropertyRelative("enabled");
			EditorGUI.BeginChangeCheck(); // this one ends at the very end of the function, 1000 lines later

			EditorGUI.BeginChangeCheck(); // this one ends at the very end of the function, 1000 lines later
			EditorGUI.PropertyField(enableRect, isEnabled, GUIContent.none);
			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				if (modType.enumValueIndex == (int)ShotModifierType.SetBulletParams)
				{
					// TODO : when setting the parent, abort if parent isn't equal to "sp" already
					ShotParams newParent = (mod.FindPropertyRelative("enabled").boolValue) ? sp : null;
					EmitterProfileUtility.SetParentOfBullet(sp.modifiers[index].bulletParams, newParent, this);
				}

				// TODO : delete this
				/* *
				BulletParams bp = mod.FindPropertyRelative("bulletParams").objectReferenceValue as BulletParams;
				if (bp != null && modType.enumValueIndex == (int)ShotModifierType.SetBulletParams)
				{
					if (isEnabled.boolValue)
						SetParent(bp, sp);
					else
						SetParent(bp, null);
				}
				/* */
			}

			EditorGUI.BeginDisabledGroup(!isEnabled.boolValue); // also ends at the very end of the function, 1000 lines later
			
			EditorGUI.BeginChangeCheck();
			modType.enumValueIndex = EditorGUI.Popup(enumRect, GUIContent.none, modType.enumValueIndex, modOptions);
			//EditorGUI.PropertyField(enumRect, modType, GUIContent.none);
			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				if (mod.FindPropertyRelative("enabled").boolValue)
				{
					// TODO : when setting the parent, abort if parent isn't equal to "sp" already
					ShotParams newParent = (modType.enumValueIndex == (int)ShotModifierType.SetBulletParams) ? sp : null;
					EmitterProfileUtility.SetParentOfBullet(sp.modifiers[index].bulletParams, newParent, this);
				}
				
				// TODO : delete this
				/* *
				BulletParams bp = mod.FindPropertyRelative("bulletParams").objectReferenceValue as BulletParams;
				if (bp != null && mod.FindPropertyRelative("enabled").boolValue)
				{
					if (modType.enumValueIndex == (int)ShotModifierType.SetBulletParams)
						SetParent(bp, sp);
					else
						SetParent(bp, null);
				}
				/* */

			}

			#endregion

			// The rest depends on modifier type.
			
			#region spread bullets
			if (modType.enumValueIndex == (int)ShotModifierType.SpreadBullets)
			{
				SerializedProperty deg = mod.FindPropertyRelative("degrees");
				float wu = usableSpace.width;
				GUIContent gc = wu > 100 ? new GUIContent("Degrees") : GUIContent.none;
				EditorGUI.PropertyField(usableSpace, deg, gc);
				/* *
				if (wu > 100)
				{
					float w4 = 60;
					Rect labelRect = new Rect(usableSpace.x, usableSpace.y, w4, h);
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					EditorGUI.LabelField(labelRect, "Degrees");
					deg.floatValue = CustomFloatField(floatRect, dragZone, deg.floatValue);
				}
				else
				{
					float w4 = 0;
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					deg.floatValue = CustomFloatField(floatRect, dragZone, deg.floatValue);
				}
				/* */
			}
			#endregion

			#region spread bullets total
			if (modType.enumValueIndex == (int)ShotModifierType.SpreadBulletsTotal)
			{
				SerializedProperty deg = mod.FindPropertyRelative("degreesTotal");
				float wu = usableSpace.width;
				GUIContent gc = wu > 100 ? new GUIContent("Degrees") : GUIContent.none;
				EditorGUI.PropertyField(usableSpace, deg, gc);
				/* *
				if (wu > 100)
				{
					float w4 = 60;
					Rect labelRect = new Rect(usableSpace.x, usableSpace.y, w4, h);
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					EditorGUI.LabelField(labelRect, "Degrees");
					deg.floatValue = CustomFloatField(floatRect, dragZone, deg.floatValue);
				}
				else
				{
					float w4 = 0;
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					deg.floatValue = CustomFloatField(floatRect, dragZone, deg.floatValue);
				}
				/* */
			}
			#endregion

			#region rotate layout
			if (modType.enumValueIndex == (int)ShotModifierType.RotateAroundPivot)
			{
				SerializedProperty deg = mod.FindPropertyRelative("layoutDegrees");
				float wu = usableSpace.width;
				GUIContent gc = wu > 100 ? new GUIContent("Degrees") : GUIContent.none;
				EditorGUI.PropertyField(usableSpace, deg, gc);
				/* *
				if (wu > 100)
				{
					float w4 = 60;
					Rect labelRect = new Rect(usableSpace.x, usableSpace.y, w4, h);
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					EditorGUI.LabelField(labelRect, "Degrees");
					deg.floatValue = CustomFloatField(floatRect, dragZone, deg.floatValue);
				}
				else
				{
					float w4 = 0;
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					deg.floatValue = CustomFloatField(floatRect, dragZone, deg.floatValue);
				}
				/* */
			}
			#endregion

			#region change bullet params
			if (modType.enumValueIndex == (int)ShotModifierType.SetBulletParams)
			{
				SerializedProperty bp = mod.FindPropertyRelative("bulletParams");
				float wu = usableSpace.width;
				if (wu > 130)
				{
					float w4 = 80;
					Rect labelRect = new Rect(usableSpace.x, usableSpace.y, w4, h);
					Rect objRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					EditorGUI.LabelField(labelRect, "Bullet Style");
					fieldHandler.DynamicParamField<BulletParams>(objRect, GUIContent.none, bp);
					//bp.objectReferenceValue = EditorGUI.ObjectField(objRect, bp.objectReferenceValue as BulletParams, typeof(BulletParams), false) as BulletParams;
				}
				else
				{
					float w4 = 0;
					Rect objRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					fieldHandler.DynamicParamField<BulletParams>(objRect, GUIContent.none, bp);
					//bp.objectReferenceValue = EditorGUI.ObjectField(objRect, bp.objectReferenceValue as BulletParams, typeof(BulletParams), false) as BulletParams;					
				}
			}
			#endregion

			#region horizontal spacing
			if (modType.enumValueIndex == (int)ShotModifierType.HorizontalSpacing)
			{
				SerializedProperty hs = mod.FindPropertyRelative("horizontalSpacing");
				float wu = usableSpace.width;
				GUIContent gc = wu > 100 ? new GUIContent("Spacing") : GUIContent.none;
				EditorGUI.PropertyField(usableSpace, hs, gc);
				/* *
				if (wu > 100)
				{
					float w4 = 60;
					Rect labelRect = new Rect(usableSpace.x, usableSpace.y, w4, h);
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					EditorGUI.LabelField(labelRect, "Spacing");
					hs.floatValue = CustomFloatField(floatRect, dragZone, hs.floatValue);
				}
				else
				{
					float w4 = 0;
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					hs.floatValue = CustomFloatField(floatRect, dragZone, hs.floatValue);
				}
				/* */
			}
			#endregion

			#region vertical spacing
			if (modType.enumValueIndex == (int)ShotModifierType.VerticalSpacing)
			{
				SerializedProperty vs = mod.FindPropertyRelative("verticalSpacing");
				float wu = usableSpace.width;
				GUIContent gc = wu > 100 ? new GUIContent("Spacing") : GUIContent.none;
				EditorGUI.PropertyField(usableSpace, vs, gc);
				/* *
				if (wu > 100)
				{
					float w4 = 60;
					Rect labelRect = new Rect(usableSpace.x, usableSpace.y, w4, h);
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					EditorGUI.LabelField(labelRect, "Spacing");
					vs.floatValue = CustomFloatField(floatRect, dragZone, vs.floatValue);
				}
				else
				{
					float w4 = 0;
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					vs.floatValue = CustomFloatField(floatRect, dragZone, vs.floatValue);
				}
				/* */
			}
			#endregion

			#region horizontal spacing total
			if (modType.enumValueIndex == (int)ShotModifierType.HorizontalSpacingTotal)
			{
				SerializedProperty hs = mod.FindPropertyRelative("xSpacingTotal");
				float wu = usableSpace.width;
				GUIContent gc = wu > 100 ? new GUIContent("Spacing") : GUIContent.none;
				EditorGUI.PropertyField(usableSpace, hs, gc);
				/* *
				if (wu > 100)
				{
					float w4 = 60;
					Rect labelRect = new Rect(usableSpace.x, usableSpace.y, w4, h);
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					EditorGUI.LabelField(labelRect, "Spacing");
					hs.floatValue = CustomFloatField(floatRect, dragZone, hs.floatValue);
				}
				else
				{
					float w4 = 0;
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					hs.floatValue = CustomFloatField(floatRect, dragZone, hs.floatValue);
				}
				/* */
			}
			#endregion

			#region vertical spacing total
			if (modType.enumValueIndex == (int)ShotModifierType.VerticalSpacingTotal)
			{
				SerializedProperty vs = mod.FindPropertyRelative("ySpacingTotal");
				float wu = usableSpace.width;
				GUIContent gc = wu > 100 ? new GUIContent("Spacing") : GUIContent.none;
				EditorGUI.PropertyField(usableSpace, vs, gc);
				/* *
				if (wu > 100)
				{
					float w4 = 60;
					Rect labelRect = new Rect(usableSpace.x, usableSpace.y, w4, h);
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					EditorGUI.LabelField(labelRect, "Spacing");
					vs.floatValue = CustomFloatField(floatRect, dragZone, vs.floatValue);
				}
				else
				{
					float w4 = 0;
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					vs.floatValue = CustomFloatField(floatRect, dragZone, vs.floatValue);
				}
				/* */
			}
			#endregion

			#region rotation
			if (modType.enumValueIndex == (int)ShotModifierType.Rotation)
			{
				SerializedProperty deg = mod.FindPropertyRelative("rotationDegrees");
				float wu = usableSpace.width;
				GUIContent gc = wu > 100 ? new GUIContent("Degrees") : GUIContent.none;
				EditorGUI.PropertyField(usableSpace, deg, gc);

				/* *
				SerializedProperty gm = mod.FindPropertyRelative("globalMovement");
				float wu = usableSpace.width;
				if (wu > 100)
				{
					float w4 = 60;
					Rect labelRect = new Rect(usableSpace.x, usableSpace.y, w4, h);
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 30, floatRect.y, 35, h);
					EditorGUI.LabelField(labelRect, "Rotation");
					Vector3 v3 = new Vector3(gm.vector3Value.x, gm.vector3Value.y, gm.vector3Value.z);
					v3.z = CustomFloatField(floatRect, dragZone, gm.vector3Value.z);
					gm.vector3Value = v3;
				}
				else
				{
					float w4 = 0;
					Rect floatRect = new Rect(usableSpace.x + w4, usableSpace.y, wu - w4, h);
					Rect dragZone = new Rect(floatRect.x - 20, floatRect.y, 35, h);
					Vector3 v3 = new Vector3(gm.vector3Value.x, gm.vector3Value.y, gm.vector3Value.z);
					v3.z = CustomFloatField(floatRect, dragZone, gm.vector3Value.z);
					gm.vector3Value = v3;
				}
				/* */
			}
			#endregion

			#region reset XYZ
			if (modType.enumValueIndex == (int)ShotModifierType.ResetCoordinates)
			{
				SerializedProperty rx = mod.FindPropertyRelative("resetX");
				SerializedProperty ry = mod.FindPropertyRelative("resetY");
				SerializedProperty rz = mod.FindPropertyRelative("resetZ");
				float wu = usableSpace.width;
				EditorGUIUtility.labelWidth = 32f;
				EditorGUIUtility.fieldWidth = 10f;
				float wb = 80f;
				float ws = 10f;
				float widthUsed = 0f;
				Rect xRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, wb, h); widthUsed += wb + ws;
				Rect yRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, wb, h); widthUsed += wb + ws;
				Rect zRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, wb, h); widthUsed += wb + ws;
				EditorGUI.PropertyField(xRect, rx, new GUIContent("PosX"));
				EditorGUI.PropertyField(yRect, ry, new GUIContent("PosY"));
				EditorGUI.PropertyField(zRect, rz, new GUIContent("RotZ"));
				/* *
				if (wu > 180)
				{
					float w4 = 35;
					float w5 = 25;
					float widthUsed = 0;
					Rect labelRectX = new Rect(usableSpace.x, usableSpace.y, w4, h); widthUsed += w4;
					Rect toggleRectX = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
					Rect labelRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
					Rect toggleRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
					Rect labelRectZ = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
					Rect toggleRectZ = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
					EditorGUI.LabelField(labelRectX, "PosX");
					EditorGUI.LabelField(labelRectY, "PosY");
					EditorGUI.LabelField(labelRectZ, "RotZ");
					rx.boolValue = EditorGUI.Toggle(toggleRectX, GUIContent.none, rx.boolValue);
					ry.boolValue = EditorGUI.Toggle(toggleRectY, GUIContent.none, ry.boolValue);
					rz.boolValue = EditorGUI.Toggle(toggleRectZ, GUIContent.none, rz.boolValue);
				}
				else
				{
					float w4 = 12;
					float w5 = 14;
					usableSpace = new Rect(usableSpace.xMax - 3 * (w4 + w5), usableSpace.y, 3 * (w4 + w5), h);
					float widthUsed = 0;
					Rect labelRectX = new Rect(usableSpace.x, usableSpace.y, w4, h); widthUsed += w4;
					Rect toggleRectX = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
					Rect labelRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
					Rect toggleRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
					Rect labelRectZ = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
					Rect toggleRectZ = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
					EditorGUI.LabelField(labelRectX, "X");
					EditorGUI.LabelField(labelRectY, "Y");
					EditorGUI.LabelField(labelRectZ, "Z");
					rx.boolValue = EditorGUI.Toggle(toggleRectX, GUIContent.none, rx.boolValue);
					ry.boolValue = EditorGUI.Toggle(toggleRectY, GUIContent.none, ry.boolValue);
					rz.boolValue = EditorGUI.Toggle(toggleRectZ, GUIContent.none, rz.boolValue);
				}
				/* */
			}
			#endregion

			#region look at point
			if (modType.enumValueIndex == (int)ShotModifierType.LookAtPoint)
			{
				SerializedProperty lookat = mod.FindPropertyRelative("pointToLookAt");

				EditorGUI.PropertyField(usableSpace, lookat, GUIContent.none);

				/* *

				float wu = usableSpace.width;

				float w4 = 16;
				float w5 = 42;
				float widthUsed = 0;

				if (wu < 120)
				{
					w4 = 13;
					w5 = 30;
					usableSpace = new Rect(usableSpace.xMax - 2 * (w4 + w5), usableSpace.y, 2 * (w4 + w5), h);
				}

				Rect labelRectX = new Rect(usableSpace.x, usableSpace.y, w4, h); widthUsed += w4;
				Rect floatRectX = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
				Rect dragzoneX = new Rect(floatRectX.x - 15, floatRectX.y, 20, h);
				if (wu > 119) widthUsed += 4;
				Rect labelRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
				Rect floatRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
				Rect dragzoneY = new Rect(floatRectY.x - 15, floatRectY.y, 20, h);
				EditorGUI.LabelField(labelRectX, "X");
				EditorGUI.LabelField(labelRectY, "Y");
				float x = CustomFloatField(floatRectX, dragzoneX, lookat.vector2Value.x);
				float y = CustomFloatField(floatRectY, dragzoneY, lookat.vector2Value.y);
				lookat.vector2Value = new Vector2(x, y);
				/* */
			}
			#endregion

			#region look away from point
			if (modType.enumValueIndex == (int)ShotModifierType.LookAwayFromPoint)
			{
				SerializedProperty lookAway = mod.FindPropertyRelative("pointToFleeFrom");
				
				EditorGUI.PropertyField(usableSpace, lookAway, GUIContent.none);

				/* *
				
				float wu = usableSpace.width;

				float w4 = 16;
				float w5 = 42;
				float widthUsed = 0;

				if (wu < 120)
				{
					w4 = 13;
					w5 = 30;
					usableSpace = new Rect(usableSpace.xMax - 2 * (w4 + w5), usableSpace.y, 2 * (w4 + w5), h);
				}

				Rect labelRectX = new Rect(usableSpace.x, usableSpace.y, w4, h); widthUsed += w4;
				Rect floatRectX = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
				Rect dragzoneX = new Rect(floatRectX.x - 15, floatRectX.y, 20, h);
				if (wu > 119) widthUsed += 4;
				Rect labelRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
				Rect floatRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
				Rect dragzoneY = new Rect(floatRectY.x - 15, floatRectY.y, 20, h);
				EditorGUI.LabelField(labelRectX, "X");
				EditorGUI.LabelField(labelRectY, "Y");
				float x = CustomFloatField(floatRectX, dragzoneX, lookAway.vector2Value.x);
				float y = CustomFloatField(floatRectY, dragzoneY, lookAway.vector2Value.y);
				lookAway.vector2Value = new Vector2(x, y);
				/* */
			}
			#endregion

			#region translate global
			if (modType.enumValueIndex == (int)ShotModifierType.GlobalTranslation)
			{
				SerializedProperty gm = mod.FindPropertyRelative("globalMovement");
				EditorGUI.PropertyField(usableSpace, gm, GUIContent.none);

				/* *
				float wu = usableSpace.width;

				float w4 = 16;
				float w5 = 42;
				float widthUsed = 0;

				if (wu < 120)
				{
					w4 = 13;
					w5 = 30;
					usableSpace = new Rect(usableSpace.xMax - 2 * (w4 + w5), usableSpace.y, 2 * (w4 + w5), h);
				}

				Rect labelRectX = new Rect(usableSpace.x, usableSpace.y, w4, h); widthUsed += w4;
				Rect floatRectX = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
				Rect dragzoneX = new Rect(floatRectX.x - 15, floatRectX.y, 20, h);
				if (wu > 119) widthUsed += 4;
				Rect labelRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
				Rect floatRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
				Rect dragzoneY = new Rect(floatRectY.x - 15, floatRectY.y, 20, h);
				EditorGUI.LabelField(labelRectX, "X");
				EditorGUI.LabelField(labelRectY, "Y");
				float x = CustomFloatField(floatRectX, dragzoneX, gm.vector3Value.x);
				float y = CustomFloatField(floatRectY, dragzoneY, gm.vector3Value.y);
				gm.vector3Value = new Vector3(x, y, gm.vector3Value.z);
				/* */
			}
			#endregion

			#region translate local
			if (modType.enumValueIndex == (int)ShotModifierType.LocalTranslation)
			{
				SerializedProperty gm = mod.FindPropertyRelative("localMovement");
				
				EditorGUI.PropertyField(usableSpace, gm, GUIContent.none);
				
				/* *
				float wu = usableSpace.width;

				float w4 = 61;
				float w5 = 42;
				float widthUsed = 0;
				bool mini = wu < 216;
				bool semi = mini && wu > 119;

				if (semi) w4 = 16;
				else if (mini)
				{
					w4 = 13;
					w5 = 30;
					usableSpace = new Rect(usableSpace.xMax - 2 * (w4 + w5), usableSpace.y, 2 * (w4 + w5), h);
				}

				Rect labelRectX = new Rect(usableSpace.x, usableSpace.y, w4, h); widthUsed += w4;
				Rect floatRectX = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
				Rect dragzoneX = new Rect(floatRectX.x - 15, floatRectX.y, 20, h);
				if (!mini) widthUsed += 10;
				else if (semi) widthUsed += 4;
				Rect labelRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
				Rect floatRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
				Rect dragzoneY = new Rect(floatRectY.x - 15, floatRectY.y, 20, h);
				EditorGUI.LabelField(labelRectX, mini ? "X" : "Sideways");
				EditorGUI.LabelField(labelRectY, mini ? "Y" : "Forward");
				float x = CustomFloatField(floatRectX, dragzoneX, gm.vector2Value.x);
				float y = CustomFloatField(floatRectY, dragzoneY, gm.vector2Value.y);
				gm.vector2Value = new Vector2(x, y);
				/* */
			}
			#endregion

			#region scale
			if (modType.enumValueIndex == (int)ShotModifierType.ScaleLayout)
			{
				SerializedProperty sc = mod.FindPropertyRelative("scale");
				
				EditorGUI.PropertyField(usableSpace, sc, GUIContent.none);

				/* *

				float wu = usableSpace.width;

				float w4 = 16;
				float w5 = 42;
				float widthUsed = 0;

				if (wu < 120)
				{
					w4 = 13;
					w5 = 30;
					usableSpace = new Rect(usableSpace.xMax - 2 * (w4 + w5), usableSpace.y, 2 * (w4 + w5), h);
				}

				Rect labelRectX = new Rect(usableSpace.x, usableSpace.y, w4, h); widthUsed += w4;
				Rect floatRectX = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
				Rect dragzoneX = new Rect(floatRectX.x - 15, floatRectX.y, 20, h);
				if (wu > 119) widthUsed += 4;
				Rect labelRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
				Rect floatRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
				Rect dragzoneY = new Rect(floatRectY.x - 15, floatRectY.y, 20, h);
				EditorGUI.LabelField(labelRectX, "X");
				EditorGUI.LabelField(labelRectY, "Y");
				float x = CustomFloatField(floatRectX, dragzoneX, sc.vector2Value.x);
				float y = CustomFloatField(floatRectY, dragzoneY, sc.vector2Value.y);
				sc.vector2Value = new Vector2(x, y);

				/* */
			}
			#endregion

			#region set pivot
			if (modType.enumValueIndex == (int)ShotModifierType.SetPivot)
			{
				SerializedProperty sc = mod.FindPropertyRelative("pivot");
				SerializedProperty sColor = mod.FindPropertyRelative("pivotColor");
				float wu = usableSpace.width;
				float spaceWidth = 10;
				float colorWidth = 50f;
				float remWidth = wu - (colorWidth + spaceWidth);

				Rect colorRect = new Rect(usableSpace.x, usableSpace.y, colorWidth, usableSpace.height);
				Rect vectorRect = new Rect(usableSpace.x + colorWidth + spaceWidth, usableSpace.y, remWidth, usableSpace.height);
				EditorGUI.PropertyField(colorRect, sColor, GUIContent.none);
				EditorGUI.PropertyField(vectorRect, sc, GUIContent.none);

				/* *
				float w4 = 16; // label
				float w5 = 42; // float
				float w6 = 60; // color
				float widthUsed = 0;

				if (wu < 180)
				{
					w4 = 13;
					w5 = 30;
					w6 = 40;
					spaceWidth = 5;
					float total = 2 * (w4 + w5) + w6 + spaceWidth;
					if (wu < 120) total = 2 * (w4 + w5);
					usableSpace = new Rect(usableSpace.xMax - total, usableSpace.y, total, h);
				}

				Rect labelRectX = new Rect(usableSpace.x, usableSpace.y, w4, h); widthUsed += w4;
				Rect floatRectX = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
				Rect dragzoneX = new Rect(floatRectX.x - 15, floatRectX.y, 20, h);
				if (wu > 179) widthUsed += 4;
				Rect labelRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
				Rect floatRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
				Rect dragzoneY = new Rect(floatRectY.x - 15, floatRectY.y, 20, h);
				widthUsed += spaceWidth;
				Rect colorRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, w6, h); widthUsed += w6;
				EditorGUI.LabelField(labelRectX, "X");
				EditorGUI.LabelField(labelRectY, "Y");
				float x = CustomFloatField(floatRectX, dragzoneX, sc.vector2Value.x);
				float y = CustomFloatField(floatRectY, dragzoneY, sc.vector2Value.y);
				sc.vector2Value = new Vector2(x, y);
				if (wu > 119) sColor.colorValue = EditorGUI.ColorField(colorRect, sColor.colorValue);
				/* */
			}
			#endregion

			#region flip
			if (modType.enumValueIndex == (int)ShotModifierType.FlipOrientation)
			{
				SerializedProperty fx = mod.FindPropertyRelative("flipX");
				SerializedProperty fy = mod.FindPropertyRelative("flipY");
				float wu = usableSpace.width;
				EditorGUIUtility.labelWidth = 38f;
				float wb = 80f;
				float ws = 20f;
				float widthUsed = 0f;
				Rect xRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, wb, h); widthUsed += wb + ws;
				Rect yRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, wb, h); widthUsed += wb + ws;
				EditorGUI.PropertyField(xRect, fx, new GUIContent("Flip X"));
				EditorGUI.PropertyField(yRect, fy, new GUIContent("Flip Y"));
				/* *
				if (wu > 140)
				{
					float w4 = 45;
					float w5 = 25;
					float widthUsed = 0;
					Rect labelRectX = new Rect(usableSpace.x, usableSpace.y, w4, h); widthUsed += w4;
					Rect toggleRectX = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
					Rect labelRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
					Rect toggleRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
					EditorGUI.LabelField(labelRectX, "Flip X");
					EditorGUI.LabelField(labelRectY, "Flip Y");
					fx.boolValue = EditorGUI.Toggle(toggleRectX, GUIContent.none, fx.boolValue);
					fy.boolValue = EditorGUI.Toggle(toggleRectY, GUIContent.none, fy.boolValue);
				}
				else
				{
					float w4 = 15;
					float w5 = 14;
					usableSpace = new Rect(usableSpace.xMax - 2 * (w4 + w5) - 4, usableSpace.y, 2 * (w4 + w5) + 4, h);
					float widthUsed = 0;
					Rect labelRectX = new Rect(usableSpace.x, usableSpace.y, w4, h); widthUsed += w4;
					Rect toggleRectX = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
					widthUsed += 4;
					Rect labelRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
					Rect toggleRectY = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h); widthUsed += w5;
					EditorGUI.LabelField(labelRectX, "X");
					EditorGUI.LabelField(labelRectY, "Y");
					fx.boolValue = EditorGUI.Toggle(toggleRectX, GUIContent.none, fx.boolValue);
					fy.boolValue = EditorGUI.Toggle(toggleRectY, GUIContent.none, fy.boolValue);
				}
				/* */
			}
			#endregion

			#region select bullets
			if (modType.enumValueIndex == (int)ShotModifierType.OnlySomeBullets)
			{
				SerializedProperty color = mod.FindPropertyRelative("selectionColor");
				SerializedProperty numberOfMods = mod.FindPropertyRelative("numberOfModifiersAffected");
				SerializedProperty isEditing = mod.FindPropertyRelative("isEditingSelection");
				SerializedProperty selectionRectsVisible = mod.FindPropertyRelative("selectionRectsVisible");
				float wu = usableSpace.width;
				float spaceWidth = 10; // space

				// default appearance : user is not trying to edit selection
				if (!isEditing.boolValue)
				{
					float w4 = 60; // color
					#if UNITY_2019_3_OR_NEWER
					float w5 = 72; // Edit Zone button
					#else
					float w5 = 60; // Edit Zone button
					#endif
					float w6 = 56; // "range" string
					float w7 = 20; // button -
					float w8 = 20; // button +
					float w9 = 20; // eye button
					float widthUsed = 0;
					string affects = "Range: "+numberOfMods.intValue.ToString();

					int displayed = 0; // number of elements displayed

					Rect textRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, w6, h); widthUsed += w6;
					widthUsed += spaceWidth;
					if (widthUsed < wu) displayed++; // 1
					Rect buttonMinusRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, w7, h-1); widthUsed += w7;
					Rect buttonPlusRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, w8, h-1); widthUsed += w8;
					widthUsed += spaceWidth;
					if (widthUsed < wu) displayed++; // 2
					Rect buttonRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h-1); widthUsed += w5;
					widthUsed += spaceWidth;
					if (widthUsed < wu) displayed++; // 3
					Rect colorRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
					widthUsed += spaceWidth;
					if (widthUsed < wu) displayed++; // 4
					Rect eyeRect = new Rect(usableSpace.x + widthUsed, usableSpace.y+3, w9, h); widthUsed += w9;
					if (widthUsed < wu) displayed++; // 5

					if (displayed > 1)
						EditorGUI.LabelField(textRect, affects);
					else EditorGUI.LabelField(textRect, "(Please expand)", EditorStyles.miniLabel);
					if (displayed > 1)
					{
						EditorGUI.BeginDisabledGroup(numberOfMods.intValue < 2);
						if (GUI.Button(buttonMinusRect, "-", EditorStyles.miniButtonLeft)) numberOfMods.intValue--;
						EditorGUI.EndDisabledGroup();
						if (GUI.Button(buttonPlusRect, "+", EditorStyles.miniButtonRight)) numberOfMods.intValue++;
					}
					if (displayed > 2)
						if (GUI.Button(buttonRect, "Edit Zone", EditorStyles.miniButton))
						{
							// quit selection mode (and change button string) for all other modifiers
							for (int i=0; i<modifiers.arraySize; i++)
								modifiers.GetArrayElementAtIndex(i).FindPropertyRelative("isEditingSelection").boolValue = false;
							// enter selection mode
							isEditing.boolValue = true;
							outputTexture.FindPropertyRelative("isEditingSelection").boolValue = true;
							outputTexture.FindPropertyRelative("currentColor").colorValue = color.colorValue;
						}

					if (displayed > 3)
						color.colorValue = EditorGUI.ColorField(colorRect, color.colorValue);		

					if (displayed > 4)
						if (GUI.Button(eyeRect, selectionRectsVisible.boolValue?eyeOn:eyeOff, EditorStyles.label))
							selectionRectsVisible.boolValue = !selectionRectsVisible.boolValue;
				}

				// edit mode : user can/should make selection rects to modify currently affected bullets
				else
				{
					float w4 = 120; // "Select bullets in the preview below"
					float w5 = 70; // cancel button
					float w6 = 50; // cancel button if not enough room
					float widthUsed = 0;
					Rect textRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, w4, h); widthUsed += w4;
					widthUsed += spaceWidth;
					Rect buttonRect = new Rect(usableSpace.x + widthUsed, usableSpace.y, w5, h-1); widthUsed += w5;
					if (widthUsed < wu)
					{
						EditorGUI.LabelField(textRect, "Select bullets below.");
						if (GUI.Button(buttonRect, "Cancel", EditorStyles.miniButton))
						{
							isEditing.boolValue = false;
							outputTexture.FindPropertyRelative("isEditingSelection").boolValue = false;
							outputTexture.FindPropertyRelative("selectionRects").arraySize = 0;
						}
					}
					else
					{
						Rect fallbackCancelRect = new Rect(usableSpace.x, usableSpace.y, w6, h);
						if (GUI.Button(fallbackCancelRect, "Cancel", EditorStyles.miniButton))
						{
							isEditing.boolValue = false;
							outputTexture.FindPropertyRelative("isEditingSelection").boolValue = false;
							outputTexture.FindPropertyRelative("selectionRects").arraySize = 0;
						}
					}
				}				
			}
			#endregion

			EditorGUI.EndDisabledGroup();

			EditorGUI.indentLevel = oldIndent;
			EditorGUIUtility.labelWidth = oldLabelWidth;
			EditorGUIUtility.fieldWidth = oldFieldWidth;
			
			if (EditorGUI.EndChangeCheck())
				shouldRecalcAll = true;
		}

		// Called by the reorderable list upon adding a new one : initializes newly created modifier.
		void RecalculateLastModifier()
		{
			// Set default values
			SerializedProperty newItem = modifiers.GetArrayElementAtIndex(modifiers.arraySize - 1);
			newItem.FindPropertyRelative("enabled").boolValue = true;

			DynamicParameterUtility.SetFixedBool(newItem.FindPropertyRelative("resetX"), false, true);
			DynamicParameterUtility.SetFixedBool(newItem.FindPropertyRelative("resetY"), false, true);
			DynamicParameterUtility.SetFixedBool(newItem.FindPropertyRelative("resetZ"), false, true);
			DynamicParameterUtility.SetFixedBool(newItem.FindPropertyRelative("flipX"), false, true);
			DynamicParameterUtility.SetFixedBool(newItem.FindPropertyRelative("flipY"), false, true);

			DynamicParameterUtility.SetFixedFloat(newItem.FindPropertyRelative("degrees"), 0f, true);
			DynamicParameterUtility.SetFixedFloat(newItem.FindPropertyRelative("rotationDegrees"), 0f, true);
			DynamicParameterUtility.SetFixedFloat(newItem.FindPropertyRelative("degreesTotal"), 0f, true);
			DynamicParameterUtility.SetFixedFloat(newItem.FindPropertyRelative("horizontalSpacing"), 0f, true);
			DynamicParameterUtility.SetFixedFloat(newItem.FindPropertyRelative("verticalSpacing"), 0f, true);
			DynamicParameterUtility.SetFixedFloat(newItem.FindPropertyRelative("xSpacingTotal"), 0f, true);
			DynamicParameterUtility.SetFixedFloat(newItem.FindPropertyRelative("ySpacingTotal"), 0f, true);

			DynamicParameterUtility.SetFixedVector2(newItem.FindPropertyRelative("globalMovement"), Vector2.zero, true);
			DynamicParameterUtility.SetFixedVector2(newItem.FindPropertyRelative("localMovement"), Vector2.zero, true);
			DynamicParameterUtility.SetFixedVector2(newItem.FindPropertyRelative("pointToLookAt"), Vector2.zero, true);
			DynamicParameterUtility.SetFixedVector2(newItem.FindPropertyRelative("pointToFleeFrom"), Vector2.zero, true);
			DynamicParameterUtility.SetFixedVector2(newItem.FindPropertyRelative("pivot"), Vector2.zero, true);
			DynamicParameterUtility.SetFixedVector2(newItem.FindPropertyRelative("scale"), Vector2.one, true);

			DynamicParameterUtility.SetFixedObject(newItem.FindPropertyRelative("bulletParams"), null, true);

			newItem.FindPropertyRelative("pivotColor").colorValue = Color.blue;

			newItem.FindPropertyRelative("isEditingSelection").boolValue = false;
			newItem.FindPropertyRelative("selectionRectsVisible").boolValue = true;
			newItem.FindPropertyRelative("indexesOfBulletsAffected").arraySize = 0;

			// helps prevent lag if huge shots are edited, by avoiding a default config that reorders whole array
			if (sp.bulletSpawns.Length > 100 || modifiers.arraySize > 1)
				newItem.FindPropertyRelative("modifierType").enumValueIndex = (int)ShotModifierType.GlobalTranslation;

			shouldRecalcAll = true;
			shouldRecalcLast = false;

			serializedObject.ApplyModifiedProperties();
		}

		// Use reflection to access internal functions and customize float fields
		private float CustomFloatField(Rect position, Rect dragHotZone, float value, GUIStyle style = null)
		{
			if (style == null) style = EditorStyles.numberField;
			int controlID = GUIUtility.GetControlID("EditorTextField".GetHashCode(), FocusType.Keyboard, position);

			object[] parameters = new object[] { recycledEditor, position, dragHotZone, controlID, value, "g7", style, true };

			return (float)doFloatFieldMethod.Invoke(null, parameters);
		}

		// Recalculates bullet spawns in each modifier, starting from sp.modifiers[index]
		void RecalculateLayoutFromModifier(int index)
		{
			RefreshBulletSpawnsIndexes();

			if (modifiers.arraySize == 0)
				return;

			// let's find out whether we're using a narrowed list of selected bullets
			int impactedModsLeft = 0;
			int indexOfSelectorModifier = -1; // -1 means "none"

			// pivot used for "RotateLayout" and "ScaleLayout", can be modified throughout modifier browsing
			Vector2 curPivot = Vector2.zero;

			for (int i = 0; i < modifiers.arraySize; i++)
			{
				SerializedProperty curMod = modifiers.GetArrayElementAtIndex(i);
				if (curMod.FindPropertyRelative("modifierType").enumValueIndex == (int)ShotModifierType.OnlySomeBullets)
				{
					if (curMod.FindPropertyRelative("enabled").boolValue)
					{
						impactedModsLeft = curMod.FindPropertyRelative("numberOfModifiersAffected").intValue;
						indexOfSelectorModifier = i;
					}
					else impactedModsLeft = 0;
				}
				else
				{
					impactedModsLeft--;
					if (impactedModsLeft < 0)
						indexOfSelectorModifier = -1;
				}

				curPivot = RefreshModifier(i, indexOfSelectorModifier, curPivot);
			}
		}

		// Re-stores different indexes in each bullet modifier of a shot.
		void RefreshBulletSpawnsIndexes()
		{
			if (sp.bulletSpawns != null)
				if (sp.bulletSpawns.Length > 0)
					for (int j = 0; j < sp.bulletSpawns.Length; j++)
						sp.bulletSpawns[j].index = j;

			//EditorUtility.SetDirty(sp);
		}

		// Recalculates bullet spawns in one modifier, based on previous spawn locations. Returns current pivot point.
		Vector2 RefreshModifier(int index, int indexOfSelectorModifier, Vector2 curPivot)
		{
			ShotModifier sm = sp.modifiers[index];
			BulletSpawn[] source = index == 0 ? sp.bulletSpawns : sp.modifiers[index - 1].postEffectBulletSpawns;

			// first things first, if it's a selector modifier we just refresh the indexes of affected bullets
			if (sm.modifierType == ShotModifierType.OnlySomeBullets)
			{
				List<int> indexes = new List<int>();										
				
				if (source != null)
					if (source.Length > 0)
						for (int i = 0; i < source.Length; i++)
							if (sm.selectionRects != null)
								if (sm.selectionRects.Length > 0)
								{
									bool hasBeenAdded = false;
									for (int j = 0; j < sm.selectionRects.Length; j++)
										if (!hasBeenAdded)
											if (IsInRectangle(source[i], sm.selectionRects[j]))
											{
												hasBeenAdded = true;
												indexes.Add(source[i].index);
											}
								}


				sm.indexesOfBulletsAffected = indexes.ToArray();
				
				if (source == null) sm.postEffectBulletSpawns = null;
				else
				{
					sm.postEffectBulletSpawns = new BulletSpawn[source.Length];
					if (source.Length > 0)
						source.CopyTo(sm.postEffectBulletSpawns, 0);
				}
				
				sp.modifiers[index] = sm;				
				//EditorUtility.SetDirty(sp);
				return curPivot;
			}

			// also, if this modifier is here to set rotate/scale pivot, just do it now and don't waste time browsing bullets.
			if (sm.modifierType == ShotModifierType.SetPivot && sm.enabled)
			{
				curPivot = DynamicParameterUtility.GetAverageVector2Value(sm.pivot);

				if (source == null) sm.postEffectBulletSpawns = null;
				else
				{
					sm.postEffectBulletSpawns = new BulletSpawn[source.Length];
					if (source.Length > 0)
						source.CopyTo(sm.postEffectBulletSpawns, 0);
				}
				
				sp.modifiers[index] = sm;				
				//EditorUtility.SetDirty(sp);
				return curPivot;
			}
			
			if (source == null) sm.postEffectBulletSpawns = null;
			else sm.postEffectBulletSpawns = new BulletSpawn[source.Length];

			// Of course we won't treat non-existing or empty arrays. We also reject disabled modifiers
			bool abort = false;
			if (source == null) abort = true;
			else if (source.Length == 0) abort = true;
			if (abort)
			{
				sp.modifiers[index] = sm;
				//EditorUtility.SetDirty(sp);
				return curPivot;
			}

			source.CopyTo(sm.postEffectBulletSpawns, 0);

			if (!sm.enabled)
			{
				sp.modifiers[index] = sm;
				//EditorUtility.SetDirty(sp);				
				return curPivot;
			}

			// If we're using a selection rect that contains exactly 0 bullet, we also don't apply the modification
			if (indexOfSelectorModifier > -1)
			{
				if (sp.modifiers[indexOfSelectorModifier].indexesOfBulletsAffected == null)
					abort = true;
				else if (sp.modifiers[indexOfSelectorModifier].indexesOfBulletsAffected.Length == 0)
					abort = true;
				
				if (abort)
				{
					sp.modifiers[index] = sm;
					//EditorUtility.SetDirty(sp);					
					return curPivot;
				}
			}

			// do the calc based on the modifierType of sm

			#region modifiers that induce reordering (like spread and spacing), which means spawns need to know each other

			bool reordered = false;

			if (sm.modifierType == ShotModifierType.SpreadBullets || sm.modifierType == ShotModifierType.SpreadBulletsTotal)
			{
				if (indexOfSelectorModifier > -1) // if narrowed by selector
				{
					List<BulletSpawn> selectedBullets = new List<BulletSpawn>();
					int nextSelectedBullet = 0;
					for (int i = 0; i < sm.postEffectBulletSpawns.Length; i++)
					{	
						if (nextSelectedBullet == sp.modifiers[indexOfSelectorModifier].indexesOfBulletsAffected.Length) continue;
						else if (sm.postEffectBulletSpawns[i].index != sp.modifiers[indexOfSelectorModifier].indexesOfBulletsAffected[nextSelectedBullet])
							continue;
						else nextSelectedBullet++;
						selectedBullets.Add(sm.postEffectBulletSpawns[i]);
					}

					BulletSpawn[] toEdit = selectedBullets.ToArray();
					toEdit = GetSortedBullets(toEdit, BulletSortMode.Z, true);

					int bullets = toEdit.Length;
					bool even = (bullets % 2 == 0);
					int centerBullet = even ? (bullets / 2 - 1) : ((bullets - 1) / 2);

					// apply step
					float actualAngle = sm.modifierType == ShotModifierType.SpreadBullets ? DynamicParameterUtility.GetAverageFloatValue(sm.degrees) : (DynamicParameterUtility.GetAverageFloatValue(sm.degreesTotal)/(float)bullets);
					for (int i = 0; i < bullets; i++)
					{
						float steps = centerBullet - i;
						if (even) steps += 0.5f;
						toEdit[i].bulletOrigin.z += actualAngle * steps;
					}
					toEdit = GetSortedBulletsByIndex(toEdit, false);
					int nextRelevantBullet = 0;
					
					sm.postEffectBulletSpawns = GetSortedBulletsByIndex(sm.postEffectBulletSpawns, false);
					
					// once back to index sort, use it to apply changes in the whole array
					for (int i = 0; i < sm.postEffectBulletSpawns.Length; i++)
					{
						if (nextRelevantBullet == toEdit.Length) continue;
						else if (sm.postEffectBulletSpawns[i].index != toEdit[nextRelevantBullet].index) continue;
						
						sm.postEffectBulletSpawns[i] = toEdit[nextRelevantBullet];
						nextRelevantBullet++;
					}
				}
				else // unmodified by selector, default function
				{
					sm.postEffectBulletSpawns = GetSortedBullets(sm.postEffectBulletSpawns, BulletSortMode.Z, true);
					int bullets = sm.postEffectBulletSpawns.Length;
					bool even = (bullets % 2 == 0);
					int centerBullet = even ? (bullets / 2 - 1) : ((bullets - 1) / 2);

					// apply step
					float actualAngle = sm.modifierType == ShotModifierType.SpreadBullets ? DynamicParameterUtility.GetAverageFloatValue(sm.degrees) : (DynamicParameterUtility.GetAverageFloatValue(sm.degreesTotal)/(float)bullets);
					for (int i = 0; i < bullets; i++)
					{
						float steps = centerBullet - i;
						if (even) steps += 0.5f;
						sm.postEffectBulletSpawns[i].bulletOrigin.z += actualAngle * steps;
					}
				}

				reordered = true;
			}

			else if (sm.modifierType == ShotModifierType.HorizontalSpacing || sm.modifierType == ShotModifierType.HorizontalSpacingTotal)
			{
				if (indexOfSelectorModifier > -1)
				{
					List<BulletSpawn> selectedBullets = new List<BulletSpawn>();
					int nextSelectedBullet = 0;
					for (int i = 0; i < sm.postEffectBulletSpawns.Length; i++)
					{	
						if (nextSelectedBullet == sp.modifiers[indexOfSelectorModifier].indexesOfBulletsAffected.Length) continue;
						else if (sm.postEffectBulletSpawns[i].index != sp.modifiers[indexOfSelectorModifier].indexesOfBulletsAffected[nextSelectedBullet])
							continue;
						else nextSelectedBullet++;
						selectedBullets.Add(sm.postEffectBulletSpawns[i]);
					}

					BulletSpawn[] toEdit = selectedBullets.ToArray();
					toEdit = GetSortedBullets(toEdit, BulletSortMode.X, true);

					int bullets = toEdit.Length;
					bool even = (bullets % 2 == 0);
					int centerBullet = even ? (bullets / 2 - 1) : ((bullets - 1) / 2);

					// apply step
					float actualSpacing = sm.modifierType == ShotModifierType.HorizontalSpacing ? DynamicParameterUtility.GetAverageFloatValue(sm.horizontalSpacing) : (DynamicParameterUtility.GetAverageFloatValue(sm.xSpacingTotal)/(float)bullets);
					for (int i = 0; i < bullets; i++)
					{
						float steps = centerBullet - i;
						if (even) steps += 0.5f;
						toEdit[i].bulletOrigin.x += actualSpacing * steps;
					}
					toEdit = GetSortedBulletsByIndex(toEdit, false);
					sm.postEffectBulletSpawns = GetSortedBulletsByIndex(sm.postEffectBulletSpawns, false);
					int nextRelevantBullet = 0;
					for (int i = 0; i < sm.postEffectBulletSpawns.Length; i++)
					{
						if (nextRelevantBullet == toEdit.Length) continue;
						else if (sm.postEffectBulletSpawns[i].index != toEdit[nextRelevantBullet].index) continue;
						
						sm.postEffectBulletSpawns[i] = toEdit[nextRelevantBullet];
						nextRelevantBullet++;
					}
				}
				else
				{
					sm.postEffectBulletSpawns = GetSortedBullets(sm.postEffectBulletSpawns, BulletSortMode.X, true);
					int bullets = sm.postEffectBulletSpawns.Length;
					bool even = (bullets % 2 == 0);
					int centerBullet = even ? (bullets / 2 - 1) : ((bullets - 1) / 2);

					// apply step
					float actualSpacing = sm.modifierType == ShotModifierType.HorizontalSpacing ? DynamicParameterUtility.GetAverageFloatValue(sm.horizontalSpacing) : (DynamicParameterUtility.GetAverageFloatValue(sm.xSpacingTotal)/(float)bullets);
					for (int i = 0; i < bullets; i++)
					{
						float steps = centerBullet - i;
						if (even) steps += 0.5f;
						sm.postEffectBulletSpawns[i].bulletOrigin.x += actualSpacing * steps;
					}
				}

				reordered = true;
			}

			else if (sm.modifierType == ShotModifierType.VerticalSpacing || sm.modifierType == ShotModifierType.VerticalSpacingTotal)
			{
				if (indexOfSelectorModifier > -1)
				{
					List<BulletSpawn> selectedBullets = new List<BulletSpawn>();
					int nextSelectedBullet = 0;
					for (int i = 0; i < sm.postEffectBulletSpawns.Length; i++)
					{	
						if (nextSelectedBullet == sp.modifiers[indexOfSelectorModifier].indexesOfBulletsAffected.Length) continue;
						else if (sm.postEffectBulletSpawns[i].index != sp.modifiers[indexOfSelectorModifier].indexesOfBulletsAffected[nextSelectedBullet])
							continue;
						else nextSelectedBullet++;
						selectedBullets.Add(sm.postEffectBulletSpawns[i]);
					}

					BulletSpawn[] toEdit = selectedBullets.ToArray();
					toEdit = GetSortedBullets(toEdit, BulletSortMode.Y, true);

					int bullets = toEdit.Length;
					bool even = (bullets % 2 == 0);
					int centerBullet = even ? (bullets / 2 - 1) : ((bullets - 1) / 2);

					// apply step
					float actualSpacing = sm.modifierType == ShotModifierType.VerticalSpacing ? DynamicParameterUtility.GetAverageFloatValue(sm.verticalSpacing) : (DynamicParameterUtility.GetAverageFloatValue(sm.ySpacingTotal)/(float)bullets);
					for (int i = 0; i < bullets; i++)
					{
						float steps = centerBullet - i;
						if (even) steps += 0.5f;
						toEdit[i].bulletOrigin.y += actualSpacing * steps;
					}
					toEdit = GetSortedBulletsByIndex(toEdit, false);
					sm.postEffectBulletSpawns = GetSortedBulletsByIndex(sm.postEffectBulletSpawns, false);
					int nextRelevantBullet = 0;
						
					for (int i = 0; i < sm.postEffectBulletSpawns.Length; i++)
					{
						if (nextRelevantBullet == toEdit.Length) continue;
						else if (sm.postEffectBulletSpawns[i].index != toEdit[nextRelevantBullet].index) continue;
						
						sm.postEffectBulletSpawns[i] = toEdit[nextRelevantBullet];
						nextRelevantBullet++;
					}
				}
				else
				{
					sm.postEffectBulletSpawns = GetSortedBullets(sm.postEffectBulletSpawns, BulletSortMode.Y, true);
					int bullets = sm.postEffectBulletSpawns.Length;
					bool even = (bullets % 2 == 0);
					int centerBullet = even ? (bullets / 2 - 1) : ((bullets - 1) / 2);

					// apply step
					float actualSpacing = sm.modifierType == ShotModifierType.VerticalSpacing ? DynamicParameterUtility.GetAverageFloatValue(sm.verticalSpacing) : (DynamicParameterUtility.GetAverageFloatValue(sm.ySpacingTotal)/(float)bullets);
					for (int i = 0; i < bullets; i++)
					{
						float steps = centerBullet - i;
						if (even) steps += 0.5f;
						sm.postEffectBulletSpawns[i].bulletOrigin.y += actualSpacing * steps;
					}
				}
				
				reordered = true;
			}

			if (reordered)
			{
				sp.modifiers[index] = sm;
				//EditorUtility.SetDirty(sp);
				return curPivot;
			}

			#endregion

			#region all other modifiers

			int nextBullet = 0;
			for (int i = 0; i < sm.postEffectBulletSpawns.Length; i++)
			{
				// discard unaffected bullets when using a modifier
				if (indexOfSelectorModifier > -1)
				{
					if (nextBullet == sp.modifiers[indexOfSelectorModifier].indexesOfBulletsAffected.Length) continue;
					else if (sm.postEffectBulletSpawns[i].index != sp.modifiers[indexOfSelectorModifier].indexesOfBulletsAffected[nextBullet])
						continue;
					else nextBullet++;					
				}

				if (sm.modifierType == ShotModifierType.LookAtPoint)
				{
					sm.postEffectBulletSpawns[i].bulletOrigin.z = LookAtTarget(sm.postEffectBulletSpawns[i].bulletOrigin, DynamicParameterUtility.GetAverageVector2Value(sm.pointToLookAt));
					sm.postEffectBulletSpawns[i].bulletOrigin.z += 720;
					sm.postEffectBulletSpawns[i].bulletOrigin.z = sm.postEffectBulletSpawns[i].bulletOrigin.z % 360;
					// display debug
					if (Mathf.Abs(sm.postEffectBulletSpawns[i].bulletOrigin.z) < 0.001f) sm.postEffectBulletSpawns[i].bulletOrigin.z = 0;
				}
				else if (sm.modifierType == ShotModifierType.LookAwayFromPoint)
				{
					sm.postEffectBulletSpawns[i].bulletOrigin.z = LookAwayFromTarget(sm.postEffectBulletSpawns[i].bulletOrigin, DynamicParameterUtility.GetAverageVector2Value(sm.pointToFleeFrom));
					sm.postEffectBulletSpawns[i].bulletOrigin.z += 720;
					sm.postEffectBulletSpawns[i].bulletOrigin.z = sm.postEffectBulletSpawns[i].bulletOrigin.z % 360;
					// display debug
					if (Mathf.Abs(sm.postEffectBulletSpawns[i].bulletOrigin.z) < 0.001f) sm.postEffectBulletSpawns[i].bulletOrigin.z = 0;
				}
				else if (sm.modifierType == ShotModifierType.ResetCoordinates)
				{
					bool resetX = DynamicParameterUtility.GetBool(sm.resetX);
					bool resetY = DynamicParameterUtility.GetBool(sm.resetY);
					bool resetZ = DynamicParameterUtility.GetBool(sm.resetZ);
					if (resetX) sm.postEffectBulletSpawns[i].bulletOrigin.x = 0;
					if (resetY) sm.postEffectBulletSpawns[i].bulletOrigin.y = 0;
					if (resetZ) sm.postEffectBulletSpawns[i].bulletOrigin.z = 0;
				}
				else if (sm.modifierType == ShotModifierType.GlobalTranslation)
				{
					Vector2 globalMovement = DynamicParameterUtility.GetAverageVector2Value(sm.globalMovement);
					Vector3 gm = new Vector3(globalMovement.x, globalMovement.y, 0);
					sm.postEffectBulletSpawns[i].bulletOrigin += gm;
				}
				else if (sm.modifierType == ShotModifierType.LocalTranslation)
				{
					Vector2 localMovement = DynamicParameterUtility.GetAverageVector2Value(sm.localMovement);
					Vector2 x = GetRelativeRight(sm.postEffectBulletSpawns[i]);
					Vector2 y = GetRelativeUp(sm.postEffectBulletSpawns[i]);
					Vector2 total = x * localMovement.x + y * localMovement.y;
					Vector3 totalV3 = new Vector3(total.x, total.y, 0);
					sm.postEffectBulletSpawns[i].bulletOrigin += totalV3;
				}
				else if (sm.modifierType == ShotModifierType.Rotation)
				{
					Vector3 gm = Vector3.forward * DynamicParameterUtility.GetAverageFloatValue(sm.rotationDegrees);
					sm.postEffectBulletSpawns[i].bulletOrigin += gm;
					sm.postEffectBulletSpawns[i].bulletOrigin.z += 720;
					sm.postEffectBulletSpawns[i].bulletOrigin.z = sm.postEffectBulletSpawns[i].bulletOrigin.z % 360;
				}
				else if (sm.modifierType == ShotModifierType.RotateAroundPivot)
				{
					float layoutDegrees = DynamicParameterUtility.GetAverageFloatValue(sm.layoutDegrees);

					float x = sm.postEffectBulletSpawns[i].bulletOrigin.x;
					float y = sm.postEffectBulletSpawns[i].bulletOrigin.y;
					float a = Mathf.Deg2Rad * layoutDegrees;
					float c = Mathf.Cos(a);
					float s = Mathf.Sin(a);

					x -= curPivot.x;
					y -= curPivot.y;
					float newX = x*c - y*s;
					float newY = x*s + y*c;
					newX += curPivot.x;
					newY += curPivot.y;

					float newZ = sm.postEffectBulletSpawns[i].bulletOrigin.z;
					newZ += layoutDegrees;
					newZ += 720;
					newZ = newZ % 360;

					sm.postEffectBulletSpawns[i].bulletOrigin = new Vector3(newX, newY, newZ);
				}
				else if (sm.modifierType == ShotModifierType.ScaleLayout)
				{
					Vector2 scale = DynamicParameterUtility.GetAverageVector2Value(sm.scale);
					Vector3 o = sm.postEffectBulletSpawns[i].bulletOrigin;
					float newX = curPivot.x + (o.x-curPivot.x) * scale.x;
					float newY = curPivot.y + (o.y-curPivot.y) * scale.y;
					
					float newZ = o.z;
					bool flipX = scale.x < 0;
					bool flipY = scale.y < 0;
					if (flipY != flipX) newZ *= -1;
					if (flipY) newZ += 180;
					newZ += 720;
					newZ = newZ % 360;
					
					sm.postEffectBulletSpawns[i].bulletOrigin = new Vector3(newX, newY, newZ);
				}
				else if (sm.modifierType == ShotModifierType.FlipOrientation)
				{
					bool flipX = DynamicParameterUtility.GetBool(sm.flipX);
					bool flipY = DynamicParameterUtility.GetBool(sm.flipY);

					float newZ = sm.postEffectBulletSpawns[i].bulletOrigin.z;
					if (flipY != flipX) newZ *= -1;
					if (flipY) newZ += 180;
					newZ += 720;
					newZ = newZ % 360;
					sm.postEffectBulletSpawns[i].bulletOrigin = new Vector3(sm.postEffectBulletSpawns[i].bulletOrigin.x, sm.postEffectBulletSpawns[i].bulletOrigin.y, newZ);
				}
				else if (sm.modifierType == ShotModifierType.SetBulletParams)
				{
					if (sm.bulletParams.baseValue) // TODO : this could first check that bulletParams is an initialized, fixed value. And otherwise treat it as if baseValue was null.
					{
						sm.postEffectBulletSpawns[i].bulletParams = sm.bulletParams.baseValue;
						sm.postEffectBulletSpawns[i].useDifferentBullet = true;
					}
				}
			}

			#endregion

			sp.modifiers[index] = sm;
			//EditorUtility.SetDirty(sp);
			return curPivot;
		}

		// Returns needed angle to look at Vector2(x,y).
		public float LookAtTarget(Vector3 bulletSpawn, Vector2 target)
		{
			Vector2 bullet = new Vector2(bulletSpawn.x, bulletSpawn.y);
			Vector2 diff = target - bullet;
			if (diff == Vector2.zero) return bulletSpawn.z;

			Vector2 oldOrientation = new Vector2(Mathf.Cos(bulletSpawn.z * Mathf.Deg2Rad), Mathf.Sin(bulletSpawn.z * Mathf.Deg2Rad));
			float angle = Vector2.Angle(oldOrientation, diff);
			if (Vector3.Cross(oldOrientation, diff).z < 0) angle *= -1;
			return bulletSpawn.z + angle - 90;
			// -90 because "0 rotation" would look to the right in math, but gameplay main direction is Vector2.up
		}

		// Returns needed angle to look away from Vector2(x,y).
		public float LookAwayFromTarget(Vector3 bulletSpawn, Vector2 target)
		{
			Vector2 bullet = new Vector2(bulletSpawn.x, bulletSpawn.y);
			Vector2 diff = target - bullet;
			if (diff == Vector2.zero) return bulletSpawn.z;

			Vector2 oldOrientation = new Vector2(Mathf.Cos(bulletSpawn.z * Mathf.Deg2Rad), Mathf.Sin(bulletSpawn.z * Mathf.Deg2Rad));
			float angle = Vector2.Angle(oldOrientation, diff);
			if (Vector3.Cross(oldOrientation, diff).z < 0) angle *= -1;
			return bulletSpawn.z + angle + 180 - 90;
			// +180 because it's basically the same as "look at", but in the opposite direction
			// -90 because "0 rotation" would look to the right in math, but gameplay main direction is Vector2.up
		}

		// Sorts bullets of a same shot, by X, Y or Z - used in spreading and spacing bullets
		public BulletSpawn[] GetSortedBullets(BulletSpawn[] raw, BulletSortMode sortMode, bool descending)
		{
			if (raw == null) return null;

			int bulletCount = raw.Length;
			if (bulletCount < 2) return raw;

			List<BulletSpawn> listResult = new List<BulletSpawn>();
			List<BulletSpawn> rawList = new List<BulletSpawn>();
			for (int i = 0; i < bulletCount; i++)
				rawList.Add(raw[i]);

			for (int i = 0; i < bulletCount; i++)
			{
				int indexOfNext = 0;
				for (int j = 0; j < rawList.Count; j++)
				{
					// Sort mode : by X
					if (sortMode == BulletSortMode.X)
					{
						if (rawList[j].bulletOrigin.x == rawList[indexOfNext].bulletOrigin.x)
						{
							// tie-breaker : Y
							if (!descending && rawList[j].bulletOrigin.y < rawList[indexOfNext].bulletOrigin.y) indexOfNext = j;
							else if (descending && rawList[j].bulletOrigin.y > rawList[indexOfNext].bulletOrigin.y) indexOfNext = j;
						}
						else
						{
							if (!descending && rawList[j].bulletOrigin.x < rawList[indexOfNext].bulletOrigin.x) indexOfNext = j;
							else if (descending && rawList[j].bulletOrigin.x > rawList[indexOfNext].bulletOrigin.x) indexOfNext = j;
						}
					}

					// Sort mode : by Y
					else if (sortMode == BulletSortMode.Y)
					{
						if (rawList[j].bulletOrigin.y == rawList[indexOfNext].bulletOrigin.y)
						{
							// tie-breaker : X
							if (!descending && rawList[j].bulletOrigin.x < rawList[indexOfNext].bulletOrigin.x) indexOfNext = j;
							else if (descending && rawList[j].bulletOrigin.x > rawList[indexOfNext].bulletOrigin.x) indexOfNext = j;
						}
						else
						{
							if (!descending && rawList[j].bulletOrigin.y < rawList[indexOfNext].bulletOrigin.y) indexOfNext = j;
							else if (descending && rawList[j].bulletOrigin.y > rawList[indexOfNext].bulletOrigin.y) indexOfNext = j;
						}
						
					}

					// Sort mode : by Z
					else if (sortMode == BulletSortMode.Z)
					{
						// get angle values from 0 to 360
						float jz = rawList[j].bulletOrigin.z %360;
						float nz = rawList[indexOfNext].bulletOrigin.z %360;

						if (jz == nz)
						{
							// tie-breaker : X
							if (rawList[j].bulletOrigin.x == rawList[indexOfNext].bulletOrigin.x)
							{
								// tie-breaker's tie-breaker : Y
								if (!descending && rawList[j].bulletOrigin.y < rawList[indexOfNext].bulletOrigin.y) indexOfNext = j;
								else if (descending && rawList[j].bulletOrigin.y > rawList[indexOfNext].bulletOrigin.y) indexOfNext = j;
							}
							else
							{
								if (!descending && rawList[j].bulletOrigin.x < rawList[indexOfNext].bulletOrigin.x) indexOfNext = j;
								else if (descending && rawList[j].bulletOrigin.x > rawList[indexOfNext].bulletOrigin.x) indexOfNext = j;
							}
						}
						else
						{
							if (!descending && jz < nz) indexOfNext = j;
							else if (descending && jz > nz) indexOfNext = j;	
						}
					}
				}

				listResult.Add(rawList[indexOfNext]);
				rawList.RemoveAt(indexOfNext);
			}

			return listResult.ToArray();
		}

		// Sorts bullets of a same shot, by internal index - used in managing selector modifiers when spreading and spacing bullets
		public BulletSpawn[] GetSortedBulletsByIndex(BulletSpawn[] raw, bool descending)
		{
			if (raw == null) return null;

			int bulletCount = raw.Length;
			if (bulletCount < 2) return raw;

			List<BulletSpawn> listResult = new List<BulletSpawn>();
			List<BulletSpawn> rawList = new List<BulletSpawn>();
			for (int i = 0; i < bulletCount; i++)
				rawList.Add(raw[i]);

			for (int i = 0; i < bulletCount; i++)
			{
				int indexOfNext = 0;
				for (int j = 0; j < rawList.Count; j++)
				{
					if (!descending && rawList[j].index < rawList[indexOfNext].index) indexOfNext = j;
					if (descending && rawList[j].index > rawList[indexOfNext].index) indexOfNext = j;
				}

				listResult.Add(rawList[indexOfNext]);
				rawList.RemoveAt(indexOfNext);
			}

			return listResult.ToArray();
		}

		// Returns the "self.right" vector of a BulletSpawn :
		public Vector2 GetRelativeRight(BulletSpawn bs)
		{
			float z = bs.bulletOrigin.z;
			float x = 1;
			float y = 0;

			float cos = Mathf.Cos(z * Mathf.Deg2Rad);
			float sin = Mathf.Sin(z * Mathf.Deg2Rad);

			Vector2 result = new Vector2(x * cos - y * sin, x * sin + y * cos);
			// boils down to (cos, sin), but makes it way more explicit.

			return result.normalized;
		}

		// Returns the "self.up" vector of a BulletSpawn :
		public Vector2 GetRelativeUp(BulletSpawn bs)
		{
			float z = bs.bulletOrigin.z;
			float x = 0;
			float y = 1;

			float cos = Mathf.Cos(z * Mathf.Deg2Rad);
			float sin = Mathf.Sin(z * Mathf.Deg2Rad);

			Vector2 result = new Vector2(x * cos - y * sin, x * sin + y * cos);
			// ^ boils down to (-sin,cos), but makes it way more explicit.

			return result.normalized;
		}

		// And their inverted counterparts
		public Vector2 GetRelativeDown(BulletSpawn bs) { return -1 * GetRelativeUp(bs); }
		public Vector2 GetRelativeLeft(BulletSpawn bs) { return -1 * GetRelativeRight(bs); }

		// Calculates extreme spawn positions. Is not registered in the undo stack, may even be called at undo.
		public void ReplaceExtremeValues()
		{
			if (sp.bulletSpawns == null) return;
			if (sp.bulletSpawns.Length == 0) return;

			Vector3 highest = Vector3.zero;
			Vector3 lowest = Vector3.zero;

			// If there are modifiers, use their BulletSpawn locations instead of the raw one stored in ShotParams
			bool useMod = true;
			if (sp.modifiers == null) useMod = false;
			if (sp.modifiers.Count == 0) useMod = false;
			BulletSpawn[] spawnLocs = null;
			if (useMod) spawnLocs = sp.modifiers[sp.modifiers.Count - 1].postEffectBulletSpawns;
			else spawnLocs = sp.bulletSpawns;

			highest = spawnLocs[0].bulletOrigin;
			lowest = spawnLocs[0].bulletOrigin;

			if (sp.bulletSpawns.Length == 1)
			{
				sp.highestValues = highest;
				sp.lowestValues = lowest;
				//EditorUtility.SetDirty(sp);
				return;
			}

			for (int i = 1; i < sp.bulletSpawns.Length; i++)
			{
				highest.x = Mathf.Max(highest.x, spawnLocs[i].bulletOrigin.x);
				highest.y = Mathf.Max(highest.y, spawnLocs[i].bulletOrigin.y);
				highest.z = Mathf.Max(highest.z, spawnLocs[i].bulletOrigin.z);

				lowest.x = Mathf.Min(lowest.x, spawnLocs[i].bulletOrigin.x);
				lowest.y = Mathf.Min(lowest.y, spawnLocs[i].bulletOrigin.y);
				lowest.z = Mathf.Min(lowest.z, spawnLocs[i].bulletOrigin.z);
			}

			sp.highestValues = highest;
			sp.lowestValues = lowest;

			//EditorUtility.SetDirty(sp);
		}

		// Finds a nice color for displaying modifiers impacted by a bullet selection.
		public Color MakeGUIFriendly(Color col)
		{
			float alpha = 0.5f;

			Color result = col;
			float val = result.r*0.3f + result.g*0.59f + result.b*0.11f;
			if (val == 0) return new Color(0.5f, 0.5f, 0.5f, alpha);
			if (val < 0.7f) result *= (0.7f/val);
			result.a = alpha;
			return result;
		}

		// Apply GUI-friendly color to a gradient texture that does not mess with ReorderableLists
		public Texture2D GradientTex(Color col)
		{
			Texture2D tex = new Texture2D(128, 1);
			for (int i=0; i<tex.width; i++)
			{
				float ratio = 1f - (float)i/(float)tex.width;
				tex.SetPixel(i, 0, new Color(col.r, col.g, col.b, col.a*ratio*ratio)); // squared ratio looks cooler
			}
			tex.wrapMode = TextureWrapMode.Clamp;
			tex.Apply();
			return tex;
		}

		#endregion

		#region texture toolbox

		// Calculates the bullets from a texture, so the user can draw a shot (black on white)
		public BulletSpawn[] GetBulletsFromTexture(Texture2D tex, int slice, Vector2 ingameSizeOfTex)
		{
			List<BulletSpawn> output = new List<BulletSpawn>();
			List<Vector2> preOutput = new List<Vector2>();

			int texW = tex.width;
			int texH = tex.height;
			float pixelsPerSliceX = texW / slice;
			float pixelsPerSliceY = texH / slice;

			// get black pixels
			for (int i = 0; i < slice; i++)
				for (int j = 0; j < slice; j++)
					for (int u = 0; u < pixelsPerSliceX; u++)
					{
						bool hasFoundBlackPixel = false;
						for (int v = 0; v < pixelsPerSliceY; v++)
						{
							Color px = tex.GetPixel((int)(pixelsPerSliceX * i) + u, (int)(pixelsPerSliceY * j) + v);
							if (px.r < 0.5f)
							{
								preOutput.Add(new Vector2(i, j));
								hasFoundBlackPixel = true;
								break;
							}
						}
						if (hasFoundBlackPixel) break;
					}

			if (preOutput.Count == 0)
			{
				Debug.Log("No bullets found. Your texture is either too bright, or too small for this slice rate.");
				return null;
			}

			// for each black pixel, map it to world coordinates and output it as a bullet spawn origin
			for (int i = 0; i < preOutput.Count; i++)
			{
				BulletSpawn bs = new BulletSpawn();
				float x = preOutput[i].x;
				float y = preOutput[i].y;

				// map from UV to XY
				x -= slice * 0.5f;
				y -= slice * 0.5f;
				// map from big pixels XY to ratio [-0.5 to 0.5]
				x /= slice;
				y /= slice;
				// map from ratio to actual ingame distance
				x *= ingameSizeOfTexture.vector2Value.x;
				y *= ingameSizeOfTexture.vector2Value.y;

				bs.bulletOrigin = new Vector3(x, y, 0);
				output.Add(bs);
			}

			return output.ToArray();
		}

		// Calculates the preview texture from a list of bullets. Also updates width of the shot.
		public void RefreshOutputTexture()
		{
			// target shouldn't be null, but it can somehow bypass the OnEnable check
			if (sp == null) return;

			// rarely needed fix (retrocompatibility)
			if (sp.outputTexture.tex == null)
				sp.outputTexture.tex = new Texture2D(256, 256);

			Texture2D tex = sp.outputTexture.tex;

			int reso = tex.width;

			// Start with the background + grid
			for (int i = 0; i < reso; i++)
				for (int j = 0; j < reso; j++)
				{
					bool border = i<borderWidth || j<borderWidth || i>=reso-borderWidth || j>=reso-borderWidth;
					bool grid = ((i-gridOffset.x)%Mathf.Ceil(reso/10)==0) || ((j-gridOffset.y)%Mathf.Ceil(reso/10)==0);
					if (border) grid = false;
					Color col = border?outputTextureBorderColor:outputTextureBgColor;
					if (!border && grid) col = outputTextureGridColor;
					tex.SetPixel(i, j, col);
				}

			// Should we read modifiers first, or just base spawn positions ?
			BulletSpawn[] bs = null;
			bool useShot = false;
			if (sp.modifiers == null) useShot = true;
			else if (sp.modifiers.Count == 0) useShot = true;

			if (useShot) bs = sp.bulletSpawns;
			else bs = sp.modifiers[sp.modifiers.Count - 1].postEffectBulletSpawns;

			bool abort = false;
			if (bs == null) abort = true;
			else if (bs.Length == 0) abort = true;
			if (abort)
			{
				tex.Apply();
				//EditorUtility.SetDirty(sp);
				return;
			}

			// get shot size in world space
			float farthestFromZero = 0;
			for (int i = 0; i < bs.Length; i++)
			{
				Vector3 o = bs[i].bulletOrigin;
				float absX = Mathf.Abs(o.x);
				float absY = Mathf.Abs(o.y);
				if (absX > farthestFromZero) farthestFromZero = absX;
				if (absY > farthestFromZero) farthestFromZero = absY;
			}
			// browse pivot points as they can also be "the farthest from zero"
			if (!useShot)
				for (int i = 0; i < sp.modifiers.Count; i++)
				{
					if (sp.modifiers[i].modifierType != ShotModifierType.SetPivot)
						continue;
					Vector2 piv = DynamicParameterUtility.GetAverageVector2Value(sp.modifiers[i].pivot);
					float absX = Mathf.Abs(piv.x);
					float absY = Mathf.Abs(piv.y);
					if (absX > farthestFromZero) farthestFromZero = absX;
					if (absY > farthestFromZero) farthestFromZero = absY;
				}

			farthestFromZero *= 1.1f; // get a little margin so the farthest bullets don't end up in an edge
			if (farthestFromZero == 0) farthestFromZero = 1; // we want to be able to return a point if all bullets are at zero

			// So, the whole texture width will represent an ingame distance of 2*farthestFromZero.
			// sp.outputTexture.ingameDistance = 2 * farthestFromZero;

			// Let's make that to the next power of five to display clearer movement :
			float log = Mathf.Log(2 * farthestFromZero, 5);
			log = Mathf.Ceil(log);
			float newDist = Mathf.Pow(5, log);

			// If the new scale is different, redraw selection rects accordingly
			if (sp.outputTexture.ingameDistance != newDist)
			{
				float ratio = sp.outputTexture.ingameDistance / newDist;
				
				Vector2 currentPivot = Vector2.zero;
				if (sp.modifiers != null)
					if (sp.modifiers.Count > 0)
						for (int i = 0; i < sp.modifiers.Count; i++)
						{
							if (sp.modifiers[i].modifierType == ShotModifierType.SetPivot)
								currentPivot = DynamicParameterUtility.GetAverageVector2Value(sp.modifiers[i].pivot);
							else if (sp.modifiers[i].modifierType == ShotModifierType.OnlySomeBullets)
							{
								if (sp.modifiers[i].selectionRects != null)
									if (sp.modifiers[i].selectionRects.Length > 0)
										for (int j = 0; j < sp.modifiers[i].selectionRects.Length; j++)
										{
											Rect r = sp.modifiers[i].selectionRects[j];
											r = new Rect(r.x - 0.5f, r.y - 0.5f, r.width, r.height); // go to shot coordinates
											r = new Rect(r.x * ratio, r.y * ratio, r.width * ratio, r.height * ratio); // proceed to scale
											r = new Rect(r.x + 0.5f, r.y + 0.5f, r.width, r.height); // go back to rect coordinates
											sp.modifiers[i].selectionRects[j] = r;
										}
							}
						}
			}

			// Apply changes
			sp.outputTexture.ingameDistance = newDist;
			float halfIngameDist = sp.outputTexture.ingameDistance * 0.5f;

			for (int i = 0; i < bs.Length; i++)
			{
				Vector3 o = bs[i].bulletOrigin;
				float px = Mathf.InverseLerp(-halfIngameDist, halfIngameDist, o.x) * reso;
				float py = Mathf.InverseLerp(-halfIngameDist, halfIngameDist, o.y) * reso;
				int pxi = (int)px;
				int pyi = (int)py;

				DrawPointOnTexture(pxi, pyi, o.z, outputTextureBulletColor);
			}

			if (!useShot)
				for (int i = 0; i < sp.modifiers.Count; i++)
				{
					ShotModifier sm = sp.modifiers[i];
					if (sm.modifierType != ShotModifierType.SetPivot) continue;
					if (!sm.enabled) continue;

					Vector2 pivot = DynamicParameterUtility.GetAverageVector2Value(sm.pivot);
					float px = Mathf.InverseLerp(-halfIngameDist, halfIngameDist, pivot.x) * reso;
					if (px < 2 || px > reso-3) continue;
					float py = Mathf.InverseLerp(-halfIngameDist, halfIngameDist, pivot.y) * reso;
					if (py < 2 || py > reso-3) continue;
					int pxi = (int)px;
					int pyi = (int)py;
					DrawPointOnTexture(pxi, pyi, 0, sm.pivotColor, false);					
				}

			// We're done !
			RefreshTextureSavedSelectionRects();
			tex.Apply();
			//EditorUtility.SetDirty(sp);
		}

		// Draws a 3x3 black square (thick pixel) at wanted coordinates. Alters x/y/z if the texture needs to look down/left/right.
		public void DrawPointOnTexture(int x, int y, float z, Color color, bool drawLine=true)
		{
			Texture2D tex = sp.outputTexture.tex;
			int reso = tex.width;

			ShotTextureOrientation sto = sp.outputTexture.orientation;
			int quarters = 0;
			if (sto == ShotTextureOrientation.Left) quarters = 1;
			if (sto == ShotTextureOrientation.Down) quarters = 2;
			if (sto == ShotTextureOrientation.Right) quarters = 3;
			while (quarters > 0)
			{
				quarters--;
				int newX = (reso-1)-y;
				int newY = x;
				x = newX;
				y = newY;
				z += 90;
			}

			int initX = x;
			int initY = y;

			x--;
			y--;
			for (int u = 0; u < 3; u++)
			{
				for (int v = 0; v < 3; v++)
				{
					if (x > 0 && x < reso && y > 0 && y < reso)
						tex.SetPixel(x, y, color);
					y++;
				}
				x++;
				y -= 3;
			}

			if (drawLine) DrawLineOnTexture(initX, initY, z);
		}

		// Draws a thin line from bullet to a point it looks at, to render its orientation.
		public void DrawLineOnTexture(int x, int y, float z)
		{
			// get texture info
			Texture2D tex = sp.outputTexture.tex;
			int reso = tex.width;

			// get the line as a Vector2
			float angle = z * Mathf.Deg2Rad;
			Vector2 selfUp = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
			Vector2 line = selfUp.normalized * reso * 0.5f; // each line will be half the tex width long

			// get line ends coordinates
			int endX = x + (int)line.x;
			int endY = y + (int)line.y;

			// determine how we will count pixels
			float pixelCount = (Mathf.Abs(line.x) > Mathf.Abs(line.y)) ? Mathf.Abs(endX - x) : Mathf.Abs(endY - y);
			float invPc = 1 / pixelCount;

			// actual drawing
			for (int i = 0; i < pixelCount; i++)
			{
				float px = Mathf.Lerp(x, endX, i * invPc);
				float py = Mathf.Lerp(y, endY, i * invPc);

				// if we went out of bounds, stop here
				if (px < 0) break;
				if (py < 0) break;
				if (px >= reso) break;
				if (py >= reso) break;

				Color prev = tex.GetPixel((int)px, (int)py);
				Color newCol = Color.Lerp(outputTextureBulletColor, prev, i * invPc);
				tex.SetPixel((int)px, (int)py, newCol);
				
				//float colorVal = Mathf.Lerp(0.5f, 1, i * invPc);
				//if (tex.GetPixel((int)px, (int)py).r > colorVal)
					//tex.SetPixel((int)px, (int)py, Color.white * colorVal);
			}
		}

		// Saves selection rects into the OutputTexture struct so they can be drawn each frame
		// EDIT : calling this each frame.
		public void RefreshTextureSavedSelectionRects()
		{
			List<SelectionRectsFromModifier> srfms = new List<SelectionRectsFromModifier>();
			if (modifiers.arraySize > 0)
				for (int i = 0; i < modifiers.arraySize; i++)
				{
					SerializedProperty mod = modifiers.GetArrayElementAtIndex(i);
					if ((mod.FindPropertyRelative("modifierType").enumValueIndex == (int)ShotModifierType.OnlySomeBullets) && mod.FindPropertyRelative("enabled").boolValue && !mod.FindPropertyRelative("isEditingSelection").boolValue && mod.FindPropertyRelative("selectionRectsVisible").boolValue)
					{
						SelectionRectsFromModifier srfm = new SelectionRectsFromModifier();
						srfm.color = mod.FindPropertyRelative("selectionColor").colorValue;
						SerializedProperty selectionRects = mod.FindPropertyRelative("selectionRects");
						srfm.selectionRects = new Rect[selectionRects.arraySize];
						if (selectionRects.arraySize > 0)
							for (int j=0; j<selectionRects.arraySize; j++)
								srfm.selectionRects[j] = selectionRects.GetArrayElementAtIndex(j).rectValue;

						srfms.Add(srfm);
					}
				}

			// using raw values is much lighter and we don't need the Undo here
			sp.outputTexture.selectionRectsFromExistingMods = srfms.ToArray();
			//EditorUtility.SetDirty(sp);
		}

		// Is a certain bullet in a certain selection rect? Called upon refreshing selector modifier indexes.
		public bool IsInRectangle(BulletSpawn bs, Rect rect)
		{
			float halfIngameDist = sp.outputTexture.ingameDistance * 0.5f;

			Vector3 o = bs.bulletOrigin;
			float px = Mathf.InverseLerp(-halfIngameDist, halfIngameDist, o.x);
			float py = 1 - Mathf.InverseLerp(-halfIngameDist, halfIngameDist, o.y);

			bool result = (px > rect.xMin) && (px < rect.xMax) && (py > rect.yMin) && (py < rect.yMax);

			return result;
		}

		#endregion
	}
}