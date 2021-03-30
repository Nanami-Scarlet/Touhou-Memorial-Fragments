using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// Module that handles any transform-related operations : translation, rotation, scaling
	public class BulletModuleMovement : BulletModule
	{
		#region properties

		// transform get/set
		public float baseSpeed, baseAngularSpeed, baseScale;
		public float currentSpeed { get; private set; }
		public float currentAngularSpeed { get; private set; }
		public float currentScale
		{
			get { return _currentScale; }
			private set
			{
				_currentScale = value;
				if (bullet)
				{
					// Preserves mirroring. Not using Mathf.Sign because this allows getting rid of the zero case
					Vector3 baseVector = Vector3.one;
					if (self.localScale.x < 0) baseVector.x = -1;
					if (self.localScale.y < 0) baseVector.y = -1;
					if (self.localScale.z < 0) baseVector.z = -1;
					self.localScale = baseVector * value;
				}
				if (bullet) moduleCollision.RefreshScale();
			}
		}
		private float _currentScale;

		public Space animationMovementSpace;
		private Vector4 prevAnimValues;

		private float[] lastBounceTimestamp;

		#endregion

		#region curves

		public BulletCurve speedOverLifetime;
		public BulletCurve angularSpeedOverLifetime;
		public BulletCurve scaleOverLifetime;

		public BulletCurve moveXFromAnim;
		public BulletCurve moveYFromAnim;
		public BulletCurve rotateFromAnim;
		public BulletCurve scaleFromAnim;

		#endregion

		public override void Enable() { base.Enable(); }
		public override void Disable() { base.Disable(); }

		// Called in Bullet.Update()
		public void Update()
		{
			// enabled spawn module means we're still waiting for the actual spawn
			if (moduleSpawn.isEnabled) return;
			
			// translation if driven by AnimationClip
			if (moveXFromAnim.enabled || moveYFromAnim.enabled)
			{
				Vector3 move = Vector3.zero;
				if (moveXFromAnim.enabled)
				{
					moveXFromAnim.Update();
					float cr = moveXFromAnim.GetCurveResult();
					move.x = cr - prevAnimValues.x;
					prevAnimValues.x = cr;
				}
				if (moveYFromAnim.enabled)
				{
					moveYFromAnim.Update();
					float cr = moveYFromAnim.GetCurveResult();
					move.y = cr - prevAnimValues.y;
					prevAnimValues.y = cr;
				}
				Translate(move, animationMovementSpace);
			}
			// translation if driven by simple values
			else
			{
				if (speedOverLifetime.enabled)
				{
					speedOverLifetime.Update();
					currentSpeed = baseSpeed * speedOverLifetime.GetCurveResult();
				}
				else currentSpeed = baseSpeed;
				Translate(Vector3.up * currentSpeed * Time.deltaTime, Space.Self);
			}

			// rotation if driven by an AnimationClip
			if (rotateFromAnim.enabled)
			{
				float angle = 0;
				rotateFromAnim.Update();
				float cr = rotateFromAnim.GetCurveResult();
				angle = cr - prevAnimValues.z;
				prevAnimValues.z = cr;
				Rotate(angle);
			}
			// rotation if driven by simple values
			else
			{
				if (angularSpeedOverLifetime.enabled)
				{
					angularSpeedOverLifetime.Update();
					currentAngularSpeed = baseAngularSpeed * angularSpeedOverLifetime.GetCurveResult();
				}
				else currentAngularSpeed = baseAngularSpeed;
				Rotate(currentAngularSpeed * Time.deltaTime);
			}

			// scale if driven by simple values
			if (scaleFromAnim.enabled)
			{
				float newScale = 1;
				scaleFromAnim.Update();
				float cr = scaleFromAnim.GetCurveResult();
				newScale = cr - prevAnimValues.w;
				prevAnimValues.w = cr;
				currentScale = newScale;
			}
			else
			{
				if (scaleOverLifetime.enabled)
				{
					scaleOverLifetime.Update();
					currentScale = baseScale * scaleOverLifetime.GetCurveResult();
				}
				else currentScale = baseScale;
			}
		}

		// Called in Bullet.ApplyBulletParams()
		public void ApplyBulletParams(BulletParams bp)
		{
			isEnabled = bp.canMove;

			// make sure curves are not running unless told to
			speedOverLifetime.Stop();
			angularSpeedOverLifetime.Stop();
			scaleOverLifetime.Stop();
			moveXFromAnim.Stop();
			moveYFromAnim.Stop();
			rotateFromAnim.Stop();
			scaleFromAnim.Stop();

			if (!isEnabled)
			{
				// reset curves in case module gets reenabled later on
				speedOverLifetime.enabled = false;
				angularSpeedOverLifetime.enabled = false;
				scaleOverLifetime.enabled = false;
				moveXFromAnim.enabled = false;
				moveYFromAnim.enabled = false;
				rotateFromAnim.enabled = false;
				scaleFromAnim.enabled = false;
				return;
			}

			prevAnimValues = Vector4.zero;
			animationMovementSpace = bp.animationMovementSpace;

			baseSpeed = solver.SolveDynamicFloat(bp.forwardSpeed, 5414344, ParameterOwner.Bullet);
			baseAngularSpeed = solver.SolveDynamicFloat(bp.angularSpeed, 14855506, ParameterOwner.Bullet);
			baseScale = solver.SolveDynamicFloat(bp.startScale, 20654741, ParameterOwner.Bullet);

			currentSpeed = baseSpeed;
			currentAngularSpeed = baseAngularSpeed;
			currentScale = baseScale;

			// Speed curve
			speedOverLifetime = solver.SolveDynamicBulletCurve(bp.speedOverLifetime, 3760732, ParameterOwner.Bullet);
			speedOverLifetime.UpdateInternalValues(bullet);
			if (speedOverLifetime.enabled)
			{
				speedOverLifetime.Boot();
				currentSpeed *= speedOverLifetime.GetCurveResult();
			}

			// Rotation curve
			angularSpeedOverLifetime = solver.SolveDynamicBulletCurve(bp.angularSpeedOverLifetime, 29743360, ParameterOwner.Bullet);
			angularSpeedOverLifetime.UpdateInternalValues(bullet);
			if (angularSpeedOverLifetime.enabled)
			{
				angularSpeedOverLifetime.Boot();
				currentAngularSpeed *= angularSpeedOverLifetime.GetCurveResult();
			}

			// Scale curve
			scaleOverLifetime = solver.SolveDynamicBulletCurve(bp.scaleOverLifetime, 4293275, ParameterOwner.Bullet);
			scaleOverLifetime.UpdateInternalValues(bullet);
			if (scaleOverLifetime.enabled)
			{
				scaleOverLifetime.Boot();
				currentScale *= scaleOverLifetime.GetCurveResult();
			}

			// Initializing anim curves
			moveXFromAnim = bp.xMovementFromAnim;
			moveYFromAnim = bp.yMovementFromAnim;
			rotateFromAnim = bp.rotationFromAnim;
			scaleFromAnim = bp.scaleFromAnim;
			moveXFromAnim.UpdateInternalValues(bullet);
			moveYFromAnim.UpdateInternalValues(bullet);
			rotateFromAnim.UpdateInternalValues(bullet);
			scaleFromAnim.UpdateInternalValues(bullet);

			// Booting anim curves
			if (moveXFromAnim.enabled) moveXFromAnim.Boot();
			if (moveYFromAnim.enabled) moveYFromAnim.Boot();
			if (rotateFromAnim.enabled) rotateFromAnim.Boot();
			if (scaleFromAnim.enabled) scaleFromAnim.Boot();

			// Reset bounce helpers
			lastBounceTimestamp = new float[8];
			for (int i = 0; i < lastBounceTimestamp.Length; i++)
				lastBounceTimestamp[i] = -10f;
		}

		// Translate of any vector
		public void Translate(Vector3 movement, Space space)
		{
			if (space == Space.Self) self.Translate(movement, Space.Self);

			if (space == Space.World)
				self.Translate(bulletCanvas.up * movement.y + bulletCanvas.right * movement.x + bulletCanvas.forward * movement.z, Space.World);
		}

		// Float overload
		public void Translate(float x, float y, float z, Space space)
		{
			if (space == Space.Self) self.Translate(x, y, z, Space.Self);

			if (space == Space.World)
				self.Translate(bulletCanvas.up * y + bulletCanvas.right * x + bulletCanvas.forward * z, Space.World);
		}

		// Rotate of any angle - having the rotate in a function will ease the implementation of some advanced features
		public void Rotate(float angle)
		{
			self.Rotate(Vector3.forward, angle, Space.Self);
		}

		// Look at a certain transform, as if homing
		public void LookAt(Transform target, float ratio=1)
		{
			if (!target) return;
			if (ratio == 0) return;
			Rotate(GetAngleTo(target, ratio));
		}

		// Look at a certain position, as if homing
		public void LookAt(Vector3 target, float ratio=1)
		{
			if (ratio == 0) return;
			Rotate(GetAngleTo(target, ratio));
		}

		// Finds out what angle is needed to perform a LookAt and returns it, but does not actually do the LookAt
		public float GetAngleTo(Transform target, float ratio=1)
		{
			if (!target) return 0;
			return GetAngleTo(target.position, ratio);
		}
		public float GetAngleTo(Vector3 target, float ratio=1)
		{
			Vector2 diff = target - self.position;
			diff *= Mathf.Sign(ratio);
			float angle = Vector2.Angle(self.up, diff);
			Vector3 cross = Vector3.Cross(self.up, diff);
			cross = bulletCanvas.InverseTransformVector(cross);
			if (cross.z < 0) angle *= -1;
			return angle * Mathf.Abs(ratio);
		}

		// Bounce on any flat surface, ie. a wall. Bounce will be aborted if a previous bounce occured in the same channel during the last <cooldownTime> seconds
		public void Bounce(Vector3 wallDirection, float cooldownTime=0.1f, BounceChannel bounceChannel = BounceChannel.Horizontal)
		{
			// discard unavailable wall directions
			if (wallDirection == Vector3.zero) return;

			// Check if another bounce had just occured in one of the wanted channels. If so, discard the function.
			for (int i = 0; i < 8; i++)
			{
				BounceChannel cur = (BounceChannel)(1 << i);
				if ((bounceChannel & cur) == cur)
					if (Time.time - lastBounceTimestamp[i] < cooldownTime) return;
			}

			// apply actual bounce
			Vector3 rotationAxis = Vector3.Cross(self.up, wallDirection);
			Vector3 wallNormal = Quaternion.AngleAxis(90, rotationAxis) * wallDirection;
			Vector3 newUp = Vector3.Reflect(self.up, wallNormal);
			self.Rotate(Quaternion.FromToRotation(self.up, newUp).eulerAngles);

			// update timestamps in wanted channels
			for (int i = 0; i < 8; i++)
			{
				BounceChannel cur = (BounceChannel)(1 << i);
				if ((bounceChannel & cur) == cur)
					lastBounceTimestamp[i] = Time.time;
			}
		}

		// Gets "2D world position", relative to the manager, accounting for manager rotation
		public Vector2 GetGlobalPosition()
		{
			if (!poolManager) return new Vector2(self.position.x, self.position.y);
			Vector3 relPos = poolManager.mainTransform.InverseTransformPoint(self.position);
			return new Vector2(relPos.x, relPos.y);
		}

		// Gets "2D world rotation", relative to the manager, accounting for manager rotation
		public float GetGlobalRotation()
		{
			if (!poolManager) return self.eulerAngles.z;
			Quaternion relRot = Quaternion.Inverse(poolManager.mainTransform.rotation) * self.rotation;
			return relRot.eulerAngles.z;
		}

		// Sets "2D world position", relative to the manager, accounting for manager rotation
		public void SetGlobalPosition(Vector2 newValue)
		{
			if (!poolManager) self.position = new Vector3(newValue.x, newValue.y, self.position.z);
			else
			{
				Vector3 managerPos = poolManager.mainTransform.position;
				self.position = poolManager.mainTransform.InverseTransformPoint(new Vector3(newValue.x, newValue.y, 0f));
			}
		}

		// Sets "2D world rotation", relative to the manager, accounting for manager rotation
		public void SetGlobalRotation(float newValue)
		{
			newValue += 3600;
			newValue = newValue % 360;
			if (!poolManager) self.eulerAngles = new Vector3(self.eulerAngles.x, self.eulerAngles.y, newValue);
			else
			{
				self.rotation = poolManager.mainTransform.rotation;
				self.Rotate(poolManager.mainTransform.forward, newValue, Space.World);
			}
		}

		// Quickly sets local position (relative to closest parent)
		public void SetLocalPosition(Vector2 newValue)
		{
			self.localPosition = new Vector3(newValue.x, newValue.y, self.localPosition.z);
		}

		// Quickly sets local rotation (relative to closest parent)
		public void SetLocalRotation(float newValue)
		{
			self.localEulerAngles = new Vector3(self.localEulerAngles.x, self.localEulerAngles.y, newValue);
		}
	}

	[System.Flags]
	public enum BounceChannel
	{
		Horizontal = (1 << 0),
		Vertical = (1 << 1),
		SlashDiagonal = (1 << 2),
		AntislashDiagonal = (1 << 3),
		Custom0 = (1 << 4),
		Custom1 = (1 << 5),
		Custom2 = (1 << 6),
		Custom3 = (1 << 7)
	}
}