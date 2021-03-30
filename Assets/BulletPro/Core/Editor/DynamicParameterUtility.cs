using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
    // A bunch of helper functions that read SerializedProperties as DynamicParameters.
    public static class DynamicParameterUtility
    {
        // Returns false if parameter isn't fully initialized and needs one more frame
        public static bool IsInitialized(SerializedProperty prop)
        {
            if (prop == null) return false;

            // using iterator yields performance gains
            SerializedProperty valueTree = prop.Copy(); valueTree.NextVisible(true);
            //SerializedProperty valueTree = prop.FindPropertyRelative("valueTree");

            if (valueTree == null) return false;
            return valueTree.arraySize > 1;
        }

        // Is a parameter pointing to a fixed value?
        public static bool IsFixed(SerializedProperty prop)
        {
            // with iterator (slight performance gain)
            /* */
            SerializedProperty spIterator = prop.Copy();
            spIterator.NextVisible(true); // value tree
            if (spIterator.arraySize < 2) return false;
            SerializedProperty val = spIterator.GetArrayElementAtIndex(1); // value
            val.NextVisible(true); // settings
            val.NextVisible(true); // valueType
            return val.enumValueIndex == (int)DynamicParameterSorting.Fixed;
            /* */

            // without iterator
            /* *
            if (!IsInitialized(prop)) return false;
            SerializedProperty valueTree = prop.FindPropertyRelative("valueTree");
            SerializedProperty settings = valueTree.GetArrayElementAtIndex(1).FindPropertyRelative("settings");
            return settings.FindPropertyRelative("valueType").enumValueIndex == (int)DynamicParameterSorting.Fixed;
            /* */
        }

        // Is <value index> part of a blend ? Returns index in blend list, -1 if not part of blend. Assumes "from-to" does not exist.
        public static int GetPositionOfBlendIndex(SerializedProperty prop, int indexToLocate, int startingSearchIndex = 1)
        {
            SerializedProperty valueTree = prop.FindPropertyRelative("valueTree");
            SerializedProperty valueProp = valueTree.GetArrayElementAtIndex(startingSearchIndex);
            SerializedProperty settings = valueProp.FindPropertyRelative("settings");
            SerializedProperty valueType = settings.FindPropertyRelative("valueType");
            // if fixed, this is simply not what we're looking for
            if (valueType.enumValueIndex == (int)DynamicParameterSorting.Fixed)
            {
                return -1;
            }
            else // if blend
            {
                SerializedProperty indexOfBlendedChildren = settings.FindPropertyRelative("indexOfBlendedChildren");
                if (indexOfBlendedChildren.arraySize == 0) return -1;
                for (int i = 0; i < indexOfBlendedChildren.arraySize; i++)
                {
                    int blendIndex = indexOfBlendedChildren.GetArrayElementAtIndex(i).intValue;
                    if (blendIndex == indexToLocate) return i;

                    // we didn't locate the index but since we found a blend, we can dig deeper
                    int resultOfSubSearch = GetPositionOfBlendIndex(prop, indexToLocate, blendIndex);
                    if (resultOfSubSearch > -1) return resultOfSubSearch;
                    else continue;
                }
            }

            // if we reached this, it means the index has not been located
            return -1;
        }

        // Gets bool value if fixed, otherwise return bool provided in arguments
        public static bool GetBool(SerializedProperty prop, bool valueIfError=false, bool valueIfNonFixed=true)
        {
            // with iterator (better perfs)
            SerializedProperty spIterator = prop.Copy();
            spIterator.NextVisible(true); // value tree
            if (spIterator.arraySize < 2) return valueIfError;
            SerializedProperty val = spIterator.GetArrayElementAtIndex(1); // value
            val.NextVisible(true); // settings
            SerializedProperty defVal = val.Copy();
            val.NextVisible(true); // valueType
            if (val.enumValueIndex != (int)DynamicParameterSorting.Fixed) return valueIfNonFixed;
            defVal.NextVisible(false); // default value
            return defVal.boolValue;
                        
            // without iterator
            /* *
            if (!IsInitialized(prop)) return valueIfError;
            if (!IsFixed(prop)) return valueIfNonFixed;
            SerializedProperty valueTree = prop.FindPropertyRelative("valueTree");
            return valueTree.GetArrayElementAtIndex(1).FindPropertyRelative("defaultValue").boolValue;
            /* */
        }

        // Overload with no serialized property
        public static bool GetBool(DynamicBool dynBool, bool valueIfError=false, bool valueIfNonFixed=true)
        {
            if (dynBool.valueTree == null) return valueIfError;
            if (dynBool.valueTree.Length == 0) return valueIfError;
            DynamicBoolValue root = dynBool.root;
            if (root.settings.valueType == DynamicParameterSorting.Fixed)
                return root.defaultValue;
            else return valueIfNonFixed;
        }

        // Gets float value if fixed, 0 otherwise
        public static float GetFixedFloat(SerializedProperty prop)
        {
            if (!IsFixed(prop)) return 0;

            SerializedProperty valueTree = prop.FindPropertyRelative("valueTree");
            return valueTree.GetArrayElementAtIndex(1).FindPropertyRelative("defaultValue").floatValue;
        }

        // Clamps all float values in tree to be above zero
        public static void ClampAboveZero(SerializedProperty dynParameter, int indexInTree=1)
        {
            if (!IsInitialized(dynParameter)) return;

            // with iterator
            /* */
            SerializedProperty spIterator = dynParameter.Copy();
            spIterator.NextVisible(true); // valuetree
            spIterator = spIterator.GetArrayElementAtIndex(indexInTree); // value
            spIterator.NextVisible(true); // settings
            SerializedProperty spIteratorTwo = spIterator.Copy(); // save settings for branching
            spIterator.NextVisible(false); // default value
            if (spIterator.floatValue < 0) spIterator.floatValue = 0; // clamp
            spIteratorTwo.NextVisible(true); // valuetype
            if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.FromAToB)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                ClampAboveZero(dynParameter, spIteratorTwo.intValue);
                spIteratorTwo.NextVisible(false); // index of To                
                ClampAboveZero(dynParameter, spIteratorTwo.intValue);
            }
            else if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.Blend)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                spIteratorTwo.NextVisible(false); // index of To
                spIteratorTwo.NextVisible(false); // index of blended children
                if (spIteratorTwo.arraySize == 0) return;
                for (int i = 0; i < spIteratorTwo.arraySize; i++)
                    ClampAboveZero(dynParameter, spIteratorTwo.GetArrayElementAtIndex(i).intValue);
            }
            /* */
            
            // without iterator
            /* *
            SerializedProperty valueTree = dynParameter.FindPropertyRelative("valueTree");
            SerializedProperty dynValue = valueTree.GetArrayElementAtIndex(indexInTree);
            SerializedProperty floatValue = dynValue.FindPropertyRelative("defaultValue");
            if (floatValue.floatValue < 0) floatValue.floatValue = 0;

            SerializedProperty settings = dynValue.FindPropertyRelative("settings");
            SerializedProperty valueType = settings.FindPropertyRelative("valueType");
            if (valueType.enumValueIndex == (int)DynamicParameterSorting.FromAToB)
            {
                ClampValueAboveZero(dynParameter, settings.FindPropertyRelative("indexOfFrom").intValue);
                ClampValueAboveZero(dynParameter, settings.FindPropertyRelative("indexOfTo").intValue);
            }
            else if (valueType.enumValueIndex == (int)DynamicParameterSorting.Blend)
            {
                SerializedProperty blended = settings.FindPropertyRelative("indexOfBlendedChildren");
                if (blended.arraySize == 0) return;
                for (int i = 0; i < blended.arraySize; i++)
                    ClampValueAboveZero(dynParameter, blended.GetArrayElementAtIndex(i).intValue);
            }
            /* */
        }

        public static void ClampIntAboveZero(SerializedProperty dynParameter, int indexInTree=1)
        {
            if (!IsInitialized(dynParameter)) return;

            SerializedProperty spIterator = dynParameter.Copy();
            spIterator.NextVisible(true); // valuetree
            spIterator = spIterator.GetArrayElementAtIndex(indexInTree); // value
            spIterator.NextVisible(true); // settings
            SerializedProperty spIteratorTwo = spIterator.Copy(); // save settings for branching
            spIterator.NextVisible(false); // default value
            if (spIterator.intValue < 0) spIterator.intValue = 0; // clamp
            spIteratorTwo.NextVisible(true); // valuetype
            if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.FromAToB)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                ClampIntAboveZero(dynParameter, spIteratorTwo.intValue);
                spIteratorTwo.NextVisible(false); // index of To                
                ClampIntAboveZero(dynParameter, spIteratorTwo.intValue);
            }
            else if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.Blend)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                spIteratorTwo.NextVisible(false); // index of To
                spIteratorTwo.NextVisible(false); // index of blended children
                if (spIteratorTwo.arraySize == 0) return;
                for (int i = 0; i < spIteratorTwo.arraySize; i++)
                    ClampIntAboveZero(dynParameter, spIteratorTwo.GetArrayElementAtIndex(i).intValue);
            }
        }

        public static int GetHighestValueOfInt(SerializedProperty dynParameter, bool clampAboveZero, int indexInTree=1)
        {
            SerializedProperty spIterator = dynParameter.Copy();
            spIterator.NextVisible(true); // valuetree
            
            SerializedProperty spIteratorOne = spIterator.GetArrayElementAtIndex(indexInTree); // value
            spIteratorOne.NextVisible(true); // settings
            SerializedProperty spIteratorTwo = spIteratorOne.Copy(); // save settings for branching
            spIteratorOne.NextVisible(false); // default value
            if (clampAboveZero && spIteratorOne.intValue < 0) spIteratorOne.intValue = 0;
            int fixedVal = spIteratorOne.intValue;
            spIteratorTwo.NextVisible(true); // valuetype
            if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.Fixed)
                return fixedVal;
            else if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.FromAToB)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                int fromVal = GetHighestValueOfInt(dynParameter, true, spIteratorTwo.intValue);
                spIteratorTwo.NextVisible(false); // index of To                
                int toVal = GetHighestValueOfInt(dynParameter, true, spIteratorTwo.intValue);
                return Mathf.Max(fromVal, toVal);
            }
            else// implying if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.Blend)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                spIteratorTwo.NextVisible(false); // index of To
                spIteratorTwo.NextVisible(false); // index of blended children
                if (spIteratorTwo.arraySize == 0) return 0;
                int result = 0;
                for (int i = 0; i < spIteratorTwo.arraySize; i++)
                {
                    int val = GetHighestValueOfInt(dynParameter, true, spIteratorTwo.GetArrayElementAtIndex(i).intValue);
                    result = Mathf.Max(result, val);
                }
                return result;
            }
        }

        public static float GetAverageFloatValue(SerializedProperty dynParameter, int indexInTree=1)
        {
            SerializedProperty spIterator = dynParameter.Copy();
            spIterator.NextVisible(true); // valuetree
            
            SerializedProperty spIteratorOne = spIterator.GetArrayElementAtIndex(indexInTree); // value
            spIteratorOne.NextVisible(true); // settings
            SerializedProperty spIteratorTwo = spIteratorOne.Copy(); // save settings for branching
            spIteratorOne.NextVisible(false); // default value
            float fixedVal = spIteratorOne.floatValue;
            spIteratorTwo.NextVisible(true); // valuetype
            if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.Fixed)
                return fixedVal;
            else if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.FromAToB)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                float fromVal = GetAverageFloatValue(dynParameter, spIteratorTwo.intValue);
                spIteratorTwo.NextVisible(false); // index of To                
                float toVal = GetAverageFloatValue(dynParameter, spIteratorTwo.intValue);
                return (fromVal + toVal) * 0.5f;
            }
            else// implying if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.Blend)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                spIteratorTwo.NextVisible(false); // index of To
                spIteratorTwo.NextVisible(false); // index of blended children
                if (spIteratorTwo.arraySize == 0) return 0;
                float sum = 0;
                for (int i = 0; i < spIteratorTwo.arraySize; i++)
                {
                    float val = GetAverageFloatValue(dynParameter, spIteratorTwo.GetArrayElementAtIndex(i).intValue);
                    sum += val;
                }
                return sum / (float)spIteratorTwo.arraySize;
            }
        }

        public static bool IsAlwaysAboveZero(SerializedProperty dynParameter, int indexInTree=1)
        {
            SerializedProperty spIterator = dynParameter.Copy();
            spIterator.NextVisible(true); // valuetree
            
            SerializedProperty spIteratorOne = spIterator.GetArrayElementAtIndex(indexInTree); // value
            spIteratorOne.NextVisible(true); // settings
            SerializedProperty spIteratorTwo = spIteratorOne.Copy(); // save settings for branching
            spIteratorOne.NextVisible(false); // default value
            float fixedVal = spIteratorOne.floatValue;
            spIteratorTwo.NextVisible(true); // valuetype
            if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.Fixed)
                return fixedVal > 0;
            else if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.FromAToB)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                bool fromVal = IsAlwaysAboveZero(dynParameter, spIteratorTwo.intValue);
                spIteratorTwo.NextVisible(false); // index of To                
                bool toVal = IsAlwaysAboveZero(dynParameter, spIteratorTwo.intValue);
                return fromVal && toVal;
            }
            else// implying if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.Blend)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                spIteratorTwo.NextVisible(false); // index of To
                spIteratorTwo.NextVisible(false); // index of blended children
                if (spIteratorTwo.arraySize == 0) return fixedVal > 0;
                for (int i = 0; i < spIteratorTwo.arraySize; i++)
                {
                    bool val = IsAlwaysAboveZero(dynParameter, spIteratorTwo.GetArrayElementAtIndex(i).intValue);
                    if (!val) return false;
                }
                return true;
            }
        }

        // For DynamicBools only
        public static bool CanBeTrue(SerializedProperty dynParameter, int indexInTree=1)
        {
            if (!IsInitialized(dynParameter)) return false;

            SerializedProperty spIterator = dynParameter.Copy();
            spIterator.NextVisible(true); // valuetree
            
            SerializedProperty spIteratorOne = spIterator.GetArrayElementAtIndex(indexInTree); // value
            spIteratorOne.NextVisible(true); // settings
            SerializedProperty spIteratorTwo = spIteratorOne.Copy(); // save settings for branching
            spIteratorOne.NextVisible(false); // default value
            bool fixedVal = spIteratorOne.boolValue;
            spIteratorTwo.NextVisible(true); // valuetype
            if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.Fixed)
                return fixedVal;
            else if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.FromAToB)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                bool fromVal = CanBeTrue(dynParameter, spIteratorTwo.intValue);
                spIteratorTwo.NextVisible(false); // index of To                
                bool toVal = CanBeTrue(dynParameter, spIteratorTwo.intValue);
                return fromVal || toVal;
            }
            else// implying if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.Blend)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                spIteratorTwo.NextVisible(false); // index of To
                spIteratorTwo.NextVisible(false); // index of blended children
                if (spIteratorTwo.arraySize == 0) return fixedVal;
                for (int i = 0; i < spIteratorTwo.arraySize; i++)
                {
                    bool val = CanBeTrue(dynParameter, spIteratorTwo.GetArrayElementAtIndex(i).intValue);
                    if (val) return true;
                }
                return false;
            }
        }

        public static bool CanBeFalse(SerializedProperty dynParameter, int indexInTree=1)
        {
            if (!IsInitialized(dynParameter)) return false;

            SerializedProperty spIterator = dynParameter.Copy();
            spIterator.NextVisible(true); // valuetree
            
            SerializedProperty spIteratorOne = spIterator.GetArrayElementAtIndex(indexInTree); // value
            spIteratorOne.NextVisible(true); // settings
            SerializedProperty spIteratorTwo = spIteratorOne.Copy(); // save settings for branching
            spIteratorOne.NextVisible(false); // default value
            bool fixedVal = spIteratorOne.boolValue;
            spIteratorTwo.NextVisible(true); // valuetype
            if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.Fixed)
                return !fixedVal;
            else if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.FromAToB)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                bool fromVal = CanBeFalse(dynParameter, spIteratorTwo.intValue);
                spIteratorTwo.NextVisible(false); // index of To                
                bool toVal = CanBeFalse(dynParameter, spIteratorTwo.intValue);
                return fromVal || toVal;
            }
            else// implying if (spIteratorTwo.enumValueIndex == (int)DynamicParameterSorting.Blend)
            {
                spIteratorTwo.NextVisible(false); // interpolation value
                spIteratorTwo.NextVisible(false); // combined parameters
                spIteratorTwo.NextVisible(false); // index of self
                spIteratorTwo.NextVisible(false); // index of parent                
                spIteratorTwo.NextVisible(false); // index of From                
                spIteratorTwo.NextVisible(false); // index of To
                spIteratorTwo.NextVisible(false); // index of blended children
                if (spIteratorTwo.arraySize == 0) return fixedVal;
                for (int i = 0; i < spIteratorTwo.arraySize; i++)
                {
                    bool val = CanBeFalse(dynParameter, spIteratorTwo.GetArrayElementAtIndex(i).intValue);
                    if (val) return true;
                }
                return false;
            }
        }

        // An overload that does not use serialized properties
        public static float GetAverageFloatValue(DynamicFloat dynFloat, int indexInTree=1)
        {
            DynamicFloatValue val = dynFloat[indexInTree];
            DynamicParameterSettings settings = val.settings;
            if (settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;
            else if (settings.valueType == DynamicParameterSorting.FromAToB)
            {
                DynamicFloatValue fromVal = dynFloat[settings.indexOfFrom];
                DynamicFloatValue toVal = dynFloat[settings.indexOfTo];
                return (fromVal.defaultValue + toVal.defaultValue) * 0.5f;
            }
            else
            {
                if (settings.indexOfBlendedChildren == null) return val.defaultValue;
                else if (settings.indexOfBlendedChildren.Length == 0) return val.defaultValue;
                else
                {
                    float sum = 0;
                    float sumOfWeights = 0;
                    for (int i = 0; i < settings.indexOfBlendedChildren.Length; i++)
                    {
                        DynamicFloatValue blendedVal = dynFloat[settings.indexOfBlendedChildren[i]];
                        sum += blendedVal.defaultValue * blendedVal.settings.weight;
                        sumOfWeights += blendedVal.settings.weight;
                    }
                    if (sumOfWeights == 0) return val.defaultValue;
                    return sum / sumOfWeights;
                }
            }
        }

        public static Vector2 GetAverageVector2Value(DynamicVector2 dynVector, int indexInTree = 1)
        {
            DynamicVector2Value val = dynVector[indexInTree];
            DynamicParameterSettings settings = val.settings;
            if (settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;
            else if (settings.valueType == DynamicParameterSorting.FromAToB)
            {
                DynamicVector2Value fromVal = dynVector[settings.indexOfFrom];
                DynamicVector2Value toVal = dynVector[settings.indexOfTo];
                return (fromVal.defaultValue + toVal.defaultValue) * 0.5f;
            }
            else
            {
                if (settings.indexOfBlendedChildren == null) return val.defaultValue;
                else if (settings.indexOfBlendedChildren.Length == 0) return val.defaultValue;
                else
                {
                    Vector2 sum = Vector2.zero;
                    float sumOfWeights = 0;
                    for (int i = 0; i < settings.indexOfBlendedChildren.Length; i++)
                    {
                        DynamicVector2Value blendedVal = dynVector[settings.indexOfBlendedChildren[i]];
                        sum += blendedVal.defaultValue * blendedVal.settings.weight;
                        sumOfWeights += blendedVal.settings.weight;
                    }
                    if (sumOfWeights == 0) return val.defaultValue;
                    return sum / sumOfWeights;
                }
            }
        }

        // Gets object reference value if fixed.
        public static Object GetFixedObject(SerializedProperty prop)
        {
            if (!IsInitialized(prop)) return null;

            SerializedProperty valueTree = prop.FindPropertyRelative("valueTree");
            return valueTree.GetArrayElementAtIndex(1).FindPropertyRelative("defaultValue").objectReferenceValue;
        }

        #region setters

        // Sets up the value tree when needed. Returns the value tree.
        public static SerializedProperty InitializeTree(SerializedProperty prop)
        {
            SerializedProperty vTree = prop.FindPropertyRelative("valueTree");
            vTree.arraySize = 2;
            SerializedProperty rootVal = vTree.GetArrayElementAtIndex(1);
            SerializedProperty settings = rootVal.FindPropertyRelative("settings");
            settings.FindPropertyRelative("index").intValue = 1;
            settings.FindPropertyRelative("interpolationValue").FindPropertyRelative("repartitionCurve").animationCurveValue = AnimationCurve.Linear(0,0,1,1);
            return vTree;
        }

        // For DynamicObjectReferences only
        public static void SetObjectNarrowType(SerializedProperty prop, System.Type tName)
        {
            prop.FindPropertyRelative("narrowType").boolValue = true;
            prop.FindPropertyRelative("typeName").stringValue = tName.AssemblyQualifiedName;
        }

        // For DynamicEnums only
        public static void SetEnumType(SerializedProperty prop, System.Type tName)
        {
            string enumTypeName = tName.AssemblyQualifiedName;
            string[] enumOptions = System.Enum.GetNames(tName);

            // Exceptional fix for WrapMode because Unity somehow scrambles the strings
            if (tName == typeof(WrapMode)) enumOptions = new string[] { "Default", "Clamp", "Loop", "Clamp Forever", "Ping Pong"};            

            prop.FindPropertyRelative("enumTypeName").stringValue = enumTypeName;
            SerializedProperty propOptions = prop.FindPropertyRelative("enumOptions");
            propOptions.arraySize = enumOptions.Length;
            for (int i = 0; i < enumOptions.Length; i++)
                propOptions.GetArrayElementAtIndex(i).stringValue = enumOptions[i];
        }

        // Changes fixed value to match one object in particular 
        public static void SetFixedObject(SerializedProperty prop, Object newVal, bool initializeIfNeeded=false)
        {
            SerializedProperty valueTree = null;
            if (!IsInitialized(prop))
            {
                if (initializeIfNeeded) valueTree = InitializeTree(prop);
                else return;
            }
            else if (initializeIfNeeded) valueTree = InitializeTree(prop);
            else valueTree = prop.FindPropertyRelative("valueTree");
            SerializedProperty rootValue = valueTree.GetArrayElementAtIndex(1);
            rootValue.FindPropertyRelative("settings").FindPropertyRelative("valueType").enumValueIndex = (int)DynamicParameterSorting.Fixed;
            rootValue.FindPropertyRelative("defaultValue").objectReferenceValue = newVal;
        }

        public static void SetFixedInt(SerializedProperty prop, int newVal, bool initializeIfNeeded=false)
        {
            SerializedProperty valueTree = null;
            if (!IsInitialized(prop))
            {
                if (initializeIfNeeded) valueTree = InitializeTree(prop);
                else return;
            }
            else valueTree = prop.FindPropertyRelative("valueTree");
            SerializedProperty rootValue = valueTree.GetArrayElementAtIndex(1);
            rootValue.FindPropertyRelative("settings").FindPropertyRelative("valueType").enumValueIndex = (int)DynamicParameterSorting.Fixed;
            rootValue.FindPropertyRelative("defaultValue").intValue = newVal;
        }

        // (also works with the Slider01 type)
        public static void SetFixedFloat(SerializedProperty prop, float newVal, bool initializeIfNeeded=false)
        {
            SerializedProperty valueTree = null;
            if (!IsInitialized(prop))
            {
                if (initializeIfNeeded) valueTree = InitializeTree(prop);
                else return;
            }
            else valueTree = prop.FindPropertyRelative("valueTree");
            SerializedProperty rootValue = valueTree.GetArrayElementAtIndex(1);
            rootValue.FindPropertyRelative("settings").FindPropertyRelative("valueType").enumValueIndex = (int)DynamicParameterSorting.Fixed;
            rootValue.FindPropertyRelative("defaultValue").floatValue = newVal;
        }

        public static void SetFixedVector2(SerializedProperty prop, Vector2 newVal, bool initializeIfNeeded=false)
        {
            SerializedProperty valueTree = null;
            if (!IsInitialized(prop))
            {
                if (initializeIfNeeded) valueTree = InitializeTree(prop);
                else return;
            }
            else valueTree = prop.FindPropertyRelative("valueTree");
            SerializedProperty rootValue = valueTree.GetArrayElementAtIndex(1);
            rootValue.FindPropertyRelative("settings").FindPropertyRelative("valueType").enumValueIndex = (int)DynamicParameterSorting.Fixed;
            rootValue.FindPropertyRelative("defaultValue").vector2Value = newVal;
        }

        public static void SetFixedBool(SerializedProperty prop, bool newVal, bool initializeIfNeeded=false)
        {
            SerializedProperty valueTree = null;
            if (!IsInitialized(prop))
            {
                if (initializeIfNeeded) valueTree = InitializeTree(prop);
                else return;
            }
            else valueTree = prop.FindPropertyRelative("valueTree");
            SerializedProperty rootValue = valueTree.GetArrayElementAtIndex(1);
            rootValue.FindPropertyRelative("settings").FindPropertyRelative("valueType").enumValueIndex = (int)DynamicParameterSorting.Fixed;
            rootValue.FindPropertyRelative("defaultValue").boolValue = newVal;
        }

        public static void SetFixedAnimationCurve(SerializedProperty prop, AnimationCurve newVal, bool initializeIfNeeded=false)
        {
            SerializedProperty valueTree = null;
            if (!IsInitialized(prop))
            {
                if (initializeIfNeeded) valueTree = InitializeTree(prop);
                else return;
            }
            else valueTree = prop.FindPropertyRelative("valueTree");
            SerializedProperty rootValue = valueTree.GetArrayElementAtIndex(1);
            rootValue.FindPropertyRelative("settings").FindPropertyRelative("valueType").enumValueIndex = (int)DynamicParameterSorting.Fixed;
            rootValue.FindPropertyRelative("defaultValue").animationCurveValue = newVal;
        }

        public static void SetFixedColor(SerializedProperty prop, Color newVal, bool initializeIfNeeded=false)
        {
            SerializedProperty valueTree = null;
            if (!IsInitialized(prop))
            {
                if (initializeIfNeeded) valueTree = InitializeTree(prop);
                else return;
            }
            else valueTree = prop.FindPropertyRelative("valueTree");
            SerializedProperty rootValue = valueTree.GetArrayElementAtIndex(1);
            rootValue.FindPropertyRelative("settings").FindPropertyRelative("valueType").enumValueIndex = (int)DynamicParameterSorting.Fixed;
            rootValue.FindPropertyRelative("defaultValue").colorValue = newVal;
        }

        public static void SetFixedString(SerializedProperty prop, string newVal, bool initializeIfNeeded=false)
        {
            SerializedProperty valueTree = null;
            if (!IsInitialized(prop))
            {
                if (initializeIfNeeded) valueTree = InitializeTree(prop);
                else return;
            }
            else valueTree = prop.FindPropertyRelative("valueTree");
            SerializedProperty rootValue = valueTree.GetArrayElementAtIndex(1);
            rootValue.FindPropertyRelative("settings").FindPropertyRelative("valueType").enumValueIndex = (int)DynamicParameterSorting.Fixed;
            rootValue.FindPropertyRelative("defaultValue").stringValue = newVal;
        }

        public static void SetFixedGradient(SerializedProperty prop, Gradient newVal, bool initializeIfNeeded=false)
        {
            SerializedProperty valueTree = null;
            if (!IsInitialized(prop))
            {
                if (initializeIfNeeded) valueTree = InitializeTree(prop);
                else return;
            }
            else valueTree = prop.FindPropertyRelative("valueTree");
            SerializedProperty rootValue = valueTree.GetArrayElementAtIndex(1);
            rootValue.FindPropertyRelative("settings").FindPropertyRelative("valueType").enumValueIndex = (int)DynamicParameterSorting.Fixed;
            SerializedProperty gradProperty = rootValue.FindPropertyRelative("defaultValue");
            SerializedProperty alphaKeys = gradProperty.FindPropertyRelative("m_NumAlphaKeys");
            SerializedProperty colorKeys = gradProperty.FindPropertyRelative("m_NumColorKeys");
            alphaKeys.intValue = 0;
            colorKeys.intValue = 0;

            if (newVal.alphaKeys != null)
                if (newVal.alphaKeys.Length > 0)
                {
                    alphaKeys.intValue = newVal.alphaKeys.Length;
                    for (int i = 0; i < newVal.alphaKeys.Length; i++)
                    {
                        SerializedProperty key = gradProperty.FindPropertyRelative("key"+i.ToString());
                        key.colorValue = new Color(key.colorValue.r, key.colorValue.g, key.colorValue.b, newVal.alphaKeys[i].alpha);
                        gradProperty.FindPropertyRelative("atime"+i.ToString()).intValue = (int)(65535*newVal.alphaKeys[i].time);
                    }
                }

            if (newVal.colorKeys != null)
                if (newVal.colorKeys.Length > 0)
                {
                    colorKeys.intValue = newVal.colorKeys.Length;
                    for (int i = 0; i < newVal.alphaKeys.Length; i++)
                    {
                        SerializedProperty key = gradProperty.FindPropertyRelative("key"+i.ToString());
                        key.colorValue = new Color(newVal.colorKeys[i].color.r, newVal.colorKeys[i].color.g, newVal.colorKeys[i].color.b, key.colorValue.a);
                        gradProperty.FindPropertyRelative("ctime"+i.ToString()).intValue = (int)(65535*newVal.colorKeys[i].time);
                    }
                }
        }

        public static void SetFixedBulletCurve(SerializedProperty prop, bool initializeIfNeeded=false)
        {
            prop.FindPropertyRelative("enabled").boolValue = true;
            
            SetFixedBool(prop.FindPropertyRelative("periodIsLifespan"), false, true);
            SetFixedFloat(prop.FindPropertyRelative("period"), 1f, true);
            
            SerializedProperty curve = prop.FindPropertyRelative("curve");
            SetFixedAnimationCurve(curve, AnimationCurve.EaseInOut(0,0,1,1), true);
            curve.FindPropertyRelative("forceZeroToOne").boolValue = true;

            SerializedProperty wrapMode = prop.FindPropertyRelative("wrapMode");
            SetFixedInt(wrapMode, 0, true);
            SetEnumType(wrapMode, typeof(WrapMode));
        }

        #endregion
    }
}