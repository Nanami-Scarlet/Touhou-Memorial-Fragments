using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BulletPro;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
    // This class handles everything related to drawing fields for DynamicBullet, DynamicShot and DynamicPattern objects.
    public class BulletHierarchyFieldHandler
    {
		int indexOfFieldBeingRenamed, indexOfFieldBeingClicked, currentParamFieldIndex;
		Rect renamingRect;
        SerializedProperty propertyBeingClicked;
		string nameOfPropertyBeingClicked;
		UnityEngine.Object targetOfPropertyBeingClicked;
		bool focusText;
		int indexOfSelectedSubAsset;
		EmissionParams oldValueOfField;

		public EmitterProfileInspector profileInspector;
        public SerializedObject serializedObject;
        public EditorWindow ownerWindow;
        public EmissionParamsInspector ownerInspector;

        // constructor if used in window
        public BulletHierarchyFieldHandler(EmitterProfileInspector profileInsp, SerializedObject serObj, EditorWindow owner)
        {
            indexOfFieldBeingRenamed = -1;
			renamingRect = new Rect(0,0,0,0);
            serializedObject = serObj;
            profileInspector = profileInsp;
            ownerWindow = owner;
            ownerInspector = null;
        }

        // constructor if used in inspector
        public BulletHierarchyFieldHandler(EmitterProfileInspector profileInsp, SerializedObject serObj, EmissionParamsInspector owner)
        {
            indexOfFieldBeingRenamed = -1;
			renamingRect = new Rect(0,0,0,0);
            serializedObject = serObj;
            profileInspector = profileInsp;
            ownerInspector = owner;
            ownerWindow = null;
        }

        public void OnInspectorBeginning()
        {
            // handle renaming
			if (indexOfFieldBeingRenamed > -1)
			{
				Event e = Event.current;
				if (e.type == EventType.KeyDown)
				{
					if (e.keyCode == KeyCode.Tab || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.KeypadEnter)
					{
						indexOfFieldBeingRenamed = -1;
						if (ownerWindow) ownerWindow.Repaint();
						if (ownerInspector) ownerInspector.Repaint();
					}
				}
				else if (e.type == EventType.MouseDown)
					if (!renamingRect.Contains(e.mousePosition))
					{
						indexOfFieldBeingRenamed = -1;
						if (ownerWindow) ownerWindow.Repaint();
						if (ownerInspector) ownerInspector.Repaint();
					}
			}
			

			currentParamFieldIndex = 0;
        }
        
        #region Non-dynamic ParamField methods

		public void LayoutParamField<T>(GUIContent label, SerializedProperty property, float ratio=0.5f, GUIStyle style=null) where T : EmissionParams
		{
			Rect rect = EditorGUILayout.GetControlRect(false);

			ParamField<T>(rect, label, property, ratio, style);
		}

		public void ParamField<T>(Rect rect, GUIContent label, SerializedProperty property, float ratio=0.5f, GUIStyle style=null) where T : EmissionParams
		{
			// necessary check due to a bug
			if (rect.width < 10) return;

			currentParamFieldIndex++;

			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			// widths, tweakable
			float space1, labelWidth, nameWidth;

			// constants and helpers
			float startX = rect.x + oldIndent * 16;
			float usableWidth = rect.width - (oldIndent * 16);
			float curX = startX;
            bool emptyLabel = string.IsNullOrEmpty(label.text);

			// initializing condition-dependant tweakable stuff
			if (emptyLabel)
			{
				labelWidth = space1 = 0;
				nameWidth = usableWidth;
			}
			else
			{
				space1 = 10;
				float remaining = usableWidth - space1;
				labelWidth = remaining * (1-ratio);
				nameWidth = remaining * ratio;
			}

			// rects and styles
			Rect labelRect = new Rect(curX, rect.y, labelWidth, rect.height); curX += labelWidth + space1;
			Rect nameRect = new Rect(curX, rect.y, nameWidth, rect.height); curX += nameWidth;
			GUIStyle paramFieldStyle = new GUIStyle((GUIStyle)"TextFieldDropDownText");
			//paramFieldStyle.fontStyle = FontStyle.Italic;

			bool renaming = currentParamFieldIndex == indexOfFieldBeingRenamed;
			if (renaming && !emptyLabel) label.text += " (name)";
			
			// label
			if (!emptyLabel)
			{
				if (style == null) EditorGUI.LabelField(labelRect, label);
				else EditorGUI.LabelField(labelRect, label, style);
			}
			
			// renaming the field
			if (renaming)
			{
				Undo.RecordObject(property.objectReferenceValue, "Renamed Emission Params");
				GUI.SetNextControlName("EmissionParamName");
				property.objectReferenceValue.name = EditorGUI.TextField(nameRect, property.objectReferenceValue.name);
				// too bad the rename callback doesn't support struct arguments, so we do it here.
				renamingRect = nameRect;
				if (focusText)
				{
					EditorGUI.FocusTextInControl("EmissionParamName");
					focusText = false;
				}
			}
			// usual functional field
			else
			{
				string valName = property.objectReferenceValue == null ? "- None (Click to set)" : property.objectReferenceValue.name;
				EditorGUI.LabelField(nameRect, valName, paramFieldStyle);
				Color defC = GUI.color;
				Color filledColor = new Color(0.6f, 1f, 0.7f, 1f);
				Color emptyColor = new Color(1.0f, 0.9f, 0.6f, 1f);
				GUI.color = property.objectReferenceValue == null ? emptyColor : filledColor;
				if (GUI.Button(nameRect, valName, paramFieldStyle))
				{
					indexOfFieldBeingClicked = currentParamFieldIndex;
					propertyBeingClicked = property;
					// Since 2020.2.6, property must be manually retrieved:
					targetOfPropertyBeingClicked = property.serializedObject.targetObject;
					nameOfPropertyBeingClicked = property.propertyPath;

					oldValueOfField = property.objectReferenceValue as EmissionParams;
					ContextMenuForParams<T>();
				}
				GUI.color = defC;
			}
			
			EditorGUI.indentLevel = oldIndent;
		}

		#endregion

		#region Dynamic ParamField methods

		public void LayoutDynamicParamField<T>(GUIContent label, SerializedProperty dynProperty, int valueIndex=1, float ratio=0.5f, GUIStyle style=null) where T : EmissionParams
		{
			Rect rect = EditorGUILayout.GetControlRect(false);

			DynamicParamField<T>(rect, label, dynProperty, valueIndex, ratio, style);
		}

		public void DynamicParamField<T>(Rect rect, GUIContent label, SerializedProperty dynProperty, int valueIndex=1, float ratio=0.5f, GUIStyle style=null) where T : EmissionParams
		{
			// necessary check due to a bug
			if (rect.width < 10) return;

			currentParamFieldIndex++;

			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			// widths, tweakable
			float space1, labelWidth, nameWidth;
            float btnSpace = 5;
			float extendButtonWidth = 21;

			// constants and helpers
			float startX = rect.x + oldIndent * 16;
			float usableWidth = rect.width - (oldIndent * 16 + btnSpace + extendButtonWidth);
			float curX = startX;
            bool emptyLabel = string.IsNullOrEmpty(label.text);

			// initializing condition-dependant tweakable stuff
			if (emptyLabel)
			{
				labelWidth = space1 = 0;
				nameWidth = usableWidth;
			}
			else
			{
				space1 = 10;
				float remaining = usableWidth - space1;
				labelWidth = remaining * (1-ratio);
				nameWidth = remaining * ratio;
			}

			// rects and styles
			Rect labelRect = new Rect(curX, rect.y, labelWidth, rect.height); curX += labelWidth + space1;
			Rect nameRect = new Rect(curX, rect.y, nameWidth, rect.height); curX += nameWidth + btnSpace;
			Rect btnRect = new Rect(curX, rect.y, extendButtonWidth, 16); curX += extendButtonWidth;
			GUIStyle paramFieldStyle = new GUIStyle((GUIStyle)"TextFieldDropDownText");
			//paramFieldStyle.fontStyle = FontStyle.Italic;

			bool renaming = currentParamFieldIndex == indexOfFieldBeingRenamed;
			if (renaming && !emptyLabel) label.text += " (name)";
			
			// label
			if (!emptyLabel)
			{
				if (style == null) EditorGUI.LabelField(labelRect, label);
				else EditorGUI.LabelField(labelRect, label, style);
			}

			// blend : list of values
			SerializedProperty valueProp = dynProperty.FindPropertyRelative("valueTree").GetArrayElementAtIndex(valueIndex);
			SerializedProperty valueType = valueProp.FindPropertyRelative("settings").FindPropertyRelative("valueType");
			if (valueType.enumValueIndex == (int)DynamicParameterSorting.Blend)
			{
				EditorGUI.LabelField(nameRect, "Multiple possible values. Click for more.");
			}
			// fixed : drawing a field
			else
			{
				SerializedProperty fixedValue = valueProp.FindPropertyRelative("defaultValue");
				// renaming the field
				if (renaming)
				{
					Undo.RecordObject(fixedValue.objectReferenceValue, "Renamed Emission Params");
					GUI.SetNextControlName("EmissionParamName");
					fixedValue.objectReferenceValue.name = EditorGUI.TextField(nameRect, fixedValue.objectReferenceValue.name);
					// too bad the rename callback doesn't support struct arguments, so we do it here.
					renamingRect = nameRect;
					if (focusText)
					{
						EditorGUI.FocusTextInControl("EmissionParamName");
						focusText = false;
					}
				}
				// usual functional field
				else
				{
					string valName = fixedValue.objectReferenceValue == null ? "- None (Click to set)" : fixedValue.objectReferenceValue.name;
					EditorGUI.LabelField(nameRect, valName, paramFieldStyle);
					Color oldC = GUI.color;
					Color filledColor = new Color(0.6f, 1f, 0.7f, 1f);
					Color emptyColor = new Color(1.0f, 0.9f, 0.6f, 1f);
					GUI.color = fixedValue.objectReferenceValue == null ? emptyColor : filledColor;
					if (GUI.Button(nameRect, valName, paramFieldStyle))
					{
						indexOfFieldBeingClicked = currentParamFieldIndex;
						propertyBeingClicked = fixedValue;
						// Since 2020.2.6, property must be manually retrieved:
						targetOfPropertyBeingClicked = fixedValue.serializedObject.targetObject;
						nameOfPropertyBeingClicked = fixedValue.propertyPath;
						oldValueOfField = fixedValue.objectReferenceValue as EmissionParams;
						ContextMenuForParams<T>();
					}
					GUI.color = oldC;
				}
			}

			// "extend" button
			Color defC = GUI.color;
            if (valueType.enumValueIndex > 0) GUI.color = Color.green;
            GUIContent dynamicButtonGC = new GUIContent("...", "Make this parameter Dynamic: randomize it or have its value be based on various parameters.");
            if (GUI.Button(btnRect, dynamicButtonGC, EditorStyles.miniButton))
            {
                GUI.color = defC;
                DynamicParameterWindow dpw = EditorWindow.GetWindow(typeof(DynamicParameterWindow)) as DynamicParameterWindow;
                System.Type t = typeof(T);
				if (t == typeof(BulletParams)) dpw.LoadProperty(dynProperty.serializedObject, dynProperty, valueIndex, BulletHierarchyObject.Bullet, profileInspector);
				else if (t == typeof(ShotParams)) dpw.LoadProperty(dynProperty.serializedObject, dynProperty, valueIndex, BulletHierarchyObject.Shot, profileInspector);
				else if (t == typeof(PatternParams)) dpw.LoadProperty(dynProperty.serializedObject, dynProperty, valueIndex, BulletHierarchyObject.Pattern, profileInspector);
            }
            GUI.color = defC;
			
			EditorGUI.indentLevel = oldIndent;
		}

		#endregion		

		#region ParamField toolbox

		public void ContextMenuForParams<T>() where T : EmissionParams
		{
			GenericMenu gm = new GenericMenu();
			
			string path = oldValueOfField == null ? "" : "Set Value/";

			// Setup available param list
			gm.AddItem(new GUIContent(path+"Create new"), false, ReplaceByNew);
			if (oldValueOfField != null)
			{
				gm.AddSeparator("Set Value/");
				gm.AddItem(new GUIContent("Set Value/Set to \"None\""), false, SetToNull);
			}
			if (profileInspector.HasElementsInHierarchy<T>(oldValueOfField as T))
			{
				gm.AddSeparator(path);
				gm.AddDisabledItem(new GUIContent(path+"Copy from Bullet Hierarchy :"));

				for (int i = 0; i < profileInspector.subAssets.arraySize; i++)
				{
					Object ep = profileInspector.subAssets.GetArrayElementAtIndex(i).objectReferenceValue;
					if ((ep as EmissionParams).isInRecycleBin) continue;
					if (!(ep is T)) continue;
					//if (ep == oldValueOfField) continue;
					gm.AddItem(new GUIContent(path+ep.name), false, ReplaceByCloneFromHierarchy, ep);
				}
			}
			if (profileInspector.HasElementsInRecycleBin<T>(oldValueOfField as T))
			{
				gm.AddSeparator(path);
				gm.AddDisabledItem(new GUIContent(path+"Copy from Recycle Bin :"));
				for (int i = 0; i < profileInspector.subAssets.arraySize; i++)
				{
					Object ep = profileInspector.subAssets.GetArrayElementAtIndex(i).objectReferenceValue;
					if (!((ep as EmissionParams).isInRecycleBin)) continue;
					if (!(ep is T)) continue;
					if (ep == oldValueOfField) continue;
					gm.AddItem(new GUIContent(path+ep.name), false, ReplaceByCloneFromHierarchy, ep);
				}
			}

			if (oldValueOfField != null)
			{
				gm.AddItem(new GUIContent("Inspect"), false, SelectParams);
				gm.AddItem(new GUIContent("Rename"), false, StartRenaming);
			}

			gm.ShowAsContext();
		}

		public void SelectParams()
		{
			profileInspector.SelectElement(oldValueOfField);
		}

		public void StartRenaming()
		{
			indexOfFieldBeingRenamed = indexOfFieldBeingClicked;
			focusText = true;
		}

		public void ReplaceByNew()
		{
			SetToNull();
			FillWithNew();
			StartRenaming();
		}

		// Called first for any option. Sets current property to null then breaks the child-parent link
		public void SetToNull()
		{
			// only treat non-empty fields
			if (oldValueOfField == null) return;

			// sets parent property to null and isInRecycleBin to true
			SetParent(oldValueOfField, null);
			
			// Since 2020.2.6, serializedObject gets reset after an import...
            #if UNITY_2020_2_OR_NEWER
			serializedObject = new SerializedObject(targetOfPropertyBeingClicked);
			propertyBeingClicked = serializedObject.FindProperty(nameOfPropertyBeingClicked);
            #endif
			
			propertyBeingClicked.objectReferenceValue = null;
			
            serializedObject.ApplyModifiedProperties();
			profileInspector.serializedObject.ApplyModifiedProperties();
		}

		// Creates a new asset to fill an empty field (previously emptied with SetToNull)
		public void FillWithNew()
		{
			EmissionParams ep = serializedObject.targetObject as EmissionParams;
			EmissionParams newChild = null;
			if (ep is BulletParams) newChild = profileInspector.AddNewParams<PatternParams>(ep, false, true);
			if (ep is ShotParams) newChild = profileInspector.AddNewParams<BulletParams>(ep, false, true);
			if (ep is PatternParams) newChild = profileInspector.AddNewParams<ShotParams>(ep, false, true);

			// Since 2020.2.6, serializedObject gets reset after an import...
            #if UNITY_2020_2_OR_NEWER
			serializedObject = new SerializedObject(targetOfPropertyBeingClicked);
			propertyBeingClicked = serializedObject.FindProperty(nameOfPropertyBeingClicked);
            #endif
			
			propertyBeingClicked.objectReferenceValue = newChild;
			newChild.isInRecycleBin = ep.isInRecycleBin;

			// update list of children
			SerializedProperty childrenProp = serializedObject.FindProperty("children");
			childrenProp.arraySize++;
			childrenProp.GetArrayElementAtIndex(childrenProp.arraySize-1).objectReferenceValue = newChild;

			serializedObject.ApplyModifiedProperties();
			profileInspector.serializedObject.ApplyModifiedProperties();
		}

		// Copies an object from the hierarchy to fill the slot with it.
		public void ReplaceByCloneFromHierarchy(object newValue)
		{
			SetToNull();

			EmissionParams newEp = newValue as EmissionParams;
			EmissionParams ep = serializedObject.targetObject as EmissionParams;

			EmissionParams newClone = DuplicateSubAsset(newEp);
			profileInspector.serializedObject.ApplyModifiedProperties();

			SetParent(newClone, ep);
			
			// Since 2020.2.6, serializedObject gets reset after an import...
            #if UNITY_2020_2_OR_NEWER
			serializedObject = new SerializedObject(targetOfPropertyBeingClicked);
			propertyBeingClicked = serializedObject.FindProperty(nameOfPropertyBeingClicked);
            #endif
			propertyBeingClicked.objectReferenceValue = newClone;
			
			serializedObject.ApplyModifiedProperties();

			StartRenaming();
		}

		// No longer in use. Brings an object from the recycle bin to fill this slot.
		public void ReplaceByRecycled(object newValue)
		{
			SetToNull();

			EmissionParams newEp = newValue as EmissionParams;
			EmissionParams ep = serializedObject.targetObject as EmissionParams;
			
			SetParent(newEp, ep);
			
			#if UNITY_2020_2_OR_NEWER
			serializedObject = new SerializedObject(targetOfPropertyBeingClicked);
			propertyBeingClicked = serializedObject.FindProperty(nameOfPropertyBeingClicked);
            #endif
			propertyBeingClicked.objectReferenceValue = newEp;

			serializedObject.ApplyModifiedProperties();
			profileInspector.serializedObject.ApplyModifiedProperties();
		}

		// Duplicate function (from source to this) that preserves hierarchy.
        EmissionParams DuplicateSubAsset(EmissionParams ep, bool isRoot=true)
        {
            // create a perfect clone
            EmissionParams newEp = Object.Instantiate(ep) as EmissionParams;
            newEp.hideFlags = HideFlags.HideInHierarchy;
			EmitterProfile profile = ep.profile;
			newEp.name = EmitterProfileUtility.MakeUniqueName(ep.name, profile);			
			EmitterProfileUtility.RidNewObjectOfUnusedReferences(newEp);
            Undo.RegisterCreatedObjectUndo(newEp, "Edit Emission Profile");

            // manage assets and sub-assets
            AssetDatabase.AddObjectToAsset(newEp, profile);
            string path = AssetDatabase.GetAssetPath(newEp);
            AssetDatabase.ImportAsset(path);
            
            // manage hierarchy
            newEp.profile = profile;
            newEp.isInRecycleBin = true;

			// Since 2020.2.6, serializedObject gets reset after an import...
            #if UNITY_2020_2_OR_NEWER
            profileInspector.subAssets = profileInspector.serializedObject.FindProperty("subAssets");
            #endif
            profileInspector.subAssets.arraySize++;
			profile.numberOfSubAssets++;
            profileInspector.subAssets.GetArrayElementAtIndex(profileInspector.subAssets.arraySize-1).objectReferenceValue = newEp;

            // recursion, then recreating missing links based on type
            if (newEp.children != null)
                if (newEp.children.Length > 0)
                    for (int i = 0; i < newEp.children.Length; i++)
                    {
                        EmissionParams child = DuplicateSubAsset(newEp.children[i], false);
                        
                        EmitterProfileUtility.ReplaceChild(newEp, i, child);
						//newEp.ReplaceChild(i, child);
                    }

            if (!isRoot) return newEp;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return newEp;
        }

		#endregion

        #region utility functions (hierarchy management) - copied from EmissionParamsInspector

		// Sets parent of "elem" to "newParent", updates the recycle bin state along the way.
        public void SetParent(EmissionParams elem, EmissionParams newParent)
        {
			if (elem == null) return;

            // detach object (and its children) from its parent
            if (elem.parent)
            {
                // delete the ".children" reference
                SerializedObject oldParentSO = new SerializedObject(elem.parent);
				// Since 2020.2.6, serializedObject gets reset after an import...
				#if UNITY_2020_2_OR_NEWER
				serializedObject = new SerializedObject(targetOfPropertyBeingClicked);
				#endif
				// avoid conflict with existing instance
				if (elem.parent == serializedObject.targetObject)
					oldParentSO = serializedObject;

				oldParentSO.Update();
                SerializedProperty childrenProp = oldParentSO.FindProperty("children");
                int indexOfChild = -1;
				if (childrenProp.arraySize > 0)
					for (int i = 0; i < childrenProp.arraySize; i++)
					{
						if (childrenProp.GetArrayElementAtIndex(i).objectReferenceValue != elem)
							continue;
						indexOfChild = i;
						break;
					}
                if (indexOfChild > -1)
                {
					childrenProp.DeleteArrayElementAtIndex(indexOfChild); // first time sets it to null
					#if !UNITY_2021_1_OR_NEWER
                    childrenProp.DeleteArrayElementAtIndex(indexOfChild); // second time empties it
					#endif
					oldParentSO.ApplyModifiedProperties();
                }
            }

			// update the ".parent" reference
			SerializedObject so = new SerializedObject(elem);
			so.Update();
			so.FindProperty("parent").objectReferenceValue = newParent;
			so.ApplyModifiedProperties();

			// update children array and recycle bin state
			if (newParent != null)
			{
				SerializedObject newParentSO = new SerializedObject(newParent);
				// Since 2020.2.6, serializedObject gets reset after an import...
				#if UNITY_2020_2_OR_NEWER
				serializedObject = new SerializedObject(targetOfPropertyBeingClicked);
				#endif
				if (newParent == serializedObject.targetObject)
					newParentSO = serializedObject;
				newParentSO.Update();
				SerializedProperty childrenProp = newParentSO.FindProperty("children");
				childrenProp.arraySize++;
				childrenProp.GetArrayElementAtIndex(childrenProp.arraySize-1).objectReferenceValue = elem;
				newParentSO.ApplyModifiedProperties();

				MarkAsRecycleBinElementWithChildren(elem, newParent.isInRecycleBin);
			}

            // if no parent, mark orphan and its children as part of the bin
            else MarkAsRecycleBinElementWithChildren(elem, true);
        }

        // Recursively marks a property and all its children as elements of either bullet hierarchy or recycle bin
        void MarkAsRecycleBinElementWithChildren(EmissionParams ep, bool isInBin)
        {
            SerializedObject so = new SerializedObject(ep);
            so.Update();
            so.FindProperty("isInRecycleBin").boolValue = isInBin;
            so.ApplyModifiedProperties();

            if (ep.children != null)
                if (ep.children.Length != 0)
                    for (int i = 0; i < ep.children.Length; i++)
                        MarkAsRecycleBinElementWithChildren(ep.children[i], isInBin);
        }

		#endregion
    }
}