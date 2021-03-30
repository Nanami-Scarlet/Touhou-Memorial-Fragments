using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// Module for handling homing behaviours (i.e. going towards a target)
	public class BulletModuleHoming : BulletModule
	{
		public PreferredTarget preferredTarget;
		public float homingSpawnRate, homingAngularSpeed, homingAngleThreshold, targetRefreshInterval;
		public float currentHomingSpeed { get; private set; }
		public CollisionTags homingTags;
		public bool useSameTagsAsCollision;
		public BulletCurve homingOverLifetime;

		public Transform currentTarget;

		public List<BulletReceiver> possibleTargets { get; private set; }

		// Called at Bullet.AWake()
		public override void Awake()
		{
			base.Awake();
			possibleTargets = new List<BulletReceiver>();
		}

		public override void Enable() { base.Enable(); RefreshTarget(); }
		public override void Disable() { base.Disable(); }

		// Called at Bullet.Update()
		public void Update()
		{
			// enabled spawn module means we're still waiting for the actual spawn
			if (moduleSpawn.isEnabled) return;

			// No need to bother for homing if we only enabled it for an orientated spawn
			if (homingAngularSpeed == 0) return;

			if (targetRefreshInterval > 0)
				if (bullet.timeSinceAlive % targetRefreshInterval < Time.deltaTime)
					RefreshTarget();

			if (!currentTarget) return;

			if (homingOverLifetime.enabled)
			{
				homingOverLifetime.Update();
				currentHomingSpeed = homingAngularSpeed * homingOverLifetime.GetCurveResult();
			}
			else currentHomingSpeed = homingAngularSpeed;

			Vector3 diff = currentTarget.position - self.position;
			if (Vector3.Angle(self.up, diff) > homingAngleThreshold)
			{
				Vector3 cross = Vector3.Cross(self.up, diff);
				cross = bulletCanvas.InverseTransformVector(cross);
				moduleMovement.Rotate(currentHomingSpeed * Mathf.Sign(cross.z) * Time.deltaTime);
			}
		}

		// Called at Bullet.ApplyBulletParams()
		public void ApplyBulletParams(BulletParams bp)
		{
			isEnabled = bp.homing;

			// make sure curves are not running unless told to
			homingOverLifetime.Stop();
			
			if (!isEnabled)
			{
				// reset curves in case module gets reenabled later on
				homingOverLifetime.enabled = false;
				return;
			}

			useSameTagsAsCollision = bp.useSameTagsAsCollision;
			homingTags = bp.homingTags;

			preferredTarget = (PreferredTarget) solver.SolveDynamicEnum(bp.preferredTarget, 17149663, ParameterOwner.Bullet);
			RefreshTarget();
			homingSpawnRate = solver.SolveDynamicFloat(bp.lookAtTargetAtSpawn, 7826475, ParameterOwner.Bullet);

			homingAngularSpeed = solver.SolveDynamicFloat(bp.homingAngularSpeed, 19096746, ParameterOwner.Bullet);

			// if we're only here for the homing spawn, we're done :
			if (homingAngularSpeed == 0) return;

			targetRefreshInterval = solver.SolveDynamicFloat(bp.targetRefreshInterval, 855685, ParameterOwner.Bullet);
			homingAngleThreshold = solver.SolveDynamicFloat(bp.homingAngleThreshold, 2909616, ParameterOwner.Bullet);

			currentHomingSpeed = homingAngularSpeed;

			homingOverLifetime = solver.SolveDynamicBulletCurve(bp.homingOverLifetime, 26372642, ParameterOwner.Bullet);
			homingOverLifetime.UpdateInternalValues();
			if (homingOverLifetime.enabled)
			{
				currentHomingSpeed *= homingOverLifetime.GetCurveResult();
				homingOverLifetime.Boot();
			}
		}

		// Finds and changes target if possible
		public Transform RefreshTarget()
		{
			currentTarget = GetNewPossibleTarget(preferredTarget);
			return currentTarget;
		}

		// Overload where the user can override the preferred target
		public Transform RefreshTarget(PreferredTarget targetChoiceType)
		{
			currentTarget = GetNewPossibleTarget(targetChoiceType);
			return currentTarget;
		}

		// If homing, changes target
		public Transform GetNewPossibleTarget(PreferredTarget targetChoiceType)
		{
			RefreshListOfPotentialTargets();

			// This case actually happens quite often in gameplay, which is normal - no need to spam log warnings
			if (possibleTargets == null) return null; //{ Debug.LogWarning(name + " : no possible target for homing!"); return null; }
			if (possibleTargets.Count == 0) return null; //{ Debug.LogWarning(name + " : no possible target for homing!"); return null; }

			if (preferredTarget == PreferredTarget.Oldest)
			{
				for (int i = 0; i < possibleTargets.Count; i++)
					if (possibleTargets[i].enabled)
						return possibleTargets[i].self;

				return null;
			}

			if (preferredTarget == PreferredTarget.Newest)
			{
				for (int i = possibleTargets.Count - 1; i > -1; i--)
					if (possibleTargets[i].enabled)
						return possibleTargets[i].self;

				return null;
			}

			if (preferredTarget == PreferredTarget.Random)
			{
				bool nothingEnabled = true;
				BulletReceiver firstPossibleResult = null;
				for (int i = 0; i < possibleTargets.Count; i++)
					if (possibleTargets[i].enabled)
					{
						firstPossibleResult = possibleTargets[i];
						nothingEnabled = false;
						break;
					}

				if (nothingEnabled) return null;

				BulletReceiver randomResult = possibleTargets[Random.Range(0, possibleTargets.Count)];
				int j = 0;
				while (!randomResult.enabled)
				{
					randomResult = possibleTargets[Random.Range(0, possibleTargets.Count)];
					j++;
					if (j > 2 * possibleTargets.Count) randomResult = firstPossibleResult; // don't let random take too much time
				}

				return randomResult.self;
			}

			// the following is, implicitely, if PreferredTarget.Closest or PreferredTarget.Furthest :

			bool nothingIsEnabled = true;
			BulletReceiver firstPossibleTarget = null;
			for (int i = 0; i < possibleTargets.Count; i++)
				if (possibleTargets[i].enabled)
				{
					firstPossibleTarget = possibleTargets[i];
					nothingIsEnabled = false;
					break;
				}

			if (nothingIsEnabled) return null;

			Transform result = firstPossibleTarget.self;
			if (possibleTargets.Count == 1) return result;

			float x = (result.position.x - self.position.x);
			float y = (result.position.y - self.position.y);
			float z = (result.position.z - self.position.z);
			float resDist2 = y * y + x * x + z * z;

			for (int i = 1; i < possibleTargets.Count; i++)
			{
				if (!possibleTargets[i].enabled) continue;
				Transform cur = possibleTargets[i].self;
				float xc = (cur.position.x - self.position.x);
				float yc = (cur.position.y - self.position.y);
				float zc = (cur.position.z - self.position.z);
				float curDist2 = yc * yc + xc * xc + zc * zc;
				if ((curDist2 < resDist2 && preferredTarget == PreferredTarget.Closest) || (curDist2 > resDist2 && preferredTarget == PreferredTarget.Farthest))
				{
					result = cur;
					resDist2 = curDist2;
				}
			}

			return result;
		}

		// Overload using default target choice type
		public Transform GetNewPossibleTarget()
		{
			return GetNewPossibleTarget(preferredTarget);
		}

		// When called, instantly rotates.
		// argument from -1 to 1. 0 = don't do anything, 0.5 = look between current orientation and target, 1 = look at target.
		// negative values will flee from target rather than looking at it.
		public void LookAtTarget(float ratio=1)
		{
			// if not already targeting something, try to get a target to begin with (using Oldest which is the most efficient way to get it)
			if (!currentTarget) currentTarget = GetNewPossibleTarget(PreferredTarget.Oldest);
			if (!currentTarget) return;

			moduleMovement.LookAt(currentTarget, ratio);
		}

		// Functions the same as LookAtTarget, but just returns the angle and doesn't perform the rotation
		public float GetAngleToTarget(float ratio=1)
		{
			if (!currentTarget) currentTarget = GetNewPossibleTarget(PreferredTarget.Oldest);
			if (!currentTarget) return 0;

			return moduleMovement.GetAngleTo(currentTarget, ratio);
		}

		// Called the potential target list should be updated
		void RefreshListOfPotentialTargets()
		{
			possibleTargets = collisionManager.GetTargetListLocal(useSameTagsAsCollision ? moduleCollision.collisionTags : homingTags);
		}
	}
}