using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System;
#endif

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	[System.Serializable]
	public struct DynamicCustomParameter
	{
		// every parameter has a type and a name
        public ParameterType type;
        public string name;

        // reflects how SerializedProperties work
        public DynamicInt intValue;
        public DynamicFloat floatValue;
		public DynamicSlider01 sliderValue;
        public double doubleValue; // cannot be dynamic
        public long longValue; // cannot be dynamic
        public DynamicBool boolValue;
        public DynamicString stringValue;
        public DynamicColor colorValue;
		public DynamicGradient gradientValue;
        public DynamicAnimationCurve animationCurveValue;
        public DynamicVector2 vector2Value;
        public DynamicVector3 vector3Value;
        public DynamicVector4 vector4Value;
        public Quaternion quaternionValue; // cannot be dynamic
        public DynamicRect rectValue;
        public Bounds boundsValue; // cannot be dynamic
        public DynamicObjectReference objectReferenceValue;
	}

	#if UNITY_EDITOR
	public enum ParamInspectorPart { Renderer, Movement, Collision, Homing, SpawnAndLifetime, Emission, Behaviours, Parameters }
	#endif

	// Stores any bullet parameters in order to read them just before shooting a bullet.
	[System.Serializable]
	public class BulletParams : EmissionParams
	{
		#region properties

		// This bool allows to set custom default values for certain vars (i.e. colors)
		public bool hasBeenSerializedOnce;

		// Appearance
		public DynamicObjectReference sprite;
		public BulletRenderMode renderMode;
		public DynamicObjectReference mesh;
		public DynamicBool animated;
		public DynamicObjectReference material;
		public DynamicFloat animationFramerate;
		public WrapMode animationWrapMode;
		public DynamicObjectReference[] sprites;
		public DynamicInt sortingOrder;
		public DynamicString sortingLayerName;
		public DynamicColor color;
		public DynamicEnum evolutionBlendWithBaseColor;
		public DynamicGradient colorEvolution; // replaces "color" if colorOverLifetime is enabled
		public DynamicBool playVFXOnBirth, playVFXOnDeath;
		public DynamicFloat vfxParticleSize;
		public bool useCustomBirthVFX, useCustomDeathVFX;
		public DynamicObjectReference customBirthVFX, customDeathVFX;
		
		#if UNITY_EDITOR
		public bool foldoutVFX, hideSpriteList;
		public ParamInspectorPart currentlyDisplayedModule;
		#endif

		// These bools aren't randomizable, being the bullet's "root behaviour"
		public bool canMove;
		public bool canCollide;
		public bool isVisible;
		public bool hasLifespan;
		public bool hasPatterns
		{ get {
			if (patternsShot == null) return false;
			else return patternsShot.Length > 0;
		}} // readonly because toggling it does not make sense

		// Movement
		public DynamicFloat forwardSpeed;
		public DynamicFloat angularSpeed;
		public DynamicFloat startScale;
		public DynamicBool isChildOfEmitter;

		// Animation
		public bool animFoldout;
		public AnimationClip animationClip;
		public Space animationMovementSpace;
		public Texture animationTexture;

		// Collision
		public CollisionTags collisionTags;
		public BulletCollider[] colliders;
		public bool dieOnCollision;
		public bool shapeFoldout, collisionTagsFoldout;

		// Homing
		public bool homing;
		public CollisionTags homingTags;
		public bool useSameTagsAsCollision; // if true, homingTags won't be used, collisionTags will be used instead
		public DynamicFloat lookAtTargetAtSpawn; // from 0 to 1. 0 means regular spawn, 1 means spawn directly turned towards target
		public DynamicFloat homingAngularSpeed; // speed at which we look at target
		public DynamicFloat targetRefreshInterval; // if this bullet has many targets, change target every X seconds
		public DynamicEnum preferredTarget; // when changing target, go to closest one, oldest one, or a random one
		public DynamicFloat homingAngleThreshold; // when rotation delta goes below the threshold, the bullet will cease to adjust to its target, to avoid ugly shaking
		public bool homingTagsFoldout; // editor only

		// Lifespan
		public DynamicFloat lifespan;

		// Curves
		public DynamicBulletCurve speedOverLifetime;
		public DynamicBulletCurve angularSpeedOverLifetime;
		public DynamicBulletCurve scaleOverLifetime;
		public DynamicBulletCurve colorOverLifetime;
		public DynamicBulletCurve alphaOverLifetime;
		public DynamicBulletCurve homingOverLifetime;

		// Curves from AnimationClip
		public BulletCurve xMovementFromAnim, yMovementFromAnim, rotationFromAnim, scaleFromAnim;

		// In case this bullet has patterns and/or shoots bullets
		public DynamicPattern[] patternsShot;
		public bool dieWhenAllPatternsAreDone;

		// An additional behaviour can be inside bullet child object
		public DynamicObjectReference[] behaviourPrefabs;

		// Bullet spawn can also be delayed.
		public bool delaySpawn;
		public DynamicFloat timeBeforeSpawn;
		public DynamicBool playAudioAtSpawn;
		public DynamicObjectReference audioClip;

		// custom parameters
		public DynamicCustomParameter[] customParameters;

		#endregion

#if UNITY_EDITOR
		// In the inspector, we'll store bullet preview data here.
		public BulletPreview preview;

		// Called from inspectors upon bullet creation
		public override void FirstInitialization()
		{
			Material default2DMaterial = Resources.Load("Default2DBulletMaterial") as Material;
			Sprite defaultBulletSprite = Resources.Load<Sprite>("DefaultBulletSprite");

			hasBeenSerializedOnce = true;

			SetUniqueIndex();

			// misc renderer info
			color = new DynamicColor(Color.black);
			evolutionBlendWithBaseColor = new DynamicEnum(0);
			evolutionBlendWithBaseColor.SetEnumType(typeof(ColorBlend));
			Gradient grad = new Gradient();
			GradientAlphaKey[] gak = new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };
			GradientColorKey[] gck = new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.black, 1) };
			grad.SetKeys(gck, gak);
			colorEvolution = new DynamicGradient(grad);
			sprite = new DynamicObjectReference(defaultBulletSprite);
			sprite.SetNarrowType(typeof(Sprite));
			sprites = new DynamicObjectReference[1];
			DynamicObjectReference firstSprite = new DynamicObjectReference(defaultBulletSprite);
			firstSprite.SetNarrowType(typeof(Sprite));
			sprites[0] = firstSprite;
			sortingOrder = new DynamicInt(0);
			sortingLayerName = new DynamicString("");
			material = new DynamicObjectReference(default2DMaterial);
			material.SetNarrowType(typeof(Material));
			mesh = new DynamicObjectReference(null);
			mesh.SetNarrowType(typeof(Mesh));

			// vfx
			customBirthVFX = new DynamicObjectReference(null);
			customDeathVFX = new DynamicObjectReference(null);
			customBirthVFX.SetNarrowType(typeof(ParticleSystem));
			customDeathVFX.SetNarrowType(typeof(ParticleSystem));
			playVFXOnBirth = new DynamicBool(false);
			playVFXOnDeath = new DynamicBool(true);
			vfxParticleSize = new DynamicFloat(0.5f);

			// animation info
			animated = new DynamicBool(false);
			animationFramerate = new DynamicFloat(12f);
			renderMode = BulletRenderMode.Sprite;
			animationWrapMode = WrapMode.Loop;

			// default collision params
			dieOnCollision = true;
			colliders = new BulletCollider[1];
			colliders[0].size = 0.1f;
			colliders[0].colliderType = BulletColliderType.Circle;

			// default tags
			collisionTags = new CollisionTags();
			collisionTags.tagList = 1; // defaults to "Player"
			homingTags = new CollisionTags();
			homingTags.tagList = 1; // defaults to "Player"
			useSameTagsAsCollision = true;

			// homing params
			lookAtTargetAtSpawn = new DynamicFloat(0f);
			lookAtTargetAtSpawn.EnableSlider(-1f, 1f);
			homingAngularSpeed = new DynamicFloat(90f);
			homingAngleThreshold = new DynamicFloat(0f);
			targetRefreshInterval = new DynamicFloat(0f);
			preferredTarget = new DynamicEnum(0);
			preferredTarget.SetEnumType(typeof(PreferredTarget));

			// default curves
			speedOverLifetime = new DynamicBulletCurve(true);
			angularSpeedOverLifetime = new DynamicBulletCurve(true);
			scaleOverLifetime = new DynamicBulletCurve(true);
			homingOverLifetime = new DynamicBulletCurve(true);
			colorOverLifetime = new DynamicBulletCurve(true);
			alphaOverLifetime = new DynamicBulletCurve(true);

			// pattern module
			patternsShot = new DynamicPattern[0];
			dieWhenAllPatternsAreDone = false;
			// Commented out 2020-09: array should start empty
			//patternsShot[0] = new DynamicPattern(null);

			// custom parameters
			customParameters = new DynamicCustomParameter[1];
			DynamicCustomParameter dcp = new DynamicCustomParameter();
			dcp.name = "_PowerLevel";
			dcp.type = ParameterType.Float;
			dcp.floatValue = new DynamicFloat(1.0f);
			customParameters[0] = dcp;

			// lifespan and spawn
			lifespan = new DynamicFloat(5.0f);
			timeBeforeSpawn = new DynamicFloat(0.0f);
			playAudioAtSpawn = new DynamicBool(false);
			audioClip = new DynamicObjectReference(null);
			audioClip.SetNarrowType(typeof(AudioClip));
			isChildOfEmitter = new DynamicBool(false);

			// movement
			forwardSpeed = new DynamicFloat(5.0f);
			angularSpeed = new DynamicFloat(0f);
			startScale = new DynamicFloat(1.0f);

			// enabling modules
			isVisible = true;
			canMove = true;
			canCollide = true;
			hasLifespan = true;

			// editor
			preview.gizmoColor = new Color(0, 0, 0.5f, 1);
			currentlyDisplayedModule = ParamInspectorPart.Renderer;
		}
#endif
	}

	// Editor only : this struct stores data for the bullet preview in the inspector.
#if UNITY_EDITOR
	[System.Serializable]
	public struct BulletPreview
	{
		public Texture collidersTex;
		public Sprite sprite;
		public Color gizmoColor;
	}
#endif

	// Rendering mode : 2D sprite, or 3D mesh
	public enum BulletRenderMode { Sprite, Mesh }

	// Homing settings : which target will we chase ?
	public enum PreferredTarget { Oldest, Newest, Closest, Farthest, Random }

	// Can help if we want to call Bullet.ChangeBulletParams() for only part of the params
	[System.Flags]
	public enum BulletParamMask
	{
		Visibility = (1 << 0),
		Lifespan = (1 << 1),
		Movement = (1 << 2),
		Collision = (1 << 3),
		Homing = (1 << 4),
		Patterns = (1 << 5),
		DelaySpawn = (1 << 6),
		Parameters = (1 << 7)
	}
}