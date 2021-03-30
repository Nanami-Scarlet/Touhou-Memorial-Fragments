using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// This tiny struct is a part of the bullet's hitbox. One bullet can have more than one of them, to simulate more complex shapes.
	[System.Serializable]
	public struct BulletCollider
	{
		public BulletColliderType colliderType;
		public float size;
		public Vector3 offset;
		public Vector2 lineStart, lineEnd;

		// Those two unserialized shortcuts are used by compute shader logic in BulletCollisionManager.cs
		[System.NonSerialized]
		public uint collisionTags;
		[System.NonSerialized]
		public Bullet bullet;
	}

	// If compute shaders are disabled, this struct provides info on whether a collision happened, and when
	public struct CPUCollisionResult
	{
		public bool happened; // true if collided, false if not collided
		public Vector3 hitPoint;
	}

	public enum BulletColliderType { Circle, Line }

	// Module that handles bullet collisions
	public class BulletModuleCollision : BulletModule
	{
		// Hitbox shape and size, which is actually a collection of circles and lines
		private BulletCollider[] colliders;

		// Taking scale into account
		public float scale { get; private set; }

		public bool dieOnCollision;
		// On collision, it's actually BulletReceiver which orders it to die, to avoid calling it twice

		public CollisionTags collisionTags;

		public bool isColliding { get; private set; }
		public bool isCollidingThisFrame { get; private set; }
		public bool wasCollidingLastFrame { get; private set; }

		#region Monobehaviour

		// Called at Bullet.Awake()
		public override void Awake() { base.Awake(); }

		// If enabling or disabling the module outside of bullet birth/death, this must be called.
        public override void Enable() { if (isEnabled) return; isEnabled = true; collisionManager.AddBulletLocal(bullet); }
        public override void Disable() { if (!isEnabled) return; isEnabled = false; collisionManager.RemoveBulletLocal(bullet); }

		// Called at Bullet.Update()
		public void Update()
		{
			// enabled spawn module means we're still waiting for the actual spawn
			if (moduleSpawn.isEnabled) return;

			// Error handling : if collision can't be process, we just throw the OnExit event if the bullet was hitting something, then we leave.
			if (!collisionManager) { CheckCollisionExit(); return; }

			// Two completely different Updates can be called, based on whether GPU is involved in collisions or not.
			if (collisionManager.disableComputeShaders)
				UpdateForCPUCollisions();
			else UpdateForGPUCollisions();
		}

		// Collision detection that uses Compute Shaders
		void UpdateForGPUCollisions()
		{
			// The potential OnExit event is called at start of frame, because CollisionManager does the global check in LateUpdate.
			if (wasCollidingLastFrame && !isColliding)
				if (bullet.additionalBehaviourScripts.Count > 0)
					for (int i = 0; i < bullet.additionalBehaviourScripts.Count; i++)
						bullet.additionalBehaviourScripts[i].OnBulletCollisionExit();

			// Prepare the value for next frame :
			wasCollidingLastFrame = isColliding;
			isColliding = false;

			// This variable is unused in this GPU variant, but let's keep consistency
			isCollidingThisFrame = false;

			// As said earlier, actual check happens at BulletCollisionManager.LateUpdate().
		}

		// Collision detection that does not use Compute Shaders
		void UpdateForCPUCollisions()
		{
			isCollidingThisFrame = false;

			// Checks all targets for collisions
			for (int i = 0; i < collisionManager.allBulletReceivers.Count; i++)
			{
				BulletReceiver br = collisionManager.allBulletReceivers[i];
				if (!collisionManager.CheckCollisionCompatibility(collisionTags, br.collisionTags)) continue;

				// this trick allows to avoid setting up a "for" loop in most cases
				if (colliders.Length == 1)
				{
					CPUCollisionResult collResult = ComputeCollisionBetween(colliders[0], br);
					if (collResult.happened)
						CollideWith(br, collResult.hitPoint);
				}
				else
					for (int j = 0; j < colliders.Length; j++)
					{
						CPUCollisionResult collResult = ComputeCollisionBetween(colliders[j], br);
						if (collResult.happened)
						{
							CollideWith(br, collResult.hitPoint);
							break;
						}
					}
			}

			// This line is only here to ease up the OnCollisionStart detection
			wasCollidingLastFrame = isColliding;

			// OnCollisionExit detection here
			if (!isCollidingThisFrame)
				CheckCollisionExit();
		}

		#endregion

		#region toolbox


		// Called at Bullet.ApplyBulletParams()
		public void ApplyBulletParams(BulletParams bp)
		{
			// reset behaviour
			isColliding = false;
			isCollidingThisFrame = false;
			wasCollidingLastFrame = false;

			isEnabled = bp.canCollide;
			if (!isEnabled) return;
			collisionManager.AddBulletLocal(bullet);

			SetColliders(bp.colliders);
			collisionTags = bp.collisionTags;

			dieOnCollision = bp.dieOnCollision;
		}

		// Called when setting scale of movement module
		public void RefreshScale()
		{
			scale = moduleMovement.currentScale;
		}

		// Get a collider
		public BulletCollider GetCollider(int index) { return colliders[index];	}
		// Get all colliders
		public BulletCollider[] GetColliders() { return colliders; }

		// Modify colliders
		public void SetColliders(BulletCollider[] newColliders)
		{
			// ensuring the array is valid
			if (newColliders == null) return;
			if (newColliders.Length == 0) return;

			for (int i=0; i<newColliders.Length; i++)
			{
				newColliders[i].collisionTags = collisionTags.tagList;
				newColliders[i].bullet = bullet;
			}			

            // Refresh informations in the manager
            bool wasEnabled = isEnabled;
            if (isEnabled) Disable();
			colliders = newColliders;
            if (wasEnabled) Enable();
		}

		#endregion

		#region mathematical collision detection methods (CPU only)

		// Collision detection per BulletCollider ( = per part of the hitbox)
		public CPUCollisionResult ComputeCollisionBetween(BulletCollider col, BulletReceiver br)
		{
			if (br.colliderType == BulletReceiverType.Circle)
			{
				if (col.colliderType == BulletColliderType.Circle) return ComputeCircleCollision(col, br);
				if (col.colliderType == BulletColliderType.Line) return ComputeLaserCollision(col, br);
			}
			else
			{
				if (col.colliderType == BulletColliderType.Circle) return ComputeCircleCollisionWithLine(col, br);
				if (col.colliderType == BulletColliderType.Line) return ComputeLaserCollisionWithLine(col, br);
			}

			CPUCollisionResult result;
			result.happened = false;
			result.hitPoint = self.position;
			return result;
		}

		// Collision detection for circle : radius and distance comparison
		public CPUCollisionResult ComputeCircleCollision(BulletCollider col, BulletReceiver br)
		{
			Transform tr = br.self;

			Vector3 pos = self.position;
			if (col.offset.x != 0) pos += scale * col.offset.x * self.right;
			if (col.offset.y != 0) pos += scale * col.offset.y * self.up;

			Vector3 brPos = tr.position;
			if (br.hitboxOffset.x != 0) brPos += tr.lossyScale.x * br.hitboxOffset.x * tr.right;
			if (br.hitboxOffset.y != 0) brPos += tr.lossyScale.y * br.hitboxOffset.y * tr.up;

			//float scaledReceiverRadius = br.hitboxSize * (tr.localScale.x * 0.5f + tr.localScale.y * 0.5f);
			float scaledReceiverRadius = br.hitboxSize * (tr.lossyScale.x + tr.lossyScale.y) * 0.5f;

			float x = (brPos.x - pos.x);
			float y = (brPos.y - pos.y);
			float z = (brPos.z - pos.z);
			float dist2 = x * x + y * y + z * z;
			float bcRadius = col.size * scale;
			float radius = (bcRadius + scaledReceiverRadius);

			CPUCollisionResult result;
			result.happened = (dist2 < (radius * radius));
			result.hitPoint = pos;
			if (radius > 0) result.hitPoint = Vector3.Lerp(pos, brPos, (bcRadius/radius));
			return result;
		}

		// Collision detection for lasers : they're treated as a bunch of points which are <br.hitboxRadius> from each other.
		CPUCollisionResult ComputeLaserCollision(BulletCollider col, BulletReceiver br)
		{
			CPUCollisionResult result;
			result.happened = false;
			result.hitPoint = self.position;
				
			if (col.lineEnd - col.lineStart == Vector2.zero)
				return result;

			Transform tr = br.self;
			Vector3 brPos = tr.position;
			if (br.hitboxOffset.x != 0) brPos += tr.lossyScale.x * br.hitboxOffset.x * tr.right;
			if (br.hitboxOffset.y != 0) brPos += tr.lossyScale.y * br.hitboxOffset.y * tr.up;

			float rad = br.hitboxSize * (tr.lossyScale.x + tr.lossyScale.y) * 0.5f;
			float rad2 = rad * rad;

			float laserLengthBrowsed = 0;
			Vector3 curPoint = new Vector3(self.position.x, self.position.y, self.position.z);
			curPoint += scale * (self.right * col.lineStart.x + self.up * col.lineStart.y);

			// Calculating laser length, squared
			float xDelta = col.lineEnd.x - col.lineStart.x;
			float yDelta = col.lineEnd.y - col.lineStart.y;
			float laserLength2 = xDelta * xDelta + yDelta * yDelta;
			float laserLength = Mathf.Sqrt(laserLength2);

			float realLaserLength = laserLength * scale;
			Vector3 laserDirection = (self.position + self.right * (col.lineEnd.x - col.lineStart.x) + self.up * (col.lineEnd.y - col.lineStart.y)).normalized;
			while (true)
			{
				float x = (brPos.x - curPoint.x);
				float y = (brPos.y - curPoint.y);
				float z = (brPos.z - curPoint.z);
				float dist2 = x * x + y * y + z * z;
				if (dist2 <= rad2)
				{
					result.happened = true;
					result.hitPoint = curPoint;
					break;
				}

				if (laserLengthBrowsed == realLaserLength) break;

				laserLengthBrowsed += rad;
				if (laserLengthBrowsed > realLaserLength) laserLengthBrowsed = realLaserLength;
				curPoint.x = self.position.x + laserDirection.x * laserLengthBrowsed;
				curPoint.y = self.position.y + laserDirection.y * laserLengthBrowsed;
				curPoint.z = self.position.z + laserDirection.z * laserLengthBrowsed;
			}

			return result;
		}

		// Collision detection for circle, but the receiver is a line : same as laser bullet + circle receiver.
		CPUCollisionResult ComputeCircleCollisionWithLine(BulletCollider col, BulletReceiver br)
		{
			Vector3 pos = self.position + scale * (self.right * col.offset.x + self.up * col.offset.y);
			float rad = col.size * scale;
			float rad2 = rad * rad;

			float lineLengthBrowsed = 0;
			Transform tr = br.self;
			Vector3 brPos = tr.position;
			if (br.hitboxOffset.x != 0) brPos += tr.lossyScale.x * br.hitboxOffset.x * tr.right;
			if (br.hitboxOffset.y != 0) brPos += tr.lossyScale.y * br.hitboxOffset.y * tr.up;
			Vector3 curPoint = new Vector3(brPos.x, brPos.y, brPos.z);
			//float brScale = tr.localScale.x * 0.5f + tr.localScale.y * 0.5f;
			// ^ commented out: there's no point in taking scale.x into account
			float brScale = tr.lossyScale.y;

			CPUCollisionResult result;
			result.happened = false;
			result.hitPoint = pos;

			float realLineLength = br.hitboxSize * brScale;
			while (true)
			{
				float x = (pos.x - curPoint.x);
				float y = (pos.y - curPoint.y);
				float z = (pos.z - curPoint.z);
				float dist2 = x * x + y * y + z * z;
				if (dist2 <= rad2)
				{
					result.happened = true;
					result.hitPoint = curPoint;
					break;
				}

				if (lineLengthBrowsed == realLineLength) break;

				lineLengthBrowsed += rad;
				if (lineLengthBrowsed > realLineLength) lineLengthBrowsed = realLineLength;
				curPoint.x = brPos.x + tr.up.x * lineLengthBrowsed;
				curPoint.y = brPos.y + tr.up.y * lineLengthBrowsed;
				curPoint.z = brPos.z + tr.up.z * lineLengthBrowsed;
			}

			return result;
		}

		// Collision detection for laser + line : two segments crossing. Computationally a bit more expensive.
		CPUCollisionResult ComputeLaserCollisionWithLine(BulletCollider col, BulletReceiver br)
		{
			CPUCollisionResult result;

			if (col.lineEnd - col.lineStart == Vector2.zero)
			{
				result.happened = false;
				result.hitPoint = self.position;
				return result;
			}

			Vector3 colStart, colEnd, brStart, brEnd;

			colStart = self.position + scale * (self.right * col.lineStart.x + self.up * col.lineStart.y);
			colEnd = self.position + scale * (self.right * col.lineEnd.x + self.up * col.lineEnd.y);

			Transform brtr = br.self;
			brStart = brtr.position;
			if (br.hitboxOffset.x != 0) brStart += brtr.lossyScale.x * br.hitboxOffset.x * brtr.right;
			if (br.hitboxOffset.y != 0) brStart += brtr.lossyScale.y * br.hitboxOffset.y * brtr.up;
			//float brScale = brtr.lossyScale.x * 0.5f + brtr.lossyScale.y * 0.5f;
			// there's no point in taking scale.x into account
			float brScale = brtr.lossyScale.y;
			brEnd = brStart + brtr.up * br.hitboxSize * brScale;

			Vector3 intersect = Vector3.one * 3000;

			if (!LineLineIntersection(out intersect, colStart, (colEnd-colStart).normalized, brStart, brtr.up))
			{
				result.happened = false;
				result.hitPoint = intersect;
				return result;
			}
			
			result.hitPoint = intersect;
			result.happened = true;

			// is the intersection on both segments? (Unity lacks a Vector3.InverseLerpUnclamped.)
			float colX = (intersect.x - colStart.x) / (colEnd.x - colStart.x);
			if (colX <= 0 || colX >= 1) result.happened = false;
			else
			{
				float colY = (intersect.y - colStart.y) / (colEnd.y - colStart.y);
				if (colY <= 0 || colY >= 1) result.happened = false;
				else
				{
					float colZ = (intersect.z - colStart.z) / (colEnd.z - colStart.z);
					if (colZ <= 0 || colZ >= 1) result.happened = false;
					else
					{
						float brX = (intersect.x - brStart.x) / (brEnd.x - brStart.x);
						if (brX <= 0 || brX >= 1) result.happened = false;
						else
						{
							float brY = (intersect.y - brStart.y) / (brEnd.y - brStart.y);
							if (brY <= 0 || brY >= 1) result.happened = false;
							else
							{
								float brZ = (intersect.z - brStart.z) / (brEnd.z - brStart.z);
								if (brZ <= 0 || brZ >= 1) result.happened = false;
							}
						}
					}
				}
			}

			return result;
		}

		// Line-line collision detection, as proposed in wiki.unity3d.com
		bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
		{
			Vector3 lineVec3 = linePoint2 - linePoint1;
			Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
			Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

			float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

			// is coplanar, and not parallel
			if (Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f)
			{
				float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
				intersection = linePoint1 + (lineVec1 * s);
				return true;
			}
			else
			{
				intersection = Vector3.zero;
				return false;
			}
		}

		#endregion

		#region collision callbacks

		// When actual collision with a Receiver happens, this function applies the collision.
		public void CollideWith(BulletReceiver br)
		{
			if (!br.CanAcceptCollisionsThisFrame()) return;
			if (br.HasAlreadyCollidedThisFrame(bullet)) return;

			Vector3 collisionPoint = bullet.self.position * 0.5f + br.self.position * 0.5f;

			if (bullet.additionalBehaviourScripts.Count > 0)
				for (int i = 0; i < bullet.additionalBehaviourScripts.Count; i++)
				{
					if (!wasCollidingLastFrame) bullet.additionalBehaviourScripts[i].OnBulletCollisionEnter(br, collisionPoint); 
					bullet.additionalBehaviourScripts[i].OnBulletCollision(br, collisionPoint);
				}

			isColliding = true;
			isCollidingThisFrame = true;

			br.GetHit(collisionPoint, bullet);
		}

		// Overload providing collision position.
		public void CollideWith(BulletReceiver br, Vector3 collisionPoint)
		{
			if (!br.CanAcceptCollisionsThisFrame()) return;
			if (br.HasAlreadyCollidedThisFrame(bullet)) return;

			if (bullet.additionalBehaviourScripts.Count > 0)
				for (int i = 0; i < bullet.additionalBehaviourScripts.Count; i++)
				{
					if (!wasCollidingLastFrame) bullet.additionalBehaviourScripts[i].OnBulletCollisionEnter(br, collisionPoint); 
					bullet.additionalBehaviourScripts[i].OnBulletCollision(br, collisionPoint);
				}

			isColliding = true;
			isCollidingThisFrame = true;

			br.GetHit(collisionPoint, bullet);
		}

		// Checks whether we must send an OnCollisionExit() message to the custom behaviours, and does it if needed.
		private void CheckCollisionExit()
		{
			bool wasColliding = isColliding;
			isColliding = false;

			if (!wasColliding) return;

			if (bullet.additionalBehaviourScripts.Count > 0)
				for (int i = 0; i < bullet.additionalBehaviourScripts.Count; i++)
					bullet.additionalBehaviourScripts[i].OnBulletCollisionExit();
		}

		#endregion
	}
}