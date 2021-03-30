using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// Every "Additional Behaviour Object" of a bullet should carry a script that inherits BulletBehaviour.
	public class BaseBulletBehaviour : MonoBehaviour
	{
		// The Bullet object that instantiated this script.
		[System.NonSerialized]
		public Bullet bullet;

		// BulletBehaviours are pooled to avoid instantiating at runtime
		[System.NonSerialized]
		public bool isAvailableInPool;

		// This object's Transform.
		[HideInInspector]
		public Transform self;

		// Where to put this object back after it's used
		[HideInInspector]
		public Transform defaultParent;

		// When the bullet dies, this script get destroyed along with its whole gameobject.
		[Tooltip("When the bullet dies, you can delay the behaviour's death by a set amount of seconds.")]
		public float lifetimeAfterBulletDeath;

		// Helper vars with death-timer
		protected float deathCountdown;
		protected bool isDestructing;

		// When starting the scene, these behaviours shouldn't be in use.
		void Awake() { OnBehaviourDeath(); }

		// Always called when the bullet gets spawned. Some other things can be added to this callback.
		public virtual void OnBulletBirth()
		{
			gameObject.SetActive(true);
			enabled = true;
			bullet.additionalBehaviourScripts.Add(this);

			if (self == null) self = transform; // just in case the serialization failed

			self.SetParent(bullet.self);
			self.position = bullet.self.position;
			self.rotation = bullet.self.rotation;
			self.localScale = Vector3.one;

			gameObject.name = bullet.name + " Behaviour";
		}

		// Always called when the bullet dies. Some other things can be added to this callback.
		public virtual void OnBulletDeath()
		{
			// Commented out: removal at the exact frame of death may cause IndexOutOfRangeExceptions.
			// Removal is rather done at bullet's next birth, in ApplyBulletParams.
			//bullet.additionalBehaviourScripts.Remove(this);
			
			isDestructing = true;
			deathCountdown = lifetimeAfterBulletDeath;

			if (self == null) self = transform;

			self.SetParent(defaultParent);
		}

		// Update is still available, as in any MonoBehaviour
		public virtual void Update()
		{
			if (isDestructing)
			{
				deathCountdown -= Time.deltaTime;
				if (deathCountdown < 0) OnBehaviourDeath();
			}
		}

		// Called whenever the bullet shoots another pattern
		public virtual void OnBulletShotAnotherBullet(int patternIndex)
		{

		}

		// Called whenever the bullet collides with a BulletReceiver. The most common callback.
		public virtual void OnBulletCollision(BulletReceiver br, Vector3 collisionPoint)
		{

		}

		// Called whenever the bullet collides with a BulletReceiver AND was not colliding during the previous frame
		public virtual void OnBulletCollisionEnter(BulletReceiver br, Vector3 collisionPoint)
		{

		}

		// Called whenever the bullet stops colliding with any BulletReceiver
		public virtual void OnBulletCollisionExit()
		{

		}

		// Called a few moments (user-defined) after bullet's death, to have the behaviour get back to pool.
		public virtual void OnBehaviourDeath()
		{
			isDestructing = false;
			isAvailableInPool = true;
			enabled = false;

			if (self == null) self = transform;

			// Now that we know this behaviour is invisible, inactive and reparented to the Manager, we can reset its transform.
			self.localPosition = Vector3.zero;
			self.localEulerAngles = Vector3.zero;
			self.localScale = Vector3.one;
		}
	}
}
