using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	[AddComponentMenu("BulletPro/Managers/Bullet VFX Manager")]
	public class BulletVFXManager : MonoBehaviour
	{
		public static BulletVFXManager instance;
		[HideInInspector]
		public BulletVFX[] effectPool;
		public ParticleSystem defaultParticles;
		public ParticleSystemRenderer defaultParticleRenderer;
		public GameObject vfxPrefab; // prefab to be instantiated in Editor mode, will then get pooled

		void Awake()
		{
			if (instance == null) instance = this;
			else Debug.LogWarning("Beware : there is more than one instance of BulletVFXManager in the scene!");
		}

		public static BulletVFX GetFreeBullet()
		{
			if (instance == null)
			{
				Debug.LogWarning("No bullet pool found in scene !");
				return null;
			}

			return instance.GetFreeBulletLocal();
		}

		public BulletVFX GetFreeBulletLocal()
		{
			if (effectPool == null)
			{
				Debug.LogWarning(name + " has no pool!");
				return null;
			}
			if (effectPool.Length == 0)
			{
				Debug.LogWarning(name + " : pool is empty!");
				return null;
			}

			for (int i = 0; i < effectPool.Length; i++)
				if (!effectPool[i].thisParticleSystem.isPlaying)
					return effectPool[i];

			Debug.LogWarning(name + " has not enough bullets in pool!");
			return null;
		}

		// Overload 1 : play the default VFX with wanted orientation, color and size
		public void PlayVFXAt(Vector3 position, float rotation, Color color, float size)
		{
			BulletVFX bvfx = GetFreeBulletLocal();
			if (!bvfx) return;
			bvfx.Play(position, rotation, color, size);
		}

		// Overload 2 : set VFX to wanted ParticleSystem settings and then play it
		public void PlayVFXAt(Vector3 position, float rotation, ParticleSystem psSettings, float size)
		{
			BulletVFX bvfx = GetFreeBulletLocal();
			if (!bvfx) return;
			bvfx.Play(position, rotation, psSettings, size);
		}

		// Overload 3 : similar to overload 1 but with full rotation transmitted via (global) eulerAngles
		public void PlayVFXAt(Vector3 position, Vector3 eulerAngles, Color color, float size)
		{
			BulletVFX bvfx = GetFreeBulletLocal();
			if (!bvfx) return;
			bvfx.Play(position, eulerAngles, color, size);
		}

		// Overload 4 : similar to overload 2 but with full rotation transmitted via (global) eulerAngles		
		public void PlayVFXAt(Vector3 position, Vector3 eulerAngles, ParticleSystem psSettings, float size)
		{
			BulletVFX bvfx = GetFreeBulletLocal();
			if (!bvfx) return;
			bvfx.Play(position, eulerAngles, psSettings, size);
		}

	}
}
