using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// This manager stores any BulletReceiver (object that can get hit)
	[AddComponentMenu("BulletPro/Managers/Bullet Collision Manager")]
	public class BulletCollisionManager : MonoBehaviour
	{
		public static BulletCollisionManager instance;
		// *Only contains enabled BulletReceivers.
		[System.NonSerialized]
		public List<BulletReceiver> allBulletReceivers;
		// *Only contains colliders with their moduleCollision enabled.
		private List<Bullet> allBullets;

		#region copied from BulletProSettings
		[System.NonSerialized]
		public bool disableComputeShaders = false;
		[System.NonSerialized]
		public uint maximumAmountOfCollisionsPerFrame = 64;

		[System.NonSerialized]
		public string[] collisionTags;
		[System.NonSerialized]
		public int maxAmountOfBullets = 2000;
		[System.NonSerialized]
		public int maxAmountOfReceivers = 200;
		#endregion

		#region compute shader stuff
		private ComputeShader computeShader;
		private int csKernelIndex;
		// Four buffers :
		private ComputeBuffer bufferEmission, bufferReception, bufferCollisions, bufferCollisionAmount;
		// Four lists/arrays to feed them :
		private ColliderInfo[] bulletData, receiverData; // list, read-only for the compute, variable size
		private CollisionData[] collisionData; // array, RW for the compute, fixed size
		private int[] curCollisions; // single int passed as array so it can be seen by compute across all threads
		private CollisionData defaultCollisionData;
		// avoids bugs with lists when killing bullets and receivers
		private List<BulletReceiverPair> curCollActors;

		#endregion

		#region MonoBehaviour

		void Awake()
		{
			// Setting up singleton instance
			if (!instance) instance = this;
			else Debug.LogWarning("BulletPro Warning: there is more than one instance of BulletCollisionManager in the scene.");
			allBulletReceivers = new List<BulletReceiver>();
			allBullets = new List<Bullet>();

			// Checking for compute shader compatibility
			bool supports = SystemInfo.supportsComputeShaders;

			// Getting collision tags in case they're needed at runtime
			BulletProSettings bcs = Resources.Load("BulletProSettings") as BulletProSettings;
			if (bcs != null)
			{
				if (bcs.computeShaders == ComputeShadersEnabling.EnabledWhenPossible)
					disableComputeShaders = !supports;
				else disableComputeShaders = bcs.computeShaders == ComputeShadersEnabling.AlwaysOff;
				maximumAmountOfCollisionsPerFrame = bcs.maxAmountOfCollisionsPerFrame;
				collisionTags = bcs.collisionTags.tags;
			}
			else // defaults
			{
				disableComputeShaders = true;
				collisionTags = new string[32];
				collisionTags[0] = "Player";
				collisionTags[1] = "Enemy";
				for (int i=2; i<32; i++) collisionTags[i] = "Tag "+i.ToString();
				Debug.LogError("BulletPro Error: Your BulletProSettings.asset file has not been found in Resources.");
				Debug.LogError("To solve this problem, click \"Manage BulletPro Tags\" from the GameObject that sent this error.");
			}

			// Compute shader warnings
			if (supports && disableComputeShaders)
				Debug.Log("BulletPro: Target platform can run Compute Shaders. You can enable them from BulletProSettings. (Edit -> Project Settings -> BulletPro)");
			if (!supports && !disableComputeShaders)
				Debug.LogError("BulletPro Error: Target platform cannot run Compute Shaders. You can disable them from BulletProSettings. (Edit -> Project Settings -> BulletPro)");

			if (!disableComputeShaders) InitializeComputeShader(bcs);
		}

		void LateUpdate()
		{
			if (disableComputeShaders) return;
			RunComputeShader();
		}

		#endregion

		#region ComputeShader Toolbox

		// At Start if Compute Shaders are enabled
		void InitializeComputeShader(BulletProSettings bcs)
		{
			maxAmountOfBullets = bcs.maxAmountOfBullets;
			maxAmountOfReceivers = bcs.maxAmountOfReceivers;

			computeShader = bcs.collisionHandler;
			if (computeShader == null) computeShader = Resources.Load("BulletProCollisionHandler") as ComputeShader;
			if (computeShader == null) Debug.LogError("BulletPro Error: file BulletProCollisionHandler.compute has not been found. Try reimporting the package.");

			csKernelIndex = computeShader.FindKernel("ProcessBulletCollisions");

			// Initialize arrays
			bulletData = new ColliderInfo[maxAmountOfBullets];
			receiverData = new ColliderInfo[maxAmountOfReceivers];
			collisionData = new CollisionData[maximumAmountOfCollisionsPerFrame];
			curCollisions = new int[1];
			curCollActors = new List<BulletReceiverPair>();
			
			// Default values
			ColliderInfo defaultColliderData = new ColliderInfo();
			defaultCollisionData = new CollisionData(-1, -1, Vector3.zero);
			for (int i=0; i<maxAmountOfBullets; i++)
				bulletData[i] = defaultColliderData;
			for (int i=0; i<maxAmountOfReceivers; i++)
				receiverData[i] = defaultColliderData;
			for (int i=0; i<maximumAmountOfCollisionsPerFrame; i++)
				collisionData[i] = defaultCollisionData;
		}

		// At LateUpdate if Compute Shader are enabled
		void RunComputeShader()
		{
			// Find out necessary array size and number of thread groups for this frame :
			int curBulletAmount = allBullets.Count;
			int curReceiverAmount = allBulletReceivers.Count;

			if (curBulletAmount == 0) return;
			if (curReceiverAmount == 0) return;

			// Track total amount of bullet colliders because there might be more than just bullets
			int totalAmountOfColliders = 0;

			// Renew buffers
			bufferEmission = new ComputeBuffer(maxAmountOfBullets, 64);
			bufferReception = new ComputeBuffer(maxAmountOfReceivers, 64);
			bufferCollisions = new ComputeBuffer((int)maximumAmountOfCollisionsPerFrame, 20);
			bufferCollisionAmount = new ComputeBuffer(1, 4);

			// Fill arrays :
			// 1 - Bullets
			for (int i=0; i<curBulletAmount; i++)
			{
				Bullet curBullet = allBullets[i];
				BulletCollider[] colliders = curBullet.moduleCollision.GetColliders();
				for (int j=0; j<colliders.Length; j++)
				{
					if (totalAmountOfColliders == maxAmountOfBullets)
					{
						Debug.LogError("BulletPro Error: The max amount of bullets specified in BulletProSettings has been reached. Consider raising this maximum.");
						totalAmountOfColliders = maxAmountOfBullets;
						break;
					}
					BulletCollider cur = colliders[j];
					if (cur.colliderType == BulletColliderType.Circle)
					{
						bulletData[totalAmountOfColliders].index = i + 1;
						bulletData[totalAmountOfColliders].offset = cur.offset;
						bulletData[totalAmountOfColliders].size = cur.size;
						// offset.z is free. For lines, it allows using "offset.z" and "size" as "lineEnd.x" and "lineEnd.y".
					}
					else // if line
					{
						bulletData[totalAmountOfColliders].index = (i + 1) * -1;
						bulletData[totalAmountOfColliders].offset.x = cur.lineStart.x;
						bulletData[totalAmountOfColliders].offset.y = cur.lineStart.y;
						// store line end on free slots
						bulletData[totalAmountOfColliders].offset.z = cur.lineEnd.x;
						bulletData[totalAmountOfColliders].size = cur.lineEnd.y;
					}
					bulletData[totalAmountOfColliders].position = curBullet.self.position;
					bulletData[totalAmountOfColliders].orientation = curBullet.self.up;
					bulletData[totalAmountOfColliders].extraVector = curBullet.self.right;
					bulletData[totalAmountOfColliders].scale = curBullet.moduleCollision.scale;
					bulletData[totalAmountOfColliders].tags = curBullet.moduleCollision.collisionTags.tagList;
					
					totalAmountOfColliders++;
				}
			}

			// 1b - Fill the end of the array
			ColliderInfo defaultColliderData = new ColliderInfo();
			for (int i=totalAmountOfColliders; i<bulletData.Length; i++)
				bulletData[i] = defaultColliderData;

			// 2 - Receivers
			for (int i=0; i<curReceiverAmount; i++)
			{
				if (i == maxAmountOfReceivers)
				{
					Debug.Log("BulletPro Error: The max amount of receivers specified in BulletProSettings has been reached. Consider raising this maximum.");
					curReceiverAmount = maxAmountOfReceivers;
					break;
				}
				BulletReceiver cur = allBulletReceivers[i];
				receiverData[i].index = (i + 1) * (cur.colliderType == BulletReceiverType.Line ? -1 : 1);
				receiverData[i].position = cur.self.position;
				receiverData[i].offset = cur.hitboxOffset;
				receiverData[i].offset.z = cur.self.lossyScale.x; // (float assignation is somewhat twisted, to fit every float needed)
				receiverData[i].orientation = cur.self.up;
				receiverData[i].extraVector = cur.self.right;
				receiverData[i].scale = cur.self.lossyScale.y;
				receiverData[i].size = cur.hitboxSize;
				receiverData[i].tags = cur.collisionTags.tagList;
			}

			// 2b - Fill the end of the array
			for (int i=curReceiverAmount; i<receiverData.Length; i++)
				receiverData[i] = defaultColliderData;

			// 3 - Collisions
			for (int i=0; i<maximumAmountOfCollisionsPerFrame; i++)
			{
				collisionData[i].bulletIndex = -10;
				collisionData[i].receiverIndex = -10;
			}
			// 4 - Current collisions in frame
			curCollisions[0] = 0;

			// Initialize misc compute shader values
			computeShader.SetInt("numberOfBullets", totalAmountOfColliders);
			computeShader.SetInt("numberOfReceivers", curReceiverAmount);
			computeShader.SetInt("maxCollisions", (int)maximumAmountOfCollisionsPerFrame);

			// Set data in buffers
			bufferEmission.SetData(bulletData);
			bufferReception.SetData(receiverData);
			bufferCollisions.SetData(collisionData);
			bufferCollisionAmount.SetData(curCollisions);

			// Feed buffers to the compute shader
			computeShader.SetBuffer(csKernelIndex, "bullets", bufferEmission);
			computeShader.SetBuffer(csKernelIndex, "receivers", bufferReception);
			computeShader.SetBuffer(csKernelIndex, "collisions", bufferCollisions);
			computeShader.SetBuffer(csKernelIndex, "curNumberOfCollisions", bufferCollisionAmount);

			// Which is the lowest multiple of 768 we can do ? That will be our number of total threads.
			int xy = totalAmountOfColliders * curReceiverAmount;
			int mult = xy + 768 - (xy%768);
			// How many thread groups does it make ?
			int threadGroups = mult / 768;

			// Actually run shader and get collisions
			computeShader.Dispatch(csKernelIndex, threadGroups, 1, 1);
			bufferCollisions.GetData(collisionData);

			// Release buffers
			bufferEmission.Dispose();
			bufferReception.Dispose();
			bufferEmission.Release();
			bufferReception.Release();
			bufferCollisions.Dispose();
			bufferCollisions.Release();
			bufferCollisionAmount.Dispose();
			bufferCollisionAmount.Release();

			// Send collisions messages
			for (int i=0; i<collisionData.Length; i++)
			{
				CollisionData coll = collisionData[i];

				if (coll.bulletIndex < 0) break;
				if (coll.receiverIndex < 0) break;

				curCollActors.Add(new BulletReceiverPair(allBullets[coll.bulletIndex], allBulletReceivers[coll.receiverIndex], coll.position));
			}
			if (curCollActors.Count > 0)
				for (int i=0; i<curCollActors.Count; i++)
					curCollActors[i].bullet.moduleCollision.CollideWith(curCollActors[i].receiver, curCollActors[i].position);

			curCollActors.Clear();
		}

		#endregion

		#region actual functions called by instance of manager

		// Register a new freshly spawned BulletReceiver in the dictionary
		public void AddReceiverLocal(BulletReceiver br)
		{
			if (br == null) return;
			allBulletReceivers.Add(br);
		}

		// Remove an old BulletReceiver from the dictionary
		public void RemoveReceiverLocal(BulletReceiver br)
		{
			if (br == null) return;
			allBulletReceivers.Remove(br);
		}

		// Register a new freshly spawned BulletReceiver in the dictionary
		public void AddBulletLocal(Bullet b)
		{
			if (b == null) return;
			allBullets.Add(b);
		}

		// Remove an old BulletReceiver from the dictionary
		public void RemoveBulletLocal(Bullet b)
		{
			if (b == null) return;
			allBullets.Remove(b);
		}

		// Called at bullet spawn : for a given set of tags, browse the dictionary and find out matching targets
		public List<BulletReceiver> GetTargetListLocal(CollisionTags tags)
		{
			List<BulletReceiver> result = new List<BulletReceiver>();

			if (allBulletReceivers.Count > 0)
				for (int i = 0; i < allBulletReceivers.Count; i++)
					if (CheckCollisionCompatibility(tags, allBulletReceivers[i].collisionTags))
						result.Add(allBulletReceivers[i]);

			return result;
		}

		#endregion

		#region static shortcuts
		
		public static void AddReceiver(BulletReceiver br)
		{
			if (instance == null) { Debug.LogWarning("No CollisionManager found in scene!"); return; }
			instance.AddReceiverLocal(br);
		}

		public static void RemoveReceiver(BulletReceiver br)
		{
			if (instance == null) return;
			instance.RemoveReceiverLocal(br);
		}

		public static void AddBullet(Bullet b)
		{
			if (instance == null) { Debug.LogWarning("No CollisionManager found in scene!"); return; }
			instance.AddBulletLocal(b);
		}

		public static void RemoveBullet(Bullet b)
		{
			if (instance == null) return;
			instance.RemoveBulletLocal(b);
		}

		public static List<BulletReceiver> GetTargetList(CollisionTags tags)
		{
			if (instance == null) { Debug.LogWarning("No CollisionManager found in scene!"); return null; }
			return instance.GetTargetListLocal(tags);
		}

		#endregion

		#region bitwise collision tags handling

		// Strings perform poorly, so this isn't ever called, but well, it has to exist
		public int GetCollisionTagIndex(string _tag)
		{
			for(int i=0; i<32; i++)
				if (collisionTags[i] == _tag) return i;

			Debug.LogError("BulletPro : Requested string is not a collision tag.");
			return -1;
		}

		// Are two sets of collision tags compatible ? If a bullet has t1 and a receiver has t2 (or vice versa), they can collide.
		public bool CheckCollisionCompatibility(CollisionTags t1, CollisionTags t2)
		{
			return (t1.tagList & t2.tagList) != 0; 
		}

		#endregion
	}

	#region structs for CollisionTags

	// We store the strings used for collision tags here. Having a struct allows a better custom drawer in inspector.
	[System.Serializable]
	public struct CollisionTagLabels
	{
		public string this[int i]
		{
			get
			{
				if (tags == null) tags = new string[32];
				if (tags.Length == 0) tags = new string[32];
				return tags[i];
			}
			set	{ tags[i] = value; }
		}
		public string[] tags;
	}

	// Actual collisions "tags" are just 32 bits to flip when needed
	[System.Serializable]
	public struct CollisionTags
	{
		public uint tagList;

		public bool this[int idx]
		{
			get
			{
				if (idx < 0 || idx > 31)
				{
					//Debug.LogError("BulletPro Error: trying to get invalid index of CollisionTags.");
					return false;
				}
				return (tagList & (1 << idx)) != 0;
			}
			set
			{
				if (idx > -1 && idx < 32)
				{
					if (value) tagList |= (uint)(1 << idx);
					else tagList &= ~(uint)(1 << idx);
				}
				//else Debug.LogError("BulletPro Error: trying to set invalid index of CollisionTags.");
			}
		}

		public bool this[string str]
		{
			get
			{
				BulletCollisionManager bcm = BulletCollisionManager.instance;
				if (bcm == null)
				{
					Debug.LogError("BulletPro Error: BulletCollisionManager not found.");
					return false;
				}
				return this[bcm.GetCollisionTagIndex(str)];
			}
			set
			{
				BulletCollisionManager bcm = BulletCollisionManager.instance;
				if (bcm != null)
				{
					int idx = bcm.GetCollisionTagIndex(str);
					if (idx >= 0) this[idx] = value;
				}
				else Debug.LogError("BulletPro Error: BulletCollisionManager not found.");
			}
		}
	}

	#endregion

	#region structs for ComputeBuffers

	public struct ColliderInfo
	{
		public int index; // 4 bytes, sign stores shape (positive = circle, negative = line)
		public Vector3 position; // 12 bytes
		public Vector3 offset; // 12 bytes
		public Vector3 orientation; // 12 bytes
		public Vector3 extraVector; // 12 bytes
		public float scale; // 4 bytes
		public float size; // 4 bytes
		public uint tags; // 4 bytes

		public ColliderInfo(int idx=-1, Vector3 pos=default(Vector3), Vector3 _off=default(Vector3), Vector3 rot=default(Vector3), Vector3 extV=default(Vector3), float _scale=0, float _size=0, uint collTags=0)
		{
			index = idx;
			position = pos;
			offset = _off;
			orientation = rot;
			extraVector = extV;
			scale = _scale;
			size = _size;
			tags = collTags;
		}
	}

	public struct CollisionData
	{
		public int bulletIndex; // 4 bytes, index of bullet
		public int receiverIndex; // 4 bytes, index of receiver
		public Vector3 position; // 12 bytes, location of collision
		
		public CollisionData(int bulIndex, int recIndex, Vector3 pos)
		{
			bulletIndex = bulIndex;
			receiverIndex = recIndex;
			position = pos;
		}
	}

	// A temp struct listing actors of collisions in a same frame, to avoid screwing up lists when killing bullets and receivers
	public struct BulletReceiverPair
	{
		public Bullet bullet;
		public BulletReceiver receiver;
		public Vector3 position;
		public BulletReceiverPair(Bullet b, BulletReceiver r, Vector3 pos)
		{
			bullet = b;
			receiver = r;
			position = pos;
		}
	}


	#endregion
}