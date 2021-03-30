using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// Struct that contains similar data to PatternInstruction, but every dynamic parameter has been solved.
	public struct SolvedInstruction
	{
		// non-dynamic anyway
		public bool enabled;
		public PatternInstructionType instructionType;
		public InstructionTiming instructionTiming;
		public bool endless;
		public PatternCurveType curveAffected;
		public CollisionTagAction collisionTagAction;
		public PatternControlTarget patternControlTarget;
		public CurvePeriodType newPeriodType;
		public VFXPlayType vfxPlayType;

		// solved dynamic
		public float waitTime;
		public ShotParams shot;
		public int iterations;
		public Vector2 globalMovement, localMovement;
		public float rotation;
		public AudioClip audioClip;
		public float speedValue;
		public float scaleValue;
		public float factor;
		public ParticleSystem vfxToPlay;
		public PreferredTarget preferredTarget;
		public float turnIntensity;
		public string collisionTag;
		public string patternTag;

		public AnimationCurve newCurveValue;
		public float newPeriodValue;
		public WrapMode newWrapMode;
		public float curveRawTime;
		public float curveTime;

		public Color color;
		public float alpha;
		public Gradient gradient;

		// general, for MicroActions
		public float instructionDuration;
		public AnimationCurve operationCurve;

		// properties stating if the dynamic params need to be rerolled - these are arrays because there can be multiple rerollable params per instruction
		public RerollFrequency[] rerollFrequency;
		public int[] rerollLoopDepth;
		public bool[] useComplexRerollSequence;
		public int[] checkEveryNLoops;
		public int[] loopSequence;
	}

	// Struct that contains similar data to PatternInstructionList, but every dynamic parameter has been solved.
	public struct SolvedInstructionList
	{
		public SolvedInstruction this[int i] { get { return instructions[i]; } }
		public SolvedInstruction[] instructions;
	}

	// Struct that contains similar data to PatternParams, but every dynamic parameter has been solved.
	public struct SolvedPatternParams
	{
		public string[] patternTags;
		public bool playAtStart;
		public bool containsNullPattern;

		// This is still an array, so the code is not incompatible with having multiple parallel stacks,
		// but it's unused as of now, and could be a single SolvedInstructionList. (2020-09)
		public SolvedInstructionList[] instructionLists;		 
	}

	// Struct that contains pattern ingame logic, only used at runtime, as we won't edit scriptable objects. Unserialized.
	public struct PatternRuntimeInfo
	{
		// Basic info, read by bullet emitter in case we need to kill the Bullet afterwards
		public bool isPlaying;
		public bool isDone { get; private set; }

		// Always keep track of these (lifetime counters)
		public float timeSinceLive { get; private set; }
		public float shotsShotSinceLive { get; private set; }
		
		// instructions
		public InstructionListRuntimeInfo[] instructionLists { get; private set; }

		#region data passed at initialization
		
		public PatternParams patternParams;
		public SolvedPatternParams solvedPatternParams;
		public Bullet bullet { get; private set; }
		public int patternIndex { get; private set; }
		public bool playAtStart { get { return solvedPatternParams.playAtStart; } }

		#endregion

		#if UNITY_EDITOR
		// if the two ints get out of sync, it's time to reboot the pattern
		int safetyForPlaymode;
		#endif

		// Starts this by loading PatternParams into it.
		public void Init(PatternParams pp, SolvedPatternParams spp, Bullet currentOwner, int indexOfPattern)
		{
			isDone = false;

			if (pp == null) isDone = true;
			else if (pp.instructionLists == null) isDone = true;
			else if (pp.instructionLists.Length == 0) isDone = true;

			if (isDone) return;

			isDone = false;

			// set references
			patternParams = pp;
			solvedPatternParams = spp;
			bullet = currentOwner;
			patternIndex = indexOfPattern;
			instructionLists = new InstructionListRuntimeInfo[pp.instructionLists.Length];
			#if UNITY_EDITOR
			safetyForPlaymode = pp.safetyForPlaymode;
			#endif

			// init time
			timeSinceLive = 0;
			shotsShotSinceLive = 0;

			// instructions vars
			for (int i = 0; i < instructionLists.Length; i++)
				instructionLists[i].Init(spp.instructionLists[i].instructions, pp.instructionLists[i], bullet, patternIndex, i);
		}

		// Called when the bullet which emitted this pattern dies.
		public void OnEmitterDeath()
		{
			bullet = null;
		}

		// Called every frame by a bullet's pattern module. In return, it can tell the module to shoot, move, and play audio.
		public void Update()
		{
			// allows editing in play mode
			#if UNITY_EDITOR
			if (patternParams)
			{
				if (patternParams.safetyForPlaymode != safetyForPlaymode)
				{
					if (bullet != null)
					{
						solvedPatternParams = bullet.dynamicSolver.SolvePatternParams(patternParams);
						bullet.modulePatterns.solvedPatternInfo[patternIndex] = solvedPatternParams;
						Init(patternParams, solvedPatternParams, bullet, patternIndex);
					}
				}
				// TODO : delete this
				/* *
				else for (int i = 0; i < instructionLists.Length; i++)
					instructionLists[i].instructions = solvedPatternParams.instructionLists[i].instructions;
				/* */
			}
			#endif

			if (!isPlaying) return;
			if (isDone) return;

			timeSinceLive += Time.deltaTime;

			// Update all instruction, and see if they're all finished
			bool allDone = true;
			for (int i = 0; i < instructionLists.Length; i++)
			{
				instructionLists[i].Update();
				if (!instructionLists[i].isDone) allDone = false;
			}
			if (allDone)
			{
				isDone = true;
				isPlaying = false;
			}
		}

		// Returns whether this pattern has the required tag.
		public bool HasTag(string patternTag)
		{
			if (solvedPatternParams.patternTags == null) return false;
			if (solvedPatternParams.patternTags.Length == 0) return false;
			for (int i = 0; i < solvedPatternParams.patternTags.Length; i++)
				if (solvedPatternParams.patternTags[i] == patternTag)
					return true;
			return false;
		}

		// Shoot
		public void Shoot(ShotParams shot)
		{
			if (shot == null) return;

			if (bullet.additionalBehaviourScripts.Count > 0)
				for (int i = 0; i < bullet.additionalBehaviourScripts.Count; i++)
					bullet.additionalBehaviourScripts[i].OnBulletShotAnotherBullet(patternIndex);
				
			TempEmissionData ted = new TempEmissionData();
			ted.patternID = patternParams.uniqueIndex;
			ted.patternID = shot.uniqueIndex;
			ted.patternIndexInEmitter = patternIndex;
			ted.shotTimeInPattern = timeSinceLive;
			ted.shotIndexInPattern = shotsShotSinceLive;

			bullet.modulePatterns.ShootBullets(shot, ted);
			shotsShotSinceLive++;
		}
	}
}