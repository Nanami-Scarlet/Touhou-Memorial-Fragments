using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	public struct LoopInfo
	{
		public int indexOfBeginInstruction;
		public int iterationsRemaining;
		public int iterationsFinished;
		public bool endless;
	}

	public enum InstructionResult { None, InterruptedItself, RebootedItself }

	// Struct held by PatternRuntimeInfo, handles logic and loops
	public struct InstructionListRuntimeInfo
	{
		// instruction-related vars
		public int indexOfNextInstruction;
		public bool isDone;
		float waitTimeLeft;
		int endLoopTagsWanted; // helps skip everything until we stumble across the end of current loop. Skipping means avoiding a loop that user wants played "0 times".
		public Stack<LoopInfo> loops, tempStack;
		bool shouldBreakLoopNow; // avoids infinite endless loops without a Wait instruction in it
		bool hadWaitBlockDuringLoop; // checks whether we saw a Wait block during an endless loop
		public SolvedInstruction[] instructions;
		public PatternInstructionList dynamicInstructions; // used as fallback if a parameter has to be rerolled at every instruction

		// allow data transmission through structs
		public Bullet bullet;
		public int patternIndex;
		public int indexOfThisList;

		int numberOfRerolls;

		public void Init(SolvedInstruction[] newInstructions, PatternInstructionList dynamicInstList, Bullet emitter, int indexOfPattern, int indexOfList)
		{
			isDone = false;
			indexOfNextInstruction = 0;
			loops = new Stack<LoopInfo>();
			tempStack = new Stack<LoopInfo>();
			loops.Clear();
			tempStack.Clear();
			endLoopTagsWanted = 0;
			waitTimeLeft = 0;
			shouldBreakLoopNow = false;
			hadWaitBlockDuringLoop = false;

			dynamicInstructions = dynamicInstList;
			numberOfRerolls = 1;

			bullet = emitter;
			patternIndex = indexOfPattern;
			indexOfThisList = indexOfList;
			instructions = newInstructions;

			bool empty = false;
			if (instructions == null) empty = true;
			else if (instructions.Length == 0) empty = true;
			if (empty) isDone = true;	

			if (bullet == null)	isDone = true;
		}

		public void Update()
		{
			if (isDone) return;

			waitTimeLeft -= Time.deltaTime;

			shouldBreakLoopNow = false;

			// keep executing instructions one by one in the same frame until one of them is a Wait()
			while (waitTimeLeft <= 0)
			{
				// end of behaviour : auto-repair missing EndLoop tags at the end of the instruction list
				if (indexOfNextInstruction == instructions.Length)
				{
					while ((indexOfNextInstruction == instructions.Length) && (loops.Count > 0))
					{
						if (EndLoop())
							indexOfNextInstruction++;
					}

					if (indexOfNextInstruction == instructions.Length)
					{
						isDone = true;
						break;
					}
				}

				// If we just exited an endless loop with no Wait block, this bool is true and we break the loop, which means "wait for next frame".
				if (shouldBreakLoopNow) break;

				// actual instruction call
				InstructionResult res = DoInstruction(indexOfNextInstruction);

				// Hard-break the loop if the pattern rebooted itself, without even incrementing the instruction index.
				if (res == InstructionResult.RebootedItself)
					break;

				indexOfNextInstruction++;

				// Still hard-break the loop if the pattern paused itself without rebooting, but this time we incremented the index.
				if (res == InstructionResult.InterruptedItself) break;

				if (shouldBreakLoopNow) break;
			}
		}

		// A giant conditional structure that executes the instruction among the 60+ possible types.
		// Returns whether the pattern interrupted/rebooted itself due to a flow function.
		InstructionResult DoInstruction(int index)
		{
			SolvedInstruction ip = instructions[index];
			PatternInstruction rawInst = dynamicInstructions[index];
			if (!ip.enabled) return InstructionResult.None;

			// if we're currently skipping a loop, that means we only want to process BeginLoop and EndLoop blocks.
			if (endLoopTagsWanted > 0)
			{
				if (ip.instructionType == PatternInstructionType.BeginLoop) endLoopTagsWanted++; // handle nesting
				if (ip.instructionType == PatternInstructionType.EndLoop) EndLoop();

				return InstructionResult.None;
			}

			#region main
			
			// Wait
			if (ip.instructionType == PatternInstructionType.Wait)
			{
				if (IsRerollNecessary(ip, 0))
					instructions[index].waitTime = bullet.dynamicSolver.SolveDynamicFloat(rawInst.waitTime, 1258491 * (numberOfRerolls++), ParameterOwner.Pattern);
				
				waitTimeLeft = instructions[index].waitTime;
				if (waitTimeLeft > 0) hadWaitBlockDuringLoop = true;
			}

			// Shoot
			else if (ip.instructionType == PatternInstructionType.Shoot)
			{
				if (IsRerollNecessary(ip, 0))				
					ip.shot = bullet.dynamicSolver.SolveDynamicShot(rawInst.shot, 8427153 * (numberOfRerolls++), ParameterOwner.Pattern);

				bullet.modulePatterns.patternRuntimeInfo[patternIndex].Shoot(ip.shot);
			}

			// Begin Loop
			else if (ip.instructionType == PatternInstructionType.BeginLoop)
			{
				if (index == instructions.Length - 1) return InstructionResult.None;

				if (IsRerollNecessary(ip, 0))
					ip.iterations = bullet.dynamicSolver.SolveDynamicInt(rawInst.iterations, 5581211 * (numberOfRerolls++), ParameterOwner.Pattern);
				
				LoopInfo li = new LoopInfo();
				li.indexOfBeginInstruction = index;
				li.endless = ip.endless;
				li.iterationsRemaining = ip.iterations;
				li.iterationsFinished = 0;
				loops.Push(li);

				// if it's a non-endless loop that must be done zero times, it means we're skipping it.
				if (li.iterationsRemaining < 1 && !li.endless)
					endLoopTagsWanted = 1;

				// if it's an endless loop, we need to start checking for Wait blocks
				if (li.endless)
					hadWaitBlockDuringLoop = false;
			}

			// End Loop
			else if (ip.instructionType == PatternInstructionType.EndLoop)
			{
				EndLoop();
			}

			// Play Audio
			else if (ip.instructionType == PatternInstructionType.PlayAudio)
			{
				if (IsRerollNecessary(ip, 0))
					ip.audioClip = bullet.dynamicSolver.SolveDynamicObjectReference(rawInst.audioClip, 5815777 * (numberOfRerolls++), ParameterOwner.Pattern) as AudioClip;

				if (ip.audioClip != null)
					bullet.audioManager.PlayLocal(ip.audioClip);
			}

			// Play VFX
			else if (ip.instructionType == PatternInstructionType.PlayVFX)
			{
				if (ip.vfxPlayType == VFXPlayType.Default)
				{
					bullet.moduleRenderer.SpawnDefaultVFX();
				}
				else
				{
					if (IsRerollNecessary(ip, 0))
						ip.vfxToPlay = bullet.dynamicSolver.SolveDynamicObjectReference(rawInst.vfxToPlay, 5815777 * (numberOfRerolls++), ParameterOwner.Pattern) as ParticleSystem;

					bullet.moduleRenderer.SpawnFX(ip.vfxToPlay);
				}
			}

			// Die
			else if (ip.instructionType == PatternInstructionType.Die)
			{
				bullet.Die(true);
			}

			#endregion

			#region transform

			#region transform / misc
			
			// Enable Movement
			else if (ip.instructionType == PatternInstructionType.EnableMovement)
			{
				bullet.moduleMovement.Enable();
			}

			// Disable Movement
			else if (ip.instructionType == PatternInstructionType.DisableMovement)
			{
				bullet.moduleMovement.Disable();
			}

			// Attach To Emitter
			else if (ip.instructionType == PatternInstructionType.AttachToEmitter)
			{
				if (bullet.subEmitter) bullet.self.SetParent(bullet.subEmitter.self, true);
				else if (bullet.emitter)
					if (bullet.emitter.patternOrigin)
						bullet.self.SetParent(bullet.emitter.patternOrigin, true);
			}

			// Detach From Emitter
			else if (ip.instructionType == PatternInstructionType.DetachFromEmitter)
			{
				bullet.self.SetParent(null);
			}

			#endregion

			#region transform / position

			// Move (world)
			else if (ip.instructionType == PatternInstructionType.TranslateGlobal)
			{
				if (IsRerollNecessary(ip, 0))
					ip.globalMovement = bullet.dynamicSolver.SolveDynamicVector2(rawInst.globalMovement, 6417182 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.Translate(ip.globalMovement.x, ip.globalMovement.y, 0, Space.World);
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 4598782 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 1185973 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionTranslateGlobal(
						bullet, ip.instructionDuration, ip.operationCurve, ip.globalMovement));
				}
			}

			// Move (self)
			else if (ip.instructionType == PatternInstructionType.TranslateLocal)
			{
				if (IsRerollNecessary(ip, 0))
					ip.localMovement = bullet.dynamicSolver.SolveDynamicVector2(rawInst.localMovement, 3598499 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.Translate(ip.localMovement.x, ip.localMovement.y, 0, Space.Self);
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 6488153 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 4574129 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionTranslateLocal(
						bullet, ip.instructionDuration, ip.operationCurve, ip.localMovement));
				}		
			}

			// Set Position (world)
			else if (ip.instructionType == PatternInstructionType.SetWorldPosition)
			{
				if (IsRerollNecessary(ip, 0))
					ip.globalMovement = bullet.dynamicSolver.SolveDynamicVector2(rawInst.globalMovement, 4857248 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.SetGlobalPosition(ip.globalMovement);
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 1801063 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 7879069 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionPositionSetGlobal(
						bullet, ip.instructionDuration, ip.operationCurve, ip.globalMovement));
				}
			}

			// Set Position (local)
			else if (ip.instructionType == PatternInstructionType.SetLocalPosition)
			{
				if (IsRerollNecessary(ip, 0))
					ip.localMovement = bullet.dynamicSolver.SolveDynamicVector2(rawInst.localMovement, 7170115 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.SetLocalPosition(ip.localMovement);
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 7805621 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 3335729 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionTranslateLocal(
						bullet, ip.instructionDuration, ip.operationCurve, ip.localMovement));
				}		
			}

			// Set Speed
			else if (ip.instructionType == PatternInstructionType.SetSpeed)
			{
				if (IsRerollNecessary(ip, 0))
					ip.speedValue = bullet.dynamicSolver.SolveDynamicFloat(rawInst.speedValue, 8484235 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.baseSpeed = ip.speedValue;
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 4942481 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 3280941 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionSpeedSet(
						bullet, ip.instructionDuration, ip.operationCurve, ip.speedValue));
				}		
			}

			// Multiply Speed
			else if (ip.instructionType == PatternInstructionType.MultiplySpeed)
			{
				if (IsRerollNecessary(ip, 0))
					ip.factor = bullet.dynamicSolver.SolveDynamicFloat(rawInst.factor, 4840981 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.baseSpeed *= ip.factor;
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 1803682 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 8930561 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionSpeedMultiply(
						bullet, ip.instructionDuration, ip.operationCurve, ip.factor));
				}		
			}

			#endregion

			#region transform / rotation

			// Rotate
			else if (ip.instructionType == PatternInstructionType.Rotate)
			{
				if (IsRerollNecessary(ip, 0))
					ip.rotation = bullet.dynamicSolver.SolveDynamicFloat(rawInst.rotation, 4811093 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.Rotate(ip.rotation);
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 4870930 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 1518401 * (numberOfRerolls++), ParameterOwner.Pattern);

					bullet.microActions.Add(new MicroActionRotate(
						bullet, ip.instructionDuration, ip.operationCurve, ip.rotation));
				}
			}

			// Set Rotation (world)
			else if (ip.instructionType == PatternInstructionType.SetWorldRotation)
			{
				if (IsRerollNecessary(ip, 0))
					ip.rotation = bullet.dynamicSolver.SolveDynamicFloat(rawInst.rotation, 5568921 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.SetGlobalRotation(ip.rotation);
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 2874109 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 2935842 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionRotationSetGlobal(
						bullet, ip.instructionDuration, ip.operationCurve, ip.rotation));
				}
			}

			// Set Rotation (local)
			else if (ip.instructionType == PatternInstructionType.SetLocalRotation)
			{
				if (IsRerollNecessary(ip, 0))
					ip.rotation = bullet.dynamicSolver.SolveDynamicFloat(rawInst.rotation, 7210485 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.SetLocalRotation(ip.rotation);
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 3384766 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 2547811 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionRotationSetLocal(
						bullet, ip.instructionDuration, ip.operationCurve, ip.rotation));
				}
			}

			// Set Angular Speed
			else if (ip.instructionType == PatternInstructionType.SetAngularSpeed)
			{
				if (IsRerollNecessary(ip, 0))
					ip.speedValue = bullet.dynamicSolver.SolveDynamicFloat(rawInst.speedValue, 6652142 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.baseAngularSpeed = ip.speedValue;
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 5693541 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 5035712 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionAngularSpeedSet(
						bullet, ip.instructionDuration, ip.operationCurve, ip.speedValue));
				}
			}

			// Multiply Angular Speed
			else if (ip.instructionType == PatternInstructionType.MultiplyAngularSpeed)
			{
				if (IsRerollNecessary(ip, 0))
					ip.factor = bullet.dynamicSolver.SolveDynamicFloat(rawInst.factor, 1084126 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.baseAngularSpeed *= ip.factor;
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 4823511 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 7842106 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionAngularSpeedMultiply(
						bullet, ip.instructionDuration, ip.operationCurve, ip.factor));
				}
			}

			#endregion

			#region transform / scale

			// Set Scale
			else if (ip.instructionType == PatternInstructionType.SetScale)
			{
				if (IsRerollNecessary(ip, 0))
					ip.scaleValue = bullet.dynamicSolver.SolveDynamicFloat(rawInst.scaleValue, 9897452 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.baseScale = ip.scaleValue;
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 4847213 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 1069507 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionScaleSet(
						bullet, ip.instructionDuration, ip.operationCurve, ip.scaleValue));
				}
			}

			// Multiply Scale
			else if (ip.instructionType == PatternInstructionType.MultiplyScale)
			{
				if (IsRerollNecessary(ip, 0))
					ip.factor = bullet.dynamicSolver.SolveDynamicFloat(rawInst.factor, 5478120 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleMovement.baseScale *= ip.factor;
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 4890236 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 8415231 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionScaleMultiply(
						bullet, ip.instructionDuration, ip.operationCurve, ip.factor));
				}
			}

			#endregion

			#endregion

			#region homing

			// Enable Homing
			else if (ip.instructionType == PatternInstructionType.EnableHoming)
			{
				bullet.moduleHoming.Enable();
			}

			// Disable Homing
			else if (ip.instructionType == PatternInstructionType.DisableHoming)
			{
				bullet.moduleHoming.Disable();
			}

			// Turn to Target
			else if (ip.instructionType == PatternInstructionType.TurnToTarget)
			{
				if (IsRerollNecessary(ip, 0))
					ip.turnIntensity = bullet.dynamicSolver.SolveDynamicFloat(rawInst.turnIntensity, 5601481 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleHoming.LookAtTarget(ip.turnIntensity);
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 5948307 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 7060582 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionTurnToTarget(
						bullet, ip.instructionDuration, ip.operationCurve, ip.turnIntensity));
				}		
			}

			// Change Target
			else if (ip.instructionType == PatternInstructionType.ChangeTarget)
			{
				if (IsRerollNecessary(ip, 0))
					ip.preferredTarget = (PreferredTarget)bullet.dynamicSolver.SolveDynamicEnum(rawInst.preferredTarget, 1748721 * (numberOfRerolls++), ParameterOwner.Pattern);

				bullet.moduleHoming.RefreshTarget(ip.preferredTarget);
			}

			// Set Homing Speed
			else if (ip.instructionType == PatternInstructionType.SetHomingSpeed)
			{
				if (IsRerollNecessary(ip, 0))
					ip.speedValue = bullet.dynamicSolver.SolveDynamicFloat(rawInst.speedValue, 8715244 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleHoming.homingAngularSpeed = ip.speedValue;
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 2368501 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 5849872 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionHomingSpeedSet(
						bullet, ip.instructionDuration, ip.operationCurve, ip.speedValue));
				}
			}

			// Multiply Homing Speed
			else if (ip.instructionType == PatternInstructionType.MultiplyHomingSpeed)
			{
				if (IsRerollNecessary(ip, 0))
					ip.factor = bullet.dynamicSolver.SolveDynamicFloat(rawInst.factor, 4578021 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleHoming.homingAngularSpeed *= ip.factor;
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 6598121 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 3050681 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionHomingSpeedMultiply(
						bullet, ip.instructionDuration, ip.operationCurve, ip.factor));
				}
			}

			// Change Homing Tag
			else if (ip.instructionType == PatternInstructionType.ChangeHomingTag)
			{
				if (IsRerollNecessary(ip, 0))
					ip.collisionTag = bullet.dynamicSolver.SolveDynamicString(rawInst.collisionTag, 4369122 * (numberOfRerolls++), ParameterOwner.Pattern);

				bullet.moduleHoming.homingTags[ip.collisionTag] = (ip.collisionTagAction == CollisionTagAction.Add);
			}

			#endregion

			#region curves - controls

			// Enable Curve
			else if (ip.instructionType == PatternInstructionType.EnableCurve)
			{
				if (ip.curveAffected == PatternCurveType.Alpha)
					bullet.moduleRenderer.alphaOverLifetime.enabled = true;
				else if (ip.curveAffected == PatternCurveType.Color)
					bullet.moduleRenderer.colorOverLifetime.enabled = true;
				else if (ip.curveAffected == PatternCurveType.Homing)
					bullet.moduleHoming.homingOverLifetime.enabled = true;
				else if (ip.curveAffected == PatternCurveType.Speed)
					bullet.moduleMovement.speedOverLifetime.enabled = true;
				else if (ip.curveAffected == PatternCurveType.AngularSpeed)
					bullet.moduleMovement.angularSpeedOverLifetime.enabled = true;
				else if (ip.curveAffected == PatternCurveType.Scale)
					bullet.moduleMovement.scaleOverLifetime.enabled = true;
				else if (ip.curveAffected == PatternCurveType.AnimX)
					bullet.moduleMovement.moveXFromAnim.enabled = true;
				else if (ip.curveAffected == PatternCurveType.AnimY)
					bullet.moduleMovement.moveYFromAnim.enabled = true;
				else if (ip.curveAffected == PatternCurveType.AnimAngle)
					bullet.moduleMovement.rotateFromAnim.enabled = true;
				else if (ip.curveAffected == PatternCurveType.AnimScale)
					bullet.moduleMovement.scaleFromAnim.enabled = true;
			}

			// Disable Curve
			else if (ip.instructionType == PatternInstructionType.EnableCurve)
			{
				if (ip.curveAffected == PatternCurveType.Alpha)
					bullet.moduleRenderer.alphaOverLifetime.enabled = false;
				else if (ip.curveAffected == PatternCurveType.Color)
					bullet.moduleRenderer.colorOverLifetime.enabled = false;
				else if (ip.curveAffected == PatternCurveType.Homing)
					bullet.moduleHoming.homingOverLifetime.enabled = false;
				else if (ip.curveAffected == PatternCurveType.Speed)
					bullet.moduleMovement.speedOverLifetime.enabled = false;
				else if (ip.curveAffected == PatternCurveType.AngularSpeed)
					bullet.moduleMovement.angularSpeedOverLifetime.enabled = false;
				else if (ip.curveAffected == PatternCurveType.Scale)
					bullet.moduleMovement.scaleOverLifetime.enabled = false;
				else if (ip.curveAffected == PatternCurveType.AnimX)
					bullet.moduleMovement.moveXFromAnim.enabled = false;
				else if (ip.curveAffected == PatternCurveType.AnimY)
					bullet.moduleMovement.moveYFromAnim.enabled = false;
				else if (ip.curveAffected == PatternCurveType.AnimAngle)
					bullet.moduleMovement.rotateFromAnim.enabled = false;
				else if (ip.curveAffected == PatternCurveType.AnimScale)
					bullet.moduleMovement.scaleFromAnim.enabled = false;
			}

			// Play Curve
			else if (ip.instructionType == PatternInstructionType.PlayCurve)
			{
				if (ip.curveAffected == PatternCurveType.Alpha)
					bullet.moduleRenderer.alphaOverLifetime.Play();
				else if (ip.curveAffected == PatternCurveType.Color)
					bullet.moduleRenderer.colorOverLifetime.Play();
				else if (ip.curveAffected == PatternCurveType.Homing)
					bullet.moduleHoming.homingOverLifetime.Play();
				else if (ip.curveAffected == PatternCurveType.Speed)
					bullet.moduleMovement.speedOverLifetime.Play();
				else if (ip.curveAffected == PatternCurveType.AngularSpeed)
					bullet.moduleMovement.angularSpeedOverLifetime.Play();
				else if (ip.curveAffected == PatternCurveType.Scale)
					bullet.moduleMovement.scaleOverLifetime.Play();
				else if (ip.curveAffected == PatternCurveType.AnimX)
					bullet.moduleMovement.moveXFromAnim.Play();
				else if (ip.curveAffected == PatternCurveType.AnimY)
					bullet.moduleMovement.moveYFromAnim.Play();
				else if (ip.curveAffected == PatternCurveType.AnimAngle)
					bullet.moduleMovement.rotateFromAnim.Play();
				else if (ip.curveAffected == PatternCurveType.AnimScale)
					bullet.moduleMovement.scaleFromAnim.Play();
			}

			// Pause Curve
			else if (ip.instructionType == PatternInstructionType.PauseCurve)
			{
				if (ip.curveAffected == PatternCurveType.Alpha)
					bullet.moduleRenderer.alphaOverLifetime.Pause();
				else if (ip.curveAffected == PatternCurveType.Color)
					bullet.moduleRenderer.colorOverLifetime.Pause();
				else if (ip.curveAffected == PatternCurveType.Homing)
					bullet.moduleHoming.homingOverLifetime.Pause();
				else if (ip.curveAffected == PatternCurveType.Speed)
					bullet.moduleMovement.speedOverLifetime.Pause();
				else if (ip.curveAffected == PatternCurveType.AngularSpeed)
					bullet.moduleMovement.angularSpeedOverLifetime.Pause();
				else if (ip.curveAffected == PatternCurveType.Scale)
					bullet.moduleMovement.scaleOverLifetime.Pause();
				else if (ip.curveAffected == PatternCurveType.AnimX)
					bullet.moduleMovement.moveXFromAnim.Pause();
				else if (ip.curveAffected == PatternCurveType.AnimY)
					bullet.moduleMovement.moveYFromAnim.Pause();
				else if (ip.curveAffected == PatternCurveType.AnimAngle)
					bullet.moduleMovement.rotateFromAnim.Pause();
				else if (ip.curveAffected == PatternCurveType.AnimScale)
					bullet.moduleMovement.scaleFromAnim.Pause();
			}

			// Rewind Curve
			else if (ip.instructionType == PatternInstructionType.RewindCurve)
			{
				if (ip.curveAffected == PatternCurveType.Alpha)
					bullet.moduleRenderer.alphaOverLifetime.Rewind();
				else if (ip.curveAffected == PatternCurveType.Color)
					bullet.moduleRenderer.colorOverLifetime.Rewind();
				else if (ip.curveAffected == PatternCurveType.Homing)
					bullet.moduleHoming.homingOverLifetime.Rewind();
				else if (ip.curveAffected == PatternCurveType.Speed)
					bullet.moduleMovement.speedOverLifetime.Rewind();
				else if (ip.curveAffected == PatternCurveType.AngularSpeed)
					bullet.moduleMovement.angularSpeedOverLifetime.Rewind();
				else if (ip.curveAffected == PatternCurveType.Scale)
					bullet.moduleMovement.scaleOverLifetime.Rewind();
				else if (ip.curveAffected == PatternCurveType.AnimX)
					bullet.moduleMovement.moveXFromAnim.Rewind();
				else if (ip.curveAffected == PatternCurveType.AnimY)
					bullet.moduleMovement.moveYFromAnim.Rewind();
				else if (ip.curveAffected == PatternCurveType.AnimAngle)
					bullet.moduleMovement.rotateFromAnim.Rewind();
				else if (ip.curveAffected == PatternCurveType.AnimScale)
					bullet.moduleMovement.scaleFromAnim.Rewind();
			}

			// Reset Curve
			else if (ip.instructionType == PatternInstructionType.ResetCurve)
			{
				if (ip.curveAffected == PatternCurveType.Alpha)
					bullet.moduleRenderer.alphaOverLifetime.Reset();
				else if (ip.curveAffected == PatternCurveType.Color)
					bullet.moduleRenderer.colorOverLifetime.Reset();
				else if (ip.curveAffected == PatternCurveType.Homing)
					bullet.moduleHoming.homingOverLifetime.Reset();
				else if (ip.curveAffected == PatternCurveType.Speed)
					bullet.moduleMovement.speedOverLifetime.Reset();
				else if (ip.curveAffected == PatternCurveType.AngularSpeed)
					bullet.moduleMovement.angularSpeedOverLifetime.Reset();
				else if (ip.curveAffected == PatternCurveType.Scale)
					bullet.moduleMovement.scaleOverLifetime.Reset();
				else if (ip.curveAffected == PatternCurveType.AnimX)
					bullet.moduleMovement.moveXFromAnim.Reset();
				else if (ip.curveAffected == PatternCurveType.AnimY)
					bullet.moduleMovement.moveYFromAnim.Reset();
				else if (ip.curveAffected == PatternCurveType.AnimAngle)
					bullet.moduleMovement.rotateFromAnim.Reset();
				else if (ip.curveAffected == PatternCurveType.AnimScale)
					bullet.moduleMovement.scaleFromAnim.Reset();
			}

			// Stop Curve
			else if (ip.instructionType == PatternInstructionType.StopCurve)
			{
				if (ip.curveAffected == PatternCurveType.Alpha)
					bullet.moduleRenderer.alphaOverLifetime.Stop();
				else if (ip.curveAffected == PatternCurveType.Color)
					bullet.moduleRenderer.colorOverLifetime.Stop();
				else if (ip.curveAffected == PatternCurveType.Homing)
					bullet.moduleHoming.homingOverLifetime.Stop();
				else if (ip.curveAffected == PatternCurveType.Speed)
					bullet.moduleMovement.speedOverLifetime.Stop();
				else if (ip.curveAffected == PatternCurveType.AngularSpeed)
					bullet.moduleMovement.angularSpeedOverLifetime.Stop();
				else if (ip.curveAffected == PatternCurveType.Scale)
					bullet.moduleMovement.scaleOverLifetime.Stop();
				else if (ip.curveAffected == PatternCurveType.AnimX)
					bullet.moduleMovement.moveXFromAnim.Stop();
				else if (ip.curveAffected == PatternCurveType.AnimY)
					bullet.moduleMovement.moveYFromAnim.Stop();
				else if (ip.curveAffected == PatternCurveType.AnimAngle)
					bullet.moduleMovement.rotateFromAnim.Stop();
				else if (ip.curveAffected == PatternCurveType.AnimScale)
					bullet.moduleMovement.scaleFromAnim.Stop();
			}

			#endregion

			#region curves - values

			// Set Curve
			else if (ip.instructionType == PatternInstructionType.SetCurveValue)
			{
				if (IsRerollNecessary(ip, 0))
					ip.newCurveValue = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.newCurveValue, 1593461 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.curveAffected == PatternCurveType.Alpha)
					bullet.moduleRenderer.alphaOverLifetime.curve = ip.newCurveValue;
				else if (ip.curveAffected == PatternCurveType.Color)
					bullet.moduleRenderer.colorOverLifetime.curve = ip.newCurveValue;
				else if (ip.curveAffected == PatternCurveType.Homing)
					bullet.moduleHoming.homingOverLifetime.curve = ip.newCurveValue;
				else if (ip.curveAffected == PatternCurveType.Speed)
					bullet.moduleMovement.speedOverLifetime.curve = ip.newCurveValue;
				else if (ip.curveAffected == PatternCurveType.AngularSpeed)
					bullet.moduleMovement.angularSpeedOverLifetime.curve = ip.newCurveValue;
				else if (ip.curveAffected == PatternCurveType.Scale)
					bullet.moduleMovement.scaleOverLifetime.curve = ip.newCurveValue;
				else if (ip.curveAffected == PatternCurveType.AnimX)
					bullet.moduleMovement.moveXFromAnim.curve = ip.newCurveValue;
				else if (ip.curveAffected == PatternCurveType.AnimY)
					bullet.moduleMovement.moveYFromAnim.curve = ip.newCurveValue;
				else if (ip.curveAffected == PatternCurveType.AnimAngle)
					bullet.moduleMovement.rotateFromAnim.curve = ip.newCurveValue;
				else if (ip.curveAffected == PatternCurveType.AnimScale)
					bullet.moduleMovement.scaleFromAnim.curve = ip.newCurveValue;
			}

			// Set Wrap Mode
			else if (ip.instructionType == PatternInstructionType.SetWrapMode)
			{
				if (IsRerollNecessary(ip, 0))
					ip.newWrapMode = (WrapMode)bullet.dynamicSolver.SolveDynamicEnum(rawInst.newWrapMode, 6603417 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.curveAffected == PatternCurveType.Alpha)
					bullet.moduleRenderer.alphaOverLifetime.wrapMode = ip.newWrapMode;
				else if (ip.curveAffected == PatternCurveType.Color)
					bullet.moduleRenderer.colorOverLifetime.wrapMode = ip.newWrapMode;
				else if (ip.curveAffected == PatternCurveType.Homing)
					bullet.moduleHoming.homingOverLifetime.wrapMode = ip.newWrapMode;
				else if (ip.curveAffected == PatternCurveType.Speed)
					bullet.moduleMovement.speedOverLifetime.wrapMode = ip.newWrapMode;
				else if (ip.curveAffected == PatternCurveType.AngularSpeed)
					bullet.moduleMovement.angularSpeedOverLifetime.wrapMode = ip.newWrapMode;
				else if (ip.curveAffected == PatternCurveType.Scale)
					bullet.moduleMovement.scaleOverLifetime.wrapMode = ip.newWrapMode;
				else if (ip.curveAffected == PatternCurveType.AnimX)
					bullet.moduleMovement.moveXFromAnim.wrapMode = ip.newWrapMode;
				else if (ip.curveAffected == PatternCurveType.AnimY)
					bullet.moduleMovement.moveYFromAnim.wrapMode = ip.newWrapMode;
				else if (ip.curveAffected == PatternCurveType.AnimAngle)
					bullet.moduleMovement.rotateFromAnim.wrapMode = ip.newWrapMode;
				else if (ip.curveAffected == PatternCurveType.AnimScale)
					bullet.moduleMovement.scaleFromAnim.wrapMode = ip.newWrapMode;
			}

			// Set Period
			else if (ip.instructionType == PatternInstructionType.SetPeriod)
			{
				float newValue = 0;
				if (ip.newPeriodType == CurvePeriodType.BulletTotalLifetime)
					newValue = bullet.moduleLifespan.lifespan;
				else if (ip.newPeriodType == CurvePeriodType.RemainingLifetime)
					newValue = bullet.moduleLifespan.lifespan - bullet.timeSinceAlive;
				else if (ip.newPeriodType == CurvePeriodType.FixedValue)
				{
					if (IsRerollNecessary(ip, 0))
						ip.newPeriodValue = bullet.dynamicSolver.SolveDynamicFloat(rawInst.newPeriodValue, 5541789 * (numberOfRerolls++), ParameterOwner.Pattern);
					newValue = ip.newPeriodValue;
				}

				if (ip.instructionTiming == InstructionTiming.Progressively)
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 8839571 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 8974123 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionCurvePeriodSet(
						bullet, ip.instructionDuration, ip.operationCurve, newValue, ip.curveAffected));
				}
				else
				{
					if (ip.curveAffected == PatternCurveType.Alpha)
					{
						bullet.moduleRenderer.alphaOverLifetime.period = newValue;
						bullet.moduleRenderer.alphaOverLifetime.periodIsLifespan = false;
					}
					else if (ip.curveAffected == PatternCurveType.Color)
					{
						bullet.moduleRenderer.colorOverLifetime.period = newValue;
						bullet.moduleRenderer.colorOverLifetime.periodIsLifespan = false;
					}
					else if (ip.curveAffected == PatternCurveType.Homing)
					{
						bullet.moduleHoming.homingOverLifetime.period = newValue;
						bullet.moduleHoming.homingOverLifetime.periodIsLifespan = false;
					}
					else if (ip.curveAffected == PatternCurveType.Speed)
					{
						bullet.moduleMovement.speedOverLifetime.period = newValue;
						bullet.moduleMovement.speedOverLifetime.periodIsLifespan = false;
					}
					else if (ip.curveAffected == PatternCurveType.AngularSpeed)
					{
						bullet.moduleMovement.angularSpeedOverLifetime.period = newValue;
						bullet.moduleMovement.angularSpeedOverLifetime.periodIsLifespan = false;
					}
					else if (ip.curveAffected == PatternCurveType.Scale)
					{
						bullet.moduleMovement.scaleOverLifetime.period = newValue;
						bullet.moduleMovement.scaleOverLifetime.periodIsLifespan = false;
					}
					else if (ip.curveAffected == PatternCurveType.AnimX)
					{
						bullet.moduleMovement.moveXFromAnim.period = newValue;
						bullet.moduleMovement.moveXFromAnim.periodIsLifespan = false;
					}
					else if (ip.curveAffected == PatternCurveType.AnimY)
					{
						bullet.moduleMovement.moveYFromAnim.period = newValue;
						bullet.moduleMovement.moveYFromAnim.periodIsLifespan = false;
					}
					else if (ip.curveAffected == PatternCurveType.AnimAngle)
					{	
						bullet.moduleMovement.rotateFromAnim.period = newValue;
						bullet.moduleMovement.rotateFromAnim.periodIsLifespan = false;
					}
					else if (ip.curveAffected == PatternCurveType.AnimScale)
					{
						bullet.moduleMovement.scaleFromAnim.period = newValue;
						bullet.moduleMovement.scaleFromAnim.periodIsLifespan = false;
					}
				}
			}

			// Multiply Period
			else if (ip.instructionType == PatternInstructionType.MultiplyPeriod)
			{
				if (IsRerollNecessary(ip, 0))
					ip.factor = bullet.dynamicSolver.SolveDynamicFloat(rawInst.factor, 6142305 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Progressively)
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 1811293 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 1457841 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionCurvePeriodMultiply(
						bullet, ip.instructionDuration, ip.operationCurve, ip.factor, ip.curveAffected));
				}
				else
				{
					if (ip.curveAffected == PatternCurveType.Alpha)
					{
						if (bullet.moduleRenderer.alphaOverLifetime.periodIsLifespan)
						{
							bullet.moduleRenderer.alphaOverLifetime.period = bullet.moduleLifespan.lifespan * ip.factor;
							bullet.moduleRenderer.alphaOverLifetime.periodIsLifespan = false;
						}
						else bullet.moduleRenderer.alphaOverLifetime.period *= ip.factor;
					}
					else if (ip.curveAffected == PatternCurveType.Color)
					{
						if (bullet.moduleRenderer.colorOverLifetime.periodIsLifespan)
						{
							bullet.moduleRenderer.colorOverLifetime.period = bullet.moduleLifespan.lifespan * ip.factor;
							bullet.moduleRenderer.colorOverLifetime.periodIsLifespan = false;
						}
						else bullet.moduleRenderer.colorOverLifetime.period *= ip.factor;
					}
					else if (ip.curveAffected == PatternCurveType.Homing)
					{
						if (bullet.moduleHoming.homingOverLifetime.periodIsLifespan)
						{
							bullet.moduleHoming.homingOverLifetime.period = bullet.moduleLifespan.lifespan * ip.factor;
							bullet.moduleHoming.homingOverLifetime.periodIsLifespan = false;
						}
						else bullet.moduleHoming.homingOverLifetime.period *= ip.factor;
					}
					else if (ip.curveAffected == PatternCurveType.Speed)
					{
						if (bullet.moduleMovement.speedOverLifetime.periodIsLifespan)
						{
							bullet.moduleMovement.speedOverLifetime.period = bullet.moduleLifespan.lifespan * ip.factor;
							bullet.moduleMovement.speedOverLifetime.periodIsLifespan = false;
						}
						else bullet.moduleMovement.speedOverLifetime.period *= ip.factor;
					}
					else if (ip.curveAffected == PatternCurveType.AngularSpeed)
					{
						if (bullet.moduleMovement.angularSpeedOverLifetime.periodIsLifespan)
						{
							bullet.moduleMovement.angularSpeedOverLifetime.period = bullet.moduleLifespan.lifespan * ip.factor;
							bullet.moduleMovement.angularSpeedOverLifetime.periodIsLifespan = false;
						}
						else bullet.moduleMovement.angularSpeedOverLifetime.period *= ip.factor;
					}
					else if (ip.curveAffected == PatternCurveType.Scale)
					{
						if (bullet.moduleMovement.scaleOverLifetime.periodIsLifespan)
						{
							bullet.moduleMovement.scaleOverLifetime.period = bullet.moduleLifespan.lifespan * ip.factor;
							bullet.moduleMovement.scaleOverLifetime.periodIsLifespan = false;
						}
						else bullet.moduleMovement.scaleOverLifetime.period *= ip.factor;
					}
					else if (ip.curveAffected == PatternCurveType.AnimX)
					{
						if (bullet.moduleMovement.moveXFromAnim.periodIsLifespan)
						{
							bullet.moduleMovement.moveXFromAnim.period = bullet.moduleLifespan.lifespan * ip.factor;
							bullet.moduleMovement.moveXFromAnim.periodIsLifespan = false;
						}
						else bullet.moduleMovement.moveXFromAnim.period *= ip.factor;
					}
					else if (ip.curveAffected == PatternCurveType.AnimY)
					{
						if (bullet.moduleMovement.moveYFromAnim.periodIsLifespan)
						{
							bullet.moduleMovement.moveYFromAnim.period = bullet.moduleLifespan.lifespan * ip.factor;
							bullet.moduleMovement.moveYFromAnim.periodIsLifespan = false;
						}
						else bullet.moduleMovement.moveYFromAnim.period *= ip.factor;
					}
					else if (ip.curveAffected == PatternCurveType.AnimAngle)
					{	
						if (bullet.moduleMovement.rotateFromAnim.periodIsLifespan)
						{
							bullet.moduleMovement.rotateFromAnim.period = bullet.moduleLifespan.lifespan * ip.factor;
							bullet.moduleMovement.rotateFromAnim.periodIsLifespan = false;
						}
						else bullet.moduleMovement.rotateFromAnim.period *= ip.factor;
					}
					else if (ip.curveAffected == PatternCurveType.AnimScale)
					{
						if (bullet.moduleMovement.scaleFromAnim.periodIsLifespan)
						{
							bullet.moduleMovement.scaleFromAnim.period = bullet.moduleLifespan.lifespan * ip.factor;
							bullet.moduleMovement.scaleFromAnim.periodIsLifespan = false;
						}
						else bullet.moduleMovement.scaleFromAnim.period *= ip.factor;
					}
				}
			}

			// Set Curve Raw Time
			else if (ip.instructionType == PatternInstructionType.SetCurveRawTime)
			{
				if (IsRerollNecessary(ip, 0))
					ip.curveRawTime = bullet.dynamicSolver.SolveDynamicFloat(rawInst.curveRawTime, 6132878 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Progressively)
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 4848513 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 8965411 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionCurveRawTimeSet(
						bullet, ip.instructionDuration, ip.operationCurve, ip.curveRawTime, ip.curveAffected));
				}
				else
				{
					if (ip.curveAffected == PatternCurveType.Alpha)
						bullet.moduleRenderer.alphaOverLifetime.SetRawTime(ip.curveRawTime);
					else if (ip.curveAffected == PatternCurveType.Color)
						bullet.moduleRenderer.colorOverLifetime.SetRawTime(ip.curveRawTime);
					else if (ip.curveAffected == PatternCurveType.Homing)
						bullet.moduleHoming.homingOverLifetime.SetRawTime(ip.curveRawTime);
					else if (ip.curveAffected == PatternCurveType.Speed)
						bullet.moduleMovement.speedOverLifetime.SetRawTime(ip.curveRawTime);
					else if (ip.curveAffected == PatternCurveType.AngularSpeed)
						bullet.moduleMovement.angularSpeedOverLifetime.SetRawTime(ip.curveRawTime);
					else if (ip.curveAffected == PatternCurveType.Scale)
						bullet.moduleMovement.scaleOverLifetime.SetRawTime(ip.curveRawTime);
					else if (ip.curveAffected == PatternCurveType.AnimX)
						bullet.moduleMovement.moveXFromAnim.SetRawTime(ip.curveRawTime);
					else if (ip.curveAffected == PatternCurveType.AnimY)
						bullet.moduleMovement.moveYFromAnim.SetRawTime(ip.curveRawTime);
					else if (ip.curveAffected == PatternCurveType.AnimAngle)
						bullet.moduleMovement.rotateFromAnim.SetRawTime(ip.curveRawTime);
					else if (ip.curveAffected == PatternCurveType.AnimScale)
						bullet.moduleMovement.scaleFromAnim.SetRawTime(ip.curveRawTime);
				}
			}

			// Set Curve Ratio
			else if (ip.instructionType == PatternInstructionType.SetCurveRatio)
			{
				if (IsRerollNecessary(ip, 0))
					ip.curveTime = bullet.dynamicSolver.SolveDynamicSlider01(rawInst.curveTime, 5532618 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Progressively)
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 1013477 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 9895221 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionCurveRatioSet(
						bullet, ip.instructionDuration, ip.operationCurve, ip.curveTime, ip.curveAffected));
				}
				else
				{
					if (ip.curveAffected == PatternCurveType.Alpha)
						bullet.moduleRenderer.alphaOverLifetime.SetRatio(ip.curveTime);
					else if (ip.curveAffected == PatternCurveType.Color)
						bullet.moduleRenderer.colorOverLifetime.SetRatio(ip.curveTime);
					else if (ip.curveAffected == PatternCurveType.Homing)
						bullet.moduleHoming.homingOverLifetime.SetRatio(ip.curveTime);
					else if (ip.curveAffected == PatternCurveType.Speed)
						bullet.moduleMovement.speedOverLifetime.SetRatio(ip.curveTime);
					else if (ip.curveAffected == PatternCurveType.AngularSpeed)
						bullet.moduleMovement.angularSpeedOverLifetime.SetRatio(ip.curveTime);
					else if (ip.curveAffected == PatternCurveType.Scale)
						bullet.moduleMovement.scaleOverLifetime.SetRatio(ip.curveTime);
					else if (ip.curveAffected == PatternCurveType.AnimX)
						bullet.moduleMovement.moveXFromAnim.SetRatio(ip.curveTime);
					else if (ip.curveAffected == PatternCurveType.AnimY)
						bullet.moduleMovement.moveYFromAnim.SetRatio(ip.curveTime);
					else if (ip.curveAffected == PatternCurveType.AnimAngle)
						bullet.moduleMovement.rotateFromAnim.SetRatio(ip.curveTime);
					else if (ip.curveAffected == PatternCurveType.AnimScale)
						bullet.moduleMovement.scaleFromAnim.SetRatio(ip.curveTime);
				}
			}

			#endregion

			#region graphics

			// Turn Visible
			else if (ip.instructionType == PatternInstructionType.TurnVisible)
			{
				bullet.moduleRenderer.Enable();
			}

			// Turn Invisible
			else if (ip.instructionType == PatternInstructionType.TurnInvisible)
			{
				bullet.moduleRenderer.Disable();
			}

			// Play Animation
			else if (ip.instructionType == PatternInstructionType.PlayAnimation)
			{
				bullet.moduleRenderer.animated = true;
			}

			// Pause Animation
			else if (ip.instructionType == PatternInstructionType.PauseAnimation)
			{
				bullet.moduleRenderer.animated = false;
			}

			// Reboot Animation
			else if (ip.instructionType == PatternInstructionType.RebootAnimation)
			{
				bullet.moduleRenderer.SetFrame(0);
				bullet.moduleRenderer.animated = true;
			}

			#endregion

			#region color

			// Set Color
			else if (ip.instructionType == PatternInstructionType.SetColor)
			{
				if (IsRerollNecessary(ip, 0))
					ip.color = bullet.dynamicSolver.SolveDynamicColor(rawInst.color, 9378041 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleRenderer.startColor = ip.color;
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 1818417 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 1974311 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionColorReplace(
						bullet, ip.instructionDuration, ip.operationCurve, ip.color));
				}
			}

			// Add Color
			else if (ip.instructionType == PatternInstructionType.AddColor)
			{
				if (IsRerollNecessary(ip, 0))
					ip.color = bullet.dynamicSolver.SolveDynamicColor(rawInst.color, 7481662 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleRenderer.startColor += ip.color;
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 9874112 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 3600471 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionColorAdd(
						bullet, ip.instructionDuration, ip.operationCurve, ip.color));
				}
			}

			// Multiply Color
			else if (ip.instructionType == PatternInstructionType.MultiplyColor)
			{
				if (IsRerollNecessary(ip, 0))
					ip.color = bullet.dynamicSolver.SolveDynamicColor(rawInst.color, 8515402 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleRenderer.startColor *= ip.color;
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 1313692 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 8114503 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionColorMultiply(
						bullet, ip.instructionDuration, ip.operationCurve, ip.color));
				}
			}

			// Overlay Color
			else if (ip.instructionType == PatternInstructionType.OverlayColor)
			{
				if (IsRerollNecessary(ip, 0))
					ip.color = bullet.dynamicSolver.SolveDynamicColor(rawInst.color, 1810999 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleRenderer.startColor = bullet.moduleRenderer.BlendColors(bullet.moduleRenderer.startColor, ip.color, ColorBlend.AlphaBlend);
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 8795228 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 3471547 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionColorOverlay(
						bullet, ip.instructionDuration, ip.operationCurve, ip.color));
				}
			}

			// Set Alpha
			else if (ip.instructionType == PatternInstructionType.SetAlpha)
			{
				if (IsRerollNecessary(ip, 0))
					ip.alpha = bullet.dynamicSolver.SolveDynamicSlider01(rawInst.alpha, 9378041 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleRenderer.startColor = new Color(
						bullet.moduleRenderer.startColor.r,
						bullet.moduleRenderer.startColor.g,
						bullet.moduleRenderer.startColor.b,
						Mathf.Clamp01(ip.alpha));
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 1818417 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 1974311 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionAlphaSet(
						bullet, ip.instructionDuration, ip.operationCurve, ip.alpha));
				}
			}

			// Add Alpha
			else if (ip.instructionType == PatternInstructionType.AddAlpha)
			{
				if (IsRerollNecessary(ip, 0))
					ip.alpha = bullet.dynamicSolver.SolveDynamicSlider01(rawInst.alpha, 7764214 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleRenderer.startColor = new Color(
						bullet.moduleRenderer.startColor.r,
						bullet.moduleRenderer.startColor.g,
						bullet.moduleRenderer.startColor.b,
						Mathf.Clamp01(bullet.moduleRenderer.startColor.a + ip.alpha));
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 3848506 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 3174899 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionAlphaAdd(
						bullet, ip.instructionDuration, ip.operationCurve, ip.alpha));
				}
			}

			// Multiply Alpha
			else if (ip.instructionType == PatternInstructionType.MultiplyAlpha)
			{
				if (IsRerollNecessary(ip, 0))
					ip.factor = bullet.dynamicSolver.SolveDynamicFloat(rawInst.factor, 4856423 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleRenderer.startColor = new Color(
						bullet.moduleRenderer.startColor.r,
						bullet.moduleRenderer.startColor.g,
						bullet.moduleRenderer.startColor.b,
						Mathf.Clamp01(bullet.moduleRenderer.startColor.a * ip.factor));
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 4156711 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 7842199 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionAlphaMultiply(
						bullet, ip.instructionDuration, ip.operationCurve, ip.factor));
				}
			}

			// Set Lifetime Gradient
			else if (ip.instructionType == PatternInstructionType.SetLifetimeGradient)
			{
				if (IsRerollNecessary(ip, 0))
					ip.gradient = bullet.dynamicSolver.SolveDynamicGradient(rawInst.gradient, 7947913 * (numberOfRerolls++), ParameterOwner.Pattern);

				if (ip.instructionTiming == InstructionTiming.Instantly)
					bullet.moduleRenderer.colorEvolution = ip.gradient;
				else
				{
					if (IsRerollNecessary(ip, 1))
						ip.instructionDuration = bullet.dynamicSolver.SolveDynamicFloat(rawInst.instructionDuration, 2685468 * (numberOfRerolls++), ParameterOwner.Pattern);

					if (IsRerollNecessary(ip, 2))
						ip.operationCurve = bullet.dynamicSolver.SolveDynamicAnimationCurve(rawInst.operationCurve, 5990141 * (numberOfRerolls++), ParameterOwner.Pattern);
					
					bullet.microActions.Add(new MicroActionGradientSet(
						bullet, ip.instructionDuration, ip.operationCurve, ip.gradient));
				}
			}

			#endregion

			#region collision

			// Enable Collision
			else if (ip.instructionType == PatternInstructionType.EnableCollision)
			{
				bullet.moduleCollision.Enable();
			}

			// Disable Collision
			else if (ip.instructionType == PatternInstructionType.DisableCollision)
			{
				bullet.moduleCollision.Disable();
			}

			// Change Collision Tag
			else if (ip.instructionType == PatternInstructionType.ChangeCollisionTag)
			{
				if (IsRerollNecessary(ip, 0))
					ip.collisionTag = bullet.dynamicSolver.SolveDynamicString(rawInst.collisionTag, 8577701 * (numberOfRerolls++), ParameterOwner.Pattern);

				bullet.moduleCollision.collisionTags[ip.collisionTag] = (ip.collisionTagAction == CollisionTagAction.Add);
			}

			#endregion

			#region pattern flow

			// Play Pattern
			else if (ip.instructionType == PatternInstructionType.PlayPattern)
			{
				if (IsRerollNecessary(ip, 0))
					ip.patternTag = bullet.dynamicSolver.SolveDynamicString(rawInst.patternTag, 6428211 * (numberOfRerolls++), ParameterOwner.Pattern);

				bullet.modulePatterns.Play(ip.patternTag);
			}

			// Pause Pattern
			else if (ip.instructionType == PatternInstructionType.PausePattern)
			{
				if (ip.patternControlTarget == PatternControlTarget.ThisPattern)
				{
					bullet.modulePatterns.Pause(patternIndex);
					return InstructionResult.InterruptedItself;
				}
				else
				{
					if (IsRerollNecessary(ip, 0))
						ip.patternTag = bullet.dynamicSolver.SolveDynamicString(rawInst.patternTag, 7527107 * (numberOfRerolls++), ParameterOwner.Pattern);

					bullet.modulePatterns.Pause(ip.patternTag);

					if (bullet.modulePatterns.patternRuntimeInfo[patternIndex].HasTag(ip.patternTag))
						return InstructionResult.InterruptedItself;
				}
			}

			// Stop Pattern
			else if (ip.instructionType == PatternInstructionType.StopPattern)
			{
				if (ip.patternControlTarget == PatternControlTarget.ThisPattern)
				{
					bullet.modulePatterns.Stop(patternIndex);
					return InstructionResult.RebootedItself;
				}
				else
				{
					if (IsRerollNecessary(ip, 0))
						ip.patternTag = bullet.dynamicSolver.SolveDynamicString(rawInst.patternTag, 8148503 * (numberOfRerolls++), ParameterOwner.Pattern);

					bullet.modulePatterns.Stop(ip.patternTag);

					if (bullet.modulePatterns.patternRuntimeInfo[patternIndex].HasTag(ip.patternTag))
						return InstructionResult.RebootedItself;
				}
			}

			// Reboot Pattern
			else if (ip.instructionType == PatternInstructionType.RebootPattern)
			{
				if (ip.patternControlTarget == PatternControlTarget.ThisPattern)
				{
					bullet.modulePatterns.Boot(patternIndex);
					return InstructionResult.RebootedItself;
				}
				else
				{
					if (IsRerollNecessary(ip, 0))
						ip.patternTag = bullet.dynamicSolver.SolveDynamicString(rawInst.patternTag, 4856014 * (numberOfRerolls++), ParameterOwner.Pattern);

					bullet.modulePatterns.Boot(ip.patternTag);

					if (bullet.modulePatterns.patternRuntimeInfo[patternIndex].HasTag(ip.patternTag))
						return InstructionResult.RebootedItself;
				}
			}

			#endregion

			return InstructionResult.None;
		}

		// This is in a separate function so that it can be called at the very end of the stack.
		// Returns whether it had to go back to its corresponding BeginLoop.
		bool EndLoop()
		{
			if (loops.Count == 0) return false;

			// if we're skipping a loop that has another loop within it, that might not be the tag we're looking for
			endLoopTagsWanted--;
			if (endLoopTagsWanted > 0) return false;

			// next iteration
			LoopInfo cur = loops.Pop();
			cur.iterationsFinished++;
			if (cur.endless)
			{
				indexOfNextInstruction = cur.indexOfBeginInstruction;
				loops.Push(cur);

				// endless loop without a wait instruction should yield now and wait for next frame
				if (!hadWaitBlockDuringLoop) shouldBreakLoopNow = true;
				hadWaitBlockDuringLoop = false;

				return true;
			}
			else
			{
				cur.iterationsRemaining--;
				if (cur.iterationsRemaining > 0)
				{
					indexOfNextInstruction = cur.indexOfBeginInstruction;
					loops.Push(cur);
					return true;
				}

				return false;
			}
		}

		bool IsRerollNecessary(SolvedInstruction ip, int channel)
		{
			if (ip.rerollFrequency[channel] == RerollFrequency.WheneverCalled) return true;
			if (ip.rerollFrequency[channel] == RerollFrequency.OnlyOncePerPattern) return false;
			if (ip.rerollFrequency[channel] == RerollFrequency.AtCertainLoops)
			{
				if (loops.Count == 0) return false;
				int relevantLoopIndex = (loops.Count-1) - ip.rerollLoopDepth[channel];
				if (relevantLoopIndex < 0) return false;

				// from now on we know we can look at loops[relevantLoopIndex] and examine how many times it's been over.

				// but first, let's find out if the sub-loops (if any) have just began, which would mean it really is a new iteration
				bool abort = false;
				int totalCount = loops.Count;
				LoopInfo relevantLoop = loops.Peek();
				if (relevantLoopIndex != totalCount-1) // there can't be subloops if we already look at the most-recent one
				{
					for (int i = relevantLoopIndex+1; i < totalCount; i++)
					{
						LoopInfo li = loops.Pop();
						if (li.iterationsFinished > 0) abort = true;
						tempStack.Push(li);
					}
					// while they're all out, retrieve the relevant loop for later
					relevantLoop = loops.Peek();
					// put them back into the stack now we've looked
					for (int i = relevantLoopIndex+1; i < totalCount; i++)
					{
						LoopInfo li = tempStack.Pop();
						loops.Push(li);
					}
					if (abort) return false;
				}
				// else the relevantLoop is effectively loops.Peek so it has the proper value.
				
				// We can now search for a sequence, or simply return true for reroll if no sequence is used.
				if (!ip.useComplexRerollSequence[channel])
				{
					return true;
				}

				int indexInSequence = relevantLoop.iterationsFinished % ip.checkEveryNLoops[channel];
				return (ip.loopSequence[channel] & (1 << indexInSequence)) != 0;
			}
			return false;
		}

		// Gets if we're in the first iteration of loops until the n-th parent loop. 0 is the childmost loop (the current one) and thus would always return true.
		bool IsFirstLoopIteration(int loopDepth)
		{
			if (loops.Count == 0) return false;

			return true; // dummy
			// TODO : think the design a bit deeper. I know I could just go upwards through the loop stack and check for .iterationsFinished but...
		}
	}
}