using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro.EditorScripts
{
	// Only used when declaring BulletCurves in BulletBehaviours. Curves from BulletParams and PatternParams have their own GUILayout implementation.
	[CustomPropertyDrawer(typeof(BulletCurve))]
	public class BulletCurveDrawer : PropertyDrawer
	{
		// Ensures a curve starts at zero and ends at one. Set bypassChild to true if property directly points at an AnimationCurve.
		public static bool GoesFromZeroToOne(SerializedProperty prop, bool bypassChild = false)
		{
			AnimationCurve curve = AnimationCurve.Constant(0,1,1); // dummy value
			if (bypassChild) curve = prop.animationCurveValue;
			else curve = prop.FindPropertyRelative("curve").animationCurveValue;

			if (curve == null) return false;
			if (curve.keys == null) return false;
			if (curve.keys.Length < 2) return false;
			if (curve.keys[0].time != 0f) return false;
			if (curve.keys[curve.keys.Length-1].time != 1f) return false;

			return true;
		}

		// Fix problems if a curve does not start at zero and/or does not end at one. Returns the curve. Set bypassChild to true if property directly points at an AnimationCurve.
		public static AnimationCurve RepairCurveFromZeroToOne(SerializedProperty prop, bool bypassChild = false)
		{
			AnimationCurve curve = AnimationCurve.Constant(0,1,1); // dummy value		
			if (bypassChild) curve = prop.animationCurveValue;
			else curve = prop.FindPropertyRelative("curve").animationCurveValue;

			if (curve == null) curve = AnimationCurve.Constant(0f, 1f, 1f);
			else if (curve.keys == null) curve = AnimationCurve.Constant(0f, 1f, 1f);
			else if (curve.keys.Length < 2) curve = AnimationCurve.Constant(0f, 1f, 1f);
			else
			{
				if (curve.keys[0].time != 0f)
				{
					Keyframe[] keys = new Keyframe[curve.keys.Length];
					keys[0] = new Keyframe(0f, curve.keys[0].value);
					keys[0].inTangent = curve.keys[0].inTangent;
					keys[0].outTangent = curve.keys[0].outTangent;
					for (int i=1; i<keys.Length; i++)
					{
						keys[i] = new Keyframe(Mathf.Max(0f, curve.keys[i].time), curve.keys[i].value);
						keys[i].inTangent = curve.keys[i].inTangent;
						keys[i].outTangent = curve.keys[i].outTangent;
					}
					curve = new AnimationCurve(keys);
				}
				if (curve.keys.Length > 1)
				{
					if (curve.keys[curve.keys.Length-1].time != 1f)
					{
						Keyframe[] keys = new Keyframe[curve.keys.Length];
						keys[keys.Length-1] = new Keyframe(1f, curve.keys[keys.Length-1].value);
						keys[keys.Length-1].inTangent = curve.keys[keys.Length-1].inTangent;
						keys[keys.Length-1].outTangent = curve.keys[keys.Length-1].outTangent;
						for (int i=keys.Length-2; i>-1; i--)
						{
							keys[i] = new Keyframe(Mathf.Min(1f, curve.keys[i].time), curve.keys[i].value);
							keys[i].inTangent = curve.keys[i].inTangent;
							keys[i].outTangent = curve.keys[i].outTangent;
						}
						curve = new AnimationCurve(keys);
					}
				}
				else // for rare cases where fixing the "starts at zero" destroyed the curve to the point it has one sole key left
				{
					float val = curve.keys[0].value;
					curve = AnimationCurve.Constant(0f, 1f, val);
				}
			}

			if (bypassChild) prop.animationCurveValue = curve;
			else prop.FindPropertyRelative("curve").animationCurveValue = curve;

			return curve;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			int lines = 1; // enabled
			
			if (property.FindPropertyRelative("enabled").boolValue)
			{
				lines++; // wrapmode
				lines++; // curve
				if (!GoesFromZeroToOne(property))
				{
					lines++; // help box line
					lines++; // "fix it" button
				}
				lines++; // period is lifespan
				if (!property.FindPropertyRelative("periodIsLifespan").boolValue)
				{
					lines++; // period measured in seconds/shots
					lines++; // period
				}
			}

			return (base.GetPropertyHeight(property, label) + 2) * (lines);
		}

		// Only used when declaring BulletCurves in BulletBehaviours. Curves from BulletParams and PatternParams have their own GUILayout implementation
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			float fakeIndentLevel = 0;

			EditorGUI.BeginProperty(position, label, property);
			Rect[] rects = new Rect[16]; // arbitrary number, there should be enough lines for the whole thing
			for (int i = 0; i < rects.Length; i++)
			{
				rects[i] = new Rect(position.x + fakeIndentLevel, position.y + 18 * i, position.width - fakeIndentLevel, 18);
				// after the first one, indent level changes
				fakeIndentLevel = 16;
			}

			int line = 0;

			//EditorStyles.label.fontStyle = FontStyle.Bold; // removed, was ugly
			SerializedProperty enabled = property.FindPropertyRelative("enabled");
			EditorGUI.BeginChangeCheck();
			bool en = EditorGUI.Toggle(rects[line++], property.displayName, enabled.boolValue);
			if (EditorGUI.EndChangeCheck()) enabled.boolValue = en;
			//EditorStyles.label.fontStyle = FontStyle.Normal;

			if (enabled.boolValue)
			{
				SerializedProperty wrapMode = property.FindPropertyRelative("wrapMode");
				EditorGUI.PropertyField(rects[line++], wrapMode);

				SerializedProperty curve = property.FindPropertyRelative("curve");
				EditorGUI.PropertyField(rects[line++], curve);

				if (!GoesFromZeroToOne(property))
				{
					EditorGUI.HelpBox(rects[line++], "Error: Curve has to exactly begin at 0 and end at 1.", MessageType.Error);
					if (GUI.Button(rects[line++], "Auto-fix curve", EditorStyles.miniButton))
						RepairCurveFromZeroToOne(property);
				}

				SerializedProperty periodIsLifespan = property.FindPropertyRelative("periodIsLifespan");
				EditorGUI.PropertyField(rects[line++], periodIsLifespan);

				if (!periodIsLifespan.boolValue)
				{
					SerializedProperty period = property.FindPropertyRelative("_period");
					EditorGUI.PropertyField(rects[line++], period);
				}
			}

			//property.intValue = EditorGUI.MaskField(position, label, property.intValue, property.enumNames);
			EditorGUI.EndProperty();

			EditorGUI.indentLevel = oldIndent;
		}
	}
}
