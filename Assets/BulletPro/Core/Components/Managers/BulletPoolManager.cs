using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	[AddComponentMenu("BulletPro/Managers/Bullet Pool Manager")]
	public class BulletPoolManager : MonoBehaviour
	{
		public static BulletPoolManager instance;
		public static Transform managerTransform { get { if (!instance) return null; return instance.mainTransform; } }
		public static Transform regularPoolContainer { get { if (!instance) return null; return instance.regularPoolRoot; } }
		public static Transform meshPoolContainer { get { if (!instance) return null; return instance.meshPoolRoot; } }
		[HideInInspector]
		public Bullet[] pool, meshPool;
		[HideInInspector]
		public Transform mainTransform, regularPoolRoot, meshPoolRoot;
		[HideInInspector]
		public bool foldout;
		[System.NonSerialized]
		public int bulletsSpawnedSinceStartup, currentAmountOfBullets;

		void Awake()
		{
			if (instance == null) instance = this;
			else Debug.LogWarning("Beware : there is more than one instance of BulletPoolManager in the scene!");
			if (mainTransform == null) mainTransform = transform;
			bulletsSpawnedSinceStartup = 0;

			currentAmountOfBullets = 0;
            // this will be nullified and brought down to 0 at Start, as all bullets are initialized by dying :
			if (pool != null) currentAmountOfBullets += pool.Length;
			if (meshPool != null) currentAmountOfBullets += meshPool.Length;
		}

		public static Bullet GetFreeBullet()
		{
			if (instance == null)
			{
				Debug.LogWarning("No bullet pool found in scene !");
				return null;
			}

			return instance.GetFreeBulletLocal();
		}

		public static Bullet GetFree3DBullet()
		{
			if (instance == null)
			{
				Debug.LogWarning("No mesh-based bullet pool found in scene !");
				return null;
			}

			return instance.GetFree3DBulletLocal();
		}

		public Bullet GetFreeBulletLocal()
		{
			if (pool == null)
			{
				Debug.LogWarning(name + " has no pool!");
				return null;
			}
			if (pool.Length == 0)
			{
				Debug.LogWarning(name + ": pool is empty!");
				return null;
			}

			for (int i = 0; i < pool.Length; i++)
			{
				if (pool[i].isAvailableInPool)
				{
					pool[i].isAvailableInPool = false;
					return pool[i];
				}
			}

			Debug.LogWarning(name + " has not enough bullets in pool!");
			return null;
		}

		public Bullet GetFree3DBulletLocal()
		{
			if (meshPool == null)
			{
				Debug.LogWarning(name + " has no pool for mesh-based bullets!");
				return null;
			}
			if (meshPool.Length == 0)
			{
				Debug.LogWarning(name + ": mesh pool is empty!");
				return null;
			}

			for (int i = 0; i < meshPool.Length; i++)
			{
				if (meshPool[i].isAvailableInPool)
				{
					meshPool[i].isAvailableInPool = false;
					return meshPool[i];
				}
			}

			Debug.LogWarning(name + " has not enough mesh-based bullets in pool!");
			return null;
		}
	}
}
