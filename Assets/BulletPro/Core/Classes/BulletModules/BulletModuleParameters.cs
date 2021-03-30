using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// Module for handling bullet custom dynamic parameters.
	public class BulletModuleParameters : BulletModule
	{
		public List<CustomParameter> parameters { get; private set; }

		public override void Enable() { base.Enable(); }
		public override void Disable() { base.Disable(); }

		// Called at Bullet.ApplyBulletParams()
		public void ApplyBulletParams(BulletParams bp)
		{			
            if (parameters == null) parameters = new List<CustomParameter>();
			parameters.Clear();
			parameters.TrimExcess();
			if (bp.customParameters == null) return;
			if (bp.customParameters.Length == 0) return;
			for (int i = 0; i < bp.customParameters.Length; i++)
				AddParameterFromDynamic(bp.customParameters[i], i * 123643);			
		}

		CustomParameter GetParam(string paramName)
        {
            if (parameters != null)
                if (parameters.Count > 0)
                    for (int i = 0; i < parameters.Count; i++)
                        if (parameters[i].name == paramName)
                            return parameters[i];

            Debug.LogWarning("BulletPro Error: trying to fetch Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
            return new CustomParameter(ParameterType.None, paramName);
        }

        public void AddParameter(ParameterType paramType, string paramName)
        {
            if (parameters == null) parameters = new List<CustomParameter>();
            parameters.Add(new CustomParameter(paramType, paramName));
        }

		public void AddParameter(CustomParameter customParam)
        {
            if (parameters == null) parameters = new List<CustomParameter>();
            parameters.Add(customParam);
        }

		void AddParameterFromDynamic(DynamicCustomParameter customParam, int operationID)
		{
			if (parameters == null) parameters = new List<CustomParameter>();
			CustomParameter nonDynamicParam = new CustomParameter(customParam.type, customParam.name);
            ParameterOwner owner = ParameterOwner.Bullet;

			if (customParam.type == ParameterType.AnimationCurve) nonDynamicParam.animationCurveValue = solver.SolveDynamicAnimationCurve(customParam.animationCurveValue, operationID, owner);
			else if (customParam.type == ParameterType.Bool) nonDynamicParam.boolValue = solver.SolveDynamicBool(customParam.boolValue, operationID, owner);
			else if (customParam.type == ParameterType.Bounds) nonDynamicParam.boundsValue = customParam.boundsValue;
			else if (customParam.type == ParameterType.Color) nonDynamicParam.colorValue = solver.SolveDynamicColor(customParam.colorValue, operationID, owner);
			else if (customParam.type == ParameterType.Double) nonDynamicParam.doubleValue = customParam.doubleValue;
			else if (customParam.type == ParameterType.Float) nonDynamicParam.floatValue = solver.SolveDynamicFloat(customParam.floatValue, operationID, owner);
			else if (customParam.type == ParameterType.Gradient) nonDynamicParam.gradientValue = solver.SolveDynamicGradient(customParam.gradientValue, operationID, owner);
			else if (customParam.type == ParameterType.Integer) nonDynamicParam.intValue = solver.SolveDynamicInt(customParam.intValue, operationID, owner);
			else if (customParam.type == ParameterType.Long) nonDynamicParam.longValue = customParam.longValue;
			else if (customParam.type == ParameterType.Object) nonDynamicParam.objectReferenceValue = solver.SolveDynamicObjectReference(customParam.objectReferenceValue, operationID, owner);
			else if (customParam.type == ParameterType.Quaternion) nonDynamicParam.quaternionValue = customParam.quaternionValue;
			else if (customParam.type == ParameterType.Rect) nonDynamicParam.rectValue = solver.SolveDynamicRect(customParam.rectValue, operationID, owner);
			else if (customParam.type == ParameterType.Slider01) nonDynamicParam.floatValue = solver.SolveDynamicSlider01(customParam.sliderValue, operationID, owner);
			else if (customParam.type == ParameterType.String) nonDynamicParam.stringValue = solver.SolveDynamicString(customParam.stringValue, operationID, owner);
			else if (customParam.type == ParameterType.Vector2) nonDynamicParam.vector2Value = solver.SolveDynamicVector2(customParam.vector2Value, operationID, owner);
			else if (customParam.type == ParameterType.Vector3) nonDynamicParam.vector3Value = solver.SolveDynamicVector3(customParam.vector3Value, operationID, owner);
			else if (customParam.type == ParameterType.Vector4) nonDynamicParam.vector4Value = solver.SolveDynamicVector4(customParam.vector4Value, operationID, owner);
            
            parameters.Add(nonDynamicParam);
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
        public AnimationCurve GetAnimationCurve(string paramName) { return GetParam(paramName).animationCurveValue; }
        public Gradient GetGradient(string paramName) { return GetParam(paramName).gradientValue; }
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
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

            Debug.LogError("BulletPro Error: trying to set value for Custom Bullet Parameter \""+paramName+"\" but it does not exist.");
        }

        #endregion
	}
}