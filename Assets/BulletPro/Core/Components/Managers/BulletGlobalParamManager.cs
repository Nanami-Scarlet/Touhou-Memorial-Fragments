using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
    // Custom parameters could be of any serializable type
    public enum ParameterType { None, Integer, Float, Slider01, Bool, String, Color, Gradient, AnimationCurve, Object, Vector2, Vector3, Vector4, Quaternion, Rect, Bounds, Double, Long }

    [System.Serializable]
    public struct CustomParameter
    {
        // every parameter has a type and a name
        public ParameterType type;
        public string name;

        // reflects how SerializedProperties work
        public int intValue;
        public float floatValue;
        public double doubleValue;
        public long longValue;
        public bool boolValue;
        public string stringValue;
        public Color colorValue;
        public Gradient gradientValue;
        public AnimationCurve animationCurveValue;
        public Vector2 vector2Value;
        public Vector3 vector3Value;
        public Vector4 vector4Value;
        public Quaternion quaternionValue;
        public Rect rectValue;
        public Bounds boundsValue;
        public Object objectReferenceValue;

        public CustomParameter(ParameterType pType, string pName)
        {
            type = pType;
            name = pName;
            intValue = 0;
            floatValue = 0f;
            longValue = 0;
            doubleValue = 0d;
            boolValue = false;
            stringValue = "";
            colorValue = Color.black;
            gradientValue = new Gradient();
            animationCurveValue = AnimationCurve.Constant(0,1,1);
            vector2Value = Vector2.zero;
            vector3Value = Vector3.zero;
            vector4Value = Vector4.zero;
            quaternionValue = Quaternion.identity;
            rectValue = new Rect(0,0,1,1);
            boundsValue = new Bounds();
            objectReferenceValue = null;
        }
    }

	// This manager handles custom global parameters, if any
	[AddComponentMenu("BulletPro/Managers/Bullet Global Param Manager")]
    public class BulletGlobalParamManager : MonoBehaviour
    {
		public static BulletGlobalParamManager instance;
        public List<CustomParameter> parameters;

        void Awake()
		{
			// Setting up singleton instance
			if (!instance) instance = this;
			else Debug.LogWarning("BulletPro Warning: there is more than one instance of BulletGlobalParamManager in the scene.");

            if (parameters == null) parameters = new List<CustomParameter>();
		}

        CustomParameter GetParam(string paramName)
        {
            if (parameters != null)
                if (parameters.Count > 0)
                    for (int i = 0; i < parameters.Count; i++)
                        if (parameters[i].name == paramName)
                            return parameters[i];

            Debug.LogWarning("BulletPro Error: trying to fetch Global Parameter \""+paramName+"\" but it does not exist.");
            return new CustomParameter(ParameterType.None, paramName);
        }

        public void AddParameter(ParameterType paramType, string paramName)
        {
            if (parameters == null) parameters = new List<CustomParameter>();
            parameters.Add(new CustomParameter(paramType, paramName));
        }

        #region get value

        public int GetInt(string paramName) { return GetParam(paramName).intValue; }
        public float GetFloat(string paramName) { return GetParam(paramName).floatValue; }
        public float GetSlider01(string paramName) { return Mathf.Clamp01(GetParam(paramName).floatValue); }
        public long GetLong(string paramName) { return GetParam(paramName).longValue; }
        public double GetDouble(string paramName) { return GetParam(paramName).doubleValue; }
        public bool GetBool(string paramName) { return GetParam(paramName).boolValue; }
        public string GetString(string paramName) { return GetParam(paramName).stringValue; }
        public Color GetColor(string paramName) { return GetParam(paramName).colorValue; }
        public Gradient GetGradient(string paramName) { return GetParam(paramName).gradientValue; }
        public AnimationCurve GetAnimationCurve(string paramName) { return GetParam(paramName).animationCurveValue; }
        public Vector2 GetVector2(string paramName) { return GetParam(paramName).vector2Value; }
        public Vector3 GetVector3(string paramName) { return GetParam(paramName).vector3Value; }
        public Vector4 GetVector4(string paramName) { return GetParam(paramName).vector4Value; }
        public Quaternion GetQuaternion(string paramName) { return GetParam(paramName).quaternionValue; }
        public Rect GetRect(string paramName) { return GetParam(paramName).rectValue; }
        public Bounds GetBounds(string paramName) { return GetParam(paramName).boundsValue; }
        public Object GetObjectReference(string paramName) { return GetParam(paramName).objectReferenceValue; }

        #endregion

        #region set value

        public void SetInt (string paramName, int newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.intValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetFloat (string paramName, float newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.floatValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetSlider01 (string paramName, float newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.floatValue = Mathf.Clamp01(newValue);
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetLong (string paramName, long newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.longValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetDouble (string paramName, double newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.doubleValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetBool (string paramName, bool newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.boolValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetString (string paramName, string newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.stringValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetColor (string paramName, Color newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.colorValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetGradient (string paramName, Gradient newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.gradientValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetAnimationCurve (string paramName, AnimationCurve newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.animationCurveValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetVector2 (string paramName, Vector2 newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.vector2Value = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetVector3 (string paramName, Vector3 newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.vector3Value = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetVector4 (string paramName, Vector4 newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.vector4Value = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetQuaternion (string paramName, Quaternion newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.quaternionValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetRect (string paramName, Rect newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.rectValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetBounds (string paramName, Bounds newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.boundsValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        public void SetObjectReference (string paramName, Object newValue)
        {
            if (parameters.Count > 0)
                for (int i = 0; i < parameters.Count; i++)
                    if (parameters[i].name == paramName)
                    {
                        CustomParameter cgp = parameters[i];
                        cgp.objectReferenceValue = newValue;
                        parameters[i] = cgp;
                        return;
                    }

            Debug.LogError("BulletPro Error: trying to set value for Global Parameter \""+paramName+"\" but it does not exist.");
        }

        #endregion
    }
}