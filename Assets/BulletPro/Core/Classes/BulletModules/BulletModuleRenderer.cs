using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	public enum ColorBlend { Replace, Multiply, Add, AlphaBlend, Average }

	// Module that handles bullet rendering
	public class BulletModuleRenderer : BulletModule
	{
		#region properties

		// Restoring this when the bullet dies
		int defaultSortingLayer;
		int defaultSortingOrder;

		// VFX-related vars
		public bool playVFXOnBirth, playVFXOnDeath;
		public float vfxParticleSize;
		public bool useCustomBirthVFX, useCustomDeathVFX;
		public ParticleSystem customBirthVFX, customDeathVFX;

		// Animation-related vars
		public bool animated;
		public Sprite[] animationSprites; // used if animated
		public float animationFramerate
		{
			get { return _animationFramerate; }
			set
			{
				if (value != 0)
				{
					_animationFramerate = value;
					invAnimationFramerate = 1 / value;
				}
			}
		}
		private float _animationFramerate;
		public WrapMode animationWrapMode;
		// Sprite animation helpers
		float invAnimationFramerate;
		float timeTillNextFrame;
		public int currentFrame { get; private set; }

		// Curves
		public BulletCurve colorOverLifetime;
		public BulletCurve alphaOverLifetime;

		// Color		
		public Color startColor;
		public ColorBlend evolutionBlendWithBaseColor;
		public Gradient colorEvolution;

		// Only ever used when switching evolution gradient over time
		public Gradient nextColorEvolution;
		public float ratioToNextGradient;
		public bool isSwitchingGradient;

		// Immovable data
		public BulletRenderMode renderMode { get; private set; }

		#endregion

		// Called at Bullet.AWake()
		public override void Awake()
		{
			base.Awake();
			if (spriteRenderer)
			{
				defaultSortingLayer = spriteRenderer.sortingLayerID;
				defaultSortingOrder = spriteRenderer.sortingOrder;
				renderMode = BulletRenderMode.Sprite;
			}
			else if (meshRenderer)
			{
				defaultSortingLayer = meshRenderer.sortingLayerID;
				defaultSortingOrder = meshRenderer.sortingOrder;
				renderMode = BulletRenderMode.Mesh;
			}
		}

		public override void Enable()
		{
			base.Enable();
			SetVisible(true);
		}

		public override void Disable()
		{
			SetVisible(false);
			base.Disable();
		}

		public void SetVisible(bool visible)
		{
			if (renderMode == BulletRenderMode.Sprite)
				spriteRenderer.enabled = visible;
			else if (renderMode == BulletRenderMode.Mesh)
				meshRenderer.enabled = visible;
		}

		// Called at Bullet.Update() only for sprite bullets
		public void Update()
		{
			// enabled spawn module means we're still waiting for the actual spawn
			if (moduleSpawn.isEnabled) return;
			
			// Color curve
			Color baseColor = startColor;
			if (colorOverLifetime.enabled)
			{
				colorOverLifetime.Update();
				Color gradColor = colorEvolution.Evaluate(colorOverLifetime.GetCurveResult());
				if (isSwitchingGradient)
				{
					Color nextColor = nextColorEvolution.Evaluate(colorOverLifetime.GetCurveResult());
					gradColor = Color.Lerp(gradColor, nextColor, ratioToNextGradient);
				}
				baseColor = BlendColors(baseColor, gradColor, evolutionBlendWithBaseColor);
			}

			// Alpha curve : we don't use the same reference value if the color changes over time, because base alpha might change too
			if (alphaOverLifetime.enabled)
			{
				alphaOverLifetime.Update();
				baseColor.a *= alphaOverLifetime.GetCurveResult();
			}

			// Applying final color and alpha
			spriteRenderer.color = baseColor;

			// Animation
			if (animated && animationSprites.Length > 0)
			{
				timeTillNextFrame -= Time.deltaTime;
				if (timeTillNextFrame <= 0)
				{
					currentFrame++;
					timeTillNextFrame = invAnimationFramerate;

					if (animationWrapMode == WrapMode.Loop)
						spriteRenderer.sprite = animationSprites[currentFrame % animationSprites.Length];
					else if (animationWrapMode == WrapMode.PingPong)
						spriteRenderer.sprite = animationSprites[(animationSprites.Length - 1) - Mathf.Abs((currentFrame % (2 * animationSprites.Length - 2)) - (animationSprites.Length - 1))];
					else // if default, once, clamp
						spriteRenderer.sprite = animationSprites[currentFrame > (animationSprites.Length - 1) ? animationSprites.Length - 1 : currentFrame];
				}
			}
		}

		// Called at Bullet.Die()
		public void Die()
		{
			if (spriteRenderer)
			{
				spriteRenderer.sortingLayerID = defaultSortingLayer;
				spriteRenderer.sortingOrder = defaultSortingOrder;
			}

			if (meshRenderer)
			{
				meshRenderer.sortingLayerID = defaultSortingLayer;
				meshRenderer.sortingOrder = defaultSortingOrder;
			}
		}

		// Called at Bullet.ApplyBulletParams()
		public void ApplyBulletParams(BulletParams bp)
		{
			isEnabled = bp.isVisible;

			// make sure curves are not running unless told to
			colorOverLifetime.Stop();
			alphaOverLifetime.Stop();
			isSwitchingGradient = false;

			if (!isEnabled)
			{
				// reset curves in case module gets reenabled later on
				colorOverLifetime.enabled = false;
				alphaOverLifetime.enabled = false;
				return;
			}

			// VFX stuff
			playVFXOnBirth = solver.SolveDynamicBool(bp.playVFXOnBirth, 3956329, ParameterOwner.Bullet);
			playVFXOnDeath = solver.SolveDynamicBool(bp.playVFXOnDeath, 28263241, ParameterOwner.Bullet);
			vfxParticleSize = solver.SolveDynamicFloat(bp.vfxParticleSize, 14404239, ParameterOwner.Bullet);
			useCustomBirthVFX = bp.useCustomBirthVFX;
			useCustomDeathVFX = bp.useCustomDeathVFX;
			customBirthVFX = solver.SolveDynamicObjectReference(bp.customBirthVFX, 15079187, ParameterOwner.Bullet) as ParticleSystem;
			customDeathVFX = solver.SolveDynamicObjectReference(bp.customDeathVFX, 5117134, ParameterOwner.Bullet) as ParticleSystem;

			// If a mesh is used, only update mesh then end function, as it won't support curves and animation
			if (bp.renderMode == BulletRenderMode.Mesh)
			{
				Mesh bpMesh = solver.SolveDynamicObjectReference(bp.mesh, 10515834, ParameterOwner.Bullet) as Mesh;
				if (!bpMesh) Debug.LogError("Missing mesh in BulletParams!");
				else meshFilter.mesh = bpMesh;

				Material bpm = solver.SolveDynamicObjectReference(bp.material, 26159535, ParameterOwner.Bullet) as Material;
				if (bpm) meshRenderer.material = bpm;
				
				meshRenderer.sortingOrder = solver.SolveDynamicInt(bp.sortingOrder, 24980073, ParameterOwner.Bullet);
				string mslName = solver.SolveDynamicString(bp.sortingLayerName, 11378366, ParameterOwner.Bullet);
				if (!string.IsNullOrEmpty(mslName)) meshRenderer.sortingLayerName = mslName;

				return;
			}

			// Sprite Renderer basic properties : material, Z-sorting
			spriteRenderer.sortingOrder = solver.SolveDynamicInt(bp.sortingOrder, 21040852, ParameterOwner.Bullet);
			string slName = solver.SolveDynamicString(bp.sortingLayerName, 6838518, ParameterOwner.Bullet);
			if (!string.IsNullOrEmpty(slName)) spriteRenderer.sortingLayerName = slName;
			Material bpMaterial = solver.SolveDynamicObjectReference(bp.material, 20796646, ParameterOwner.Bullet) as Material;
			if (bpMaterial) spriteRenderer.material = bpMaterial;

			// Actual sprite
			Sprite bpSprite = solver.SolveDynamicObjectReference(bp.sprite, 4048455, ParameterOwner.Bullet) as Sprite;
			if (bpSprite)
				spriteRenderer.sprite = bpSprite;
			else if (bp.sprites != null) // comes in handy if we want to assign a sprite array without immediately animating
				if (bp.sprites.Length > 0)
					spriteRenderer.sprite = solver.SolveDynamicObjectReference(bp.sprites[0], 11591529, ParameterOwner.Bullet) as Sprite;

			// Animation
			animated = solver.SolveDynamicBool(bp.animated, 32041608, ParameterOwner.Bullet);
			if (animated)
			{
				float af = solver.SolveDynamicFloat(bp.animationFramerate, 16124287, ParameterOwner.Bullet);

				if (bp.sprites != null)
				{
					Sprite[] animSprites = new Sprite[bp.sprites.Length];
					if (bp.sprites.Length > 0)
						for (int i = 0; i < bp.sprites.Length; i++)
							animSprites[i] = solver.SolveDynamicObjectReference(bp.sprites[i], 16729285, ParameterOwner.Bullet) as Sprite;
					InitAnimation(animSprites, af, bp.animationWrapMode);
				}
				else InitAnimation(null, af, bp.animationWrapMode);
			}

			// Color
			spriteRenderer.color = solver.SolveDynamicColor(bp.color, 29256386, ParameterOwner.Bullet);
			startColor = spriteRenderer.color;

			// Color and alpha curves
			colorOverLifetime = solver.SolveDynamicBulletCurve(bp.colorOverLifetime, 13777134, ParameterOwner.Bullet);
			colorOverLifetime.UpdateInternalValues(bullet);
			alphaOverLifetime = solver.SolveDynamicBulletCurve(bp.alphaOverLifetime, 21591529, ParameterOwner.Bullet);
			alphaOverLifetime.UpdateInternalValues(bullet);

			// Computing lifetime gradient
			if (colorOverLifetime.enabled)
			{
				evolutionBlendWithBaseColor = (ColorBlend)solver.SolveDynamicEnum(bp.evolutionBlendWithBaseColor, 21335864, ParameterOwner.Bullet);
				colorEvolution = solver.SolveDynamicGradient(bp.colorEvolution, 2491116, ParameterOwner.Bullet);		
				spriteRenderer.color = BlendColors(startColor, colorEvolution.Evaluate(0), evolutionBlendWithBaseColor);
				colorOverLifetime.Boot();	
			}

			// Reassigning start alpha if alpha curve is enabled (and does not start at one)
			if (alphaOverLifetime.enabled)
			{
				float startAlpha = alphaOverLifetime.curve.Evaluate(0);
				spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, spriteRenderer.color.a * startAlpha);
				alphaOverLifetime.Boot();
			}
		}

		// Launches VFX if needed. If a mesh uses default, it will be white.
		public void SpawnFX(Vector3 position, bool birth) // true = birth, false = death
		{
			if (birth)
			{
				if (useCustomBirthVFX)
				{
					if (!customBirthVFX) { Debug.LogWarning(bullet.name + " : missing custom birth VFX!"); return; }
					vfxManager.PlayVFXAt(position, self.eulerAngles, customBirthVFX, moduleMovement.currentScale * vfxParticleSize);
				}
				else
				{
					Color col = Color.white;
					if (bullet.renderMode == BulletRenderMode.Sprite) col = spriteRenderer.color;
					vfxManager.PlayVFXAt(position, self.eulerAngles, col, moduleMovement.currentScale * vfxParticleSize);
				}
			}
			else // equals if (death).
			{
				if (useCustomDeathVFX)
				{
					if (!customDeathVFX) { Debug.LogWarning(bullet.name + " : missing custom death VFX!"); return; }
					vfxManager.PlayVFXAt(position, self.eulerAngles, customDeathVFX, moduleMovement.currentScale * vfxParticleSize);
				}
				else
				{
					Color col = Color.white;
					if (bullet.renderMode == BulletRenderMode.Sprite) col = spriteRenderer.color;
					vfxManager.PlayVFXAt(position, self.eulerAngles, col, moduleMovement.currentScale * vfxParticleSize);
				}
			}
		}

		// Launches VFX if needed. If a mesh uses default, it will be white.
		public void SpawnFX(bool birth) // true = birth, false = death
		{
			SpawnFX(self.position, birth);
		}

		// Launches a custom VFX at bullet position and size. Made to be called from Patterns. 
		public void SpawnFX(ParticleSystem particleSystemSettings)
		{
			if (particleSystemSettings == null) return;
			vfxManager.PlayVFXAt(self.position, self.eulerAngles, particleSystemSettings, moduleMovement.currentScale * vfxParticleSize);
		}

		// Launches the default VFX at bullet position, size and color.
		public void SpawnDefaultVFX(bool copyBulletColor=true)
		{
			Color col = Color.white;
			if (copyBulletColor)
				if (bullet.renderMode == BulletRenderMode.Sprite)
					col = spriteRenderer.color;
			vfxManager.PlayVFXAt(self.position, self.eulerAngles, col, moduleMovement.currentScale * vfxParticleSize);
		}

		public Color BlendColors(Color oldCol, Color newCol, ColorBlend blendType)
		{
			if (blendType == ColorBlend.Replace) return newCol;
			if (blendType == ColorBlend.Add) return newCol + oldCol;
			if (blendType == ColorBlend.Multiply) return newCol * oldCol;
			if (blendType == ColorBlend.AlphaBlend)
			{
				float invA = 1f-newCol.a;
				return new Color(oldCol.r*invA+newCol.r*newCol.a, oldCol.g*invA+newCol.g*newCol.a, oldCol.b*invA+newCol.b*newCol.a, oldCol.a+newCol.a*(1f-oldCol.a));
			}
			// default is ColorBlend.Average
			return new Color((oldCol.r+newCol.r)*0.5f, (oldCol.g+newCol.g)*0.5f, (oldCol.b+newCol.b)*0.5f, (oldCol.a+newCol.a)*0.5f);
		}

		#region bullet sprite animation toolbox

		// Resets animation with new framerate
		public void InitAnimation(Sprite[] newSprites, float framerate, WrapMode wrapMode)
		{
			if (newSprites == null) { Debug.Log("Cannot start bullet animation, sprite list is missing!"); animated = false; return; }
			if (newSprites.Length == 0) { Debug.Log("Cannot start bullet animation, sprite list is missing!"); animated = false; return; }

			animated = true;

			// set new sprites, new invFramerate (default to 12 fps) and new wrapMode
			animationFramerate = framerate;
			invAnimationFramerate = framerate > 0 ? 1 / framerate : 0.833333f;
			animationWrapMode = wrapMode;
			animationSprites = newSprites;

			// init animation with new invFramerate
			SetFrame(0);
			timeTillNextFrame = invAnimationFramerate;
		}

		// Overload : resets animation, only changing sprites
		public void InitAnimation(Sprite[] newSprites) { InitAnimation(newSprites, animationFramerate, animationWrapMode); }

		// Overload : resets animation, only changing framerate
		public void InitAnimation(float framerate) { InitAnimation(animationSprites, framerate, animationWrapMode); }

		// Overload : resets animation, only changing wrapMode
		public void InitAnimation(WrapMode wrapMode) { InitAnimation(animationSprites, animationFramerate, wrapMode); }

		// Overload : resets animation, only changing sprites and framerate
		public void InitAnimation(Sprite[] newSprites, float framerate) { InitAnimation(newSprites, framerate, animationWrapMode); }

		// Overload : resets animation, only changing sprites and wrapMode
		public void InitAnimation(Sprite[] newSprites, WrapMode wrapMode) { InitAnimation(newSprites, animationFramerate, wrapMode); }

		// Overload : resets animation, only changing framerate and wrapMode
		public void InitAnimation(float framerate, WrapMode wrapMode) { InitAnimation(animationSprites, framerate, wrapMode); }

		// Overload : resets animation without changing framerate nor wrapmode
		public void InitAnimation() { InitAnimation(animationSprites, animationFramerate, animationWrapMode); }

		// Jumps animation to selected frame
		public void SetFrame(int frameNumber)
		{
			if (animationSprites == null) return;
			if (animationSprites.Length == 0) return;

			if (frameNumber < 0) frameNumber = 0;
			frameNumber = frameNumber % animationSprites.Length;

			currentFrame = frameNumber;
			if (animationSprites[frameNumber])
				bullet.spriteRenderer.sprite = animationSprites[frameNumber];
		}

		#endregion

	}
}