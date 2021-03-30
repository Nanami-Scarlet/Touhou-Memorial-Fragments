using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// Module for handling bullet lifetime
	public class BulletModuleLifespan : BulletModule
	{
		private float _lifespan;
		public float lifespan
		{
			get { return _lifespan; }
			set
			{
				_lifespan = value;
				moduleHoming.homingOverLifetime.UpdateInternalValues(bullet);
				moduleRenderer.alphaOverLifetime.UpdateInternalValues(bullet);
				moduleRenderer.colorOverLifetime.UpdateInternalValues(bullet);
				moduleMovement.speedOverLifetime.UpdateInternalValues(bullet);
				moduleMovement.angularSpeedOverLifetime.UpdateInternalValues(bullet);
				moduleMovement.scaleOverLifetime.UpdateInternalValues(bullet);
				moduleMovement.moveXFromAnim.UpdateInternalValues(bullet);
				moduleMovement.moveYFromAnim.UpdateInternalValues(bullet);
				moduleMovement.rotateFromAnim.UpdateInternalValues(bullet);
				moduleMovement.scaleFromAnim.UpdateInternalValues(bullet);
			}
		}

		public override void Enable() { base.Enable(); }
		public override void Disable() { base.Disable(); }

		// Called at Bullet.Update()
		public void Update()
		{
			// enabled spawn module means we're still waiting for the actual spawn
			if (moduleSpawn.isEnabled) return;

			if (bullet.timeSinceAlive > lifespan)
				bullet.Die(true);
		}

		// Called at Bullet.ApplyBulletParams()
		public void ApplyBulletParams(BulletParams bp)
		{
			isEnabled = bp.hasLifespan;
			if (!isEnabled) return;

			lifespan = solver.SolveDynamicFloat(bp.lifespan, 10405888, ParameterOwner.Bullet);
		}
	}
}