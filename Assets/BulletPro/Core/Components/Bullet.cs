using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// The component every actual bullet object possesses. Split into modules to make things easier.
	public class Bullet : MonoBehaviour
	{
		#region properties

		// unique ID
		public int uniqueBulletID { get; private set; }

		public bool IsGrazed { get; set; }
		public bool IsBounce { get; set; }

		// Bullet modules
		public BulletModuleMovement moduleMovement;
		public BulletModuleCollision moduleCollision;
		public BulletModuleRenderer moduleRenderer;
		public BulletModulePatterns modulePatterns;
		public BulletModuleHoming moduleHoming;
		public BulletModuleLifespan moduleLifespan;
		public BulletModuleSpawn moduleSpawn;
		public BulletModuleParameters moduleParameters;
		public DynamicParameterSolver dynamicSolver; // works as a module but treats dynamic parameters upon bullet birth

		// Pooling availability
		[System.NonSerialized]
		public bool isAvailableInPool;

		// Lifetime counter
		[System.NonSerialized]
		public float timeSinceAlive;

		// the BulletEmitter of this bullet's current tree "emitter>pattern>shot>bullet>pattern>..."
		[System.NonSerialized]
		public BulletEmitter emitter;

		// the (other) bullet that actually emitted this one
		[System.NonSerialized]
		public Bullet subEmitter;

		// Is this bullet the Root of its BulletEmitter component?
		[System.NonSerialized]
		public bool isRootBullet;

		// Component references, serialized to spare time at Awake
		public Transform self;
		public SpriteRenderer spriteRenderer;
		public MeshRenderer meshRenderer;
		public MeshFilter meshFilter;
		public BulletRenderMode renderMode; // stored in this script because it helps handle pooling

		// Manager singleton references, assigned at frame 2
		[System.NonSerialized]
		public BulletCollisionManager collisionManager;
		[System.NonSerialized]
		public BulletGlobalParamManager globalParamManager;
		[System.NonSerialized]
		public BulletPoolManager poolManager;
		[System.NonSerialized]
		public BulletAudioManager audioManager;
		[System.NonSerialized]
		public BulletVFXManager vfxManager;
		[System.NonSerialized]
		public BulletBehaviourManager behaviourManager;

		// script reference is accessed and set by the BulletBehaviour itself, at its start, so we only call GetComponent<> once.
		[System.NonSerialized]
		public List<BaseBulletBehaviour> additionalBehaviourScripts;

		// all timed micro-actions can impact any module, so they're stored and updated here
		public List<MicroActionTimer> microActions;

		#endregion

		#region editor-only (gizmos)

#if UNITY_EDITOR
		public Color gizmoColor = Color.yellow;
		void OnDrawGizmos()
		{
			if (!EditorApplication.isPlaying) return;
			if (!self) return;

			if (!moduleCollision.isEnabled) return;
			
			BulletCollider[] bcs = moduleCollision.GetColliders();

			if (bcs == null) return;
			if (bcs.Length == 0) return;

			Matrix4x4 oldMat = Gizmos.matrix;
			Gizmos.matrix = self.localToWorldMatrix;
			Gizmos.color = gizmoColor;
			float mScale = moduleCollision.scale;
			foreach (BulletCollider bc in bcs)
			{
				if (bc.colliderType == BulletColliderType.Line)
				{
					Vector3 startPoint = (Vector3)bc.lineStart;
					Vector3 endPoint = (Vector3)bc.lineEnd;
					Gizmos.DrawLine(startPoint, endPoint);
				}
				else if (bc.size > 0)
				{
					Vector3 orig = bc.offset * mScale;
					Gizmos.DrawWireSphere(orig, bc.size);
				}
			}

			Gizmos.matrix = oldMat;
		}
#endif

		#endregion

		#region Monobehaviour

		// Start sets up all modules and references at the beginning of the scene
		public void Start()
		{
			// Fix serialization fails if it's not been done in Editor
			if (!self) self = transform;
			if (additionalBehaviourScripts == null) additionalBehaviourScripts = new List<BaseBulletBehaviour>();

			microActions = new List<MicroActionTimer>();

			// Initialize all modules

			moduleMovement = new BulletModuleMovement();
			moduleMovement.bullet = this;

			moduleCollision = new BulletModuleCollision();
			moduleCollision.bullet = this;

			moduleRenderer = new BulletModuleRenderer();
			moduleRenderer.bullet = this;

			modulePatterns = new BulletModulePatterns();
			modulePatterns.bullet = this;

			moduleHoming = new BulletModuleHoming();
			moduleHoming.bullet = this;

			moduleLifespan = new BulletModuleLifespan();
			moduleLifespan.bullet = this;

			moduleSpawn = new BulletModuleSpawn();
			moduleSpawn.bullet = this;

			moduleParameters = new BulletModuleParameters();
			moduleParameters.bullet = this;

			dynamicSolver = new DynamicParameterSolver();
			dynamicSolver.bullet = this;

			moduleMovement.Awake();
			moduleCollision.Awake();
			moduleRenderer.Awake();
			modulePatterns.Awake();
			moduleHoming.Awake();
			moduleLifespan.Awake();
			moduleSpawn.Awake();
			moduleParameters.Awake();
			dynamicSolver.Awake();

			// Get references to managers
			GetManagers();

			// Mark all bullets as available for pooling and not alive yet
			Die(false);
		}

		// Called at start, caches any manager singleton reference into the bullet and its modules.
		void GetManagers()
		{
			poolManager = BulletPoolManager.instance;
			collisionManager = BulletCollisionManager.instance;
			audioManager = BulletAudioManager.instance;
			vfxManager = BulletVFXManager.instance;
			behaviourManager = BulletBehaviourManager.instance;
			globalParamManager = BulletGlobalParamManager.instance;

			moduleMovement.GetManagers();
			moduleCollision.GetManagers();
			moduleRenderer.GetManagers();
			modulePatterns.GetManagers();
			moduleHoming.GetManagers();
			moduleLifespan.GetManagers();
			moduleSpawn.GetManagers();
			moduleParameters.GetManagers();
		}

		// Update is divided into a few sub-updates, depending on the intended behaviour.
		void Update()
		{
			// debug
			/* *
			if (Input.GetKeyDown(KeyCode.Y))
			{
				microActions.Add(new MicroActionRotate(this, 0.4f, AnimationCurve.Linear(0,0,1,1), 90));
			}
			/* */

			if (emitter.allBulletsPaused) return;

			timeSinceAlive += Time.deltaTime;

			if (moduleSpawn.isEnabled)
				moduleSpawn.Update();

			// no moduleRenderer update is needed if the bullet does not use embedded sprite
			if (renderMode == BulletRenderMode.Sprite)
				if (spriteRenderer.enabled)
					moduleRenderer.Update();

			if (moduleMovement.isEnabled)	moduleMovement.Update();
			if (moduleCollision.isEnabled)	moduleCollision.Update();
			if (moduleHoming.isEnabled)		moduleHoming.Update();
			if (modulePatterns.isEnabled)	modulePatterns.Update();
			if (moduleLifespan.isEnabled)	moduleLifespan.Update();
			// moduleParameters is not updated

			// update micro-actions
			if (microActions.Count > 0)
			{
				int toRemove = -1;
				for (int i = 0; i < microActions.Count; i++)
				{
					microActions[i].Update();
					if (microActions[i].IsDone())
						toRemove = i;
				}
				// cleans up to one routine per frame
				if (toRemove > -1) microActions.RemoveAt(toRemove);
			}
		}

        // Not recommended (at all, it would break the bullet pool), but we can't leave it into the manager if it gets destroyed
        void OnDestroy()
		{
		    if (moduleCollision.isEnabled)
                collisionManager.RemoveBulletLocal(this);
		}
		#endregion

		#region bullet toolbox (prepare, die, curve stuff)

		// Prepare this bullet to be used, when just out of pool.
		public void FirstPrepare(bool spawnFX)
		{
			isAvailableInPool = false;
			enabled = true;
			uniqueBulletID = poolManager.bulletsSpawnedSinceStartup++;
			poolManager.currentAmountOfBullets++;

			// Prepare() won't be called now if the spawn has to be delayed
			if (moduleSpawn.isEnabled && moduleSpawn.timeBeforeSpawn > 0)
				return;

			Prepare(spawnFX);
		}

		// Prepare this bullet to be used - separated from bullet availability, for cases where spawn is delayed
		public void Prepare(bool spawnFX)
		{
			// Flip necessary bools
			if (renderMode == BulletRenderMode.Sprite)
				spriteRenderer.enabled = moduleRenderer.isEnabled;
			if (renderMode == BulletRenderMode.Mesh)
				meshRenderer.enabled = moduleRenderer.isEnabled;

			// Initialize shot pattern if needed
			if (modulePatterns.isEnabled)
				modulePatterns.Prepare();

			// start counting bullet lifetime
			timeSinceAlive = 0;

			// Handle VFX
			if (spawnFX && moduleRenderer.isEnabled && moduleRenderer.playVFXOnBirth)
				moduleRenderer.SpawnFX(true);
		}

		// Make this bullet die, thus eligible to pooling again.
		void Die(bool spawnFX, bool useSelfPositionForFX=true, Vector3 customFXPosition=default(Vector3))
		{
			// this double check ensures that any kill function (such as BulletInitiator.KillAllBullets()) can't kill it twice
			if (isAvailableInPool) return;

			// Updating the manager
			poolManager.currentAmountOfBullets--;

			// Flush micro-actions
			microActions.Clear();
			microActions.TrimExcess();

			// FX, feedbacks
			if (spawnFX && moduleRenderer.isEnabled && moduleRenderer.playVFXOnDeath)
			{
				if (useSelfPositionForFX)
					moduleRenderer.SpawnFX(false);
				else moduleRenderer.SpawnFX(customFXPosition, false);
			}

			// Bullet shutdown
			isAvailableInPool = true;
			isRootBullet = false;
			enabled = false;

			// parenting, and local scale
			if (renderMode == BulletRenderMode.Sprite)
				self.SetParent(poolManager.regularPoolRoot);
			else
				self.SetParent(poolManager.meshPoolRoot);
			self.localScale = Vector3.one;

			// Simpler modules
			moduleMovement.Disable();
			moduleLifespan.Disable();
			moduleSpawn.Die();
			dynamicSolver.Die();
			
			// Collision module
			if (moduleCollision.isEnabled)
	            collisionManager.RemoveBulletLocal(this);
			moduleCollision.Disable();

			// Graphics (moduleRenderer)
			if (renderMode == BulletRenderMode.Sprite)
				spriteRenderer.enabled = false;
			if (renderMode == BulletRenderMode.Mesh)
				meshRenderer.enabled = false;
			moduleRenderer.Disable();
			moduleRenderer.Die();
			
			// Homing module
			moduleHoming.currentTarget = null;
			moduleHoming.Disable();

			// Tree stuff (below this bullet), pattern module
			if (modulePatterns.isEnabled)
				modulePatterns.Die();
			modulePatterns.Disable();

			// Tree stuff (above this bullet)
			if (emitter) emitter.bullets.Remove(this);
			emitter = null;
			if (subEmitter) subEmitter.modulePatterns.emittedBullets.Remove(this);
			subEmitter = null;

			// unlink and destroy behaviour
			if (additionalBehaviourScripts != null)
				if (additionalBehaviourScripts.Count > 0)
					for (int i = 0; i < additionalBehaviourScripts.Count; i++)
					{
						if (additionalBehaviourScripts[i])
							additionalBehaviourScripts[i].OnBulletDeath();
					}
		}

		// Overload that controls spawned FX position
		public void Die(Vector3 customFXPosition) {	Die(true, false, customFXPosition); }
		// Overload that controls whether there must be a VFX
		public void Die(bool spawnFX) { Die(spawnFX, true); }
		// Simplest overload
		public void Die() { Die(true); }

		#endregion

		#region copying values from BulletParams into the modules

		// Called by EmitSingleBullet : applies BulletParams to spawning bullet. Most params can be randomized in a range.
		public void ApplyBulletParams(BulletParams bp)
		{
			// Parameters are first initialized, so they can be used in other DynamicValues
			moduleParameters.ApplyBulletParams(bp);

			// Then, because of curves, applying Lifespan before the rest is very important
			moduleLifespan.ApplyBulletParams(bp);
			
			moduleMovement.ApplyBulletParams(bp);
			moduleCollision.ApplyBulletParams(bp);
			moduleHoming.ApplyBulletParams(bp); // applying Homing after Collision so it can read the CollisionTags
			modulePatterns.ApplyBulletParams(bp);
			moduleSpawn.ApplyBulletParams(bp);
			moduleRenderer.ApplyBulletParams(bp);

			// BulletBehaviours are not handled here but in emitter's ModulePatterns. This avoids behaviour stacking if one calls ChangeBulletParams().
			// But PREVIOUS Behaviours (from the bullet's previous lives) are flushed here.
			if (additionalBehaviourScripts.Count == 0) return;
			additionalBehaviourScripts.Clear();
			additionalBehaviourScripts.TrimExcess();	
		}

		// Overload for ApplyBulletParams, which uses a mask to select modules
		public void ApplyBulletParams(BulletParams bp, BulletParamMask bpm)
		{
			if ((bpm & BulletParamMask.Lifespan) == BulletParamMask.Lifespan)
				moduleLifespan.ApplyBulletParams(bp);

			if ((bpm & BulletParamMask.Movement) == BulletParamMask.Movement)
				moduleMovement.ApplyBulletParams(bp);

			if ((bpm & BulletParamMask.Collision) == BulletParamMask.Collision)
				moduleCollision.ApplyBulletParams(bp);

			if ((bpm & BulletParamMask.Homing) == BulletParamMask.Homing)
				moduleHoming.ApplyBulletParams(bp);

			if ((bpm & BulletParamMask.Patterns) == BulletParamMask.Patterns)
				modulePatterns.ApplyBulletParams(bp);

			if ((bpm & BulletParamMask.DelaySpawn) == BulletParamMask.DelaySpawn)
				moduleSpawn.ApplyBulletParams(bp);

			if ((bpm & BulletParamMask.Visibility) == BulletParamMask.Visibility)
				moduleRenderer.ApplyBulletParams(bp);

			if ((bpm & BulletParamMask.Parameters) == BulletParamMask.Parameters)
				moduleRenderer.ApplyBulletParams(bp);
		}

		// Change BulletParams during the bullet's life, without killing it
		public void ChangeBulletParams(BulletParams bp)
		{
			ApplyBulletParams(bp);
			Prepare(false);
		}

		// Overload for ChangeBulletParams, which uses a mask to select modules
		public void ChangeBulletParams(BulletParams bp, BulletParamMask bpm)
		{
			ApplyBulletParams(bp, bpm);
			Prepare(false);
		}

		#endregion
	}
}