using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
    public static class BulletProExtensions
    {
        public static void CopyValuesFrom(this SerializedProperty dest, SerializedProperty src)
        {
            if (dest.type == "float") dest.floatValue = src.floatValue;
            else if (dest.type == "int") dest.intValue = src.intValue;
            else if (dest.type == "string") dest.stringValue = src.stringValue;
            else if (dest.type == "bool") dest.boolValue = src.boolValue;
            else if (dest.type == "AnimationCurve") dest.animationCurveValue = src.animationCurveValue;
            else if (dest.type == "Color") dest.colorValue = src.colorValue;
            else if (dest.type == "Vector2") dest.vector2Value = src.vector2Value;
            else if (dest.type == "Vector3") dest.vector3Value = src.vector3Value;
            else if (dest.type == "Vector4") dest.vector4Value = src.vector4Value;
            else if (dest.type == "Rect") dest.rectValue = src.rectValue;                        
            else if (dest.type.Contains("PPtr")) dest.objectReferenceValue = src.objectReferenceValue;
        }

        public static Gradient DefaultGradient()
        {
            Gradient grad = new Gradient();
            GradientAlphaKey[] gak = new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };
            GradientColorKey[] gck = new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.black, 1) };
            grad.SetKeys(gck, gak);
            return grad;
        }
    }
}