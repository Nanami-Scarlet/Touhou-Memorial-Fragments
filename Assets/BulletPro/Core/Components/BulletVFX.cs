using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	[RequireComponent(typeof(ParticleSystem))]
	public class BulletVFX : MonoBehaviour
	{
		public Transform thisTransform;
		public ParticleSystem thisParticleSystem;
		private ParticleSystem defaultPS;
		public ParticleSystemRenderer thisParticleRenderer;
		private ParticleSystemRenderer defaultPSR;
		private bool isDefault; // so we don't have to copy from default particles every time
		private Transform bulletCanvas;

		void Start()
		{
			defaultPS = BulletVFXManager.instance.defaultParticles;
			defaultPSR = BulletVFXManager.instance.defaultParticleRenderer;
			bulletCanvas = BulletPoolManager.instance.mainTransform;
			CopyParticlesFrom(defaultPS, defaultPSR);
			isDefault = true;
		}

		// Overload 1 : play the default VFX with wanted orientation, color and size
		public void Play(Vector3 position, float rotation, Color color, float size)
		{
			if (!isDefault)
			{
				CopyParticlesFrom(defaultPS, defaultPSR);
				isDefault = true;
			}

			thisTransform.position = position;
			thisTransform.rotation = bulletCanvas.rotation;
			thisTransform.Rotate(Vector3.forward, rotation, Space.Self);
			ParticleSystem.MainModule mainModule = thisParticleSystem.main;
			mainModule.startColor = color;
			mainModule.startSize = size;
			thisParticleSystem.Play();
		}

		// Overload 2 : set VFX to wanted ParticleSystem settings and then play it
		public void Play(Vector3 position, float rotation, ParticleSystem psSettings, float size)
		{
			isDefault = false;
			thisTransform.position = position;
			thisTransform.rotation = bulletCanvas.rotation;
			thisTransform.Rotate(Vector3.forward, rotation, Space.Self);
			CopyParticlesFrom(psSettings, psSettings.GetComponent<ParticleSystemRenderer>());
			ParticleSystem.MainModule mainModule = thisParticleSystem.main;
			ParticleSystem.MinMaxCurve newCurve = mainModule.startSize;
			newCurve.curveMultiplier *= size;
			newCurve.constant *= size;
			newCurve.constantMin *= size;
			newCurve.constantMax *= size;
			mainModule.startSize = newCurve;
			thisParticleSystem.Play();
		}

		// Overload 3 : similar to overload 1, but with full rotation transmitted via (global) eulerAngles
		public void Play(Vector3 position, Vector3 eulerAngles, Color color, float size)
		{
			if (!isDefault)
			{
				CopyParticlesFrom(defaultPS, defaultPSR);
				isDefault = true;
			}

			thisTransform.position = position;
			thisTransform.eulerAngles = eulerAngles;
			ParticleSystem.MainModule mainModule = thisParticleSystem.main;
			mainModule.startColor = color;
			mainModule.startSize = size;
			thisParticleSystem.Play();
		}

		// Overload 4 : similar to overload 2, but with full rotation transmitted via (global) eulerAngles
		public void Play(Vector3 position, Vector3 eulerAngles, ParticleSystem psSettings, float size)
		{
			isDefault = false;
			thisTransform.position = position;
			thisTransform.eulerAngles = eulerAngles;
			CopyParticlesFrom(psSettings, psSettings.GetComponent<ParticleSystemRenderer>());
			ParticleSystem.MainModule mainModule = thisParticleSystem.main;
			ParticleSystem.MinMaxCurve newCurve = mainModule.startSize;
			newCurve.curveMultiplier *= size;
			newCurve.constant *= size;
			newCurve.constantMin *= size;
			newCurve.constantMax *= size;
			mainModule.startSize = newCurve;
			thisParticleSystem.Play();
		}

		// Copies settings from another ParticleSystem. There's a ton of properties so this is long. But it gives better perfs than reflection.
		public void CopyParticlesFrom(ParticleSystem psSettings, ParticleSystemRenderer psrSettings)
		{
			CopyMainModuleFrom(psSettings);
			CopyParticleSystemRendererFrom(psrSettings);

			if (psSettings.collision.enabled) CopyCollisionModuleFrom(psSettings);
			else { ParticleSystem.CollisionModule m = thisParticleSystem.collision; m.enabled = false; }

			if (psSettings.colorBySpeed.enabled) CopyColorBySpeedModuleFrom(psSettings);
			else { ParticleSystem.ColorBySpeedModule m = thisParticleSystem.colorBySpeed; m.enabled = false; }

			if (psSettings.colorOverLifetime.enabled) CopyColorOverLifetimeModuleFrom(psSettings);
			else { ParticleSystem.ColorOverLifetimeModule m = thisParticleSystem.colorOverLifetime; m.enabled = false; }

			if (psSettings.emission.enabled) CopyEmissionModuleFrom(psSettings);
			else { ParticleSystem.EmissionModule m = thisParticleSystem.emission; m.enabled = false; }

			if (psSettings.externalForces.enabled) CopyExternalForcesModuleFrom(psSettings);
			else { ParticleSystem.ExternalForcesModule m = thisParticleSystem.externalForces; m.enabled = false; }

			if (psSettings.forceOverLifetime.enabled) CopyForceOverLifetimeModuleFrom(psSettings);
			else { ParticleSystem.ForceOverLifetimeModule m = thisParticleSystem.forceOverLifetime; m.enabled = false; }

			if (psSettings.inheritVelocity.enabled) CopyInheritVelocityModuleFrom(psSettings);
			else { ParticleSystem.InheritVelocityModule m = thisParticleSystem.inheritVelocity; m.enabled = false; }

			if (psSettings.lights.enabled) CopyLightsModuleFrom(psSettings);
			else { ParticleSystem.LightsModule m = thisParticleSystem.lights; m.enabled = false; }

			if (psSettings.limitVelocityOverLifetime.enabled) CopyLimitVelocityOverLifetimeModuleFrom(psSettings);
			else { ParticleSystem.LimitVelocityOverLifetimeModule m = thisParticleSystem.limitVelocityOverLifetime; m.enabled = false; }

			if (psSettings.noise.enabled) CopyNoiseModuleFrom(psSettings);
			else { ParticleSystem.NoiseModule m = thisParticleSystem.noise; m.enabled = false; }

			if (psSettings.rotationBySpeed.enabled) CopyRotationBySpeedModuleFrom(psSettings);
			else { ParticleSystem.RotationBySpeedModule m = thisParticleSystem.rotationBySpeed; m.enabled = false; }

			if (psSettings.rotationOverLifetime.enabled) CopyRotationOverLifetimeModuleFrom(psSettings);
			else { ParticleSystem.RotationOverLifetimeModule m = thisParticleSystem.rotationOverLifetime; m.enabled = false; }

			if (psSettings.shape.enabled) CopyShapeModuleFrom(psSettings);
			else { ParticleSystem.ShapeModule m = thisParticleSystem.shape; m.enabled = false; }

			if (psSettings.sizeBySpeed.enabled) CopySizeBySpeedModuleFrom(psSettings);
			else { ParticleSystem.SizeBySpeedModule m = thisParticleSystem.sizeBySpeed; m.enabled = false; }

			if (psSettings.sizeOverLifetime.enabled) CopySizeOverLifetimeModuleFrom(psSettings);
			else { ParticleSystem.SizeOverLifetimeModule m = thisParticleSystem.sizeOverLifetime; m.enabled = false; }

			if (psSettings.subEmitters.enabled) CopySubEmittersModuleFrom(psSettings);
			else { ParticleSystem.SubEmittersModule m = thisParticleSystem.subEmitters; m.enabled = false; }

			if (psSettings.textureSheetAnimation.enabled) CopyTextureSheetAnimationModuleFrom(psSettings);
			else { ParticleSystem.TextureSheetAnimationModule m = thisParticleSystem.textureSheetAnimation; m.enabled = false; }

			if (psSettings.trails.enabled) CopyTrailModuleFrom(psSettings);
			else { ParticleSystem.TrailModule m = thisParticleSystem.trails; m.enabled = false; }

			if (psSettings.trigger.enabled) CopyTriggerModuleFrom(psSettings);
			else { ParticleSystem.TriggerModule m = thisParticleSystem.trigger; m.enabled = false; }

			if (psSettings.velocityOverLifetime.enabled) CopyVelocityOverLifetimeModuleFrom(psSettings);
			else { ParticleSystem.VelocityOverLifetimeModule m = thisParticleSystem.velocityOverLifetime; m.enabled = false; }
		}

		#region Copy each module property by property (Unity doesn't support something like GetModule<T>...)

		void CopyMainModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.MainModule m = thisParticleSystem.main;
			ParticleSystem.MainModule m2 = ps.main;

			m.customSimulationSpace = m2.customSimulationSpace;
			m.duration = m2.duration;
			m.gravityModifier = m2.gravityModifier;
			m.gravityModifierMultiplier = m2.gravityModifierMultiplier;
			m.loop = m2.loop;
			m.maxParticles = m2.maxParticles;
			m.playOnAwake = m2.playOnAwake;
			m.prewarm = m2.prewarm;
#if UNITY_2018_1_OR_NEWER
			m.flipRotation = m2.flipRotation;
#else
			m.randomizeRotationDirection = m2.randomizeRotationDirection;
#endif
			m.scalingMode = m2.scalingMode;
			m.simulationSpace = m2.simulationSpace;
			m.simulationSpeed = m2.simulationSpeed;
			m.startColor = m2.startColor;
			m.startDelay = m2.startDelay;
			m.startDelayMultiplier = m2.startDelayMultiplier;
			m.startLifetime = m2.startLifetime;
			m.startLifetimeMultiplier = m2.startLifetimeMultiplier;
			m.startRotation = m2.startRotation;
			m.startRotation3D = m2.startRotation3D;
			m.startRotationMultiplier = m2.startRotationMultiplier;
			m.startRotationX = m2.startRotationX;
			m.startRotationXMultiplier = m2.startRotationXMultiplier;
			m.startRotationY = m2.startRotationY;
			m.startRotationYMultiplier = m2.startRotationYMultiplier;
			m.startRotationZ = m2.startRotationZ;
			m.startRotationZMultiplier = m2.startRotationZMultiplier;
			m.startSize = m2.startSize;
			m.startSize3D = m2.startSize3D;
			m.startSizeMultiplier = m2.startSizeMultiplier;
			m.startSizeX = m2.startSizeX;
			m.startSizeXMultiplier = m2.startSizeXMultiplier;
			m.startSizeY = m2.startSizeY;
			m.startSizeYMultiplier = m2.startSizeYMultiplier;
			m.startSizeZ = m2.startSizeZ;
			m.startSizeZMultiplier = m2.startSizeZMultiplier;
			m.startSpeed = m2.startSpeed;
			m.startSpeedMultiplier = m2.startSpeedMultiplier;
		}

		void CopyCollisionModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.CollisionModule m = thisParticleSystem.collision;
			ParticleSystem.CollisionModule m2 = ps.collision;

			m.enabled = true;

			m.bounce = m2.bounce;
			m.bounceMultiplier = m2.bounceMultiplier;
			m.collidesWith = m2.collidesWith;
			m.dampen = m2.dampen;
			m.dampenMultiplier = m2.dampenMultiplier;
			m.enableDynamicColliders = m2.enableDynamicColliders;
			//m.enableInteriorCollisions = m2.enableInteriorCollisions; // deprecated as of 2017.1
			m.lifetimeLoss = m2.lifetimeLoss;
			m.lifetimeLossMultiplier = m2.lifetimeLossMultiplier;
			m.maxCollisionShapes = m2.maxCollisionShapes;
			m.maxKillSpeed = m2.maxKillSpeed;
			//m.maxPlaneCount = m2.maxPlaneCount; // is read only
			m.minKillSpeed = m2.minKillSpeed;
			m.mode = m2.mode;
			m.quality = m2.quality;
			m.radiusScale = m2.radiusScale;
			m.sendCollisionMessages = m2.sendCollisionMessages;
			m.type = m2.type;
			m.voxelSize = m2.voxelSize;

			// GetPlanes and SetPlanes could (or should) be called here, they're quite shady functions
		}

		void CopyColorBySpeedModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.ColorBySpeedModule m = thisParticleSystem.colorBySpeed;
			ParticleSystem.ColorBySpeedModule m2 = ps.colorBySpeed;

			m.enabled = true;

			m.color = m2.color;
			m.range = m2.range;
		}

		void CopyColorOverLifetimeModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.ColorOverLifetimeModule m = thisParticleSystem.colorOverLifetime;
			ParticleSystem.ColorOverLifetimeModule m2 = ps.colorOverLifetime;

			m.enabled = true;
			m.color = m2.color;
		}

		void CopyEmissionModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.EmissionModule m = thisParticleSystem.emission;
			ParticleSystem.EmissionModule m2 = ps.emission;

			m.enabled = true;

			//m.burstCount = m2.burstCount; // is read only
			//m.rate = m2.rate; // is deprecated
			//m.rateMultiplier = m2.rateMultiplier; // is deprecated
			m.rateOverDistance = m2.rateOverDistance;
			m.rateOverDistanceMultiplier = m2.rateOverDistanceMultiplier;
			m.rateOverTime = m2.rateOverTime;
			m.rateOverTimeMultiplier = m2.rateOverTimeMultiplier;
			//m.type = m2.type; // is deprecated

			if (m2.burstCount == 0)
			{
				if (m.burstCount > 0)
					m.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 0) });
			}
			else // if (m2.burstCount > 0)
			{
				ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[m2.burstCount];
				m2.GetBursts(bursts);
				m.SetBursts(bursts);
			}
		}

		void CopyExternalForcesModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.ExternalForcesModule m = thisParticleSystem.externalForces;
			ParticleSystem.ExternalForcesModule m2 = ps.externalForces;

			m.enabled = true;
			m.multiplier = m2.multiplier;
		}

		void CopyForceOverLifetimeModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.ForceOverLifetimeModule m = thisParticleSystem.forceOverLifetime;
			ParticleSystem.ForceOverLifetimeModule m2 = ps.forceOverLifetime;

			m.enabled = true;

			m.randomized = m2.randomized;
			m.space = m2.space;
			m.x = m2.x;
			m.y = m2.y;
			m.z = m2.z;
			m.xMultiplier = m2.xMultiplier;
			m.yMultiplier = m2.yMultiplier;
			m.zMultiplier = m2.zMultiplier;
		}

		void CopyInheritVelocityModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.InheritVelocityModule m = thisParticleSystem.inheritVelocity;
			ParticleSystem.InheritVelocityModule m2 = ps.inheritVelocity;

			m.enabled = true;

			m.curve = m2.curve;
			m.curveMultiplier = m2.curveMultiplier;
			m.mode = m2.mode;
		}

		void CopyLightsModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.LightsModule m = thisParticleSystem.lights;
			ParticleSystem.LightsModule m2 = ps.lights;

			m.enabled = true;

			m.alphaAffectsIntensity = m2.alphaAffectsIntensity;
			m.intensity = m2.intensity;
			m.intensityMultiplier = m2.intensityMultiplier;
			m.light = m2.light;
			m.maxLights = m2.maxLights;
			m.range = m2.range;
			m.rangeMultiplier = m2.rangeMultiplier;
			m.ratio = m2.ratio;
			m.sizeAffectsRange = m2.sizeAffectsRange;
			m.useParticleColor = m2.useParticleColor;
			m.useRandomDistribution = m2.useRandomDistribution;
		}

		void CopyLimitVelocityOverLifetimeModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.LimitVelocityOverLifetimeModule m = thisParticleSystem.limitVelocityOverLifetime;
			ParticleSystem.LimitVelocityOverLifetimeModule m2 = ps.limitVelocityOverLifetime;

			m.enabled = true;

			m.dampen = m2.dampen;
			m.limit = m2.limit;
			m.limitMultiplier = m2.limitMultiplier;
			m.limitX = m2.limitX;
			m.limitXMultiplier = m2.limitXMultiplier;
			m.limitY = m2.limitY;
			m.limitYMultiplier = m2.limitYMultiplier;
			m.limitZ = m2.limitZ;
			m.limitZMultiplier = m2.limitZMultiplier;
			m.separateAxes = m2.separateAxes;
			m.space = m2.space;
		}

		void CopyNoiseModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.NoiseModule m = thisParticleSystem.noise;
			ParticleSystem.NoiseModule m2 = ps.noise;

			m.enabled = true;

			m.damping = m2.damping;
			m.frequency = m2.frequency;
			m.octaveCount = m2.octaveCount;
			m.octaveMultiplier = m2.octaveMultiplier;
			m.octaveScale = m2.octaveScale;
			m.quality = m2.quality;
			m.remap = m2.remap;
			m.remapEnabled = m2.remapEnabled;
			m.remapMultiplier = m2.remapMultiplier;
			m.remapX = m2.remapX;
			m.remapXMultiplier = m2.remapXMultiplier;
			m.remapY = m2.remapY;
			m.remapYMultiplier = m2.remapYMultiplier;
			m.remapZ = m2.remapZ;
			m.remapZMultiplier = m2.remapZMultiplier;
			m.scrollSpeed = m2.scrollSpeed;
			m.scrollSpeedMultiplier = m2.scrollSpeedMultiplier;
			m.separateAxes = m2.separateAxes;
			m.strength = m2.strength;
			m.strengthMultiplier = m2.strengthMultiplier;
			m.strengthX = m2.strengthX;
			m.strengthXMultiplier = m2.strengthXMultiplier;
			m.strengthY = m2.strengthY;
			m.strengthYMultiplier = m2.strengthYMultiplier;
			m.strengthZ = m2.strengthZ;
			m.strengthZMultiplier = m2.strengthZMultiplier;
		}

		void CopyRotationBySpeedModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.RotationBySpeedModule m = thisParticleSystem.rotationBySpeed;
			ParticleSystem.RotationBySpeedModule m2 = ps.rotationBySpeed;

			m.enabled = true;
			m.range = m2.range;
			m.separateAxes = m2.separateAxes;
			m.x = m2.x;
			m.xMultiplier = m2.xMultiplier;
			m.y = m2.y;
			m.yMultiplier = m2.yMultiplier;
			m.z = m2.z;
			m.zMultiplier = m2.zMultiplier;
		}

		void CopyRotationOverLifetimeModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.RotationOverLifetimeModule m = thisParticleSystem.rotationOverLifetime;
			ParticleSystem.RotationOverLifetimeModule m2 = ps.rotationOverLifetime;

			m.enabled = true;

			m.separateAxes = m2.separateAxes;
			m.x = m2.x;
			m.xMultiplier = m2.xMultiplier;
			m.y = m2.y;
			m.yMultiplier = m2.yMultiplier;
			m.z = m2.z;
			m.zMultiplier = m2.zMultiplier;
		}

		void CopyShapeModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.ShapeModule m = thisParticleSystem.shape;
			ParticleSystem.ShapeModule m2 = ps.shape;

			m.enabled = true;

#if UNITY_5
		m.box = m2.box;
#else // new field name for Unity 2017
			m.scale = m2.scale;
#endif

			m.alignToDirection = m2.alignToDirection;
			m.angle = m2.angle;
			m.arc = m2.arc;
			m.length = m2.length;
			m.mesh = m2.mesh;
			m.meshMaterialIndex = m2.meshMaterialIndex;
			m.meshRenderer = m2.meshRenderer;
			//m.meshScale = m2.meshScale;
			m.scale = m2.scale; // .meshScale is now called .scale from 2017.1
			m.meshShapeType = m2.meshShapeType;
			m.normalOffset = m2.normalOffset;
			m.position = m2.position;
			m.rotation = m2.rotation;
			m.radius = m2.radius;
			//m.randomDirection = m2.randomDirection; // is deprecated
			m.randomDirectionAmount = m2.randomDirectionAmount;
			m.shapeType = m2.shapeType;
			m.skinnedMeshRenderer = m2.skinnedMeshRenderer;
			m.sphericalDirectionAmount = m2.sphericalDirectionAmount;
			m.useMeshColors = m2.useMeshColors;
			m.useMeshMaterialIndex = m2.useMeshMaterialIndex;
		}

		void CopySizeBySpeedModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.SizeBySpeedModule m = thisParticleSystem.sizeBySpeed;
			ParticleSystem.SizeBySpeedModule m2 = ps.sizeBySpeed;

			m.enabled = true;

			m.range = m2.range;
			m.separateAxes = m2.separateAxes;
			m.size = m2.size;
			m.sizeMultiplier = m2.sizeMultiplier;
			m.x = m2.x;
			m.xMultiplier = m2.xMultiplier;
			m.y = m2.y;
			m.yMultiplier = m2.yMultiplier;
			m.z = m2.z;
			m.zMultiplier = m2.zMultiplier;
		}

		void CopySizeOverLifetimeModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.SizeOverLifetimeModule m = thisParticleSystem.sizeOverLifetime;
			ParticleSystem.SizeOverLifetimeModule m2 = ps.sizeOverLifetime;

			m.enabled = true;

			m.separateAxes = m2.separateAxes;
			m.size = m2.size;
			m.sizeMultiplier = m2.sizeMultiplier;
			m.x = m2.x;
			m.xMultiplier = m2.xMultiplier;
			m.y = m2.y;
			m.yMultiplier = m2.yMultiplier;
			m.z = m2.z;
			m.zMultiplier = m2.zMultiplier;
		}

		void CopySubEmittersModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.SubEmittersModule m = thisParticleSystem.subEmitters;
			ParticleSystem.SubEmittersModule m2 = ps.subEmitters;

			m.enabled = true;

			if (m.subEmittersCount > 0)
			{
				int max = m.subEmittersCount;
				for (int i = 0; i < max; i++)
					m.RemoveSubEmitter(0);
			}

			if (m2.subEmittersCount > 0)
			{
				int max = m2.subEmittersCount;
				for (int i = 0; i < max; i++)
					m.AddSubEmitter(m2.GetSubEmitterSystem(i), m2.GetSubEmitterType(i), m2.GetSubEmitterProperties(i));
			}
		}

		void CopyTextureSheetAnimationModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.TextureSheetAnimationModule m = thisParticleSystem.textureSheetAnimation;
			ParticleSystem.TextureSheetAnimationModule m2 = ps.textureSheetAnimation;

			m.enabled = true;

			m.animation = m2.animation;
			m.cycleCount = m2.cycleCount;
			#if UNITY_2018_3_OR_NEWER
			#else
			m.flipU = m2.flipU;
			m.flipV = m2.flipV;
			#endif
			m.frameOverTime = m2.frameOverTime;
			m.frameOverTimeMultiplier = m2.frameOverTimeMultiplier;
			m.numTilesX = m2.numTilesX;
			m.numTilesY = m2.numTilesY;
			m.rowIndex = m2.rowIndex;
			m.startFrame = m2.startFrame;
			m.startFrameMultiplier = m2.startFrameMultiplier;
			#if UNITY_2019_1_OR_NEWER
			m.rowMode = m2.rowMode;
			#else
			m.useRandomRow = m2.useRandomRow;
			#endif
			m.uvChannelMask = m2.uvChannelMask;
		}

		void CopyTrailModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.TrailModule m = thisParticleSystem.trails;
			ParticleSystem.TrailModule m2 = ps.trails;

			m.enabled = true;

			m.colorOverLifetime = m2.colorOverLifetime;
			m.colorOverTrail = m2.colorOverTrail;
			m.dieWithParticles = m2.dieWithParticles;
			m.inheritParticleColor = m2.inheritParticleColor;
			m.lifetime = m2.lifetime;
			m.lifetimeMultiplier = m2.lifetimeMultiplier;
			m.minVertexDistance = m2.minVertexDistance;
			m.ratio = m2.ratio;
			m.sizeAffectsLifetime = m2.sizeAffectsLifetime;
			m.sizeAffectsWidth = m2.sizeAffectsWidth;
			m.textureMode = m2.textureMode;
			m.widthOverTrail = m2.widthOverTrail;
			m.widthOverTrailMultiplier = m2.widthOverTrailMultiplier;
			m.worldSpace = m2.worldSpace;
		}

		void CopyTriggerModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.TriggerModule m = thisParticleSystem.trigger;
			ParticleSystem.TriggerModule m2 = ps.trigger;

			m.enabled = true;

			m.enter = m2.enter;
			m.exit = m2.exit;
			m.inside = m2.inside;
			m.outside = m2.outside;
			//m.maxColliderCount = m2.maxColliderCount; // is read only
			m.radiusScale = m2.radiusScale;

			#if UNITY_2021_1_OR_NEWER
			if (m.colliderCount > 0)
			#else
			if (m.maxColliderCount > 0)
			#endif
			{
				#if UNITY_2021_1_OR_NEWER
				int max = m.colliderCount;
				#else
				int max = m.maxColliderCount;
				#endif
				for (int i = 0; i < max; i++) m.SetCollider(i, null);
				// This line may cause a nullref exception. If so, commenting it can fix the issue,
				// but this VFX's trigger module wouldn't be usable again,
				// unless it's used with more colliders than the current value of maxColliderCount.
				// (We're talking about a rare, deep edge case here)
			}

			#if UNITY_2021_1_OR_NEWER
			if (m2.colliderCount > 0)
			#else
			if (m2.maxColliderCount > 0)
			#endif
			{
				#if UNITY_2021_1_OR_NEWER
				int max = m2.colliderCount;
				#else
				int max = m2.maxColliderCount;
				#endif
				for (int i = 0; i < max; i++)
					m.SetCollider(i, m2.GetCollider(i));
			}
		}

		void CopyVelocityOverLifetimeModuleFrom(ParticleSystem ps)
		{
			ParticleSystem.VelocityOverLifetimeModule m = thisParticleSystem.velocityOverLifetime;
			ParticleSystem.VelocityOverLifetimeModule m2 = ps.velocityOverLifetime;

			m.enabled = true;

			m.x = m2.x;
			m.xMultiplier = m2.xMultiplier;
			m.y = m2.y;
			m.yMultiplier = m2.yMultiplier;
			m.z = m2.z;
			m.zMultiplier = m2.zMultiplier;
		}

		void CopyParticleSystemRendererFrom(ParticleSystemRenderer psr)
		{
			if (!psr.enabled) { thisParticleRenderer.enabled = false; return; }

			thisParticleRenderer.enabled = true;

			// inherited from Renderer
			thisParticleRenderer.sortingOrder = psr.sortingOrder;
			thisParticleRenderer.sortingLayerID = psr.sortingLayerID;
			thisParticleRenderer.sortingLayerName = psr.sortingLayerName;
			thisParticleRenderer.materials = psr.sharedMaterials;
			thisParticleRenderer.receiveShadows = psr.receiveShadows;
			thisParticleRenderer.motionVectorGenerationMode = psr.motionVectorGenerationMode;
			thisParticleRenderer.lightmapIndex = psr.lightmapIndex;
			thisParticleRenderer.lightmapScaleOffset = psr.lightmapScaleOffset;

			// PSR properties
			thisParticleRenderer.alignment = psr.alignment;
			thisParticleRenderer.cameraVelocityScale = psr.cameraVelocityScale;
			thisParticleRenderer.lengthScale = psr.lengthScale;
			thisParticleRenderer.maxParticleSize = psr.maxParticleSize;
			thisParticleRenderer.mesh = psr.mesh;
			//thisParticleRenderer.meshCount = psr.meshCount; // is read only
			thisParticleRenderer.minParticleSize = psr.minParticleSize;
			thisParticleRenderer.normalDirection = psr.normalDirection;
			thisParticleRenderer.pivot = psr.pivot;
			thisParticleRenderer.renderMode = psr.renderMode;
			thisParticleRenderer.sortingFudge = psr.sortingFudge;
			thisParticleRenderer.sortMode = psr.sortMode;
			thisParticleRenderer.trailMaterial = psr.trailMaterial;
			thisParticleRenderer.velocityScale = psr.velocityScale;
			#if UNITY_2018_3_OR_NEWER
			thisParticleRenderer.flip = psr.flip;
			#endif

			if (psr.meshCount > 0)
			{
				Mesh[] meshes = new Mesh[psr.meshCount];
				psr.GetMeshes(meshes);
				thisParticleRenderer.SetMeshes(meshes);
			}
		}

#endregion
	}
}