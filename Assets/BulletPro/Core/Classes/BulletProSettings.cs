using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// There's one instance of this class in Resources, it fills the same role as Unity's TagManager.asset, but for BulletPro.
	[System.Serializable]
	public class BulletProSettings : ScriptableObject
	{
		public string this[int idx]
		{
			get	{ return collisionTags[idx]; }
			set { collisionTags[idx] = value; }
		}
		public CollisionTagLabels collisionTags;

		[Tooltip("If specified, all the Emitter Profiles you create will look like this one.")]
		public EmitterProfile defaultEmitterProfile;

		[Tooltip("A reference to the Compute Shader in charge of collisions.")]
		public ComputeShader collisionHandler;
		public ComputeShadersEnabling computeShaders;
		[Tooltip("How many collisions can occur at once in the same single frame?")]
		public uint maxAmountOfCollisionsPerFrame = 32;
		[Tooltip("How many bullets can be processed at once in the same single frame?")]
		public int maxAmountOfBullets = 2000;
		[Tooltip("How many receivers can be processed at once in the same single frame?")]
		public int maxAmountOfReceivers = 200;

		public static int buildNumber = 10;
	}

	[System.Serializable]
	public enum ComputeShadersEnabling { EnabledWhenPossible, AlwaysOn, AlwaysOff }
}