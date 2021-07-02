using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	[System.Serializable]
	public class HitByBulletEvent : UnityEvent<Bullet, Vector3> { }

	public enum BulletReceiverType { Circle, Line }

	// A hitbox that sends events when hit by bullets. Has OnEnter, OnStay and OnExit.
	// Be careful though, as a same instance can only send one OnEnter, one OnStay and one OnExit per frame, regardless of the bullet.
	[AddComponentMenu("BulletPro/Bullet Receiver")]
	public class BulletReceiver : MonoBehaviour
	{
		public Transform self;
		public BulletReceiverType colliderType = BulletReceiverType.Circle;
		public float hitboxSize = 0.1f;
		public Vector2 hitboxOffset = Vector2.zero;
		public bool killBulletOnCollision = true;
		[Tooltip("If more bullets strike this Receiver at once, excess collisions will be negated. 0 means Infinity.")]
		public uint maxSimultaneousCollisionsPerFrame = 1;
		public List<Bullet> bulletsHitThisFrame { get; private set; }
		public List<Bullet> bulletsHitLastFrame { get; private set; }
		public CollisionTags collisionTags;
		public bool collisionTagsFoldout; // editor only

		public HitByBulletEvent OnHitByBullet;

		// Yes, they do exist, and are usable, but they're most likely confusing and rarely useful - yet might be needed in some cases.
		public HitByBulletEvent OnHitByBulletEnter, OnHitByBulletStay, OnHitByBulletExit;
		#if UNITY_EDITOR
		public bool advancedEventsFoldout;
		#endif

		public Color gizmoColor = Color.black;

		bool collisionEnabled; // helps avoiding bullets to hit this if we just disabled collisions

		// We need a reference to call it frequently, so we know if Compute Shaders are enabled
		private BulletCollisionManager collisionManager;

#if UNITY_EDITOR
		void OnDrawGizmos()
		{
			if (!self) self = transform;
			//float avgScale = thisTransform.lossyScale.x * 0.5f + thisTransform.lossyScale.y * 0.5f;
			// there's no point in taking scale.x into account
			float avgScale = self.lossyScale.y;
			Gizmos.color = gizmoColor;
			Vector3 trPos = self.position;
			if (hitboxOffset.x != 0) trPos += self.lossyScale.x * hitboxOffset.x * self.right;
			if (hitboxOffset.y != 0) trPos += self.lossyScale.y * hitboxOffset.y * self.up;

			if (colliderType == BulletReceiverType.Circle)
				Gizmos.DrawWireSphere(trPos, hitboxSize * avgScale);
			else
				Gizmos.DrawLine(trPos, trPos + self.up * hitboxSize * avgScale);
		}
#endif

		public void Awake()
		{
			if (self == null) self = transform;
			bulletsHitThisFrame = new List<Bullet>();
			bulletsHitLastFrame = new List<Bullet>();

			// wait one frame so that the managers exist. Start is unreliable since it can be disabled
			StartCoroutine(PostAwake());
		}

		IEnumerator PostAwake()
		{
			yield return new WaitForEndOfFrame();

			if (enabled) EnableCollisions();
			else collisionEnabled = false;

			collisionManager = BulletCollisionManager.instance;
		}

		// Bullet memory and Exit event are handled when the collisions are not processed - for the GPU mode, it's in Update.
		public void Update()
		{
			if (collisionManager == null)
			{
				collisionManager = BulletCollisionManager.instance;
				if (collisionManager == null)
				{
					Debug.LogError("BulletPro Error: no Collision Manager found in scene. Try redoing the Scene Setup, or check if your Manager was disabled during this object's Awake.");
					return;
				}
			}
			
			if (!collisionManager.disableComputeShaders)
				BulletMemoryUpdate();
		}

		// Bullet memory and Exit event are handled when the collisions are not processed - for the CPU mode, it's in LateUpdate.
		public void LateUpdate()
		{
			if (collisionManager == null)
			{
				collisionManager = BulletCollisionManager.instance;
				if (collisionManager == null)
				{
					Debug.LogError("BulletPro Error: no Collision Manager found in scene. Try redoing the Scene Setup, or check if your Manager was disabled during this object's Awake.");
					return;
				}
			}

			if (collisionManager.disableComputeShaders)
				BulletMemoryUpdate();
		}

		// Called at either Update or LateUpdate
		void BulletMemoryUpdate()
		{
			// Process Exit event
			if (OnHitByBulletExit != null)
				if (bulletsHitLastFrame.Count > 0)
					for (int i = 0; i < bulletsHitLastFrame.Count; i++)
						if (!bulletsHitThisFrame.Contains(bulletsHitLastFrame[i]))
							OnHitByBulletExit.Invoke(bulletsHitLastFrame[i], bulletsHitLastFrame[i].self.position);

			// Flush lists
			bulletsHitLastFrame.Clear();
			bulletsHitLastFrame.TrimExcess();
			if (bulletsHitThisFrame.Count > 0)
				for (int i = 0; i < bulletsHitThisFrame.Count; i++)
					bulletsHitLastFrame.Add(bulletsHitThisFrame[i]);
			bulletsHitThisFrame.Clear();
			bulletsHitThisFrame.TrimExcess();
		}

		// Called on collision : OnEnter and OnStay are handled if needed, but it will rarely be the case.
		// Signature says "Vector3, Bullet" instead of "Bullet, Vector3" so events cannot call this and enter an infinite loop.
		public void GetHit(Vector3 collisionPoint, Bullet bullet)
		{
			bulletsHitThisFrame.Add(bullet);
			OnHitByBullet?.Invoke(bullet, collisionPoint);

			// Compute Enter/Stay events here
			if (!bulletsHitLastFrame.Contains(bullet))
				OnHitByBulletEnter?.Invoke(bullet, collisionPoint);
			else OnHitByBulletStay?.Invoke(bullet, collisionPoint);

			// Kill bullet if needed
			if (killBulletOnCollision && bullet.moduleCollision.dieOnCollision)
				bullet.Die(collisionPoint);
		}

		#region information getters

		// Has this receiver reach its collision amount limit for this frame?
		public bool CanAcceptCollisionsThisFrame()
		{
			return (maxSimultaneousCollisionsPerFrame < 1) || (bulletsHitThisFrame.Count < maxSimultaneousCollisionsPerFrame);
		}

		public bool HasAlreadyCollidedThisFrame(Bullet bullet)
		{
			return bulletsHitThisFrame.Contains(bullet);
		}

		#endregion

		// Since toggling .enabled is way more intuitive, this whole toolbox is private
		#region collision toggle toolbox

		void EnableCollisions()
		{
			if (collisionEnabled) return;
			collisionEnabled = true;
			BulletCollisionManager.AddReceiver(this);
		}

		void DisableCollisions()
		{
			if (!collisionEnabled) return;
			collisionEnabled = false;
			BulletCollisionManager.RemoveReceiver(this);
		}

		void ToggleCollisions()
		{
			if (collisionEnabled) DisableCollisions();
			else EnableCollisions();
		}

		void SetCollisions(bool active)
		{
			if (active) EnableCollisions();
			else DisableCollisions();
		}

		#endregion

		void OnEnable()
		{
			if (BulletCollisionManager.instance == null) return;
			SetCollisions(true);
		}

		void OnDisable()
		{
			if (BulletCollisionManager.instance == null) return;
			SetCollisions(false);

			// Uncomment this to trigger OnCollisionExit if needed
			//BulletMemoryUpdate();

			// Flush lists
			bulletsHitLastFrame.Clear();
			bulletsHitLastFrame.TrimExcess();
			bulletsHitThisFrame.Clear();
			bulletsHitThisFrame.TrimExcess();
		}

		// Not recommended, but we can't leave it into the manager if it gets destroyed
		void OnDestroy()
		{
			collisionEnabled = false;
			BulletCollisionManager.RemoveReceiver(this);
		}
	}
}