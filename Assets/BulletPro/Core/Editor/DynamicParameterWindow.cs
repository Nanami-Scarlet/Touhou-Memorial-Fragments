using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
    public enum BulletHierarchyObject { Bullet, Shot, Pattern, None }

    public class DynamicParameterWindow : EditorWindow
    {
        #region properties

        public SerializedObject serObj;
        public SerializedProperty dynamicParameter, valueTree;
        public SerializedProperty currentValue, gradientValue, settings, interpolationValue;
        public SerializedProperty index, indexOfFrom, indexOfTo, indexOfParent, indexOfBlendedChildren;
        public SerializedProperty equalParameterType, equalParameterName, equalParameterParentingLevel;
        public SerializedProperty valueType, interpolationFactor, repartitionCurve;
        public SerializedProperty shareValueBetweenInstances, parameterName, relativeTo, rerollFrequency;
        public SerializedProperty loopDepth, useComplexRerollSequence, checkEveryNLoops, loopSequence;
        public SerializedProperty interpolationFactorFromBullet, interpolationFactorFromShot, interpolationFactorFromPattern;
        public SerializedProperty period, wrapMode, wrapPoint, countClockwise;
        public SerializedProperty sortMode, sortDirection, repartitionTexture, centerX, centerY, radius;
        public SerializedProperty differentValuesPerShot;
        public string tabTitle, headerTitle;
        public bool justOpenedAnotherWindow, hasChangedSelection;
        public string[] enumHierarchyChoices, randomShareHierarchyChoices;
        public GUIContent[] loopChoices;
        
        public bool isEditingBulletHierarchy; // handles ALL exceptions related to DynamicBullet/Shot/Pattern objects
        public BulletHierarchyFieldHandler fieldHandler; // draws DynamicBullet/Shot/Pattern objects
        public BulletHierarchyObject currentHierarchyObject; // chooses between DynamicBullet, Shot or Pattern

        #endregion
        
        #region formatting

        List<Editor> editors; // editors from which this window has been opened. Changing values in the window will trigger a repaint.
        bool hasBeenReloaded; // makes sure clicking the button in the middle of a reorderable list doesn't screw it up
        bool hasScrolling;
        float headerHeight, leftMargin, rightMargin, rightMarginWithoutScroll, headerHorizontalOffset;
        GUIStyle headerStyle;
        Vector2 scrollPos;
        ReorderableList blendList;
        Texture2D previewTex; // for bullet position in shot

        #endregion

        #region helpers

        bool isFromPatternBaseCurve; // true if fullPropertyPath is one of the three "overLifetime" curves

        #endregion

        #region undo and float fields

        // Custom float field : caching fields and methods obtained via reflection
		MethodInfo doFloatFieldMethod;
		object recycledEditor;

        void Awake()
        {
            Undo.undoRedoPerformed += OnUndo;
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChange;

            if (recycledEditor == null)
            {
                System.Type editorGUIType = typeof(EditorGUI);
                System.Type RecycledTextEditorType = Assembly.GetAssembly(editorGUIType).GetType("UnityEditor.EditorGUI+RecycledTextEditor");
                System.Type[] argumentTypes = new System.Type[] { RecycledTextEditorType, typeof(Rect), typeof(Rect), typeof(int), typeof(float), typeof(string), typeof(GUIStyle), typeof(bool) };
                doFloatFieldMethod = editorGUIType.GetMethod("DoFloatField", BindingFlags.NonPublic | BindingFlags.Static, null, argumentTypes, null);
                FieldInfo fieldInfo = editorGUIType.GetField("s_RecycledEditor", BindingFlags.NonPublic | BindingFlags.Static);
                recycledEditor = fieldInfo.GetValue(null);
            }
        }

        // Use reflection to access internal functions and customize float fields
		private float CustomFloatField(Rect position, Rect dragHotZone, float value, GUIStyle style = null)
		{
			if (style == null) style = EditorStyles.numberField;
			int controlID = GUIUtility.GetControlID("EditorTextField".GetHashCode(), FocusType.Keyboard, position);

			object[] parameters = new object[] { recycledEditor, position, dragHotZone, controlID, value, "g7", style, true };

			return (float)doFloatFieldMethod.Invoke(null, parameters);
		}

        void OnDestroy()
        {
            // upon closing window, if we were editing something in a Pattern during playmode, restart the pattern
            // note : this cannot be done if the cause of window closing is a selection change.
            if (!hasChangedSelection)
                if (serObj != null)
                    if (serObj.targetObject != null)
                        if (serObj.targetObject is PatternParams)
                        {
                            serObj.ApplyModifiedProperties();
                            serObj.Update();
                            serObj.FindProperty("safetyForPlaymode").intValue++;
                            serObj.ApplyModifiedProperties();
                        }

            Undo.undoRedoPerformed -= OnUndo;
            Selection.selectionChanged -= OnSelectionChanged;
            if (fieldHandler != null)
                if (fieldHandler.profileInspector != null)
                    fieldHandler.profileInspector.dynamicWindow = null;
        }

        void OnUndo() { Repaint(); hasBeenReloaded = true; }
        void OnPlayModeStateChange(PlayModeStateChange pmsc) { if (this != null) this.Close(); }
        void OnSelectionChanged()
        {
            if (this == null) return;

            hasChangedSelection = true;
            this.Close();
        }

        #endregion

        // Called on window opening. Gets editors, properties, and sets up formatting info.
        // If a profileInspector is passed, it means this window is editing an element of BulletHierarchy (DynamicBullet/Shot/Pattern).
        public void LoadProperty(SerializedObject obj, SerializedProperty dynamicParameterProperty, int indexOfValue, BulletHierarchyObject objectType=BulletHierarchyObject.None, EmitterProfileInspector profileInsp=null)
        {
            isEditingBulletHierarchy = (profileInsp != null);
            fieldHandler = new BulletHierarchyFieldHandler(profileInsp, obj, this);
            if (objectType != BulletHierarchyObject.None) // only updated when entering a new dynamic param
                currentHierarchyObject = objectType;
            if (profileInsp != null)
                profileInsp.dynamicWindow = this;

            serObj = obj;
            editors = new List<Editor>();
            Editor[] openedEditors = Resources.FindObjectsOfTypeAll<Editor>();
            if (openedEditors != null)
                if (openedEditors.Length > 0)
                    for (int i = 0; i < openedEditors.Length; i++)
                        if (openedEditors[i].serializedObject == serObj)
                            editors.Add(openedEditors[i]);

            dynamicParameter = dynamicParameterProperty;
            valueTree = dynamicParameter.FindPropertyRelative("valueTree");
            
            currentValue = valueTree.GetArrayElementAtIndex(indexOfValue);
            settings = currentValue.FindPropertyRelative("settings");
            string vType = currentValue.type;
            if (vType.Contains("Color")) gradientValue = currentValue.FindPropertyRelative("gradientValue");

            index = settings.FindPropertyRelative("index");
            indexOfFrom = settings.FindPropertyRelative("indexOfFrom");
            indexOfTo = settings.FindPropertyRelative("indexOfTo");
            indexOfParent = settings.FindPropertyRelative("indexOfParent");
            indexOfBlendedChildren = settings.FindPropertyRelative("indexOfBlendedChildren");
            interpolationValue = settings.FindPropertyRelative("interpolationValue");
            valueType = settings.FindPropertyRelative("valueType");

            equalParameterName = settings.FindPropertyRelative("parameterName");
            equalParameterType = settings.FindPropertyRelative("parameterType");
            equalParameterParentingLevel = settings.FindPropertyRelative("relativeTo");
            
            interpolationFactor = interpolationValue.FindPropertyRelative("interpolationFactor");
            repartitionCurve = interpolationValue.FindPropertyRelative("repartitionCurve");
            shareValueBetweenInstances = interpolationValue.FindPropertyRelative("shareValueBetweenInstances");
            parameterName = interpolationValue.FindPropertyRelative("parameterName");
            relativeTo = interpolationValue.FindPropertyRelative("relativeTo");
            interpolationFactorFromBullet = interpolationValue.FindPropertyRelative("interpolationFactorFromBullet");
            interpolationFactorFromShot = interpolationValue.FindPropertyRelative("interpolationFactorFromShot");
            interpolationFactorFromPattern = interpolationValue.FindPropertyRelative("interpolationFactorFromPattern");
            rerollFrequency = interpolationValue.FindPropertyRelative("rerollFrequency");

            loopDepth = interpolationValue.FindPropertyRelative("loopDepth");
            useComplexRerollSequence = interpolationValue.FindPropertyRelative("useComplexRerollSequence");
            checkEveryNLoops = interpolationValue.FindPropertyRelative("checkEveryNLoops");
            loopSequence = interpolationValue.FindPropertyRelative("loopSequence");
            
            period = interpolationValue.FindPropertyRelative("period");
            wrapMode = interpolationValue.FindPropertyRelative("wrapMode");
            wrapPoint = interpolationValue.FindPropertyRelative("wrapPoint");
            countClockwise = interpolationValue.FindPropertyRelative("countClockwise");
            sortMode = interpolationValue.FindPropertyRelative("sortMode");
            sortDirection = interpolationValue.FindPropertyRelative("sortDirection");
            repartitionTexture = interpolationValue.FindPropertyRelative("repartitionTexture");
            centerX = interpolationValue.FindPropertyRelative("centerX");
            centerY = interpolationValue.FindPropertyRelative("centerY");
            radius = interpolationValue.FindPropertyRelative("radius");

            differentValuesPerShot = interpolationValue.FindPropertyRelative("differentValuesPerShot");

            tabTitle = "BulletPro Param Details";
            SerializedProperty headerProp = settings.FindPropertyRelative("headerTitle");
            if (string.IsNullOrEmpty(headerProp.stringValue)) headerProp.stringValue = dynamicParameter.displayName;
            headerTitle = "Parameter Details: " + headerProp.stringValue;

            // set helpers for pattern
            isFromPatternBaseCurve = false;
            if (serObj.targetObject is PatternParams)
            {
                string fullPropertyPath = dynamicParameter.propertyPath;
                if (fullPropertyPath.Contains("OverLifetime"))
                    isFromPatternBaseCurve = true;
            }
            
            // window formatting
            this.minSize = new Vector2(350, 200);
            this.titleContent = new GUIContent(tabTitle);
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
            headerHeight = 26;
            headerHorizontalOffset = -5;
            leftMargin = 16;
            rightMargin = 32;
            rightMarginWithoutScroll = 16;

            loopChoices = new GUIContent[] {
                new GUIContent("Reroll value at every loop iteration"),
                new GUIContent("Choose iterations that trigger a reroll")
            };
            
            hasBeenReloaded = true;
            hasChangedSelection = false;

            BlendListSetup();

            Repaint();

            this.Show();
        }

        public void OnGUI()
        {
            if (serObj == null)
            {
                this.Close();
                return;
            }

            if (fieldHandler != null)
                fieldHandler.OnInspectorBeginning();

            serObj.Update();

            Rect partialView = new Rect(0, 0, position.width - headerHorizontalOffset, position.height);

            GUILayout.BeginArea(new Rect(headerHorizontalOffset, 0, position.width - headerHorizontalOffset*2, 10000)); 
            Rect totalView = EditorGUILayout.BeginVertical();
            totalView.width = position.width - rightMargin;
            if (totalView.height > 1)
                hasScrolling = position.height < totalView.height;
            if (hasScrolling) scrollPos = GUI.BeginScrollView(partialView, scrollPos, totalView);
            EditorGUI.BeginChangeCheck();
            DrawWindowBody();
            bool changed = EditorGUI.EndChangeCheck();
            if (hasScrolling) GUI.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();

            hasBeenReloaded = false;

            serObj.ApplyModifiedProperties();

            if (changed)
            {
                if (serObj.targetObject is PatternParams)
                {
                    serObj.FindProperty("safetyForPlaymode").intValue++;
                    serObj.ApplyModifiedProperties();
                }

                if (editors.Count > 0)
                    for (int i = 0; i < editors.Count; i++)
                        editors[i].Repaint();
            }
        }

        void DrawWindowBody()
        {
            bool hasParent = indexOfParent.intValue > 0;
            if (!hasParent) GUILayout.Space(6); // needs more height when not drawing the button
            GUILayout.Space(8);
            GUIStyle wrapBold = new GUIStyle(EditorStyles.boldLabel);
            wrapBold.wordWrap = true;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(leftMargin - headerHorizontalOffset);
            if (hasParent)
            {
                EditorGUILayout.BeginVertical(GUILayout.MaxWidth(30));
                GUILayout.Space(6);
                if (GUILayout.Button("<<", EditorStyles.miniButton, GUILayout.MaxWidth(30), GUILayout.MaxHeight(16)))
                    LoadProperty(serObj, dynamicParameter, indexOfParent.intValue, BulletHierarchyObject.None, fieldHandler.profileInspector);
                EditorGUILayout.EndVertical();
                GUILayout.Space(8);
            }
            EditorGUILayout.LabelField(headerTitle, wrapBold);
            GUILayout.Space(hasScrolling ? rightMargin : rightMarginWithoutScroll);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);

            #region possible values

            EditorGUILayout.LabelField("Possible Values", headerStyle, GUILayout.Height(headerHeight));
            GUILayout.Space(16);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(leftMargin - headerHorizontalOffset);
            EditorGUILayout.BeginVertical();

            // generating new value objects based on valueType
            EditorGUI.BeginChangeCheck();
            string vType = currentValue.type;
            bool admitsFromTo = vType.Contains("Int") || vType.Contains("Float") || vType.Contains("Slider") || vType.Contains("Color") || vType.Contains("Vector");
            List<GUIContent> optionsList = new List<GUIContent>();
            optionsList.Add(new GUIContent("Fixed"));
            if (admitsFromTo) optionsList.Add(new GUIContent("From-to"));
            optionsList.Add(new GUIContent("List of possible values"));
            if (vType.Contains("Color")) optionsList.Add(new GUIContent("From Gradient"));
            if (!isEditingBulletHierarchy) optionsList.Add(new GUIContent("Equal to another parameter"));
            // cancels skipping one value for incompatible types
            if (!admitsFromTo)
            {
                int displayed = valueType.enumValueIndex;
                if (displayed == 2) displayed = 1;
                else if (displayed == 4) displayed = 2;
                int result = EditorGUILayout.Popup(new GUIContent("Select Value"), displayed, optionsList.ToArray());
                if (result == 1) result = 2; // blend from list
                else if (result == 2) result = 4; // equal to parameter
                valueType.enumValueIndex = result;
            }
            else if (!vType.Contains("Color"))
            {
                int displayed = valueType.enumValueIndex;
                if (displayed == 4) displayed = 3;
                int result = EditorGUILayout.Popup(new GUIContent("Select Value"), displayed, optionsList.ToArray());
                if (result == 3) result = 4; // equal to parameter
                valueType.enumValueIndex = result;
            }
            else // Colors only
            {
                valueType.enumValueIndex = EditorGUILayout.Popup(new GUIContent("Select Value"), valueType.enumValueIndex, optionsList.ToArray());
            }
            if (EditorGUI.EndChangeCheck())
            {
                //RepaintParentWindows();

                // Initializing indexes for the from-to config by filling slots in the tree
                if (valueType.enumValueIndex == (int)DynamicParameterSorting.FromAToB)
                {
                    if (indexOfFrom.intValue == 0)
                    {
                        valueTree.arraySize++;
                        indexOfFrom.intValue = valueTree.arraySize-1;
                        SerializedProperty newValue = valueTree.GetArrayElementAtIndex(valueTree.arraySize-1);
                        SerializedProperty settingsOfNew = newValue.FindPropertyRelative("settings");
                        settingsOfNew.FindPropertyRelative("index").intValue = valueTree.arraySize-1;
                        settingsOfNew.FindPropertyRelative("indexOfParent").intValue = index.intValue;
                        settingsOfNew.FindPropertyRelative("indexOfFrom").intValue = 0;
                        settingsOfNew.FindPropertyRelative("indexOfTo").intValue = 0;
                        settingsOfNew.FindPropertyRelative("indexOfBlendedChildren").arraySize = 0;
                        settingsOfNew.FindPropertyRelative("valueType").enumValueIndex = 0;
                        settingsOfNew.FindPropertyRelative("headerTitle").stringValue = settings.FindPropertyRelative("headerTitle").stringValue + " : \"From\"";
                        settingsOfNew.FindPropertyRelative("interpolationValue").FindPropertyRelative("repartitionCurve").animationCurveValue = AnimationCurve.Linear(0,0,1,1);

                        // take parent's fixed value and paste it into defaults of new.
                        SerializedProperty defaultValOfParent = currentValue.FindPropertyRelative("defaultValue");
                        SerializedProperty defaultValOfNew = newValue.FindPropertyRelative("defaultValue");
                        defaultValOfNew.CopyValuesFrom(defaultValOfParent);
                    }

                    if (indexOfTo.intValue == 0)
                    {
                        valueTree.arraySize++;
                        indexOfTo.intValue = valueTree.arraySize-1;
                        SerializedProperty newValue = valueTree.GetArrayElementAtIndex(valueTree.arraySize-1);
                        SerializedProperty settingsOfNew = newValue.FindPropertyRelative("settings");
                        settingsOfNew.FindPropertyRelative("index").intValue = valueTree.arraySize-1;
                        settingsOfNew.FindPropertyRelative("indexOfParent").intValue = index.intValue;
                        settingsOfNew.FindPropertyRelative("indexOfFrom").intValue = 0;
                        settingsOfNew.FindPropertyRelative("indexOfTo").intValue = 0;
                        settingsOfNew.FindPropertyRelative("indexOfBlendedChildren").arraySize = 0;
                        settingsOfNew.FindPropertyRelative("valueType").enumValueIndex = 0;
                        settingsOfNew.FindPropertyRelative("headerTitle").stringValue = settings.FindPropertyRelative("headerTitle").stringValue + " : \"To\"";
                        settingsOfNew.FindPropertyRelative("interpolationValue").FindPropertyRelative("repartitionCurve").animationCurveValue = AnimationCurve.Linear(0,0,1,1);

                        // take parent's fixed value and paste it into defaults of new.
                        SerializedProperty defaultValOfParent = currentValue.FindPropertyRelative("defaultValue");
                        SerializedProperty defaultValOfNew = newValue.FindPropertyRelative("defaultValue");
                        defaultValOfNew.CopyValuesFrom(defaultValOfParent);
                    }
                }

                // For fixed and blend : handling parenting of dynamic bullets/shots/patterns
                else if (isEditingBulletHierarchy)
                {
                    EmissionParams newParent = serObj.targetObject as EmissionParams;
                    serObj.ApplyModifiedProperties();

                    // parent or unparent fixed value
                    SerializedProperty fixedVal = currentValue.FindPropertyRelative("defaultValue");
                    EmissionParams fixedEP = fixedVal.objectReferenceValue as EmissionParams;
                    EmissionParams parentOfFixed = (valueType.enumValueIndex == (int)DynamicParameterSorting.Fixed) ? newParent : null;
                    if (fixedEP) fieldHandler.SetParent(fixedEP, parentOfFixed);

                    // parent or unparent blended values
                    EmissionParams parentOfBlended = (valueType.enumValueIndex == (int)DynamicParameterSorting.Fixed) ? null : newParent;
                    if (indexOfBlendedChildren.arraySize > 0)
                        for (int i = 0; i < indexOfBlendedChildren.arraySize; i++)
                        {
                            int blendIndex = indexOfBlendedChildren.GetArrayElementAtIndex(i).intValue;
                            EmitterProfileUtility.SetParentOfHierarchyElementAsSerializedProperty(dynamicParameter, parentOfBlended, fieldHandler, blendIndex); 
                        }
                }
            }

            GUILayout.Space(8);

            // Fixed GUI
            if (valueType.enumValueIndex == (int)DynamicParameterSorting.Fixed)
            {
                GUIContent gc = new GUIContent("Value");

                if (isEditingBulletHierarchy)
                {
                    SerializedProperty fixedVal = currentValue.FindPropertyRelative("defaultValue");
                    if (currentHierarchyObject == BulletHierarchyObject.Bullet)
                        fieldHandler.LayoutParamField<BulletParams>(gc, fixedVal, 0.6f);
                    else if (currentHierarchyObject == BulletHierarchyObject.Shot)
                        fieldHandler.LayoutParamField<ShotParams>(gc, fixedVal, 0.6f);
                    else if (currentHierarchyObject == BulletHierarchyObject.Pattern)
                        fieldHandler.LayoutParamField<PatternParams>(gc, fixedVal, 0.6f);
                }
                else
                {
                    SerializedProperty fixedVal = currentValue.FindPropertyRelative("defaultValue");
                    if (currentValue.type.Contains("Slider")) fixedVal.floatValue = EditorGUILayout.Slider(gc, fixedVal.floatValue, 0f, 1f);
                    else if (fixedVal.type == "Vector2") fixedVal.vector2Value = EditorGUILayout.Vector2Field(gc, fixedVal.vector2Value);
                    else if (fixedVal.type == "Vector3") fixedVal.vector3Value = EditorGUILayout.Vector3Field(gc, fixedVal.vector3Value);
                    else if (fixedVal.type == "Vector4") fixedVal.vector4Value = EditorGUILayout.Vector4Field(gc, fixedVal.vector4Value);
                    else if (currentValue.type.Contains("Float") && dynamicParameter.FindPropertyRelative("useSlider").boolValue)
                        fixedVal.floatValue = EditorGUILayout.Slider(gc, fixedVal.floatValue, dynamicParameter.FindPropertyRelative("sliderMin").floatValue, dynamicParameter.FindPropertyRelative("sliderMax").floatValue);
                    else if (currentValue.type.Contains("Int"))
                    {
                        if (dynamicParameter.FindPropertyRelative("useSlider").boolValue)
                            fixedVal.intValue = EditorGUILayout.IntSlider(gc, fixedVal.intValue, dynamicParameter.FindPropertyRelative("sliderMin").intValue, dynamicParameter.FindPropertyRelative("sliderMax").intValue);
                        else
                        {
                            SerializedProperty buttons = dynamicParameter.FindPropertyRelative("buttons");
                            if (buttons.arraySize == 0) EditorGUILayout.PropertyField(fixedVal, gc);
                            else
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField(gc.text+": "+fixedVal.intValue.ToString()+". ", GUILayout.MaxWidth(100));
                                for (int i = 0; i < buttons.arraySize; i++)
                                {
                                    int val = buttons.GetArrayElementAtIndex(i).intValue;
                                    string btnStr = (val > -1 ? "+":"")+val.ToString();
                                    if (GUILayout.Button(btnStr, EditorStyles.miniButton))
                                        fixedVal.intValue += val;
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                    else if (currentValue.type.Contains("Object") && dynamicParameter.FindPropertyRelative("narrowType").boolValue)
                    {
                        System.Type specificType = System.Type.GetType(dynamicParameter.FindPropertyRelative("typeName").stringValue);
                        fixedVal.objectReferenceValue = EditorGUILayout.ObjectField(gc, fixedVal.objectReferenceValue, specificType, true);
                        if (dynamicParameter.FindPropertyRelative("requireComponent").boolValue)
                        {
                            GameObject go = fixedVal.objectReferenceValue as GameObject;
                            if (go)
                            {
                                specificType = System.Type.GetType(dynamicParameter.FindPropertyRelative("requiredComponentName").stringValue);
                                if (!go.GetComponent(specificType))
                                    fixedVal.objectReferenceValue = null;
                            }
                        }
                    }
                    else if (currentValue.type.Contains("Enum"))
                    {
                        SerializedProperty options = dynamicParameter.FindPropertyRelative("enumOptions");
                        if (options.arraySize == 0)
                            EditorGUILayout.PropertyField(fixedVal, gc);
                        else
                        {
                            GUIContent[] optionsGC = new GUIContent[options.arraySize];
                            for (int i = 0; i < options.arraySize; i++)
                                optionsGC[i] = new GUIContent(options.GetArrayElementAtIndex(i).stringValue);

                            fixedVal.intValue = EditorGUILayout.Popup(gc, fixedVal.intValue, optionsGC);
                        }
                    }
                    else if (currentValue.type.Contains("AnimationCurve"))
                    {
                        if (!dynamicParameter.FindPropertyRelative("forceZeroToOne").boolValue) EditorGUILayout.PropertyField(fixedVal, gc);
                        else
                        {
                            EditorGUILayout.PropertyField(fixedVal, gc);

                            EditorGUILayout.BeginHorizontal();
                            if (!BulletCurveDrawer.GoesFromZeroToOne(fixedVal, true))
                            {
                                EditorGUILayout.LabelField("X-axis must run from 0 to 1.", EditorStyles.boldLabel);
                                Color oldColor = GUI.color;
                                GUI.color = new Color(1.0f, 0.6f, 0.4f, 1f);
                                if (GUILayout.Button("Fix Curve", EditorStyles.miniButton))
                                    BulletCurveDrawer.RepairCurveFromZeroToOne(fixedVal, true);
                                GUI.color = oldColor;
                            }
                            else
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.LabelField("This curve has no error.");
                                if (GUILayout.Button("Fix Curve", EditorStyles.miniButton))
                                    Debug.Log("If you can see this, congratulations! I thought nobody would read the whole source code. Are you enjoying BulletPro so far?");
                                EditorGUI.EndDisabledGroup();
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    else EditorGUILayout.PropertyField(fixedVal, gc);
                }
            }

            // From-To GUI
            else if (valueType.enumValueIndex == (int)DynamicParameterSorting.FromAToB)
            {
                /* *
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(fromValue, new GUIContent("From"));
                EditorGUILayout.PropertyField(toValue, new GUIContent("To"));
                if (EditorGUI.EndChangeCheck() && !fromValue.type.Contains("Color")) RepaintParentWindows();
                /* */

                if (!isEditingBulletHierarchy)
                {
                    ValueDrawer(valueTree.GetArrayElementAtIndex(indexOfFrom.intValue), "From");
                    ValueDrawer(valueTree.GetArrayElementAtIndex(indexOfTo.intValue), "To");
                }
                else
                {
                    GUIContent gcFrom = new GUIContent("From");
                    GUIContent gcTo = new GUIContent("To");
                    if (currentHierarchyObject == BulletHierarchyObject.Bullet)
                    {
                        fieldHandler.LayoutDynamicParamField<BulletParams>(gcFrom, dynamicParameter, indexOfFrom.intValue);
                        fieldHandler.LayoutDynamicParamField<BulletParams>(gcTo, dynamicParameter, indexOfTo.intValue);
                    }
                    else if (currentHierarchyObject == BulletHierarchyObject.Shot)
                    {
                        fieldHandler.LayoutDynamicParamField<ShotParams>(gcFrom, dynamicParameter, indexOfFrom.intValue);
                        fieldHandler.LayoutDynamicParamField<ShotParams>(gcTo, dynamicParameter, indexOfTo.intValue);
                    }
                    else if (currentHierarchyObject == BulletHierarchyObject.Pattern)
                    {
                        fieldHandler.LayoutDynamicParamField<PatternParams>(gcFrom, dynamicParameter, indexOfFrom.intValue);
                        fieldHandler.LayoutDynamicParamField<PatternParams>(gcTo, dynamicParameter, indexOfTo.intValue);
                    }
                }
            }

            // Blend GUI
            else if (valueType.enumValueIndex == (int)DynamicParameterSorting.Blend)
            {
                blendList.DoLayoutList();
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Weight Repartition Visualizer:");
                DrawBlendGauge();
            }

            // Gradient GUI
            else if (valueType.enumValueIndex == (int)DynamicParameterSorting.FromGradient)
            {
                //SerializedProperty gradientVal = currentValue.FindPropertyRelative("gradientValue");
                GUIContent gc = new GUIContent("Gradient");
                EditorGUILayout.PropertyField(gradientValue, gc);
            }

            // Equal-to-parameter GUI
            else if (valueType.enumValueIndex == (int)DynamicParameterSorting.EqualToParameter)
            {
                if (vType.Contains("Enum"))
                    EditorGUILayout.HelpBox("Since the value you're editing is an Enum, your chosen parameter must be of Int type.", MessageType.Info);
                GUIContent gc = new GUIContent("Parameter Type");
                EditorGUILayout.PropertyField(equalParameterType, gc);

                if (equalParameterType.enumValueIndex == (int)UserMadeParameterType.BulletHierarchy)
                {
                    EditorGUILayout.LabelField("A Custom Parameter will be picked from the following Bullet:");

                    // helpers
                    float buttonWidth = 24f;
                    float space = 5f;
                    float indent = 32f;
                    bool typeError = false;
                    bool amountError = false;
                    bool isBehaviour = false;

                    // string builder's base
                    string targetName = "";
                    string str = "This bullet";
                    if (serObj.targetObject is ShotParams || serObj.targetObject is PatternParams)
                        str = "Direct bullet parent";
                    if (equalParameterParentingLevel.intValue > 0)
                        str = equalParameterParentingLevel.intValue.ToString() + (equalParameterParentingLevel.intValue>1?" bullets above":" bullet above");

                    // getting ancestor's name if applicable
                    if (serObj.targetObject is EmissionParams)
                    {
                        int counter = equalParameterParentingLevel.intValue;

                        // only consider Bullets. For Shots and Params, go the the closest Bullet parent.
                        EmissionParams curObj = serObj.targetObject as EmissionParams;
                        if (curObj is ShotParams)
                        {
                            curObj = curObj.parent;
                            if (curObj == null) amountError = true;
                        }
                        if (!amountError)
                        {
                            if (curObj is PatternParams)
                            {
                                curObj = curObj.parent;
                                if (curObj == null) amountError = true;
                            }
                        }

                        if (!amountError) targetName = curObj.name;

                        // go upwards from bullet to bullet
                        while (counter > 0 && !amountError)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                curObj = curObj.parent;
                                if (curObj == null)
                                {
                                    amountError = true;
                                    break;
                                }
                            }
                            if (amountError) break;

                            targetName = curObj.name;
                            counter--;
                        }
                    }
                    else if (serObj.targetObject is BaseBulletBehaviour) isBehaviour = true;
                    else typeError = true;

                    // string building
                    if (!isBehaviour && !amountError && !typeError) str += " (\""+targetName+"\")";
                    
                    // actual controls
                    int oldIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(indent);
                    EditorGUI.BeginDisabledGroup(equalParameterParentingLevel.intValue == 0);
                    if (GUILayout.Button("-1", EditorStyles.miniButtonLeft, GUILayout.MaxWidth(buttonWidth))) equalParameterParentingLevel.intValue--;
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button("+1", EditorStyles.miniButtonRight, GUILayout.MaxWidth(buttonWidth))) equalParameterParentingLevel.intValue++;
                    GUILayout.Space(space);
                    EditorGUILayout.LabelField(str);
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel = oldIndent;

                    // displaying errors, if any
                    string errorStr = "";
                    if (typeError) errorStr = "This feature is meant for Emitter Profiles and Bullet Behaviours. It's likely your parameter will not have a Bullet Hierarchy.";
                    else if (amountError) errorStr = "\""+equalParameterParentingLevel.intValue.ToString()+" bullet"+(equalParameterParentingLevel.intValue > 1 ? "s":"")+" above\" goes beyond the root of this Bullet Hierarchy. It's likely an error.";
                    if (!string.IsNullOrEmpty(errorStr)) EditorGUILayout.HelpBox(errorStr, MessageType.Warning);

                }
                EditorGUILayout.PropertyField(equalParameterName);
                // In Patterns only : prompt user for "when to reroll the parameter"
                RerollFrequencyField();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(hasScrolling ? rightMargin : rightMarginWithoutScroll);
            EditorGUILayout.EndHorizontal();

            #endregion

            #region picking the value

            GUILayout.Space(16);

            EditorGUI.BeginDisabledGroup(valueType.enumValueIndex == 0 || valueType.enumValueIndex == 4);

            GUIContent interpolationValueGC = new GUIContent("Interpolation Value (between 0 and 1)", "Select how this number will be calculated. It will then be used to choose the value of your Dynamic Parameter.");
            EditorGUILayout.LabelField(interpolationValueGC, headerStyle, GUILayout.Height(headerHeight));
            GUILayout.Space(16);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(leftMargin - headerHorizontalOffset);
            EditorGUILayout.BeginVertical();
            DoInterpolationValueGUILayout(false); // will be set to true when Combine Factors is implemented
            EditorGUILayout.EndVertical();
            GUILayout.Space(hasScrolling ? rightMargin : rightMarginWithoutScroll);            
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            #endregion

            GUILayout.Space(8);
        }

        #region blend toolbox

        void BlendListSetup()
        {
            blendList = new ReorderableList(serObj, indexOfBlendedChildren, true, true, true, true);
            blendList.drawHeaderCallback = (Rect rect) =>
			{
                float weightWidth = 40f;
                float space = 5f;
                float colorWidth = 50f;
                float remainingWidth = rect.width - (weightWidth + colorWidth + space + space);

                Rect colorRect = new Rect (rect.x + rect.width - colorWidth, rect.y, colorWidth, rect.height);
                Rect weightRect = new Rect (colorRect.x - (space + weightWidth), rect.y, weightWidth*2, rect.height);
                Rect propRect = new Rect(rect.x, rect.y, remainingWidth, rect.height);

				//EditorGUI.LabelField(colorRect, "Color");				
				EditorGUI.LabelField(weightRect, "Weights");				
				EditorGUI.LabelField(propRect, "Possible Values");
			};
			blendList.drawElementCallback = BlendElementDrawer;
			blendList.onReorderCallback += (ReorderableList list) =>
            {
                if (indexOfBlendedChildren.arraySize > 0)
                    for (int i = 0; i < indexOfBlendedChildren.arraySize; i++)
                    {
                        SerializedProperty newValue = valueTree.GetArrayElementAtIndex(indexOfBlendedChildren.GetArrayElementAtIndex(i).intValue);
                        SerializedProperty settingsOfNew = newValue.FindPropertyRelative("settings");
                        settingsOfNew.FindPropertyRelative("headerTitle").stringValue = settings.FindPropertyRelative("headerTitle").stringValue + " : Value "+(indexOfBlendedChildren.arraySize-1).ToString();
                    }
            };
			blendList.onRemoveCallback += (ReorderableList list) =>
			{
                // if dynamic bullet/shot/pattern, unparent element before removing it
                if (isEditingBulletHierarchy)
                {
                    int blendIndex = indexOfBlendedChildren.GetArrayElementAtIndex(list.index).intValue;
                    EmitterProfileUtility.SetParentOfHierarchyElementAsSerializedProperty(dynamicParameter, null, fieldHandler, blendIndex);
                }

				indexOfBlendedChildren.DeleteArrayElementAtIndex(list.index);
			};
			blendList.onAddCallback += (ReorderableList list) =>
			{
                indexOfBlendedChildren.arraySize++;
				valueTree.arraySize++;
                indexOfBlendedChildren.GetArrayElementAtIndex(indexOfBlendedChildren.arraySize-1).intValue = valueTree.arraySize-1;
                SerializedProperty newValue = valueTree.GetArrayElementAtIndex(valueTree.arraySize-1);
                SerializedProperty settingsOfNew = newValue.FindPropertyRelative("settings");
                settingsOfNew.FindPropertyRelative("index").intValue = valueTree.arraySize-1;
                settingsOfNew.FindPropertyRelative("indexOfParent").intValue = index.intValue;
                settingsOfNew.FindPropertyRelative("indexOfFrom").intValue = 0;
                settingsOfNew.FindPropertyRelative("indexOfTo").intValue = 0;
                settingsOfNew.FindPropertyRelative("indexOfBlendedChildren").arraySize = 0;
                settingsOfNew.FindPropertyRelative("valueType").enumValueIndex = 0;
                settingsOfNew.FindPropertyRelative("headerTitle").stringValue = settings.FindPropertyRelative("headerTitle").stringValue + " : Value "+(indexOfBlendedChildren.arraySize-1).ToString();
                SerializedProperty interpolationValueOfNew = settingsOfNew.FindPropertyRelative("interpolationValue");
                interpolationValueOfNew.FindPropertyRelative("repartitionCurve").animationCurveValue = AnimationCurve.Linear(0,0,1,1);
                settingsOfNew.FindPropertyRelative("blendColor").colorValue = Random.ColorHSV(0, 1, 0.5f, 1f, 0.8f, 1f, 1f, 1f);

                SerializedProperty weight = settingsOfNew.FindPropertyRelative("weight");
                if (indexOfBlendedChildren.arraySize > 1) weight.floatValue = valueTree.GetArrayElementAtIndex(indexOfBlendedChildren.GetArrayElementAtIndex(indexOfBlendedChildren.arraySize-2).intValue).FindPropertyRelative("settings").FindPropertyRelative("weight").floatValue;
                else weight.floatValue = 1f;

                // else, take parent's fixed value and paste it into defaults of new.
                SerializedProperty defaultValOfParent = currentValue.FindPropertyRelative("defaultValue");
                SerializedProperty defaultValOfNew = newValue.FindPropertyRelative("defaultValue");
                defaultValOfNew.CopyValuesFrom(defaultValOfParent);

                // exception : if dynamic bullet/shot/pattern, set this new value to null
                if (isEditingBulletHierarchy)
                    defaultValOfNew.objectReferenceValue = null;
			};
			blendList.elementHeightCallback += (int idx) =>
			{
				float numberOfLines = 1;
                float offset = 0;

                if (hasBeenReloaded)
                    return blendList.elementHeight * numberOfLines;

				int indexOfBlended = indexOfBlendedChildren.GetArrayElementAtIndex(idx).intValue;

                if (valueTree.arraySize <= indexOfBlended)
                    return blendList.elementHeight * numberOfLines;

                SerializedProperty blendElement = valueTree.GetArrayElementAtIndex(indexOfBlended);
                SerializedProperty blendSettings = blendElement.FindPropertyRelative("settings");
                
                if (blendSettings.FindPropertyRelative("valueType").enumValueIndex > 0)
                    return blendList.elementHeight * numberOfLines;

                //else if (blendElement.type.Contains("Vector")) numberOfLines = 2;

                else if (blendElement.type.Contains("Rect"))
                    numberOfLines = 2;

                else if (blendElement.type.Contains("AnimationCurve"))
                    if (dynamicParameter.FindPropertyRelative("forceZeroToOne").boolValue)
                    {
                        offset = -4;
                        numberOfLines = 2;
                    }
				
				return blendList.elementHeight * numberOfLines + offset;
			};
        }

        void BlendElementDrawer(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (hasBeenReloaded) return;

            float weightWidth = 40f;
            float space = 5f;
            float extraSpaceAfterProperty = 20f;
            float colorWidth = 50f;
            float leftOffset = 0f;

            float remainingWidth = rect.width - (weightWidth + colorWidth + space + space + extraSpaceAfterProperty + leftOffset);
            float lineHeight = EditorGUIUtility.singleLineHeight;

            Rect colorRect = new Rect (rect.x + rect.width - colorWidth, rect.y+1, colorWidth, lineHeight);
            Rect weightRect = new Rect (colorRect.x - (space + weightWidth), rect.y+1, weightWidth, lineHeight);
            Rect weightDragZone = new Rect(weightRect.x-15f, weightRect.y, 20f, weightRect.height);
            Rect propRect = new Rect(rect.x + leftOffset, rect.y+1, remainingWidth, lineHeight);

            SerializedProperty blendedValue = valueTree.GetArrayElementAtIndex(indexOfBlendedChildren.GetArrayElementAtIndex(index).intValue);
            SerializedProperty blendedSettings = blendedValue.FindPropertyRelative("settings");
            SerializedProperty blendWeight = blendedSettings.FindPropertyRelative("weight");

            DynamicDrawer.dynamicParameter = dynamicParameter;

            if (!isEditingBulletHierarchy)
                EditorGUI.PropertyField(propRect, blendedValue, GUIContent.none);
            else
            {
                int blendIndex = indexOfBlendedChildren.GetArrayElementAtIndex(index).intValue;
                if (currentHierarchyObject == BulletHierarchyObject.Bullet)
                    fieldHandler.DynamicParamField<BulletParams>(propRect, GUIContent.none, dynamicParameter, blendIndex);
                else if (currentHierarchyObject == BulletHierarchyObject.Shot)
                    fieldHandler.DynamicParamField<ShotParams>(propRect, GUIContent.none, dynamicParameter, blendIndex);
                else if (currentHierarchyObject == BulletHierarchyObject.Pattern)
                    fieldHandler.DynamicParamField<PatternParams>(propRect, GUIContent.none, dynamicParameter, blendIndex);
            }
            
            blendWeight.floatValue = CustomFloatField(weightRect, weightDragZone, blendWeight.floatValue);
            if (blendWeight.floatValue < 0f) blendWeight.floatValue = 0f;
            EditorGUI.PropertyField(colorRect, blendedSettings.FindPropertyRelative("blendColor"), GUIContent.none);
        }

        void DrawBlendGauge()
        {
            if (indexOfBlendedChildren.arraySize == 0)
            {
                EditorGUILayout.LabelField("(List is empty)");
                return;
            }

            float gaugeWidth = EditorGUIUtility.currentViewWidth - (leftMargin + rightMargin);
            Rect gaugeRect = EditorGUILayout.GetControlRect(false, 20f, GUILayout.Width(gaugeWidth));

            // calculate 1 / totalWeight
            float totalWeights = 0;
            for (int i = 0; i < indexOfBlendedChildren.arraySize; i++)
            {
                int idx = indexOfBlendedChildren.GetArrayElementAtIndex(i).intValue;
                SerializedProperty bv = valueTree.GetArrayElementAtIndex(idx);
                totalWeights += bv.FindPropertyRelative("settings").FindPropertyRelative("weight").floatValue;
            }
            if (totalWeights <= 0) totalWeights = 1f;
            float invTotalWeight = 1.0f / totalWeights;
            
            // draw proportional rects
            float curX = 0;
            for (int i = 0; i < indexOfBlendedChildren.arraySize; i++)
            {
                int idx = indexOfBlendedChildren.GetArrayElementAtIndex(i).intValue;
                SerializedProperty bvSettings = valueTree.GetArrayElementAtIndex(idx).FindPropertyRelative("settings");
                float curWeight = bvSettings.FindPropertyRelative("weight").floatValue;
                curWeight *= invTotalWeight;

                float curWidth = curWeight * gaugeWidth;
                Rect gaugePiece = new Rect(gaugeRect.x + curX, gaugeRect.y, curWidth, gaugeRect.height);
                curX += curWidth;
                EditorGUI.DrawRect(gaugePiece, bvSettings.FindPropertyRelative("blendColor").colorValue);
            }
        }

        #endregion

        #region toolbox

        void ValueDrawer(SerializedProperty val, string label)
        {
            DynamicDrawer.dynamicParameter = dynamicParameter;
            GUIContent gc = new GUIContent(label);
            EditorGUILayout.PropertyField(val, gc);
        }

        // Draws the "Reroll Frequency" field if necessary, ie. for top-level parameters belonging to a Pattern
        void RerollFrequencyField()
        {
            if (!(serObj.targetObject is PatternParams)) return;
            if (index.intValue != 1) return; // only appears at root because this doesn't support nesting (and it would be pointless)
            if (isFromPatternBaseCurve) return; // not supported either for "Over Lifetime" curve parameters
            
            GUIContent gcRerollFreq = new GUIContent("Reroll Frequency", "Should we reroll for a value everytime the related Pattern Instruction is called, or keep the same value across a same instance of Pattern?");
            EditorGUILayout.PropertyField(rerollFrequency, gcRerollFreq);
            if (rerollFrequency.enumValueIndex == (int)RerollFrequency.AtCertainLoops)
            {
                float fakeIndent = 34;
                float buttonWidth = 24;
                float space = 5;

                EditorGUI.indentLevel += 2;
                EditorGUILayout.LabelField("Relevant loop to check:");
                // Loop depth field
                int oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(fakeIndent);
                EditorGUI.BeginDisabledGroup(loopDepth.intValue < 1);
                if (GUILayout.Button("-1", EditorStyles.miniButtonLeft, GUILayout.MaxWidth(buttonWidth))) loopDepth.intValue--;
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(loopDepth.intValue > 10);
                if (GUILayout.Button("+1", EditorStyles.miniButtonRight, GUILayout.MaxWidth(buttonWidth))) loopDepth.intValue++;
                EditorGUI.EndDisabledGroup();
                GUILayout.Space(space);
                string str = "0 (Innermost loop).";
                if (loopDepth.intValue > 0)
                {
                    string plural = loopDepth.intValue>1?"s":"";
                    str = loopDepth.intValue.ToString() + " loop"+plural+" above innermost loop.";
                }
                EditorGUILayout.LabelField(str);
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel = oldIndent;

                useComplexRerollSequence.boolValue = EditorGUILayout.Popup(GUIContent.none, useComplexRerollSequence.boolValue?1:0, loopChoices, GUILayout.MaxWidth(250)) == 1;
                if (useComplexRerollSequence.boolValue)
                {
                    EditorGUI.indentLevel += 2;
                    // sequence size field
                    EditorGUILayout.LabelField("Manually choose for the first X loops:");
                    oldIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(fakeIndent * 2);
                    EditorGUI.BeginDisabledGroup(checkEveryNLoops.intValue < 1);
                    if (GUILayout.Button("-1", EditorStyles.miniButtonLeft, GUILayout.MaxWidth(buttonWidth))) checkEveryNLoops.intValue--;
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.BeginDisabledGroup(checkEveryNLoops.intValue > 30);
                    if (GUILayout.Button("+1", EditorStyles.miniButtonRight, GUILayout.MaxWidth(buttonWidth))) checkEveryNLoops.intValue++;
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Space(space);
                    str = checkEveryNLoops.intValue.ToString()+" (Reroll every loop).";
                    if (checkEveryNLoops.intValue > 1)
                    {
                        str = "First "+checkEveryNLoops.intValue.ToString() + " loop iterations.";
                    }
                    EditorGUILayout.LabelField(str);
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel = oldIndent;

                    if (checkEveryNLoops.intValue > 1)
                    {
                        EditorGUILayout.HelpBox("Click on the buttons below to select iterations that trigger a reroll.\nAfter the last iteration, the sequence will repeat.", MessageType.Info);
                        int initSequence = loopSequence.intValue;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(2*fakeIndent);
                        bool changed = false;
                        for (int i = 0; i < checkEveryNLoops.intValue; i++)
                        {
                            bool isBitOn = (initSequence & (1 << i)) != 0;
                            if (GUILayout.Button(isBitOn?"R":"", EditorStyles.miniButton, GUILayout.MaxWidth(20)))
                            {
                                changed = true;
                                if (isBitOn) initSequence &= ~(1 << i);
                                else initSequence |= (1 << i);
                            }
                            if (i % 8 == 7)
                            {
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(2*fakeIndent);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        if (changed) loopSequence.intValue = initSequence;
                    }

                    EditorGUI.indentLevel -= 2;
                }


                EditorGUI.indentLevel -= 2;
                GUILayout.Space(8);
            }
        }

        void DoInterpolationValueGUILayout(bool allowCombining)
        {
            GUIContent calcMethodGC = new GUIContent("Calculation Method", "How is the interpolation chosen from 0 to 1?");

            // In Patterns only : prompt user for "when to reroll the parameter".
            // Not drawn in "equal to parameter" cases, because it's already displayed in the upper part of the window.
            if (valueType.enumValueIndex != (int)DynamicParameterSorting.EqualToParameter)
                RerollFrequencyField();

            if (allowCombining)
                EditorGUILayout.PropertyField(interpolationFactor, calcMethodGC);
            else
            {
                GUIContent[] opts = new GUIContent[] { new GUIContent("Random"), new GUIContent("Bullet Hierarchy"), new GUIContent("Global Parameter") };
                interpolationFactor.enumValueIndex = EditorGUILayout.Popup(calcMethodGC, interpolationFactor.enumValueIndex, opts);
            }

            if (interpolationFactor.enumValueIndex == (int)InterpolationFactor.Random)
            {
                EditorGUILayout.PropertyField(shareValueBetweenInstances, new GUIContent("Share Random Seed", "If checked, the same random seed will be used across different instances of the object."));
                if (shareValueBetweenInstances.boolValue)
                {
                    EditorGUI.indentLevel += 2;
                    string shareStr = "Among bullets issued from same instance of:"; // Bullet AND Shot both say "bullet", makes things clearer
                    if (serObj.targetObject is PatternParams)
                        shareStr = "Among patterns issued from same instance of:";
                    EditorGUILayout.LabelField(shareStr);
                    BulletHierarchyObject bho = BulletHierarchyField(1, 34);
                    if (bho == BulletHierarchyObject.Pattern && (serObj.targetObject is ShotParams))
                        EditorGUILayout.PropertyField(differentValuesPerShot, new GUIContent("Reroll a value on every Shot", "Reroll a value on every Shot"));
                    EditorGUI.indentLevel -= 2;
                }
            }

            EditorGUI.BeginChangeCheck();

            if (interpolationFactor.enumValueIndex == (int)InterpolationFactor.BulletHierarchy)
            {
                EditorGUILayout.LabelField("State of the following object will be checked:");
                BulletHierarchyObject bho = BulletHierarchyField(0, 5);
                GUIContent tempGC = new GUIContent("Relevant Criteria", "Which part of this object will yield the interpolation value?");
                if (bho == BulletHierarchyObject.Bullet)
                {
                    EditorGUILayout.PropertyField(interpolationFactorFromBullet, tempGC);
                    if (interpolationFactorFromBullet.enumValueIndex == (int)BulletInterpolationFactor.CustomParameter)
                    {
                        GUIContent paramNameGC = new GUIContent("Parameter Name", "Name of the custom float or slider parameter, as set in Emitter Profile.");
                        EditorGUILayout.PropertyField(parameterName, paramNameGC);                    
                        // Commented out : delayed field returns null if window gets closed earlier
                        //EditorGUILayout.DelayedTextField(parameterName, paramNameGC);                    
                    }
                    else if (interpolationFactorFromBullet.enumValueIndex == (int)BulletInterpolationFactor.TimeSinceAlive)
                    {
                        EditorGUILayout.PropertyField(period);
                        EditorGUILayout.PropertyField(wrapMode);
                        if (period.floatValue <= 0) period.floatValue = 0.001f;
                    }
                    else if (interpolationFactorFromBullet.enumValueIndex != (int)BulletInterpolationFactor.PositionInShot) // means rotation
                    {
                        GUIContent paramNameGC = new GUIContent("Wrap Point", "The rotation value that corresponds to both 0 and 1 in the curve.");
                        EditorGUILayout.PropertyField(wrapPoint, paramNameGC);
                        paramNameGC = new GUIContent("Count Clockwise", "If true, values are mapped from 0 to 1 clockwise. If false, counter-clockwise.");
                        EditorGUILayout.PropertyField(countClockwise, paramNameGC);                    
                    }
                    else // means position
                    {
                        if (previewTex == null) RefreshPreviewTexture();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(new GUIContent("Sort Mode", "How the bullet positions will be sorted from 0 to 1."), GUILayout.MaxWidth(EditorGUIUtility.labelWidth-4));
                        EditorGUILayout.PropertyField(sortMode, GUIContent.none);
                        EditorGUILayout.PropertyField(sortDirection, GUIContent.none);
                        EditorGUILayout.EndHorizontal();
                        if (sortMode.enumValueIndex == (int)BulletPositionSortMode.Radial)
                        {
                            EditorGUILayout.PropertyField(centerX);
                            EditorGUILayout.PropertyField(centerY);
                            EditorGUILayout.PropertyField(radius);
                        }
                        else if (sortMode.enumValueIndex == (int)BulletPositionSortMode.Texture)
                        {
                            GUIContent texGC = new GUIContent("Repartition Texture", "A grayscale map indicating a value from 0 to 1 for every region in the shot.");
                            EditorGUILayout.PropertyField(repartitionTexture, texGC);
                        }

                        GUIStyle texStyle = new GUIStyle((GUIStyle)"box");
                        texStyle.normal.background = previewTex;
                        
                        EditorGUILayout.LabelField("Sorting Visualizer: (0 = black, 1 = white)");
                        float previewSide = 128;
                        float availableSpace = position.width - (leftMargin - headerHorizontalOffset + (hasScrolling ? rightMargin : rightMarginWithoutScroll));
                        float tempMargin = (availableSpace-previewSide)*0.5f;

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(tempMargin);
                        EditorGUILayout.BeginVertical(texStyle, GUILayout.Width(previewSide), GUILayout.Height(previewSide));
                        EditorGUILayout.Space();
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(tempMargin);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                if (bho == BulletHierarchyObject.Shot)
                {
                    EditorGUILayout.PropertyField(interpolationFactorFromShot, tempGC);
                    EditorGUILayout.PropertyField(period);
                    EditorGUILayout.PropertyField(wrapMode);
                    if (period.floatValue <= 0) period.floatValue = 0.001f;
                }
                if (bho == BulletHierarchyObject.Pattern)
                {
                    EditorGUILayout.PropertyField(interpolationFactorFromPattern, tempGC);
                    EditorGUILayout.PropertyField(period);
                    EditorGUILayout.PropertyField(wrapMode);
                    if (period.floatValue <= 0) period.floatValue = 0.001f;
                }
            }

            if (interpolationFactor.enumValueIndex == (int)InterpolationFactor.GlobalParameter)
            {
                GUIContent paramNameGC = new GUIContent("Parameter Name", "Name of the global float or slider parameter, as set in the Manager.");
                EditorGUILayout.PropertyField(parameterName, paramNameGC);
                // Commented out : delayed field returns null if window gets closed earlier
                //EditorGUILayout.DelayedTextField(parameterName, paramNameGC);                    
            }

            if (interpolationFactor.enumValueIndex == (int)InterpolationFactor.CombineFactors)
            {
                EditorGUILayout.LabelField("(WIP)");                  
            }

            GUIContent repartitionCurveGC = new GUIContent("Repartition Curve", "This curve can be edited to remap values from 0 to 1.");
            EditorGUILayout.PropertyField(repartitionCurve, repartitionCurveGC);
            serObj.ApplyModifiedProperties();
            bool hasError = !BulletCurveDrawer.GoesFromZeroToOne(repartitionCurve, true);
			//if (hasError)
			{
				// (Indirectly) due to Unity's Issue #930156, we have to display an extra line.
				// The line "if (hasError)" above can be uncommented to look nicer, but Unity will occasionally throw harmless exceptions.
				EditorGUI.BeginDisabledGroup(!hasError);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(hasError?"X-axis must go from 0 to 1.":"Your curve has no error.", hasError?EditorStyles.boldLabel:EditorStyles.label);
				Color defC = GUI.color;
				if (hasError) GUI.color = new Color(1.0f, 0.6f, 0.4f, 1f);
				if (GUILayout.Button("Click to Fix Curve", EditorStyles.miniButton)) BulletCurveDrawer.RepairCurveFromZeroToOne(repartitionCurve, true);
				GUI.color = defC;
				EditorGUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
			}

            if (EditorGUI.EndChangeCheck()) RefreshPreviewTexture();
        }

        // Drawing the control for selecting "X objects above". Returns the object type.
        BulletHierarchyObject BulletHierarchyField(int minimumAllowed, float indent)
        {
            BulletHierarchyObject result = BulletHierarchyObject.None;

            if (relativeTo.intValue < minimumAllowed)
                relativeTo.intValue = minimumAllowed;

            // helpers
            float buttonWidth = 24f;
            float space = 5f;
            bool typeError = false;
            bool amountError = false;
            bool isBehaviour = false;

            // string builder's base
            string targetName = serObj.targetObject.name;
            string str = "This object";
            if (relativeTo.intValue > 0)
                str = relativeTo.intValue.ToString() + (relativeTo.intValue>1?" objects above":" object above");

            // getting ancestor's name if applicable
            if (serObj.targetObject is EmissionParams)
            {
                int counter = relativeTo.intValue;
                EmissionParams curObj = serObj.targetObject as EmissionParams;
                while (counter > 0)
                {
                    curObj = curObj.parent;
                    if (curObj == null)
                    {
                        amountError = true;
                        break;
                    }
                    targetName = curObj.name;
                    counter--;
                }
            }
            else if (serObj.targetObject is BaseBulletBehaviour) isBehaviour = true;
            else typeError = true;

            // string building
            if (serObj.targetObject is BulletParams || isBehaviour)
            {
                if (relativeTo.intValue % 3 == 0) str += " (Bullet";
                else if (relativeTo.intValue % 3 == 1) str += " (Shot";
                else if (relativeTo.intValue % 3 == 2) str += " (Pattern";
            }
            else if (serObj.targetObject is ShotParams)
            {
                if (relativeTo.intValue % 3 == 0) str += " (Shot";
                else if (relativeTo.intValue % 3 == 1) str += " (Pattern";
                else if (relativeTo.intValue % 3 == 2) str += " (Bullet";
            }
            else if (serObj.targetObject is PatternParams)
            {
                if (relativeTo.intValue % 3 == 0) str += " (Pattern";
                else if (relativeTo.intValue % 3 == 1) str += " (Bullet";
                else if (relativeTo.intValue % 3 == 2) str += " (Shot";
            }
            else str += "."; // that's if (typeError)

            if (!typeError)
            {
                if (str.Contains("Bullet")) result = BulletHierarchyObject.Bullet;
                if (str.Contains("Shot")) result = BulletHierarchyObject.Shot;
                if (str.Contains("Pattern")) result = BulletHierarchyObject.Pattern;

                if (amountError || isBehaviour) str += ")";
                else str += " \""+targetName+"\")";
            }
            
            // actual controls
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            EditorGUI.BeginDisabledGroup(relativeTo.intValue == minimumAllowed);
            if (GUILayout.Button("-1", EditorStyles.miniButtonLeft, GUILayout.MaxWidth(buttonWidth))) relativeTo.intValue--;
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("+1", EditorStyles.miniButtonRight, GUILayout.MaxWidth(buttonWidth))) relativeTo.intValue++;
            GUILayout.Space(space);
            EditorGUILayout.LabelField(str);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel = oldIndent;

            // displaying errors, if any
            string errorStr = "";
            if (typeError) errorStr = "This feature is meant for Emitter Profiles and Bullet Behaviours. It's likely your parameter will not have a Bullet Hierarchy.";
            else if (amountError) errorStr = "\""+relativeTo.intValue.ToString()+" object"+(relativeTo.intValue > 1 ? "s":"")+" above\" goes beyond the root of this Bullet Hierarchy. It's likely an error.";
            if (!string.IsNullOrEmpty(errorStr)) EditorGUILayout.HelpBox(errorStr, MessageType.Warning);

            return result;
        }

        // In case the value is position-based, draw it on a texture
        void RefreshPreviewTexture()
        {
            if (interpolationFactor.enumValueIndex != (int)InterpolationFactor.BulletHierarchy) return;
            if (interpolationFactorFromBullet.enumValueIndex != (int)BulletInterpolationFactor.PositionInShot) return;

            int w = 128;
            int h = 128;
            previewTex = new Texture2D (w, h);
            bool descending = sortDirection.enumValueIndex == (int)BulletSortDirection.Descending;

            if (sortMode.enumValueIndex == (int)BulletPositionSortMode.Horizontal)
            {
                for (int i = 0; i < previewTex.width; i++)
                    for (int j = 0; j < previewTex.height; j++)
                    {
                        // border
                        if (i < 2 || i > w-3 || j < 2 || j > h-3)
                        {
                            previewTex.SetPixel(i, j, Color.black);
                            continue;
                        }

                        float gs = ((float)i)/(float)previewTex.width;
                        if (descending) gs = 1f-gs;
                        gs = repartitionCurve.animationCurveValue.Evaluate(gs);
                        previewTex.SetPixel(i, j, new Color(gs, gs, gs, 1f));
                    }
            }

            if (sortMode.enumValueIndex == (int)BulletPositionSortMode.Vertical)
            {
                for (int i = 0; i < previewTex.width; i++)
                    for (int j = 0; j < previewTex.height; j++)
                    {
                        // border
                        if (i < 2 || i > w-3 || j < 2 || j > h-3)
                        {
                            previewTex.SetPixel(i, j, Color.black);
                            continue;
                        }

                        float gs = ((float)j)/(float)previewTex.width;
                        if (descending) gs = 1f-gs;
                        gs = repartitionCurve.animationCurveValue.Evaluate(gs);
                        previewTex.SetPixel(i, j, new Color(gs, gs, gs, 1));
                    }
            }

            if (sortMode.enumValueIndex == (int)BulletPositionSortMode.Radial)
            {
                for (int i = 0; i < previewTex.width; i++)
                    for (int j = 0; j < previewTex.height; j++)
                    {
                        // border
                        if (i < 2 || i > w-3 || j < 2 || j > h-3)
                        {
                            previewTex.SetPixel(i, j, Color.black);
                            continue;
                        }
                        
                        float distX = (float)i / (float)w - centerX.floatValue;
						float distY = (float)j / (float)h - centerY.floatValue;
						float dist2 = distX * distX + distY * distY;
						float dist = Mathf.Pow(dist2, 0.5f);
                        float gs = Mathf.Clamp01(dist / radius.floatValue);
						if (descending) gs = 1f-gs;
                        gs = repartitionCurve.animationCurveValue.Evaluate(gs);
                        previewTex.SetPixel(i, j, new Color(gs, gs, gs, 1));
                    }
            }

            if (sortMode.enumValueIndex == (int)BulletPositionSortMode.Texture)
            {
                if (repartitionTexture.objectReferenceValue == null)
                {
                    for (int i = 0; i < previewTex.width; i++)
                        for (int j = 0; j < previewTex.height; j++)
                        {
                            // border
                            if (i < 2 || i > w-3 || j < 2 || j > h-3)
                            {
                                previewTex.SetPixel(i, j, Color.black);
                                continue;
                            }

                            float gs = descending ? 1f : 0f;
                            gs = repartitionCurve.animationCurveValue.Evaluate(gs);
                            previewTex.SetPixel(i, j, new Color(gs, gs, gs, 1));
                        }
                }
                else
                {
                    Texture2D refTex = repartitionTexture.objectReferenceValue as Texture2D;
                    for (int i = 0; i < previewTex.width; i++)
                        for (int j = 0; j < previewTex.height; j++)
                        {
                            // border
                            if (i < 2 || i > w-3 || j < 2 || j > h-3)
                            {
                                previewTex.SetPixel(i, j, Color.black);
                                continue;
                            }

                            Color basePx = refTex.GetPixel(i*refTex.width/w, j*refTex.height/h);
                            float gs = basePx.r * 0.3f + basePx.g * 0.59f + basePx.b * 0.11f;
                            if (descending) gs = 1-gs;
                            gs = repartitionCurve.animationCurveValue.Evaluate(gs);
                            previewTex.SetPixel(i, j, new Color(gs, gs, gs, 1));                             
                        }
                }
            }

            previewTex.Apply();
        }

        public Texture2D Monochrome(Color col)
        {
            Texture2D result = new Texture2D(1,1);
            result.SetPixel(0,0,col);
            result.Apply();
            return result;
        }

        #endregion
    }
}
