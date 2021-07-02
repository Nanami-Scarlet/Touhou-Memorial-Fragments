using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
    [CustomEditor(typeof(EmitterProfile))]
    public class EmitterProfileInspector : Editor
    {
        #region properties

        // tweakable
        Color proSeparatorColor, nonProSeparatorColor;
        float leftSideWidth, hierarchyIndent, rightMargin, headerHeight, postHelpSpace, leftTabSectionGap;
        Color selectedHierarchyColor;
        Vector2 minSize;
        string helpBubble;
    
        // cache
        Vector2 scrollPosTop, scrollPosBottom, scrollPosRight;
        GUILayoutOption mw;
        GUIStyle selectedHierarchyStyle, unselectedHierarchyStyle, headerStyle;
        public SerializedProperty subAssets;
        SerializedProperty rootBullet, hasBeenInitialized, displayHelpHierarchy, displayHelpRecycleBin, displayHelpImport, currentParamsSelected;
        SerializedProperty compactMode, foldoutImport, foldoutExport, foldoutEmptyBin, foldoutCompactMode;
        EmitterProfile profile, sourceAsset, destinationAsset;
        Editor currentParamInspector;
        Texture foldoutArrowDown, foldoutArrowRight, foldoutArrowDownLight, foldoutArrowRightLight;
        bool hasCalledPostOnEnable, importMsgDisplay, exportMsgDisplay;
        string importMsg, exportMsg;
        List<EmissionParams> displayedParams;
        public DynamicParameterWindow dynamicWindow; // keeping a reference allows repainting it when needed

        // cache for renaming and context menu
        Rect renamingRect;
		bool focusText;
        int indexOfFieldBeingRenamed;
        EmissionParams toRenameNextFrame;
        bool rightClicked; // cached to allow skipping one frame and letting the editor repaint on right click
        bool shouldRecalcChildren;

        #endregion

        #region startup

        void OnEnable()
        {
            // avoids a bug related to leftover invisible instances of editor 
			if (target == null)
			{
				DestroyImmediate(this);
				return;
			}

            // tweakable values
            proSeparatorColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            nonProSeparatorColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            minSize = new Vector2(325, 100);
            leftSideWidth = 190;
            hierarchyIndent = 15;
            rightMargin = 10;
            headerHeight = 26;
            postHelpSpace = 12;
            leftTabSectionGap = 48;
            if (EditorGUIUtility.isProSkin) selectedHierarchyColor = new Color(0f, 0.4f, 0.6f, 1f);
            else selectedHierarchyColor = new Color(0.2f, 0.5f, 0.9f, 1f);
            helpBubble = "Click here to toggle help.";

            // caching stuff
            profile = target as EmitterProfile;
            rootBullet = serializedObject.FindProperty("rootBullet");
            subAssets = serializedObject.FindProperty("subAssets");
            hasBeenInitialized = serializedObject.FindProperty("hasBeenInitialized");
            displayHelpHierarchy = serializedObject.FindProperty("displayHelpHierarchy");
            displayHelpRecycleBin = serializedObject.FindProperty("displayHelpRecycleBin");
            displayHelpImport = serializedObject.FindProperty("displayHelpImport");
            currentParamsSelected = serializedObject.FindProperty("currentParamsSelected");

            foldoutImport = serializedObject.FindProperty("foldoutImport");
            foldoutExport = serializedObject.FindProperty("foldoutExport");
            foldoutEmptyBin = serializedObject.FindProperty("foldoutEmptyBin");
            foldoutCompactMode = serializedObject.FindProperty("foldoutCompactMode");
            compactMode = serializedObject.FindProperty("compactMode");
            
            // caching hierarchy skin style
            displayedParams = new List<EmissionParams>();
            mw = GUILayout.MaxWidth(leftSideWidth);
            foldoutArrowDown = Resources.Load<Texture>("BP_DownArrow");
            foldoutArrowDownLight = Resources.Load<Texture>("BP_DownArrowLight");
            foldoutArrowRight = Resources.Load<Texture>("BP_RightArrow");
            foldoutArrowRightLight = Resources.Load<Texture>("BP_RightArrowLight");

            // renaming and context
            rightClicked = false;
            indexOfFieldBeingRenamed = -1;
			renamingRect = new Rect(0,0,0,0);

            // Undo handling
            Undo.undoRedoPerformed += OnUndoRedo; 

            // renewing random indexes upon selection
            profile.SetUniqueIndexes();
        }

        // Called after OnEnable (during OnGUI) to avoid some crashes while fetching the inspector
        void PostOnEnable()
        {
            hasCalledPostOnEnable = true;

            // set inspector min size
            EditorWindow curFocused = EditorWindow.focusedWindow;
            
            /* */
            System.Type inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            EditorWindow insp = EditorWindow.GetWindow(inspectorType);
            insp.minSize = new Vector2(minSize.x, insp.minSize.y);
            /* */

            if (curFocused) curFocused.Focus();

            // GUIStyles
            selectedHierarchyStyle = new GUIStyle();
            selectedHierarchyStyle.normal.background = Monochrome(selectedHierarchyColor);
            selectedHierarchyStyle.normal.textColor = Color.white;
            selectedHierarchyStyle.wordWrap = true;

            unselectedHierarchyStyle = new GUIStyle();
            unselectedHierarchyStyle.wordWrap = true;
            if (EditorGUIUtility.isProSkin)
            {
                float textLum = 0.9f;
                unselectedHierarchyStyle.normal.textColor = new Color(textLum, textLum, textLum, 1f);
            }

            #if UNITY_2018_3_OR_NEWER
            GUIStyle headerSource = (GUIStyle)"DD HeaderStyle";
            headerStyle = new GUIStyle(headerSource);
            #else
            GUIStyle headerSource = EditorStyles.boldLabel;
            headerStyle = new GUIStyle(headerSource);
            headerStyle.alignment = TextAnchor.MiddleCenter;
            float grayscale = 0.95f;
            headerStyle.normal.background = Monochrome(new Color(grayscale, grayscale, grayscale, 1f));
            headerStyle.normal.textColor = Color.black;
            #endif
        }

        void OnDisable()
        {
            if (currentParamInspector != null)
            {
                EmissionParamsInspector epi = currentParamInspector as EmissionParamsInspector;
                epi.OnUnselected();

                DestroyImmediate(epi);
            }

            // restore inspector min size
            EditorWindow curFocused = EditorWindow.focusedWindow;
            System.Type inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            EditorWindow insp = EditorWindow.GetWindow(inspectorType);
            //insp.minSize = oldMinSize; // can cause problems
            insp.minSize = new Vector2(250, insp.minSize.y);
            if(curFocused) curFocused.Focus();

            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        // Called for validating asset creation, deletion, and selection change.
        void OnUndoRedo()
        {
            if (currentParamInspector != null)
            {
                EmissionParamsInspector epi = currentParamInspector as EmissionParamsInspector;
                epi.OnUnselected();
            }
            currentParamInspector = null;
            
            // Calling SaveAssets only when really needed
            if (profile.numberOfSubAssets != subAssets.arraySize)
            {
                profile.numberOfSubAssets = subAssets.arraySize;
                AssetDatabase.SaveAssets();
            }
        }

        // on the very first serialization : create the basics of bullet hierarchy.
        void Initialize()
        {
            // wait until it exists
            if (!EditorUtility.IsPersistent(profile)) return;

            serializedObject.Update();

            hasBeenInitialized.boolValue = true;
            
            /* *
            displayHelpHierarchy.boolValue = true;
            displayHelpRecycleBin.boolValue = true;
            displayHelpExport.boolValue = true;
            /* */

            BulletParams root = AddNewParams<BulletParams>(null, true);
            root.name = "Root Emitter";
            root.isVisible = false;
            root.canCollide = false;
            root.canMove = false;
            root.hasLifespan = false;
            root.isChildOfEmitter = new DynamicBool(true);
            root.customParameters = new DynamicCustomParameter[0];
            rootBullet.objectReferenceValue = root;
            
            PatternParams pattern = AddNewParams<PatternParams>(root, true);
            pattern.name = "Pattern";
            if (root.patternsShot == null) root.patternsShot = new DynamicPattern[1];
            if (root.patternsShot.Length == 0) root.patternsShot = new DynamicPattern[1];
            root.patternsShot[0] = new DynamicPattern(pattern);
            root.children = new EmissionParams[] { pattern };
            
            ShotParams shot = AddNewParams<ShotParams>(pattern, true);
            shot.name = "Shot";
            pattern.instructionLists[0].instructions[1].shot = new DynamicShot(shot);
            pattern.children = new EmissionParams[] { shot };
            
            BulletParams bullet = AddNewParams<BulletParams>(shot, true);
            bullet.name = "Bullet";
            shot.bulletParams = new DynamicBullet(bullet);
            shot.children = new EmissionParams[] { bullet };            

            bullet.children = new EmissionParams[0];

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        public override bool UseDefaultMargins() { return false; }

        #endregion

        public override void OnInspectorGUI()
        {
            #region preparative stuff, actually GUI-unrelated

            // handle renaming
			if (indexOfFieldBeingRenamed > -1)
			{
				Event e = Event.current;
				if (e.type == EventType.KeyDown)
				{
					if (e.keyCode == KeyCode.Tab || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.KeypadEnter)
					{
						indexOfFieldBeingRenamed = -1;
						Repaint();
					}
				}
				else if (e.type == EventType.MouseDown)
					if (!renamingRect.Contains(e.mousePosition))
					{
						indexOfFieldBeingRenamed = -1;
						Repaint();
					}
			}

            // handle context click
            if (rightClicked)
            {
                rightClicked = false;
                RightClick(currentParamsSelected.objectReferenceValue as EmissionParams);
            }

            serializedObject.Update();

            // is called here instead of in OnEnable() because the asset doesn't exist at the moment
            if (!hasBeenInitialized.boolValue) Initialize();
            // if still waiting for initialization, don't even display the usual inspector
            if (!hasBeenInitialized.boolValue)
            {
                EditorGUILayout.HelpBox("New asset is being prepared...", MessageType.Info);
                return;
            }

            if (EditorApplication.isCompiling) hasCalledPostOnEnable = false;
            if (!hasCalledPostOnEnable && !EditorApplication.isCompiling) PostOnEnable();

            // recalc children after user has tried to undo a deletion
            if (shouldRecalcChildren)
            {
                shouldRecalcChildren = false;
                if (subAssets.arraySize > 0)
                    for (int i = 0; i < subAssets.arraySize; i++)
                    {
                        SerializedProperty elem = subAssets.GetArrayElementAtIndex(i);
                        EmissionParams elemEP = elem.objectReferenceValue as EmissionParams;
                        if (elemEP == null) continue;
                        if (elemEP.children == null) continue;
                        if (elemEP.children.Length == 0) continue;
                        Queue<int> emptyChildrenIndexes = new Queue<int>();
                        for (int j = 0; j < elemEP.children.Length; j++)
                            if (elemEP.children[j] == null)
                                emptyChildrenIndexes.Enqueue(j);
                        
                        if (emptyChildrenIndexes.Count > 0)
                        {
                            SerializedObject so = new SerializedObject(elemEP);
                            SerializedProperty childrenProp = so.FindProperty("children");
                            int iterations = 0;
                            while (emptyChildrenIndexes.Count > 0)
                            {
                                int k = emptyChildrenIndexes.Dequeue();
                                k -= iterations;
                                iterations++;
                                childrenProp.DeleteArrayElementAtIndex(k);
                            }
                            so.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
            }

            #endregion

            // constantly keep track of which objects are displayed, in what order (for keyboard controls)
            displayedParams.Clear();

            EditorGUILayout.BeginHorizontal();

            #region left (toolbar)

            if (!compactMode.boolValue)
            {

                #region Bullet Hierarchy

                // "Bullet hierarchy" header
                GUILayout.Space(-4); // get rid of margin next to header
                EditorGUILayout.BeginVertical(mw);
                
                EditorGUILayout.LabelField("Bullet Hierarchy", headerStyle, GUILayout.MaxWidth(leftSideWidth), GUILayout.Height(headerHeight));
                
                // Bullet hierarchy : help
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(4); // restore things where they were
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginVertical();
                string hierarchyHelp = 
                "Bullet Hierarchy works as follows:\n"+
                "A Pattern contains Shots.\n"+
                "A Shot contains Bullets.\n"+
                "A Bullet can contain Patterns.";
                string usedStr = displayHelpHierarchy.boolValue ? hierarchyHelp : helpBubble;
                if (GUILayout.Button(usedStr, EditorStyles.helpBox, GUILayout.MaxWidth(leftSideWidth-8)))
                    displayHelpHierarchy.boolValue = !displayHelpHierarchy.boolValue;
                GUILayout.Space(postHelpSpace);
                EditorGUILayout.EndVertical();

                // scroll view
                Rect vRect = EditorGUILayout.BeginVertical(mw);
                float maxWidthNeeded = GetMaxWidthNeededForHierarchy(false);
                bool needsScrollView = maxWidthNeeded > leftSideWidth;
                if (needsScrollView)
                {
                    maxWidthNeeded += 16;
                    Rect totalView = new Rect(vRect.x, vRect.y, maxWidthNeeded, vRect.height-leftTabSectionGap);
                    scrollPosTop = GUI.BeginScrollView(vRect, scrollPosTop, totalView);
                }
                DisplayHierarchy(profile.rootBullet, mw, 0, maxWidthNeeded);
                if (needsScrollView)
                    GUI.EndScrollView();
                GUILayout.Space(leftTabSectionGap);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                #endregion

                #region Recycle Bin

                // "Recycle bin" header
                EditorGUILayout.LabelField("Recycle Bin", headerStyle, GUILayout.MaxWidth(leftSideWidth), GUILayout.Height(headerHeight));

                // Recycle bin : help
                EditorGUILayout.BeginHorizontal();
                bool emptyBin = !HasElementsInRecycleBin<EmissionParams>();
                GUILayout.Space(emptyBin?8:4); // cf its counterpart in bullet hierarchy, but behaves differently when not displaying a list
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginVertical();
                string recycleBinHelp = 
                "All removed elements go here.\n"+
                "You can discard them for good.\n"+
                "But they can still be useful before!\n"+
                "Parts of them can be copypasted.";
                usedStr = displayHelpRecycleBin.boolValue ? recycleBinHelp : helpBubble;
                if (GUILayout.Button(usedStr, EditorStyles.helpBox, GUILayout.MaxWidth(leftSideWidth-8)))
                    displayHelpRecycleBin.boolValue = !displayHelpRecycleBin.boolValue;
                GUILayout.Space(postHelpSpace);
                EditorGUILayout.EndVertical();

                // scroll view
                Rect vRect2 = EditorGUILayout.BeginVertical(mw);
                if (emptyBin) EditorGUILayout.LabelField("Recycle Bin is empty for now.", mw);
                else
                {
                    maxWidthNeeded = GetMaxWidthNeededForHierarchy(true);
                    needsScrollView = maxWidthNeeded > leftSideWidth;
                    if (needsScrollView)
                    {
                        maxWidthNeeded += 16;
                        Rect totalView = new Rect(vRect2.x, vRect2.y, maxWidthNeeded, vRect2.height-leftTabSectionGap);
                        scrollPosBottom = GUI.BeginScrollView(vRect2, scrollPosBottom, totalView);
                    }
                    for (int i = 0; i < subAssets.arraySize; i++)
                    {
                        EmissionParams recycledElem = subAssets.GetArrayElementAtIndex(i).objectReferenceValue as EmissionParams;
                        if (recycledElem == null) continue;
                        if (recycledElem.parent != null) continue;
                        if (!recycledElem.isInRecycleBin) continue;
                        DisplayHierarchy(recycledElem, mw, 0, maxWidthNeeded);
                    }
                    if (needsScrollView)
                        GUI.EndScrollView();
                }
                GUILayout.Space(leftTabSectionGap);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                #endregion

                #region import

                // "Advanced Settings" header
                EditorGUILayout.LabelField("Advanced Settings", headerStyle, GUILayout.MaxWidth(leftSideWidth), GUILayout.Height(headerHeight));
                float indent = 18;
                float extraRightMargin = 4;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8); // cf its counterpart in bullet hierarchy
                string importHelp = 
                "Import/Export elements from one Emitter Profile to another. These will be stored in the Recycle Bin.";
                usedStr = displayHelpImport.boolValue ? importHelp : helpBubble;
                if (GUILayout.Button(usedStr, EditorStyles.helpBox, GUILayout.MaxWidth(leftSideWidth-8)))
                    displayHelpImport.boolValue = !displayHelpImport.boolValue;
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(postHelpSpace);

                // Import from other profile
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(18); // room for the arrow
                foldoutImport.boolValue = EditorGUILayout.Foldout(foldoutImport.boolValue, "Import from other profile", true);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(indent); // cf its counterpart in bullet hierarchy
                EditorGUILayout.BeginVertical();
                if (foldoutImport.boolValue)
                {
                    EditorGUILayout.LabelField("Select Source Asset :", EditorStyles.miniLabel, GUILayout.MaxWidth(leftSideWidth-indent-extraRightMargin));
                    EditorGUI.BeginChangeCheck();
                    sourceAsset = EditorGUILayout.ObjectField(GUIContent.none, sourceAsset, typeof(EmitterProfile), false, GUILayout.MaxWidth(leftSideWidth-indent-extraRightMargin)) as EmitterProfile;
                    if (EditorGUI.EndChangeCheck()) importMsgDisplay = false;
                    EditorGUI.BeginDisabledGroup(sourceAsset == null);
                    if (GUILayout.Button("Import copy to Recycle Bin", EditorStyles.miniButton, GUILayout.MaxWidth(leftSideWidth-indent-extraRightMargin)))
                    {
                        importMsgDisplay = true;
                        importMsg = ImportFromSource();

                        sourceAsset = null;
                    }
                    if (importMsgDisplay) EditorGUILayout.LabelField(importMsg, EditorStyles.miniLabel, GUILayout.MaxWidth(leftSideWidth-indent-extraRightMargin));
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Space(16);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                #endregion

                #region export

                // Export to other profile
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(18); // room for the arrow
                foldoutExport.boolValue = EditorGUILayout.Foldout(foldoutExport.boolValue, "Export to other profile", true);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(indent); // cf its counterpart in bullet hierarchy
                EditorGUILayout.BeginVertical();
                if (foldoutExport.boolValue)
                {
                    EditorGUILayout.LabelField("Select Destination Asset :", EditorStyles.miniLabel, GUILayout.MaxWidth(leftSideWidth-indent-extraRightMargin));
                    EditorGUI.BeginChangeCheck();
                    destinationAsset = EditorGUILayout.ObjectField(GUIContent.none, destinationAsset, typeof(EmitterProfile), false, GUILayout.MaxWidth(leftSideWidth-indent-extraRightMargin)) as EmitterProfile;
                    if (EditorGUI.EndChangeCheck()) exportMsgDisplay = false;
                    EditorGUI.BeginDisabledGroup(destinationAsset == null);
                    if (GUILayout.Button("Export copy to Recycle Bin", EditorStyles.miniButton, GUILayout.MaxWidth(leftSideWidth-indent-extraRightMargin)))
                    {
                        exportMsgDisplay = true;
                        exportMsg = ExportToDestination();

                        destinationAsset = null;
                    }
                    if (exportMsgDisplay) EditorGUILayout.LabelField(exportMsg, EditorStyles.miniLabel, mw);
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Space(16);
                }
                
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                #endregion

                #region empty recycle bin

                // Empty recycle bin
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(18); // room for the arrow
                foldoutEmptyBin.boolValue = EditorGUILayout.Foldout(foldoutEmptyBin.boolValue, "Empty the Recycle Bin", true);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(indent); // cf its counterpart in bullet hierarchy
                EditorGUILayout.BeginVertical();
                if (foldoutEmptyBin.boolValue)
                {
                    EditorGUILayout.LabelField("Are you sure ? No undo.", EditorStyles.miniLabel, GUILayout.MaxWidth(leftSideWidth-indent-extraRightMargin));
                    EditorGUI.BeginDisabledGroup(emptyBin);
                    string btnStr = emptyBin ? "Recycle Bin already empty" : "Empty the Recycle Bin";
                    if (GUILayout.Button(btnStr, EditorStyles.miniButton, GUILayout.MaxWidth(leftSideWidth-indent-extraRightMargin)))
                    {
                        if (EditorUtility.DisplayDialog("Empty the Recycle Bin ?", "Do you really want to empty the Recycle Bin ?\n\nThis cannot be undone.", "Delete", "Cancel"))
                            EmptyRecycleBin();
                        return;
                    }
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Space(16);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                #endregion

                #region compact mode and version update

                // Enter compact mode
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(18); // room for the arrow
                foldoutCompactMode.boolValue = EditorGUILayout.Foldout(foldoutCompactMode.boolValue, "Hide this sidebar", true);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(indent); // cf its counterpart in bullet hierarchy
                EditorGUILayout.BeginVertical();
                if (foldoutCompactMode.boolValue)
                {
                    EditorGUILayout.LabelField("Click here or press Ctrl+Space.", EditorStyles.miniLabel, GUILayout.MaxWidth(leftSideWidth-indent-extraRightMargin));
                    string btnStr = "Enter Compact Mode";
                    if (GUILayout.Button(btnStr, EditorStyles.miniButton, GUILayout.MaxWidth(leftSideWidth-indent-extraRightMargin)))
                    {
                        compactMode.boolValue = true;
                    }
                }

                if (EditorGUIUtility.currentViewWidth < 473)
                {
                    GUILayout.Space(8);
                    Color defC = GUI.color;
                    GUI.color = new Color(1f, 1f, 0.7f, 1f);
                    EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(160));
                    GUI.color = defC;
                    EditorGUILayout.HelpBox("Given the current width of this window, hiding the sidebar is recommended.", MessageType.Info);
                    EditorGUILayout.EndVertical();
                }

                if (profile.buildNumber < BulletProSettings.buildNumber)
                {
                    GUILayout.Space(8);
                    Color defC = GUI.color;
                    GUI.color = new Color(1f, 1f, 0.7f, 1f);
                    EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(160));
                    EditorGUILayout.HelpBox("Since BulletPro's last version, you need to update this asset.\nGo to Tools > BulletPro > Update Assets, or click the button below.\n\nThis may take a few seconds.", MessageType.Warning);
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(8);
                    if (GUILayout.Button("Update Assets", EditorStyles.miniButton, GUILayout.MaxWidth(160)))
                        EmitterProfileUtility.UpdateAllProfiles();
                    GUI.color = defC;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                #endregion

                // Close all groups
                GUILayout.Space(32);

                EditorGUILayout.EndVertical();
            }

            #endregion

            #region right

            GUILayout.Space(-8); // get rid of margin next to header, 4+4 from the previous column

            float emissionParamWidth = EditorGUIUtility.currentViewWidth;
            if (!compactMode.boolValue) emissionParamWidth -= leftSideWidth;
            else emissionParamWidth += 8; // makes up for the separator
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(emissionParamWidth));
            
            if (compactMode.boolValue)
            {
                EditorGUILayout.BeginVertical("box");
                string compactMsg = "You are in Compact Mode - sidebar is hidden.\nClick here or press Ctrl+Space to restore the sidebar.";
                EditorGUILayout.HelpBox(compactMsg, MessageType.Info);
                Color defC = GUI.color;
                GUI.color = new Color(1, 1, 0.7f, 1);
                if (GUILayout.Button("Exit Compact Mode", EditorStyles.miniButton))
                    compactMode.boolValue = false;
                GUI.color = defC;
                EditorGUILayout.EndVertical();
            }

            if (profile.buildNumber < BulletProSettings.buildNumber)
            {
                GUILayout.Space(8);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(12);
                EditorGUILayout.BeginVertical();
                Color defC = GUI.color;
                GUI.color = new Color(1f, 1f, 0.7f, 1f);
                EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(emissionParamWidth));
                EditorGUILayout.HelpBox("Since BulletPro's last version, you need to update this asset before editing it.\nGo to Tools > BulletPro > Update Assets, or click the button below.\n\nThis may take a few seconds.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                GUILayout.Space(8);
                if (GUILayout.Button("Update Assets", EditorStyles.miniButton, GUILayout.MaxWidth(emissionParamWidth)))
                    EmitterProfileUtility.UpdateAllProfiles();
                GUI.color = defC;
                EditorGUILayout.EndVertical();
                GUILayout.Space(8);
                EditorGUILayout.EndHorizontal();
            }
            else if (currentParamsSelected.objectReferenceValue == null)
            {
                GUILayout.Space(8);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(12);
                EditorGUILayout.BeginVertical();
                EditorGUILayout.HelpBox("Welcome!\nClick on an object from the Bullet Hierarchy to start editing it.", MessageType.Info);
                if (EditorGUIUtility.currentViewWidth < minSize.x)
                    EditorGUILayout.HelpBox("It is recommended to make your inspector a bit wider, until this message disappears.", MessageType.Info);
                EditorGUILayout.EndVertical();
                GUILayout.Space(8);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField(currentParamsSelected.objectReferenceValue.name, headerStyle, GUILayout.Height(headerHeight));
                
                EditorGUILayout.BeginHorizontal();
                // restore things where they were (4) and draw Params inspector a bit more to the right (16)
                if (!compactMode.boolValue) GUILayout.Space(4+16);
                EditorGUILayout.BeginVertical(GUILayout.MaxWidth(emissionParamWidth));
                GUILayout.Space(16);
                if (!currentParamInspector)
                {
                    Editor.CreateCachedEditor(currentParamsSelected.objectReferenceValue, null, ref currentParamInspector);
                    (currentParamInspector as EmissionParamsInspector).profileInspector = this;
                }
                EditorGUILayout.BeginHorizontal();
                if (compactMode.boolValue) GUILayout.Space(16);
                EditorGUILayout.BeginVertical();
                currentParamInspector.OnInspectorGUI();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.Space(rightMargin);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();

            #endregion

            EditorGUILayout.EndHorizontal();

            if (currentParamsSelected.objectReferenceValue != null)
                KeyboardControls();

            serializedObject.ApplyModifiedProperties();

            // separation line, drawn at the end to override everything
            if (compactMode.boolValue) return;
            float sepLineStart = 50;
            #if UNITY_2019_1_OR_NEWER
            sepLineStart = 0;
            #endif
            Rect sepLine = new Rect(leftSideWidth, sepLineStart, 4, 10000);
            EditorGUI.DrawRect(sepLine, EditorGUIUtility.isProSkin ? proSeparatorColor : nonProSeparatorColor);
        }

        #region bullet hierarchy display and navigation

        // What's the longest element to draw in hierarchy, or in recycle bin ?
        float GetMaxWidthNeededForHierarchy(bool recycleBin)
        {
            if (subAssets.arraySize == 0) return 0;

            float result = 0;
            for (int i = 0; i < subAssets.arraySize; i++)
            {
                EmissionParams elem = subAssets.GetArrayElementAtIndex(i).objectReferenceValue as EmissionParams;
                if (elem.parent) continue;
                if (elem.isInRecycleBin != recycleBin) continue;
                result = Mathf.Max(result, CheckLongestNameField(elem, 0));
            }

            return result;
        }

        float CheckLongestNameField(EmissionParams elem, int depth)
        {
            if (elem == null) return 0;
            float result = (depth+1) * hierarchyIndent + EditorStyles.label.CalcSize(new GUIContent(elem.name)).x;
            if (elem.children != null)
                if (elem.children.Length > 0)
                    for (int i = 0; i < elem.children.Length; i++)
                        result = Mathf.Max(result, CheckLongestNameField(elem.children[i], depth+1));

            return result;
        }

        void DisplayHierarchy(EmissionParams elem, GUILayoutOption mw, float depth, float maxWidthNeeded)
        {
            // Can be a leftover from object deletion
            if (elem == null)
            {
                shouldRecalcChildren = true;
                return;
            }

            GUIStyle usedStyle = currentParamsSelected.objectReferenceValue == elem ? selectedHierarchyStyle : unselectedHierarchyStyle;
            Rect rect = EditorGUILayout.GetControlRect(false, hierarchyIndent+1, usedStyle, mw);
            float curX = rect.x;
            usedStyle.wordWrap = false;

            // calculating rects, while recreating wordwrap
            Rect indentZone = new Rect(curX, rect.y, depth * hierarchyIndent, rect.height);
            curX += indentZone.width;
            indentZone.xMax += 2; // avoiding leftover blank spaces
            Rect arrowZone = new Rect(curX, rect.y, hierarchyIndent, rect.height);
            curX += arrowZone.width;
            float nameWidth = Mathf.Max(rect.width, maxWidthNeeded)-(indentZone.width+arrowZone.width);
            nameWidth += 2; // reporting those two units from indentZone
            Rect nameZone = new Rect(curX, rect.y, nameWidth, rect.height);

            bool noChildren = false;
            if (elem.children == null) noChildren = true;
            else if (elem.children.Length == 0) noChildren = true;

            // update index for keyboard navigation and renaming
            elem.index = displayedParams.Count;
            displayedParams.Add(elem);
            if (toRenameNextFrame == elem)
            {
                indexOfFieldBeingRenamed = elem.index;
                toRenameNextFrame = null;
                focusText = true;
                Repaint();
            }

            // make the left of the arrow clickable (indentZone)
            if (indexOfFieldBeingRenamed != elem.index)
                if (GUI.Button(indentZone, "", usedStyle))
                {
                    SelectElement(elem);
                    if (Event.current.button == 1)
                        rightClicked = true;
                }

            // draw the foldout arrow, or an empty space if the elem has no children (arrowZone)
            if (noChildren)
            {
                if (indexOfFieldBeingRenamed != elem.index)
                    if (GUI.Button(arrowZone, "", usedStyle))
                    {
                        SelectElement(elem);
                        if (Event.current.button == 1)
                            rightClicked = true;
                    }
            }
            else
            {
                Texture toUse = EditorGUIUtility.isProSkin ?
                    (elem.foldout ? foldoutArrowDownLight : foldoutArrowRightLight) :
                    (elem.foldout ? foldoutArrowDown : foldoutArrowRight);
                if (GUI.Button(arrowZone, toUse, usedStyle))
                {
                    if (indexOfFieldBeingRenamed != elem.index)
                    {
                        string str = elem.foldout ? "Fold ":"Unfold ";
                        Undo.RecordObject(elem, str+elem.name);
                        elem.foldout = !elem.foldout;
                    }
                }
            }

            // draw element's name (nameZone)
            if (indexOfFieldBeingRenamed == elem.index)
            {
                if (rect.width > 10) // this check because a bug with GetControlRect still duplicates it
                    renamingRect = nameZone;
                Undo.RecordObject(elem, "Renamed Emission Params");
                string uniqueName = "RenameField"+EditorApplication.timeSinceStartup.ToString();
                //GUI.SetNextControlName(uniqueName);
                // focusing text is temporarily out due to it auto-remembering it the last selection and setting it as initial name
                elem.name = EditorGUI.TextField(nameZone, elem.name);
                if (focusText)
                {
                    EditorGUI.FocusTextInControl(uniqueName);
                    focusText = false;
                }
            }
            else if (GUI.Button(nameZone, elem.name, usedStyle))
            {
                SelectElement(elem);
                if (Event.current.button == 1)
                    rightClicked = true;                    
            }

            // Recursion if unfolded
            if (!noChildren)
                if (elem.foldout)
                    for (int i = 0; i < elem.children.Length; i++)
                        DisplayHierarchy(elem.children[i], mw, depth+1, maxWidthNeeded);
        }  
        
        void KeyboardControls()
        {
            if (currentParamsSelected.objectReferenceValue == null)
                return;

            if (Event.current.type != EventType.KeyDown) return;
            KeyCode kc = Event.current.keyCode;

            if (kc == KeyCode.LeftArrow) SelectionLeft();
            else if (kc == KeyCode.RightArrow) SelectionRight();
            else if (kc == KeyCode.UpArrow) SelectionUp();
            else if (kc == KeyCode.DownArrow) SelectionDown();
            else if (kc == KeyCode.F2) StartRenaming();
            else if (kc == KeyCode.Delete || kc == KeyCode.Backspace) DeleteKey();
            else if (kc == KeyCode.Space && (Event.current.control || Event.current.command))
                compactMode.boolValue = !compactMode.boolValue;            
        }

        void RightClick(EmissionParams elem)
        {
            if (elem is BulletParams) SetupGenericMenu<BulletParams>(elem as BulletParams);
            else if (elem is ShotParams) SetupGenericMenu<ShotParams>(elem as ShotParams);
            else if (elem is PatternParams) SetupGenericMenu<PatternParams>(elem as PatternParams);
        }

        void StartRenaming()
        {
            indexOfFieldBeingRenamed = (currentParamsSelected.objectReferenceValue as EmissionParams).index;
            focusText = true;
            Repaint();
        }

        void DeleteKey()
        {
            EmissionParams elem = currentParamsSelected.objectReferenceValue as EmissionParams;
            if (elem == rootBullet.objectReferenceValue) return;

            if (elem.isInRecycleBin) DestroyFromBin();
            else if (elem.parent) SetToNull();

            Repaint();
        }

        // Collapses selected item or, if impossible, selects parent
        void SelectionLeft()
        {
            EmissionParams elem = currentParamsSelected.objectReferenceValue as EmissionParams;
            
            bool displaysChildren = true;
            if (elem.children == null) displaysChildren = false;
            else if (elem.children.Length == 0) displaysChildren = false;
            else if (!elem.foldout) displaysChildren = false;

            if (displaysChildren)
            {
                if (Event.current.alt) FoldAll(elem);
                else
                {
                    Undo.RecordObject(elem, "Fold "+elem.name);
                    elem.foldout = false;
                }
                Repaint();
            }
            else if (elem.parent != null)
                SelectElement(elem.parent);
        }

        // Expands selected item if it has children that can be displayed
        void SelectionRight()
        {
            EmissionParams elem = currentParamsSelected.objectReferenceValue as EmissionParams;
            
            if (elem.children == null) return;
            if (elem.children.Length == 0) return;
            if (elem.foldout) return;
            
            if (Event.current.alt) UnfoldAll(elem);
            else
            {
                Undo.RecordObject(elem, "Unfold "+elem.name);
                elem.foldout = true;
            }

            Repaint();
        }

        // Selects the object above : previous sibling, or parent, or last item of previous sub-hierarchy
        void SelectionUp()
        {
            EmissionParams elem = currentParamsSelected.objectReferenceValue as EmissionParams;
            
            if (elem.index == 0) return;
            SelectElement(displayedParams[elem.index-1]);
        }

        // Selects the object below : next sibling, or first child, or root of next sub-hierarchy
        void SelectionDown()
        {
            EmissionParams elem = currentParamsSelected.objectReferenceValue as EmissionParams;

            if (elem.index == displayedParams.Count-1) return;
            SelectElement(displayedParams[elem.index+1]);
        }

        void FoldAll(EmissionParams elem)
        {
            List<EmissionParams> toFold = new List<EmissionParams>();
            
            toFold = FillRecursively(toFold, elem);

            Undo.RecordObjects(toFold.ToArray(), "Fold "+elem.name);
            for (int i = 0; i < toFold.Count; i++)
                toFold[i].foldout = false;
        }

        void UnfoldAll(EmissionParams elem)
        {
            List<EmissionParams> toUnfold = new List<EmissionParams>();
            
            toUnfold = FillRecursively(toUnfold, elem);

            Undo.RecordObjects(toUnfold.ToArray(), "Unfold "+elem.name);
            for (int i = 0; i < toUnfold.Count; i++)
                toUnfold[i].foldout = true;
        }

        // Automatically called upon inspecting an object which may be hidden by its folded parents
        void UnfoldWithParents(EmissionParams elem)
        {
            List<EmissionParams> toUnfold = new List<EmissionParams>();
            toUnfold.Add(elem);
            EmissionParams cur = elem;
            while (cur.parent)
            {
                cur = cur.parent;
                toUnfold.Add(cur);
            }

            //Undo.RecordObjects(toUnfold.ToArray(), "Unfold "+elem.name);
            for (int i = 0; i < toUnfold.Count; i++)
                toUnfold[i].foldout = true;
        }

        List<EmissionParams> FillRecursively(List<EmissionParams> list, EmissionParams elem)
        {
            list.Add(elem);
            if (elem.children != null)
                if (elem.children.Length != 0)
                    for (int i = 0; i < elem.children.Length; i++)
                        list = FillRecursively(list, elem.children[i]);

            return list;
        }

        #endregion

        #region sub-asset creation / destruction management

        // Creates a new object as sub-asset
        public T AddNewParams<T>(EmissionParams parent, bool skipSavingAssets, bool allowUndo=false) where T : EmissionParams
        {
            // create it
            T newParams = ScriptableObject.CreateInstance<T>();
            if (allowUndo) Undo.RegisterCreatedObjectUndo(newParams, "Edit Emitter Profile");
            string typeName = typeof(T).Name;

            // for naming
            int numberOfSimilarObjects = 0;
            if (subAssets.arraySize > 0)
                for (int i = 0; i < subAssets.arraySize; i++)
                {
                    Object p = subAssets.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (p == rootBullet.objectReferenceValue) continue;
                    if (p == null) continue;
                    if (p is T) numberOfSimilarObjects++;
                }

            // initialize it
            newParams.name = "New "+typeName+" "+numberOfSimilarObjects.ToString();
            if (typeName == "BulletParams") newParams.parameterType = EmissionParamsType.Bullet;
            else if (typeName == "ShotParams") newParams.parameterType = EmissionParamsType.Shot;
            else if (typeName == "PatternParams") newParams.parameterType = EmissionParamsType.Pattern;
            newParams.parent = parent;
            if (parent != null)
                newParams.isInRecycleBin = parent.isInRecycleBin;
            newParams.profile = profile;
            newParams.foldout = true;
            newParams.hideFlags = HideFlags.HideInHierarchy;
            newParams.FirstInitialization();

            // manage assets and sub-assets
            AssetDatabase.AddObjectToAsset(newParams, profile);
            string path = AssetDatabase.GetAssetPath(newParams);
            if (!skipSavingAssets)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // Since 2020.2.6, serializedObject gets reset after an import...
            #if UNITY_2020_2_OR_NEWER
            subAssets = serializedObject.FindProperty("subAssets");
            #endif

            // update hierarchy.
            subAssets.arraySize++;
            profile.numberOfSubAssets++;
            subAssets.GetArrayElementAtIndex(subAssets.arraySize-1).objectReferenceValue = newParams;
            
            // we're done
            return newParams;
        }

        // Does this profile have options in hierarchy to replace a certain item ?
        public bool HasElementsInHierarchy<T>(T toReplace = null) where T : EmissionParams
        {
            if (subAssets.arraySize == 0) return false;

            for (int i = 0; i < subAssets.arraySize; i++)
            {
                SerializedProperty p = subAssets.GetArrayElementAtIndex(i);
                EmissionParams cur = p.objectReferenceValue as EmissionParams;
                if (cur.isInRecycleBin) continue;
                if (!(cur is T)) continue;
                //if (cur == toReplace) continue;

                return true;
            }

            return false;
        }

        // Does this profile have options in recycle bin to replace a certain item ?
        public bool HasElementsInRecycleBin<T>(T toReplace = null) where T : EmissionParams
        {
            if (subAssets.arraySize == 0) return false;

            for (int i = 0; i < subAssets.arraySize; i++)
            {
                SerializedProperty p = subAssets.GetArrayElementAtIndex(i);
                EmissionParams cur = p.objectReferenceValue as EmissionParams;
                if (!cur.isInRecycleBin) continue;
                if (!(cur is T)) continue;
                if (cur == toReplace) continue;

                return true;
            }

            return false;
        }

        #endregion
    
        #region import / export

        // Imports contents of sourceAsset to recycle bin. Returns log message.
        public string ImportFromSource()
        {
            if (sourceAsset == null) return "Error: Source asset is null.";
            if (sourceAsset == profile) return "Error: Cannot import from self.";

            bool validHierarchy = true;
            if (sourceAsset.subAssets == null) validHierarchy = false;
            else if (sourceAsset.subAssets.Length == 0) validHierarchy = false;
            
            if (!validHierarchy) return "Source asset is empty.";

            if (validHierarchy)
            {
                for (int i=0; i<sourceAsset.subAssets.Length; i++)
                    if (sourceAsset.subAssets[i] != null)
                        if (sourceAsset.subAssets[i].parent == null)
                            DuplicateToBinWithChildren(sourceAsset.subAssets[i]);
            }

            return "Successfully imported!";
        }

        // Actual copy function (from source to this) that preserves hierarchy.
        EmissionParams DuplicateToBinWithChildren(EmissionParams ep, bool isRoot=true)
        {
            // create a perfect clone
            EmissionParams newEp = Object.Instantiate(ep) as EmissionParams;
            newEp.hideFlags = HideFlags.HideInHierarchy;
            string newName = ep.name + " (From "+sourceAsset.name+")";
			newEp.name = EmitterProfileUtility.MakeUniqueName(newName, profile);
            EmitterProfileUtility.RidNewObjectOfUnusedReferences(newEp);
            Undo.RegisterCreatedObjectUndo(newEp, "Import Bullet Profile");

            // manage assets and sub-assets
            AssetDatabase.AddObjectToAsset(newEp, profile);
            string path = AssetDatabase.GetAssetPath(newEp);
            AssetDatabase.ImportAsset(path);
            
            // manage hierarchy
            newEp.profile = profile;
            newEp.isInRecycleBin = true;
            subAssets.arraySize++;
            profile.numberOfSubAssets++;
            subAssets.GetArrayElementAtIndex(subAssets.arraySize-1).objectReferenceValue = newEp;

            // recursion, then recreating missing links based on type
            if (newEp.children != null)
                if (newEp.children.Length > 0)
                    for (int i = 0; i < newEp.children.Length; i++)
                    {
                        EmissionParams child = DuplicateToBinWithChildren(newEp.children[i], false);
                        
                        EmitterProfileUtility.ReplaceChild(newEp, i, child);                        
                        //newEp.ReplaceChild(i, child);
                    }

            if (!isRoot) return newEp;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return newEp;
        }

        // Exports contents of recycle bin to destinationAsset. Returns log message.
        public string ExportToDestination()
        {
            if (destinationAsset == null) return "Error: Destination asset is null.";
            if (destinationAsset == profile) return "Error: Cannot export to self.";
            
            bool validHierarchy = true;
            if (profile.subAssets == null) validHierarchy = false;
            else if (profile.subAssets.Length == 0) validHierarchy = false;
            
            if (!validHierarchy) return "This asset is empty.";

            SerializedObject sObj = new SerializedObject(destinationAsset);
            sObj.Update();

            if (validHierarchy)
            {
                for (int i=0; i<profile.subAssets.Length; i++)
                    if (profile.subAssets[i] != null)
                        if (profile.subAssets[i].parent == null)
                            DuplicateToDestinationBinWithChildren(profile.subAssets[i], destinationAsset, sObj);
            }

            EditorGUIUtility.PingObject(destinationAsset);
            return "Successfully exported!";
        }

        // Actual copy function (from this to destination) that preserves hierarchy then serializes dest.
        EmissionParams DuplicateToDestinationBinWithChildren(EmissionParams ep, EmitterProfile dest, SerializedObject destSerObj, bool isRoot=true)
        {
            // create a perfect clone
            EmissionParams newEp = Object.Instantiate(ep) as EmissionParams;
            newEp.hideFlags = HideFlags.HideInHierarchy;
            string newName = ep.name + " (From "+profile.name+")";
			newEp.name = EmitterProfileUtility.MakeUniqueName(newName, profile);			
            EmitterProfileUtility.RidNewObjectOfUnusedReferences(newEp);
            Undo.RegisterCreatedObjectUndo(newEp, "Export Bullet Profile");

            // manage assets and sub-assets
            AssetDatabase.AddObjectToAsset(newEp, dest);
            string path = AssetDatabase.GetAssetPath(newEp);
            AssetDatabase.ImportAsset(path);
            
            // manage hierarchy
            newEp.profile = dest;
            newEp.isInRecycleBin = true;
            
            SerializedProperty destBin = destSerObj.FindProperty("subAssets");
            
            dest.numberOfSubAssets++;
            destBin.arraySize++;
            destBin.GetArrayElementAtIndex(destBin.arraySize-1).objectReferenceValue = newEp;

            // recursion, then recreating missing links based on type
            if (newEp.children != null)
                if (newEp.children.Length > 0)
                    for (int i = 0; i < newEp.children.Length; i++)
                    {
                        EmissionParams child = DuplicateToDestinationBinWithChildren(newEp.children[i], dest, destSerObj, false);
                        
                        EmitterProfileUtility.ReplaceChild(newEp, i, child);
                        //newEp.ReplaceChild(i, child);
                    }

            destSerObj.ApplyModifiedProperties();

            if (!isRoot) return newEp;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return newEp;
        }

        #endregion
        
        #region context menu

        void SetupGenericMenu<T>(T elem) where T : EmissionParams
        {
            GenericMenu gm = new GenericMenu();
            gm.AddItem(new GUIContent("Rename"), false, StartRenaming);

            if (elem.parent)
            {
    			gm.AddItem(new GUIContent("Replace/"+"Replace with new"), false, ReplaceByNew);
                if (HasElementsInHierarchy<T>(elem))
                {
                    gm.AddSeparator("Replace/");
                    gm.AddDisabledItem(new GUIContent("Replace/"+"Copy from Bullet Hierarchy :"));

                    for (int i = 0; i < subAssets.arraySize; i++)
                    {
                        Object ep = subAssets.GetArrayElementAtIndex(i).objectReferenceValue;
                        if ((ep as EmissionParams).isInRecycleBin) continue;
                        if (!(ep is T)) continue;
                        //if (ep == oldValueOfField) continue;
                        gm.AddItem(new GUIContent("Replace/"+ep.name), false, ReplaceByCloneFromHierarchy, ep);
                    }
                }

                if (HasElementsInRecycleBin<T>(elem))
                {
                    gm.AddSeparator("Replace/");
                    gm.AddDisabledItem(new GUIContent("Replace/"+"Copy from Recycle Bin :"));

                    for (int i = 0; i < subAssets.arraySize; i++)
                    {
                        Object ep = subAssets.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (!((ep as EmissionParams).isInRecycleBin)) continue;
                        if (!(ep is T)) continue;
                        //if (ep == oldValueOfField) continue;
                        gm.AddItem(new GUIContent("Replace/"+ep.name), false, ReplaceByCloneFromHierarchy, ep);
                    }
                }

                string str = elem.isInRecycleBin ? "Detach" : "Remove";
                gm.AddItem(new GUIContent(str), false, SetToNull);
            }

            if (elem.isInRecycleBin)
                gm.AddItem(new GUIContent("Destroy"), false, DestroyFromBin);
            

            gm.ShowAsContext();
        }

        void SetToNull()
        {
            EmissionParams elem = currentParamsSelected.objectReferenceValue as EmissionParams;
			ReplaceChildUsingSerializedObjects(elem.parent, elem, null);            
        }

        void ReplaceByNew()
        {
            EmissionParams elem = currentParamsSelected.objectReferenceValue as EmissionParams;
            EmissionParams newChild = null;
			if (elem is BulletParams) newChild = AddNewParams<BulletParams>(null, false, true);
			if (elem is ShotParams) newChild = AddNewParams<ShotParams>(null, false, true);
			if (elem is PatternParams) newChild = AddNewParams<PatternParams>(null, false, true);

			ReplaceChildUsingSerializedObjects(elem.parent, elem, newChild);
			
			serializedObject.ApplyModifiedProperties();
            SelectElement(newChild);
            serializedObject.ApplyModifiedProperties();
            toRenameNextFrame = newChild;
        }

        // Permanently destroys selected object from the recycle bin.
        void DestroyFromBin()
        {
            EmissionParams elem = currentParamsSelected.objectReferenceValue as EmissionParams;
			if (elem == null) return;

            if (!EditorUtility.DisplayDialog("Delete element from Recycle Bin ?", "Delete "+elem.name+" from Recycle Bin ?\n\nThis action cannot be undone.", "Delete", "Cancel")) return;

            if (elem.parent)
                ReplaceChildUsingSerializedObjects(elem.parent, elem, null, false);            
            
            List<EmissionParams> toDestroy = new List<EmissionParams>();
            toDestroy = FillRecursively(toDestroy, elem);
            while (toDestroy.Count > 0)
            {
                EmissionParams objectToDestroy = toDestroy[0];
                toDestroy.RemoveAt(0);
                for (int i = 0; i < subAssets.arraySize; i++)
                {
                    SerializedProperty refAsSubAsset = subAssets.GetArrayElementAtIndex(i);
                    if (refAsSubAsset.objectReferenceValue != objectToDestroy) continue;
                    
                    refAsSubAsset.objectReferenceValue = null;
                    subAssets.DeleteArrayElementAtIndex(i);
                    profile.numberOfSubAssets--;
                    break;
                }
                DestroyImmediate(objectToDestroy, true);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            string path = AssetDatabase.GetAssetPath(profile);
            AssetDatabase.ImportAsset(path);
        }

        // Replaces selected element with obj, by creating a clone of obj. Previously selected element goes to recycle bin.
        void ReplaceByCloneFromHierarchy(object obj)
        {
            EmissionParams elem = currentParamsSelected.objectReferenceValue as EmissionParams;
            EmissionParams newChild = DuplicateSubAsset(obj as EmissionParams, true);
            ReplaceChildUsingSerializedObjects(elem.parent, elem, newChild);

            serializedObject.ApplyModifiedProperties();
            SelectElement(newChild);
            serializedObject.ApplyModifiedProperties();
            toRenameNextFrame = newChild;
        }

        // No longer in use. Replaces selected element with obj, by removing obj from the recycle bin.
        void ReplaceByRecycled(object obj)
        {
            EmissionParams elem = currentParamsSelected.objectReferenceValue as EmissionParams;
            ReplaceChildUsingSerializedObjects(elem.parent, elem, obj as EmissionParams);
        }

        // Used in context menus only. Old child is sent to recycle bin.
        void ReplaceChildUsingSerializedObjects(EmissionParams parentItem, EmissionParams oldChild, EmissionParams newChild, bool allowUndo=true)
        {
            SerializedObject parentSO = new SerializedObject(parentItem);
            SerializedObject oldChildSO = new SerializedObject(oldChild);
            SerializedObject newChildSO = null;
            if (newChild) newChildSO = new SerializedObject(newChild);

            // update recycle bin flag for new child, if any
            List<EmissionParams> eps = new List<EmissionParams>();
            if (newChild)
            {
                eps = FillRecursively(eps, newChild);
                newChildSO.FindProperty("isInRecycleBin").boolValue = parentSO.FindProperty("isInRecycleBin").boolValue;
                if (eps.Count > 1)
                    for (int i = 1; i < eps.Count; i++)
                    {
                        SerializedObject so = new SerializedObject(eps[i]);
                        so.FindProperty("isInRecycleBin").boolValue = parentSO.FindProperty("isInRecycleBin").boolValue;
                        if (allowUndo) so.ApplyModifiedProperties();
                        else so.ApplyModifiedPropertiesWithoutUndo();
                    }
            }

            // send old child to recycle bin
            if (!oldChild.isInRecycleBin)
            {
                eps.Clear();
                eps.TrimExcess();
                eps = FillRecursively(eps, oldChild);
                oldChildSO.FindProperty("isInRecycleBin").boolValue = true;
                if (eps.Count > 1)
                    for (int i = 1; i < eps.Count; i++)
                    {
                        SerializedObject so = new SerializedObject(eps[i]);
                        so.FindProperty("isInRecycleBin").boolValue = true;
                        if (allowUndo) so.ApplyModifiedProperties();
                        else so.ApplyModifiedPropertiesWithoutUndo();
                    }
            }
            

            // update properties : we browse dynamic objects, and an indexInDynamic greater than -1 means the oldChild is part of said object

            if (parentItem is ShotParams)
            {
                ShotParams sp = parentItem as ShotParams;

                // main bullet reference
                int indexInDynamic = EmitterProfileUtility.LocateObjectIndexInDynamicBullet(sp.bulletParams, oldChild as BulletParams);
                if (indexInDynamic > -1)
                {
                    SerializedProperty dynParamProp = parentSO.FindProperty("bulletParams");
                    SerializedProperty valueTree = dynParamProp.FindPropertyRelative("valueTree");
                    SerializedProperty valueToReplace = valueTree.GetArrayElementAtIndex(indexInDynamic);
                    valueToReplace.FindPropertyRelative("defaultValue").objectReferenceValue = newChild;

                    // if parent has to be set to null and we're in a blend, remove the line from the list of blends
                    if (newChild == null)
                    {
                        int indexToRemove = DynamicParameterUtility.GetPositionOfBlendIndex(dynParamProp, indexInDynamic);
                        if (indexToRemove > -1)
                        {
                            int indexOfParent = valueToReplace.FindPropertyRelative("settings").FindPropertyRelative("indexOfParent").intValue;
                            valueTree.GetArrayElementAtIndex(indexOfParent).FindPropertyRelative("settings").FindPropertyRelative("indexOfBlendedChildren").DeleteArrayElementAtIndex(indexToRemove);
                        }
                    }
                }

                indexInDynamic = -1;

                // browsing "set bullet" modifiers
                if (sp.modifiers != null)
                    if (sp.modifiers.Count != 0)
                        for (int j = 0; j < sp.modifiers.Count; j++)
                            if (sp.modifiers[j].modifierType == ShotModifierType.SetBulletParams)
                            {
                                indexInDynamic = EmitterProfileUtility.LocateObjectIndexInDynamicBullet(sp.modifiers[j].bulletParams, oldChild as BulletParams);
                                if (indexInDynamic > -1)
                                {
                                    SerializedProperty dynParamProp = parentSO.FindProperty("modifiers").GetArrayElementAtIndex(j).FindPropertyRelative("bulletParams");
                                    SerializedProperty valueTree = dynParamProp.FindPropertyRelative("valueTree");
                                    SerializedProperty valueToReplace = valueTree.GetArrayElementAtIndex(indexInDynamic);
                                    valueToReplace.FindPropertyRelative("defaultValue").objectReferenceValue = newChild;

                                    // if parent has to be set to null and we're in a blend, remove the line from the list of blends
                                    if (newChild == null)
                                    {
                                        int indexToRemove = DynamicParameterUtility.GetPositionOfBlendIndex(dynParamProp, indexInDynamic);
                                        if (indexToRemove > -1)
                                        {
                                            int indexOfParent = valueToReplace.FindPropertyRelative("settings").FindPropertyRelative("indexOfParent").intValue;
                                            valueTree.GetArrayElementAtIndex(indexOfParent).FindPropertyRelative("settings").FindPropertyRelative("indexOfBlendedChildren").DeleteArrayElementAtIndex(indexToRemove);
                                        }
                                    }
                                }
                            }

                // browsing bullet spawns' alternative bullet style. Non dynamic.
                if (sp.bulletSpawns != null)
                    if (sp.bulletSpawns.Length != 0)
                        for (int j = 0; j < sp.bulletSpawns.Length; j++)
                            if (sp.bulletSpawns[j].bulletParams == oldChild)
                                parentSO.FindProperty("bulletSpawns").GetArrayElementAtIndex(j).FindPropertyRelative("bulletParams").objectReferenceValue = newChild;
            }

            else if (parentItem is BulletParams)
            {
                BulletParams bp = parentItem as BulletParams;
                
                // browsing patterns fired by a bullet
                if (bp.patternsShot != null)
                    if (bp.patternsShot.Length != 0)
                        for (int j = 0; j < bp.patternsShot.Length; j++)
                        {
                            int indexInDynamic = EmitterProfileUtility.LocateObjectIndexInDynamicPattern(bp.patternsShot[j], oldChild as PatternParams);
                            if (indexInDynamic > -1)
                            {
                                SerializedProperty dynParamProp = parentSO.FindProperty("patternsShot").GetArrayElementAtIndex(j);
                                SerializedProperty valueTree = dynParamProp.FindPropertyRelative("valueTree");
                                SerializedProperty valueToReplace = valueTree.GetArrayElementAtIndex(indexInDynamic);
                                valueToReplace.FindPropertyRelative("defaultValue").objectReferenceValue = newChild;

                                // if parent has to be set to null and we're in a blend, remove the line from the list of blends
                                if (newChild == null)
                                {
                                    int indexToRemove = DynamicParameterUtility.GetPositionOfBlendIndex(dynParamProp, indexInDynamic);
                                    if (indexToRemove > -1)
                                    {
                                        int indexOfParent = valueToReplace.FindPropertyRelative("settings").FindPropertyRelative("indexOfParent").intValue;
                                        valueTree.GetArrayElementAtIndex(indexOfParent).FindPropertyRelative("settings").FindPropertyRelative("indexOfBlendedChildren").DeleteArrayElementAtIndex(indexToRemove);
                                    }
                                }
                            }
                        }
            }

            else if (parentItem is PatternParams)
            {
                PatternParams pp = parentItem as PatternParams;

                // browsing "shoot" instructions from patterns
                if (pp.instructionLists != null)
                    if (pp.instructionLists.Length > 0)
                        for (int j = 0; j < pp.instructionLists.Length; j++)
                        {
                            PatternInstruction[] instructions = pp.instructionLists[j].instructions;
                            if (instructions != null)
                                if (instructions.Length > 0)
                                    for (int k = 0; k < instructions.Length; k++)
                                    {
                                        int indexInDynamic = EmitterProfileUtility.LocateObjectIndexInDynamicShot(instructions[k].shot, oldChild as ShotParams);
                                        if (indexInDynamic > -1)
                                        {
                                            SerializedProperty dynParamProp = parentSO.FindProperty("instructionLists").GetArrayElementAtIndex(j).FindPropertyRelative("instructions").GetArrayElementAtIndex(k).FindPropertyRelative("shot");
                                            SerializedProperty valueTree = dynParamProp.FindPropertyRelative("valueTree");
                                            SerializedProperty valueToReplace = valueTree.GetArrayElementAtIndex(indexInDynamic);
                                            valueToReplace.FindPropertyRelative("defaultValue").objectReferenceValue = newChild;
                                            
                                            // if parent has to be set to null and we're in a blend, remove the line from the list of blends
                                            if (newChild == null)
                                            {
                                                int indexToRemove = DynamicParameterUtility.GetPositionOfBlendIndex(dynParamProp, indexInDynamic);
                                                if (indexToRemove > -1)
                                                {
                                                    int indexOfParent = valueToReplace.FindPropertyRelative("settings").FindPropertyRelative("indexOfParent").intValue;
                                                    valueTree.GetArrayElementAtIndex(indexOfParent).FindPropertyRelative("settings").FindPropertyRelative("indexOfBlendedChildren").DeleteArrayElementAtIndex(indexToRemove);
                                                }
                                            }
                                        }
                                    }
                        }
            }      

            // update .children links
            SerializedProperty childrenProp = parentSO.FindProperty("children");
            int childIndex = -1;
            for (int i = 0; i < childrenProp.arraySize; i++)
            {
                SerializedProperty p = childrenProp.GetArrayElementAtIndex(i);
                if (p.objectReferenceValue == oldChild)
                {
                    p.objectReferenceValue = newChild;
                    childIndex = i;
                }                
            }
            if (childIndex > -1)
            {
                SerializedProperty p = childrenProp.GetArrayElementAtIndex(childIndex);
                if (p.objectReferenceValue == null)
                    childrenProp.DeleteArrayElementAtIndex(childIndex);
            }

            // update .parent links
            if (newChild) newChildSO.FindProperty("parent").objectReferenceValue = parentItem;
            oldChildSO.FindProperty("parent").objectReferenceValue = null;

            // Apply everything
            if (allowUndo)
            {
                parentSO.ApplyModifiedProperties();
                oldChildSO.ApplyModifiedProperties();
                if (newChild)
                    newChildSO.ApplyModifiedProperties();
            }
            else
            {
                parentSO.ApplyModifiedPropertiesWithoutUndo();
                oldChildSO.ApplyModifiedPropertiesWithoutUndo();
                if (newChild)
                    newChildSO.ApplyModifiedPropertiesWithoutUndo();
            }

            if (dynamicWindow != null) dynamicWindow.Repaint();
        }

        // Duplicate function (from source to this) that preserves hierarchy.
        EmissionParams DuplicateSubAsset(EmissionParams ep, bool isRoot)
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
            subAssets.arraySize++;
			profile.numberOfSubAssets++;
            subAssets.GetArrayElementAtIndex(subAssets.arraySize-1).objectReferenceValue = newEp;

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

        #region toolbox

        public Texture2D Monochrome(Color col)
        {
            Texture2D result = new Texture2D(1,1);
            result.SetPixel(0,0,col);
            result.Apply();
            return result;
        }

        public void SelectElement(EmissionParams elem)
        {
            if (currentParamInspector != null)
            {
                EmissionParamsInspector epi = currentParamInspector as EmissionParamsInspector;
                epi.OnUnselected();
            }
            currentParamInspector = null;
            currentParamsSelected.objectReferenceValue = elem;

            // in case it's called by another nested inspector
            serializedObject.ApplyModifiedProperties();

            // if hidden in hierarchy due to folded parents, unfold them
            UnfoldWithParents(elem);

            Repaint();
        }

        void EmptyRecycleBin()
        {
            List<EmissionParams> toDestroy = new List<EmissionParams>();            
            for (int i = 0; i < subAssets.arraySize; i++)
            {
                EmissionParams elem = subAssets.GetArrayElementAtIndex(i).objectReferenceValue as EmissionParams;
                if (elem == null) continue;
                if (elem.parent != null) continue;
                if (!elem.isInRecycleBin) continue;
                toDestroy = FillRecursively(toDestroy, elem);
            }

            while (toDestroy.Count > 0)
            {
                EmissionParams objectToDestroy = toDestroy[0];
                toDestroy.RemoveAt(0);
                for (int i = 0; i < subAssets.arraySize; i++)
                {
                    SerializedProperty refAsSubAsset = subAssets.GetArrayElementAtIndex(i);
                    if (refAsSubAsset.objectReferenceValue != objectToDestroy) continue;
                    
                    refAsSubAsset.objectReferenceValue = null;
                    subAssets.DeleteArrayElementAtIndex(i);
                    profile.numberOfSubAssets--;
                    break;
                }
                DestroyImmediate(objectToDestroy, true);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            string path = AssetDatabase.GetAssetPath(profile);
            AssetDatabase.ImportAsset(path);
        }

        #endregion
    }
}