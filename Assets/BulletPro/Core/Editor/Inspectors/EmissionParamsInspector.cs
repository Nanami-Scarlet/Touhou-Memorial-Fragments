using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	// The inspectors for BulletParams, ShotParams and PatternParams derive from this.
	// Also used a utility class for drawing EmissionParamsFields.
	public class EmissionParamsInspector : Editor
	{
		// Properties used/cached for ParamField

		public EmitterProfileInspector profileInspector;
		
		public BulletHierarchyFieldHandler fieldHandler;

		#region inherited stuff

		public virtual void OnEnable()
		{
			// avoids a bug related to leftover invisible instances of editor 
			if (target == null)
			{
				OnUnselected();
				DestroyImmediate(this);
				return;
			}

			fieldHandler = new BulletHierarchyFieldHandler(profileInspector, serializedObject, this);
		}

		public virtual void OnDisable()
		{
			
		}

		// in inheriting scripts, OnUnselected() will be called by OnDisable and also upon changing selected item in EmissionProfile.
		public virtual void OnUnselected()
		{
			/* *
			// Closes all dynamic parameter windows
			DynamicParameterWindow[] dpws = Resources.FindObjectsOfTypeAll(typeof(DynamicParameterWindow)) as DynamicParameterWindow[];
				if (dpws != null)
					if (dpws.Length != 0)
						for (int i = 0; i < dpws.Length; i++)
							dpws[i].Close();
			/* */
		}

        public override bool UseDefaultMargins() { return false; }
		
		// Beginning of all OnInspectorGUI() functions
		public override void OnInspectorGUI()
		{
			// resetting it here because OnEnable is called before profileInspector initialization
			if (fieldHandler.profileInspector == null)
				fieldHandler = new BulletHierarchyFieldHandler(profileInspector, serializedObject, this);
				
            fieldHandler.OnInspectorBeginning();

			serializedObject.Update();


			// debug
			/* *
			EditorGUILayout.LabelField("Debug : ",EditorStyles.boldLabel);
			SerializedProperty childrenProp = serializedObject.FindProperty("children");
			SerializedProperty parentProp = serializedObject.FindProperty("parent");
			if (parentProp.objectReferenceValue != null)
				EditorGUILayout.LabelField("This has a parent : "+parentProp.objectReferenceValue.name);
			else EditorGUILayout.LabelField("This has no parent.");
			string plural = childrenProp.arraySize > 1 ? "ren.":".";
			EditorGUILayout.LabelField("This has "+childrenProp.arraySize.ToString()+" child"+plural);
			if (childrenProp.arraySize > 0)
				for (int i = 0; i < childrenProp.arraySize; i++)
				{
					SerializedProperty p = childrenProp.GetArrayElementAtIndex(i);
					string str = "null";
					if (p.objectReferenceValue != null) str = p.objectReferenceValue.name;
					EditorGUILayout.LabelField(str);
				}

			/* */
		}

		#endregion

		#region utility functions (hierarchy management)

		// TODO : remove this region and have every call to SetParent made from the fieldHandler

		// Sets parent of "elem" to "newParent", updates the recycle bin state along the way.
        public void SetParent(EmissionParams elem, EmissionParams newParent)
        {
			if (elem == null) return;

            // detach object (and its children) from its parent
            if (elem.parent)
            {
                // delete the ".children" reference
                SerializedObject oldParentSO = new SerializedObject(elem.parent);
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