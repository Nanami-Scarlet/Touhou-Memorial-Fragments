using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
    // Dynamic floats, colors and vectors are parameters that can be set in different ways.
    // They can be made "dynamic", ie. have a value from A to B based on various factors.
    // Such factors are called "interpolation factors".

    // Lerp factor types as in shown in enum :
    public enum InterpolationFactor { Random, BulletHierarchy, GlobalParameter, CombineFactors }

    // For "bullet hierarchy" a second enum will pick options from the following :
    public enum ShotInterpolationFactor { SpawnTime, SpawnOrder }
    public enum PatternInterpolationFactor { TotalShotsFired, TimePlayed, PatternIndexInEmitter }
    public enum BulletInterpolationFactor { PositionInShot, Rotation, CustomParameter, TimeSinceAlive }

    // For pattern parameters only : should we reroll the value at each instruction, or once for all loops?
    public enum RerollFrequency { WheneverCalled, AtCertainLoops, OnlyOncePerPattern }

    // for BulletInterpolationFactor.PositionInShot
    public enum BulletPositionSortMode { Horizontal, Vertical, Radial, Texture }

    // How is a value chosen?
    public enum DynamicParameterSorting { Fixed, FromAToB, Blend, FromGradient, EqualToParameter }

    // If EqualToParameter : from a custom param of Bullet Hierarchy, or Global Parameter?
    public enum UserMadeParameterType { BulletHierarchy, GlobalParameter }

    // Every dynamic parameter contains this struct. It says how the value is chosen from A to B.
    [System.Serializable]
    public struct DynamicParameterSettings
    {
        public DynamicParameterSorting valueType; // if "Fixed", none of this applies, at all
        public InterpolationValue interpolationValue; // all parameters deciding the interpolation value
        
        #if UNITY_EDITOR
        public bool combinedInterpolationFactors; // dummy property to ensure same iterator structure
        #endif
        // TODO : restore this when feature is implemented. But this seems to exceed serialization depth limit of 8 levels.
        //public CombinedParameterSettings[] combinedInterpolationFactors; // if interpolation factor is "multiple parameters..."

        // navigation throughout the tree, via a system of indexes
        public int index, indexOfParent;
        // if settings.valueType == from-to
        public int indexOfFrom, indexOfTo;
        // if settings.valueType == blended
        public int[] indexOfBlendedChildren;
        public float weight; // if part of a blend between N values

        #if UNITY_EDITOR
        public string headerTitle;
        public Color blendColor;
        #endif

        // Finding out which parameter to fetch if valueType is "equal to parameter"
        public UserMadeParameterType parameterType;
        public int relativeTo; // How many parents do we go through while browsing the bullet hierarchy upwards?
        // ^ For bullets, 0 is "this one". For Shots and Patterns, 0 is "this one's direct sub-emitter"
        public string parameterName;
    }

    // Every parameter driving/deciding an interpolation value (from 0 to 1) is stored in this struct
    [System.Serializable]
    public struct InterpolationValue
    {
        // base interpolation value parameters
        public InterpolationFactor interpolationFactor; // always there
        public AnimationCurve repartitionCurve; // always there
        public bool shareValueBetweenInstances; // if interpolation factor is "random"
        public RerollFrequency rerollFrequency; // only if the parameter owner is a Pattern
        public ShotInterpolationFactor interpolationFactorFromShot; // if interpolation factor is "bullet hierarchy"
        public PatternInterpolationFactor interpolationFactorFromPattern; // if interpolation factor is "bullet hierarchy"
        public BulletInterpolationFactor interpolationFactorFromBullet; // if interpolation factor is "bullet hierarchy"
        public int relativeTo; // if interpolation factor is "random" or "bullet hierarchy". Is an int because it's "how many parents do we go through" while browsing the bullet hierarchy upwards.
        public string parameterName; // if interpolation factor is "global parameter" or "custom parameter"
        
        // if reroll frequency is set on "AtCertainLoops" :
        public int loopDepth; // how many parents should we fetch? 0 = innermost loop.
        public bool useComplexRerollSequence; // if false, will allow the user to craft complex sequences
        public int checkEveryNLoops; // said complex sequence size, up to 32
        public int loopSequence; // used as an "array" of 32 bits since serialization limit forbids arrays here

        // if interpolation factor is shot's "time", "number of shots", or pattern's "patternIndex", or bullet's "time since alive"
        public float period;
        public WrapMode wrapMode;

        // if bullet interpolation factor is "position in shot"
        public BulletPositionSortMode sortMode;
        public BulletSortDirection sortDirection;
        public Texture2D repartitionTexture;
        [Range(-1f, 2f)]
        public float centerX, centerY;
        [Range(0.001f, 2f)]
        public float radius;

        // if bullet interpolation factor is "global rotation" or "local rotation"
        [Range(-180, 180)]
        public float wrapPoint;
        public bool countClockwise;

        // only relevant when random seed is shared between shots of a same pattern
        public bool differentValuesPerShot;

        // For interpolation factors that actively use the repartition curve and its period
        public float WrapValue(float xValue)
        {
            if (period <= 0) return 0;

            float time = 0;
            // the +0.00001f is an epsilon used for preventing rounding errors.
            if (wrapMode == WrapMode.Loop) time = ((xValue / period) + 0.00001f) % 1f;
            else if (wrapMode == WrapMode.PingPong)
            {
                float ratio = (xValue / period) + 0.00001f;
                time = ratio % 1f;
                if (Mathf.FloorToInt(ratio) % 2 == 1) time = 1f - time;
            }
            else time = Mathf.Clamp01(xValue / period);

            return time; // repartition curve.Evaluate isn't called, solver does it
        }
    }

    // Multiple parameter settings can be used together. If so, the option of combining them is disabled (no point in nesting), hence the other similar struct
    [System.Serializable]
    public struct CombinedParameterSettings
    {
        public InterpolationValue interpolationValue;
        public string floatName; // name to be used in a GetFloat() method called in user-made functions
    }

    // Interfaces shared by every dynamic parameter and their values, used by the DynamicParameterSolver
    public interface IDynamicParameter
    {
        DynamicParameterSettings GetSettingsInTree(int index);
    }
    public interface IDynamicParameterValue // actually unused
    {
        DynamicParameterSettings GetSettings();
    }

    #region generic classes (doesn't work with Unity's serialization, but serves as code blueprint)

    [System.Serializable]
    public struct DynamicParameter<T> : IDynamicParameter where T : struct
    {
        public SubValue<T>[] valueTree;
        public SubValue<T> this[int index] { get { return valueTree[index]; } }
        public SubValue<T> root { get { if (valueTree.Length > 1) return this[1]; else return new SubValue<T>(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public T baseValue { get { return root.defaultValue; } }
        public int lastGivenIndex;
    }

    [System.Serializable]
    public struct SubValue<T> : IDynamicParameterValue where T : struct
    {
        // values and blending
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public T defaultValue;
        public float weight; // if part of a blend between N values
        
        // everytime settings.valueType is changed to "from-to", if indexOfChildFrom/To is zero, children items are added to valueTree and their indexes are updated.
        // everytime settings.valueType is changed to "blend" and more elements are added, if indexOfBlendedChildren doesn't have enough length, children items are added to valueTree and their indexes are updated.
        // Along with the valueTree, used indexes are tracked in the valueTree array size.
        // no index can ever be undone / erased / replaced / etc, so that users can momentarily set a valueType to "fixed" and get it back to "blend" or "from-to" later.
    }

    #endregion

    #region per-type classes (from-to compatible)

    [System.Serializable]
    public struct DynamicFloat : IDynamicParameter
    {
        public DynamicFloatValue[] valueTree;
        public DynamicFloatValue this[int index] { get { return valueTree[index]; } }
        public DynamicFloatValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicFloatValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public float baseValue { get { return root.defaultValue; } }
        #if UNITY_EDITOR
        public bool useSlider;
        public float sliderMin, sliderMax;
        #endif

        public DynamicFloat (float defaultVal)
        {
            valueTree = new DynamicFloatValue[2];
            var rootVal = new DynamicFloatValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
            #if UNITY_EDITOR
            useSlider = false;
            sliderMin = 0f;
            sliderMax = 0f;
            #endif
        }

        #if UNITY_EDITOR
        public void EnableSlider(float min, float max)
        {
            useSlider = true;
            sliderMin = min;
            sliderMax = max;
        }
        #endif
    }

    [System.Serializable]
    public struct DynamicFloatValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public float defaultValue;
    }

    [System.Serializable]
    public struct DynamicInt : IDynamicParameter
    {
        public DynamicIntValue[] valueTree;
        public DynamicIntValue this[int index] { get { return valueTree[index]; } }
        public DynamicIntValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicIntValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public int baseValue { get { return root.defaultValue; } }
        #if UNITY_EDITOR
        public bool useSlider;
        public int sliderMin, sliderMax;
        public int[] buttons;
        #endif
        
        public DynamicInt (int defaultVal)
        {
            valueTree = new DynamicIntValue[2];
            var rootVal = new DynamicIntValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
            #if UNITY_EDITOR
            useSlider = false;
            sliderMin = 0;
            sliderMax = 0;
            buttons = new int[0];
            #endif
        }   

        #if UNITY_EDITOR
        public void EnableSlider(int min, int max)
        {
            useSlider = true;
            sliderMin = min;
            sliderMax = max;
        }

        public void SetButtons(int[] buttonValues)
        {
            buttons = buttonValues;
        }
        #endif
    }

    [System.Serializable]
    public struct DynamicIntValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public int defaultValue;
    }

    [System.Serializable]
    public struct DynamicSlider01 : IDynamicParameter
    {
        public DynamicSlider01Value[] valueTree;
        public DynamicSlider01Value this[int index] { get { return valueTree[index]; } }
        public DynamicSlider01Value root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicSlider01Value(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public float baseValue { get { return root.defaultValue; } }
        
        public DynamicSlider01 (float defaultVal)
        {
            valueTree = new DynamicSlider01Value[2];
            var rootVal = new DynamicSlider01Value();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
        }
    }

    [System.Serializable]
    public struct DynamicSlider01Value : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public float defaultValue;
    }

    [System.Serializable]
    public struct DynamicColor : IDynamicParameter
    {
        public DynamicColorValue[] valueTree;
        public DynamicColorValue this[int index] { get { return valueTree[index]; } }
        public DynamicColorValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicColorValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public Color baseValue { get { return root.defaultValue; } }

        public DynamicColor (Color defaultVal)
        {
            valueTree = new DynamicColorValue[2];
            var rootVal = new DynamicColorValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
        }        
    }

    [System.Serializable]
    public struct DynamicColorValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public Color defaultValue;
        public Gradient gradientValue; // if sorting mode is "from gradient"
    }

    [System.Serializable]
    public struct DynamicVector2 : IDynamicParameter
    {
        public DynamicVector2Value[] valueTree;
        public DynamicVector2Value this[int index] { get { return valueTree[index]; } }
        public DynamicVector2Value root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicVector2Value(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public Vector2 baseValue { get { return root.defaultValue; } }
        
        public DynamicVector2 (Vector2 defaultVal)
        {
            valueTree = new DynamicVector2Value[2];
            var rootVal = new DynamicVector2Value();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
        }   
    }

    [System.Serializable]
    public struct DynamicVector2Value : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public Vector2 defaultValue;
    }

    [System.Serializable]
    public struct DynamicVector3 : IDynamicParameter
    {
        public DynamicVector3Value[] valueTree;
        public DynamicVector3Value this[int index] { get { return valueTree[index]; } }
        public DynamicVector3Value root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicVector3Value(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public Vector3 baseValue { get { return root.defaultValue; } }
        
        public DynamicVector3 (Vector3 defaultVal)
        {
            valueTree = new DynamicVector3Value[2];
            var rootVal = new DynamicVector3Value();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
        }   
    }

    [System.Serializable]
    public struct DynamicVector3Value : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public Vector3 defaultValue;
    }

    [System.Serializable]
    public struct DynamicVector4 : IDynamicParameter
    {
        public DynamicVector4Value[] valueTree;
        public DynamicVector4Value this[int index] { get { return valueTree[index]; } }
        public DynamicVector4Value root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicVector4Value(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public Vector4 baseValue { get { return root.defaultValue; } }
        
        public DynamicVector4 (Vector4 defaultVal)
        {
            valueTree = new DynamicVector4Value[2];
            var rootVal = new DynamicVector4Value();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
        }   
    }

    [System.Serializable]
    public struct DynamicVector4Value : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public Vector4 defaultValue;
    }

    #endregion

    #region per-type classes (not from-to compatible)

    [System.Serializable]
    public struct DynamicBool : IDynamicParameter
    {
        public DynamicBoolValue[] valueTree;
        public DynamicBoolValue this[int index] { get { return valueTree[index]; } }
        public DynamicBoolValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicBoolValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public bool baseValue { get { return root.defaultValue; } }
        
        public DynamicBool (bool defaultVal)
        {
            valueTree = new DynamicBoolValue[2];
            var rootVal = new DynamicBoolValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
        }   
    }

    [System.Serializable]
    public struct DynamicBoolValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public bool defaultValue;
    }

    [System.Serializable]
    public struct DynamicString : IDynamicParameter
    {
        public DynamicStringValue[] valueTree;
        public DynamicStringValue this[int index] { get { return valueTree[index]; } }
        public DynamicStringValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicStringValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public string baseValue { get { return root.defaultValue; } }
        
        public DynamicString (string defaultVal)
        {
            valueTree = new DynamicStringValue[2];
            var rootVal = new DynamicStringValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
        }   
    }

    [System.Serializable]
    public struct DynamicStringValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public string defaultValue;
    }

    [System.Serializable]
    public struct DynamicEnum : IDynamicParameter
    {
        public DynamicEnumValue[] valueTree;
        public DynamicEnumValue this[int index] { get { return valueTree[index]; } }
        public DynamicEnumValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicEnumValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public int baseValue { get { return root.defaultValue; } }
        #if UNITY_EDITOR
        public string enumTypeName;
        public string[] enumOptions;
        #endif

        public DynamicEnum (int defaultVal)
        {
            valueTree = new DynamicEnumValue[2];
            var rootVal = new DynamicEnumValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
            #if UNITY_EDITOR
            enumTypeName = "";
            enumOptions = new string[0];
            SetEnumType(typeof(WrapMode)); // some default enum to start with
            #endif
        }

        #if UNITY_EDITOR
        public void SetEnumType(System.Type tName)
        {
            enumTypeName = tName.AssemblyQualifiedName;
            enumOptions = System.Enum.GetNames(tName);
            // Exceptional fix for WrapMode because Unity somehow scrambles the strings
            if (tName == typeof(WrapMode)) enumOptions = new string[] { "Default", "Clamp", "Loop", "Clamp Forever", "Ping Pong"};
        }
        #endif
    }

    [System.Serializable]
    public struct DynamicEnumValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public int defaultValue;
    }

    [System.Serializable]
    public struct DynamicAnimationCurve : IDynamicParameter
    {
        public DynamicAnimationCurveValue[] valueTree;
        public DynamicAnimationCurveValue this[int index] { get { return valueTree[index]; } }
        public DynamicAnimationCurveValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicAnimationCurveValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public AnimationCurve baseValue { get { return root.defaultValue; } }
        #if UNITY_EDITOR
        public bool forceZeroToOne;
        #endif
        
        public DynamicAnimationCurve (AnimationCurve defaultVal)
        {
            valueTree = new DynamicAnimationCurveValue[2];
            var rootVal = new DynamicAnimationCurveValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
            #if UNITY_EDITOR
            forceZeroToOne = true;
            #endif
        }

        #if UNITY_EDITOR
        public void SetForceZeroToOne(bool newVal)
        {
            forceZeroToOne = newVal;
        }
        #endif
    }

    [System.Serializable]
    public struct DynamicAnimationCurveValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public AnimationCurve defaultValue;
    }

    [System.Serializable]
    public struct DynamicGradient : IDynamicParameter
    {
        public DynamicGradientValue[] valueTree;
        public DynamicGradientValue this[int index] { get { return valueTree[index]; } }
        public DynamicGradientValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicGradientValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public Gradient baseValue { get { return root.defaultValue; } }
        
        public DynamicGradient (Gradient defaultVal)
        {
            valueTree = new DynamicGradientValue[2];
            var rootVal = new DynamicGradientValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
        }   
    }

    [System.Serializable]
    public struct DynamicGradientValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public Gradient defaultValue;
    }

    [System.Serializable]
    public struct DynamicRect : IDynamicParameter
    {
        public DynamicRectValue[] valueTree;
        public DynamicRectValue this[int index] { get { return valueTree[index]; } }
        public DynamicRectValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicRectValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public Rect baseValue { get { return root.defaultValue; } }
        
        public DynamicRect (Rect defaultVal)
        {
            valueTree = new DynamicRectValue[2];
            var rootVal = new DynamicRectValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
        }   
    }

    [System.Serializable]
    public struct DynamicRectValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public Rect defaultValue;
    }

    [System.Serializable]
    public struct DynamicObjectReference : IDynamicParameter
    {
        public DynamicObjectReferenceValue[] valueTree;
        public DynamicObjectReferenceValue this[int index] { get { return valueTree[index]; } }
        public DynamicObjectReferenceValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicObjectReferenceValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public Object baseValue { get { return root.defaultValue; } }
        #if UNITY_EDITOR
        public bool narrowType;
        public bool requireComponent; // fields expecting a certain behaviour can reject GameObjects without said component 
        public string typeName;
        public string requiredComponentName;
        #endif

        public DynamicObjectReference (Object defaultVal)
        {
            valueTree = new DynamicObjectReferenceValue[2];
            var rootVal = new DynamicObjectReferenceValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
            #if UNITY_EDITOR
            narrowType = false;
            requireComponent = false;
            typeName = "";
            requiredComponentName = "";
            #endif
        }

        #if UNITY_EDITOR
        public void SetNarrowType(System.Type tName)
        {
            narrowType = true;
            typeName = tName.AssemblyQualifiedName;
        }

        public void RequireComponent(System.Type tName)
        {
            requireComponent = true;
            requiredComponentName = tName.AssemblyQualifiedName;
        }
        #endif
    }

    [System.Serializable]
    public struct DynamicObjectReferenceValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public Object defaultValue;
    }

    [System.Serializable]
    public struct DynamicBullet : IDynamicParameter
    {
        public DynamicBulletValue[] valueTree;
        public DynamicBulletValue this[int index] { get { return valueTree[index]; } }
        public DynamicBulletValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicBulletValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public BulletParams baseValue { get { return root.defaultValue; } }
        
        public DynamicBullet (BulletParams defaultVal)
        {
            valueTree = new DynamicBulletValue[2];
            var rootVal = new DynamicBulletValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
        }   
    }

    [System.Serializable]
    public struct DynamicBulletValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public BulletParams defaultValue;
    }

    [System.Serializable]
    public struct DynamicShot : IDynamicParameter
    {
        public DynamicShotValue[] valueTree;
        public DynamicShotValue this[int index] { get { return valueTree[index]; } }
        public DynamicShotValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicShotValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public ShotParams baseValue { get { return root.defaultValue; } }
        
        public DynamicShot (ShotParams defaultVal)
        {
            valueTree = new DynamicShotValue[2];
            var rootVal = new DynamicShotValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
        }   
    }

    [System.Serializable]
    public struct DynamicShotValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public ShotParams defaultValue;
    }

    [System.Serializable]
    public struct DynamicPattern : IDynamicParameter
    {
        public DynamicPatternValue[] valueTree;
        public DynamicPatternValue this[int index] { get { return valueTree[index]; } }
        public DynamicPatternValue root { get { if (valueTree.Length > 1) return this[1]; else return new DynamicPatternValue(); } }
        public DynamicParameterSettings GetSettingsInTree(int index) { return this[index].settings; }
        public PatternParams baseValue { get { return root.defaultValue; } }
        
        public DynamicPattern (PatternParams defaultVal)
        {
            valueTree = new DynamicPatternValue[2];
            var rootVal = new DynamicPatternValue();
            rootVal.defaultValue = defaultVal;
            rootVal.settings.index = 1;
            rootVal.settings.interpolationValue.repartitionCurve = AnimationCurve.Linear(0, 0, 1, 1);
            valueTree[1] = rootVal;
        }   
    }

    [System.Serializable]
    public struct DynamicPatternValue : IDynamicParameterValue
    {
        public DynamicParameterSettings settings;
        public DynamicParameterSettings GetSettings() { return settings; }
        public PatternParams defaultValue;
    }

    #endregion

    
}