using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{

	// Module that handles bullet internal tiny routine, named patterns. They also handle the firing of shots.
	public class BulletModulePatterns : BulletModule
	{
		// Emitter stats
		public bool dieWhenAllPatternsAreDone { get; private set; }
		public PatternParams[] patternsShot { get; private set; }
		public PatternRuntimeInfo[] patternRuntimeInfo;
		public SolvedPatternParams[] solvedPatternInfo;

		public List<Bullet> emittedBullets; // if this one emits bullets, they get stored here

		private bool isReady; // is set to true only at Bullet.Prepare, avoids shooting before the delayed spawn
		private bool didRegisterAsSubEmitter;

		// if at least one pattern is still playing, the module is considered "playing"
		public bool isPlaying
		{
			get
			{
				if (patternRuntimeInfo == null) return false;
				if (patternRuntimeInfo.Length == 0) return false;
				else for (int i = 0; i < patternRuntimeInfo.Length; i++)
				{
					if (patternRuntimeInfo[i].isPlaying)
						return true;
				}
				return false;
			}
		}

		// Called at Bullet.Awake()
		public override void Awake()
		{
			base.Awake();
		}

		public override void Enable() { base.Enable(); }
		public override void Disable() { base.Disable(); }

		// Called at Bullet.Update()
		public void Update()
		{
			if (!poolManager) return;
			if (!isReady) return;

			for (int i = 0; i < patternRuntimeInfo.Length; i++)
				patternRuntimeInfo[i].Update();

			if (dieWhenAllPatternsAreDone)
			{
				bool allAreDone = true;
				for (int i = 0; i < patternRuntimeInfo.Length; i++)
				{
					if (patternRuntimeInfo[i].patternParams == null)
					{
						allAreDone = false;
						break;
					}
					else if (!patternRuntimeInfo[i].isDone)
					{
						allAreDone = false;
						break;
					}
				}

				if (allAreDone) bullet.Die(true);
			}
		}

		// Called at Bullet.ApplyBulletParams()
		public void ApplyBulletParams(BulletParams bp)
		{
			didRegisterAsSubEmitter = false;

			// The "isEnabled" bool is driven by whether the BP object contains patterns or not
			isEnabled = bp.hasPatterns;
			if (!isEnabled)
			{
				patternsShot = new PatternParams[0];	
				return;
			}

			dieWhenAllPatternsAreDone = bp.dieWhenAllPatternsAreDone;

			patternsShot = new PatternParams[bp.patternsShot.Length];
			for (int i = 0; i < patternsShot.Length; i++)
				patternsShot[i] = solver.SolveDynamicPattern(bp.patternsShot[i], 14852293, ParameterOwner.Bullet);
			patternRuntimeInfo = new PatternRuntimeInfo[patternsShot.Length];
			solvedPatternInfo = new SolvedPatternParams[patternsShot.Length];
			if (solvedPatternInfo.Length > 0)
				for (int i = 0; i < solvedPatternInfo.Length; i++)
					solvedPatternInfo[i] = solver.SolvePatternParams(patternsShot[i]);
		}

		// Called at Bullet.Prepare() to initialize patterns
		public void Prepare()
		{
			if (emittedBullets == null)
				emittedBullets = new List<Bullet>();
			else
			{
				emittedBullets.Clear();
				emittedBullets.TrimExcess();
			}

			isReady = true;

			// Loads PatternParams into runtime structs; then play them
			ResetPattern();
			for (int i = 0; i < patternRuntimeInfo.Length; i++)
				if (patternRuntimeInfo[i].playAtStart) Play(i);
		}

		// Called at Bullet.Die()
		public void Die()
		{
			Stop();
			isReady = false;
			if (bullet.emitter) bullet.emitter.subEmitters.Remove(bullet);
			if (emittedBullets != null)
				if (emittedBullets.Count > 0)
					for (int i = 0; i < emittedBullets.Count; i++)
						emittedBullets[i].subEmitter = null;
			if (patternRuntimeInfo != null)
				if (patternRuntimeInfo.Length > 0)
					for (int i = 0; i < patternRuntimeInfo.Length; i++)
						patternRuntimeInfo[i].OnEmitterDeath();
		}

		#region bullet production pipeline : pattern > shoot > bullet 


		// Reads BulletParams (from ShotParams) and emits bullets - called by PatternRuntimeInfo.Update()
		public void ShootBullets(ShotParams sp)
		{
			if (!sp)
			{
				// "sp" being null actually isn't problematic.
				// By design a shot should be cancellable via setting a dynamic shot's value to null.
				//Debug.LogWarning(bullet.emitter.name + ": ShotParams missing in Pattern!");
				return;
			}

			BulletParams bpToUse = null;

			int bulletsAtOnce = solver.SolveDynamicInt(sp.simultaneousBulletsPerFrame, 488895, ParameterOwner.Shot);

			// If there are modifiers, use their BulletSpawn locations instead of the raw one stored in ShotParams
			bool useMod = true;
			if (sp.modifiers == null) useMod = false;
			if (sp.modifiers.Count == 0) useMod = false;
			BulletSpawn[] spawnLocs = null;
			if (useMod)
			{
				spawnLocs = ShotLayoutUtility.RecalculateBulletLayout(sp, bulletsAtOnce, bullet);
				//spawnLocs = sp.modifiers[sp.modifiers.Count - 1].postEffectBulletSpawns; // old way with everything pre-serialized.
				// TODO : maybe reintroduce this for times when no dynamic parameter is used in the shot.
			}
			else spawnLocs = sp.bulletSpawns;

			bool error = false;
			if (spawnLocs == null) error = true;
			else if (spawnLocs.Length == 0) error = true;
			else if (bulletsAtOnce == 0) error = true;
			if (error)
			{
				//Debug.LogWarning(bullet.emitter.name + ": ShotParams have zero bullet to shoot.");
				return;
			}

			// So this bullet is about to shoot other bullets: Time to register itself to the Emitter as a subEmitter.
			if (!didRegisterAsSubEmitter)
			{
				// Root bullet is already registered.
				if (!bullet.isRootBullet)
					bullet.emitter.subEmitters.Add(bullet);
				
				didRegisterAsSubEmitter = true;
			}
			
			// apply mirror from pattern (EDIT 2020-09: not anymore), then determine min and max for position-based dynamic params
			Vector2 minCoords = new Vector2(spawnLocs[0].bulletOrigin.x, spawnLocs[0].bulletOrigin.y);
			Vector2 maxCoords = new Vector2(minCoords.x, minCoords.y);

			// this because if editing things in editor, number of bullets might not update fast enough
			int realUsedBulletAmount = Mathf.Min(bulletsAtOnce, spawnLocs.Length);

			for (int i = 0; i < realUsedBulletAmount; i++)
			{
				// Flipping was useless and confusing, feature cut as of 2020-09
				// if (mirrorX || mirrorY) spawnLocs[i].bulletOrigin = ShotLayoutUtility.Mirror(spawnLocs[i].bulletOrigin, mirrorX, mirrorY);

				if (spawnLocs[i].bulletOrigin.x > maxCoords.x) maxCoords.x = spawnLocs[i].bulletOrigin.x;
				if (spawnLocs[i].bulletOrigin.y > maxCoords.y) maxCoords.y = spawnLocs[i].bulletOrigin.y;
				if (spawnLocs[i].bulletOrigin.x < minCoords.x) minCoords.x = spawnLocs[i].bulletOrigin.x;
				if (spawnLocs[i].bulletOrigin.y < minCoords.y) minCoords.y = spawnLocs[i].bulletOrigin.y;
			}

			for (int i = 0; i < realUsedBulletAmount; i++)
			{
				Vector3 spawn = spawnLocs[i].bulletOrigin;

				if (spawnLocs[i].useDifferentBullet)
				{
					bpToUse = spawnLocs[i].bulletParams;
					
					// empty modifier ? use default bullet style that should be provided in shot
					// EDIT : this line is now commented out for design reasons. Users should be able to purposely set bullet style to "none" to cancel bullets.
					// if (bpToUse == null) bpToUse = sp.bulletSpawns[i].bulletParams;
				}
				else
					bpToUse = solver.SolveDynamicBullet(sp.bulletParams, 417588, ParameterOwner.Shot);

				EmitSingleBullet(bpToUse, spawn, minCoords, maxCoords);
			}
		}

		// Overload that passes a TempEmissionData along the way, for dynamic parameters
		public void ShootBullets(ShotParams sp, TempEmissionData ted)
		{
			solver.tempEmissionData = ted;
			ShootBullets(sp);
		}

		// Init and emit another bullet.
		public void EmitSingleBullet(BulletParams bp, Vector3 delta, Vector2 minCoords, Vector2 maxCoords)
		{
			if (bp == null)
			{
				//Debug.LogWarning(bullet.emitter.name + " BulletParams missing in ShotParams!");
				return;
			}

			// Get a bullet from pool
			Bullet b = null;
			if (bp.renderMode == BulletRenderMode.Sprite) b = poolManager.GetFreeBulletLocal();
			else b = poolManager.GetFree3DBulletLocal();
			if (!b) return;

			Transform self = bullet.self;

			// Set position and orientation, except orientation delta from ShotParams which will be set later
			b.self.position = self.position + self.up * delta.y + self.right * delta.x;
			b.self.eulerAngles = self.eulerAngles; // and not "+delta.z"

			// Setup inherited data for dynamic parameters
			InheritedSpawnData isd = new InheritedSpawnData();
			isd.randomSeed = Random.value;
			isd.bulletID = bp.uniqueIndex;
			isd.shotID = solver.tempEmissionData.shotID;
			isd.patternID = solver.tempEmissionData.patternID;
			isd.patternIndexInEmitter = solver.tempEmissionData.patternIndexInEmitter;
			isd.shotIndexInPattern = solver.tempEmissionData.shotIndexInPattern;
			isd.shotTimeInPattern = solver.tempEmissionData.shotTimeInPattern;
			isd.rotationAtSpawn = self.localEulerAngles.z + delta.z;
			isd.positionInShot = new Vector2(Mathf.InverseLerp(minCoords.x, maxCoords.x, delta.x), Mathf.InverseLerp(minCoords.y, maxCoords.y, delta.y));
			isd.randomValues = b.dynamicSolver.inheritedData.randomValues;
			b.dynamicSolver.inheritedData = isd;

			// Recursion setup
			b.emitter = bullet.emitter;
			// Commented out : newly-created bullet now registers itself as subEmitter when it shoots it first Shot
			// if (bp.hasPatterns) bullet.emitter.subEmitters.Add(b);
			b.subEmitter = bullet;
			emittedBullets.Add(b);
			bullet.emitter.bullets.Add(b);

			// Apply BulletParams (it's done before handling parenting so isChildOfEmitter can depend on custom params)
			b.ApplyBulletParams(bp);

			// Handle bullet parenting
			if (b.dynamicSolver.SolveDynamicBool(bp.isChildOfEmitter, 1743405, ParameterOwner.Bullet))
				b.self.SetParent(self);
			else
			{
				if (bp.renderMode == BulletRenderMode.Sprite)
					b.self.SetParent(bullet.poolManager.regularPoolRoot);
				else
					b.self.SetParent(bullet.poolManager.meshPoolRoot);
			}			

			// If delayed spawn from homing bullets, rotation from ShotParams will only be applied at Prepare().
			bool dontApplyRotationNow = false;

			// Set orientation if homing
			BulletModuleHoming mh = b.moduleHoming;
			if (mh.isEnabled)
			{
				BulletModuleSpawn ms = b.moduleSpawn;
				if (ms.isEnabled && ms.timeBeforeSpawn > 0)
				{
					ms.MemorizeSpawnRotation(delta.z);
					dontApplyRotationNow = true;
				}
				else mh.LookAtTarget(mh.homingSpawnRate);
			}

			// Set orientation delta from ShotParams - it's done here because it shouldn't get overridden by homing.
			if (!dontApplyRotationNow)
				b.moduleMovement.Rotate(delta.z);

			// Add additional behaviour if needed.
			// Done here instead of ApplyBulletParams() to avoid behaviour stacking if one calls ChangeBulletParams().
			if (bp.behaviourPrefabs != null)
				if (bp.behaviourPrefabs.Length > 0)
					for (int i = 0; i < bp.behaviourPrefabs.Length; i++)
					{
						GameObject bpPrefab = b.dynamicSolver.SolveDynamicObjectReference(bp.behaviourPrefabs[i], 30733277, ParameterOwner.Bullet) as GameObject;

						if (bpPrefab == null) continue;

						BaseBulletBehaviour bbb = behaviourManager.GetFreeBehaviourLocal(bpPrefab);
						if (bbb)
						{
							bbb.bullet = b;
							bbb.OnBulletBirth();
						}
					}


			// Bullet's ready to be launched ingame !
			b.FirstPrepare(true);
		}

		#endregion

		#region pattern controls

		public void Play() { for (int i=0; i<patternRuntimeInfo.Length; i++) Play(i); }
		public void Pause() { for (int i=0; i<patternRuntimeInfo.Length; i++) Pause(i); }
		public void ResetPattern() { for (int i=0; i<patternRuntimeInfo.Length; i++) ResetPattern(i); }
		public void Stop() { Pause(); ResetPattern(); }
		public void Boot() { ResetPattern(); Play(); }

		public void Play(int patternIndex) { patternRuntimeInfo[patternIndex].isPlaying = true; }
		public void Pause(int patternIndex) { patternRuntimeInfo[patternIndex].isPlaying = false; }
		public void ResetPattern(int patternIndex) { patternRuntimeInfo[patternIndex].Init(patternsShot[patternIndex], solvedPatternInfo[patternIndex], bullet, patternIndex); }
		public void Stop(int patternIndex) { Pause(patternIndex); ResetPattern(patternIndex); }
		public void Boot(int patternIndex) { ResetPattern(patternIndex); Play(patternIndex); }

		public void Play(string patternTag)
		{
			if (patternRuntimeInfo != null)
				for (int i=0; i<patternRuntimeInfo.Length; i++)
					if (patternRuntimeInfo[i].HasTag(patternTag))
						Play(i);

			if (emittedBullets != null)
				if (emittedBullets.Count > 0)
					for (int i = 0; i < emittedBullets.Count; i++)
						emittedBullets[i].modulePatterns.Play(patternTag);
		}

		public void Pause(string patternTag)
		{
			if (patternRuntimeInfo != null)
				for (int i=0; i<patternRuntimeInfo.Length; i++)
					if (patternRuntimeInfo[i].HasTag(patternTag))
						Pause(i);

			if (emittedBullets != null)
				if (emittedBullets.Count > 0)
					for (int i = 0; i < emittedBullets.Count; i++)
						emittedBullets[i].modulePatterns.Pause(patternTag);
		}

		public void ResetPattern(string patternTag)
		{
			if (patternRuntimeInfo != null)
				for (int i=0; i<patternRuntimeInfo.Length; i++)
					if (patternRuntimeInfo[i].HasTag(patternTag))
						ResetPattern(i);

			if (emittedBullets != null)
				if (emittedBullets.Count > 0)
					for (int i = 0; i < emittedBullets.Count; i++)
						emittedBullets[i].modulePatterns.ResetPattern(patternTag);
		}

		public void Stop(string patternTag)
		{
			if (patternRuntimeInfo != null)
				for (int i=0; i<patternRuntimeInfo.Length; i++)
					if (patternRuntimeInfo[i].HasTag(patternTag))
						Stop(i);

			if (emittedBullets != null)
				if (emittedBullets.Count > 0)
					for (int i = 0; i < emittedBullets.Count; i++)
						emittedBullets[i].modulePatterns.Stop(patternTag);
		}

		public void Boot(string patternTag)
		{
			if (patternRuntimeInfo != null)
				for (int i=0; i<patternRuntimeInfo.Length; i++)
					if (patternRuntimeInfo[i].HasTag(patternTag))
						Boot(i);

			if (emittedBullets != null)
				if (emittedBullets.Count > 0)
					for (int i = 0; i < emittedBullets.Count; i++)
						emittedBullets[i].modulePatterns.Boot(patternTag);
		}

		// if this bullet has shot some other bullets, this kills them.
		// if killRecursive is set to true, any dying bullet which also emitted bullets will kill them too.
		public void KillEmittedBullets(bool killRecursive)
		{
			if (emittedBullets == null) return;
			if (emittedBullets.Count == 0) return;

			List<Bullet> all = new List<Bullet>();

			for (int i = 0; i < emittedBullets.Count; i++)
				all.Add(emittedBullets[i]);

			for (int i = 0; i < all.Count; i++)
			{
				Bullet b = all[i];

				if (b.modulePatterns.isEnabled && killRecursive)
					b.modulePatterns.KillEmittedBullets(true);

				b.Die(true);
			}
		}

		#endregion
	}
}