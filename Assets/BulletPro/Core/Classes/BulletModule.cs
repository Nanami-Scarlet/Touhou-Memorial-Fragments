using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// Base class for all seven modules that compose a bullet.
	public class BulletModule : UnityEngine.Object
	{
		public bool isEnabled { get; protected set; }

		// internal references
		public Bullet bullet;
		protected BulletModuleMovement moduleMovement;
		protected BulletModuleCollision moduleCollision;
		protected BulletModuleRenderer moduleRenderer;
		protected BulletModulePatterns modulePatterns;
		protected BulletModuleHoming moduleHoming;
		protected BulletModuleLifespan moduleLifespan;
		protected BulletModuleSpawn moduleSpawn;
		protected BulletModuleParameters moduleParameters;
		protected DynamicParameterSolver solver;

		// managers
		protected BulletCollisionManager collisionManager;
		protected BulletGlobalParamManager globalParamManager;
		protected BulletPoolManager poolManager;
		protected BulletAudioManager audioManager;
		protected BulletVFXManager vfxManager;
		protected BulletBehaviourManager behaviourManager;
		protected Transform bulletCanvas;

		// components
		protected Transform self;
		protected SpriteRenderer spriteRenderer;
		protected MeshRenderer meshRenderer;
		protected MeshFilter meshFilter;

		// Called at Bullet.Awake()
		public virtual void Awake()
		{
			moduleMovement = bullet.moduleMovement;
			moduleCollision = bullet.moduleCollision;
			moduleRenderer = bullet.moduleRenderer;
			moduleSpawn = bullet.moduleSpawn;
			moduleHoming = bullet.moduleHoming;
			moduleLifespan = bullet.moduleLifespan;
			modulePatterns = bullet.modulePatterns;
			moduleParameters = bullet.moduleParameters;

			self = bullet.self;
			spriteRenderer = bullet.spriteRenderer;
			meshRenderer = bullet.meshRenderer;
			meshFilter = bullet.meshFilter;
		}

		public void GetManagers()
		{
			poolManager = bullet.poolManager;
			collisionManager = bullet.collisionManager;
			globalParamManager = bullet.globalParamManager;
			audioManager = bullet.audioManager;
			vfxManager = bullet.vfxManager;
			behaviourManager = bullet.behaviourManager;

			bulletCanvas = bullet.poolManager.mainTransform;

			solver = bullet.dynamicSolver;
		}

		public virtual void Enable()
		{
			isEnabled = true;
		}

		public virtual void Disable()
		{
			isEnabled = false;
		}
	}
}