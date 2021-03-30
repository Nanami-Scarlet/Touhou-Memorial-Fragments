using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// Module for handling bullet delayed spawn
	public class BulletModuleSpawn : BulletModule
	{
		// Delayed spawn stats : duration and audio played when shot
		public float timeBeforeSpawn;
		public bool playAudio;
		public AudioClip audioClip;
		// memorizing orientation from ShotParams can be necessary if bullet is both homing and delayed
		private float rotateFromSpawn;

		public override void Enable() { base.Enable(); }
		public override void Disable() { base.Disable(); }

		// Called at Bullet.Update()
		public void Update()
		{
			timeBeforeSpawn -= Time.deltaTime;
			if (timeBeforeSpawn > 0) return;
			TriggerBulletBirth();
		}

		// Makes the bullet achieve its spawn.
		public void TriggerBulletBirth()
		{
			// Chances are targets have moved during the delay, so full restore is needed here
			if (moduleHoming.isEnabled)
			{
				moduleHoming.RefreshTarget();
				moduleHoming.LookAtTarget(moduleHoming.homingSpawnRate);
				moduleMovement.Rotate(rotateFromSpawn);
			}

			isEnabled = false;
			if (playAudio && audioClip) audioManager.PlayLocal(audioClip);
			bullet.Prepare(true);
		}

		// Called at Bullet.ApplyBulletParams()
		public void ApplyBulletParams(BulletParams bp)
		{
			isEnabled = bp.delaySpawn;
			if (!isEnabled)
			{
				// if this module isn't enabled, before returning, if there's a SFX to be played it must be done now
				playAudio = solver.SolveDynamicBool(bp.playAudioAtSpawn, 29232405, ParameterOwner.Bullet);
				if (!playAudio) return;
				audioClip = solver.SolveDynamicObjectReference(bp.audioClip, 12659374, ParameterOwner.Bullet) as AudioClip;
				if (audioClip) audioManager.PlayLocal(audioClip);
				return;
			}

			timeBeforeSpawn = solver.SolveDynamicFloat(bp.timeBeforeSpawn, 30534841, ParameterOwner.Bullet);

			playAudio = solver.SolveDynamicBool(bp.playAudioAtSpawn, 30166684, ParameterOwner.Bullet);
			audioClip = solver.SolveDynamicObjectReference(bp.audioClip, 1168027, ParameterOwner.Bullet) as AudioClip;
		}

		// Called at Bullet.Die()
		public void Die()
		{
			isEnabled = false;
			playAudio = false;
			audioClip = null;
			rotateFromSpawn = 0;
		}

		// Called by patternModule if both homing and delayed
		public void MemorizeSpawnRotation(float rotation)
		{
			rotateFromSpawn = rotation;
		}
	}
}