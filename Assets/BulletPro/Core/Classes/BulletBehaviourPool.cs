using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	[System.Serializable]
	public struct BulletBehaviourPool
	{
		public BaseBulletBehaviour[] pool;
		public GameObject prefab;

		// for inspector
		public bool foldout;
		public Transform poolRoot;
	}
}