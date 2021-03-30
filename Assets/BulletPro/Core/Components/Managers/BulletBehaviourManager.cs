using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	[System.Serializable]
	[AddComponentMenu("BulletPro/Managers/Bullet Behaviour Manager")]
	public class BulletBehaviourManager : MonoBehaviour
	{
		public static BulletBehaviourManager instance;
		public List<BulletBehaviourPool> pools;

		void Awake()
		{
			if (instance == null) instance = this;
			else Debug.LogWarning("Beware : there is more than one instance of BulletBehaviourManager in the scene!");
		}

		public static BaseBulletBehaviour GetFreeBehaviour(GameObject behaviourPrefab)
		{
			if (instance == null)
			{
				Debug.LogWarning("No BulletBehaviourManager found in scene !");
				return null;
			}

			return instance.GetFreeBehaviourLocal(behaviourPrefab);
		}

		public BaseBulletBehaviour GetFreeBehaviourLocal(GameObject behaviourPrefab)
		{
			BaseBulletBehaviour[] pool = null;
			if (pools == null)
			{
				Debug.LogWarning(name + " : no behaviour pool found!");
				return null;
			}
			if (pools.Count == 0)
			{
				Debug.LogWarning(name + " : no behaviour pool found!");
				return null;
			}

			bool succeeded = false;

			for (int i = 0; i < pools.Count; i++)
			{
				if (pools[i].prefab == behaviourPrefab)
				{
					succeeded = true;
					pool = pools[i].pool;
					break;
				}
			}

			if (!succeeded)
			{
				Debug.LogWarning(behaviourPrefab.name + " : no behaviour pool found!");
				return null;
			}

			if (pool == null)
			{
				Debug.LogWarning(behaviourPrefab.name + " has no behaviour pool!");
				return null;
			}
			if (pool.Length == 0)
			{
				Debug.LogWarning(behaviourPrefab.name + " : behaviour pool is empty!");
				return null;
			}

			for (int i = 0; i < pool.Length; i++)
				if (pool[i].isAvailableInPool)
				{
					pool[i].isAvailableInPool = false;
					return pool[i];
				}

			Debug.LogWarning(name + " has not enough objects in pool!");
			return null;
		}
	}
}