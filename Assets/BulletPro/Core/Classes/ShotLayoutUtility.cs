using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{

	// A toolbox for positioning bullets inside a Shot.
	[System.Serializable]
	public static class ShotLayoutUtility
	{
		// Recalculates bullet spawns in each modifier, starting from sp[0].modifiers[index].
		// The int as argument is because due to dynamic parameters, it may not match bulletSpawns.Length (so we need less than the max).
		// Bullet is passed to solve dynamic parameters.
		public static BulletSpawn[] RecalculateBulletLayout(ShotParams sp, int wantedNumberOfBullets, Bullet emitterBullet)
		{
			if (sp.bulletSpawns == null) return null;
			if (sp.bulletSpawns.Length == 0) return null;
			if (wantedNumberOfBullets == 0) return null;

			// adjusting to the rare cases when bullet amount is changed from the inspector during runtime
			if (wantedNumberOfBullets > sp.bulletSpawns.Length)
				wantedNumberOfBullets = sp.bulletSpawns.Length;

			BulletSpawn[] result = new BulletSpawn[wantedNumberOfBullets];
			for (int i = 0; i < wantedNumberOfBullets; i++)
				result[i] = sp.bulletSpawns[i];

			if (sp.modifiers.Count == 0) return result;

			result = RefreshBulletSpawnsIndexes(result);

			// let's find out whether we're using a narrowed list of selected bullets
			int impactedModsLeft = 0;
			int indexOfSelectorModifier = -1; // -1 means "none"

			// pivot used for "RotateLayout" and "ScaleLayout", can be modified throughout modifier browsing
			Vector2 curPivot = Vector2.zero;

			for (int i = 0; i < sp.modifiers.Count; i++)
			{
				ShotModifier curMod = sp.modifiers[i];
				if (curMod.modifierType == ShotModifierType.OnlySomeBullets)
				{
					if (curMod.enabled)
					{
						impactedModsLeft = curMod.numberOfModifiersAffected;
						indexOfSelectorModifier = i;
					}
					else impactedModsLeft = 0;
				}
				else
				{
					impactedModsLeft--;
					if (impactedModsLeft < 0)
						indexOfSelectorModifier = -1;
				}

				result = RefreshModifier(result, sp, i, indexOfSelectorModifier, emitterBullet, ref curPivot);
			}

			return result;
		}

		// Re-stores different indexes in each bullet modifier of a shot.
		public static BulletSpawn[] RefreshBulletSpawnsIndexes(BulletSpawn[] bs)
		{
			if (bs != null)
				if (bs.Length > 0)
				{
					for (int j = 0; j < bs.Length; j++)
						bs[j].index = j;
					return bs;
				}

			return null;
		}

		// Recalculates bullet spawns in one modifier, based on previous spawn locations and dynamic solver.
		// Updates current pivot point and returns new array of bullet spawns.
		public static BulletSpawn[] RefreshModifier(BulletSpawn[] bs, ShotParams sp, int index, int indexOfSelectorModifier, Bullet emitterBullet, ref Vector2 curPivot)
		{
			ShotModifier sm = sp.modifiers[index];
			ShotModifier selectorMod = sp.modifiers[0]; // dummy value, replaced later if index>-1
			
			// first things first, if it's a selector modifier we just refresh the indexes of affected bullets
			if (sm.modifierType == ShotModifierType.OnlySomeBullets)
			{
				// ShotModifier.indexesOfBulletsAffected has already been serialized from the inspector
				
				return bs;
			}

			// No need to make things heavy with further error handling, we already know bs isn't null nor empty from the previous call
			// But we still reject disabled modifiers
			
			if (!sm.enabled) return bs;

			// also, if this modifier is here to set rotate/scale pivot, just do it now and don't waste time browsing bullets.
			if (sm.modifierType == ShotModifierType.SetPivot)
			{
				curPivot = emitterBullet.dynamicSolver.SolveDynamicVector2(sm.pivot, 167789 * indexOfSelectorModifier, ParameterOwner.Shot);

				return bs;
			}

			// If we're using a selection rect that contains exactly 0 bullet, we also don't apply the modification
			if (indexOfSelectorModifier > -1)
			{
				selectorMod = sp.modifiers[indexOfSelectorModifier];

				bool abort = false;

				if (selectorMod.indexesOfBulletsAffected == null)
					abort = true;
				else if (selectorMod.indexesOfBulletsAffected.Length == 0)
					abort = true;
				
				if (abort) return bs;
			}

			// do the calc based on the modifierType of sm

			#region modifiers that induce reordering (like spread and spacing), which means spawns need to know each other

			bool reordered = false;

			if (sm.modifierType == ShotModifierType.SpreadBullets || sm.modifierType == ShotModifierType.SpreadBulletsTotal)
			{
				if (indexOfSelectorModifier > -1) // if narrowed by selector
				{
					List<BulletSpawn> selectedBullets = new List<BulletSpawn>();
					int nextSelectedBullet = 0;
					for (int i = 0; i < bs.Length; i++)
					{	
						if (nextSelectedBullet == selectorMod.indexesOfBulletsAffected.Length) continue;
						else if (bs[i].index != selectorMod.indexesOfBulletsAffected[nextSelectedBullet])
							continue;
						else nextSelectedBullet++;
						selectedBullets.Add(bs[i]);
					}

					BulletSpawn[] toEdit = selectedBullets.ToArray();
					toEdit = GetSortedBullets(toEdit, BulletSortMode.Z, true);

					int bullets = toEdit.Length;
					bool even = (bullets % 2 == 0);
					int centerBullet = even ? (bullets / 2 - 1) : ((bullets - 1) / 2);

					// apply step
					float actualAngle = 0;
					if (sm.modifierType == ShotModifierType.SpreadBullets)
						actualAngle = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.degrees, 402964 * indexOfSelectorModifier, ParameterOwner.Shot);
					else actualAngle = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.degreesTotal, 600487 * indexOfSelectorModifier, ParameterOwner.Shot)/(float)bullets;
					for (int i = 0; i < bullets; i++)
					{
						float steps = centerBullet - i;
						if (even) steps += 0.5f;
						toEdit[i].bulletOrigin.z += actualAngle * steps;
					}
					toEdit = GetSortedBulletsByIndex(toEdit, false);
					int nextRelevantBullet = 0;
					
					bs = GetSortedBulletsByIndex(bs, false);
					
					// once back to index sort, use it to apply changes in the whole array
					for (int i = 0; i < bs.Length; i++)
					{
						if (nextRelevantBullet == toEdit.Length) continue;
						else if (bs[i].index != toEdit[nextRelevantBullet].index) continue;
						
						bs[i] = toEdit[nextRelevantBullet];
						nextRelevantBullet++;
					}
				}
				else // unmodified by selector, default function
				{
					bs = GetSortedBullets(bs, BulletSortMode.Z, true);
					int bullets = bs.Length;
					bool even = (bullets % 2 == 0);
					int centerBullet = even ? (bullets / 2 - 1) : ((bullets - 1) / 2);

					// apply step
					float actualAngle = 0;
					if (sm.modifierType == ShotModifierType.SpreadBullets)
						actualAngle = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.degrees, 402964 * indexOfSelectorModifier, ParameterOwner.Shot);
					else actualAngle = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.degreesTotal, 600487 * indexOfSelectorModifier, ParameterOwner.Shot)/(float)bullets;
					for (int i = 0; i < bullets; i++)
					{
						float steps = centerBullet - i;
						if (even) steps += 0.5f;
						bs[i].bulletOrigin.z += actualAngle * steps;
					}
				}

				reordered = true;
			}

			else if (sm.modifierType == ShotModifierType.HorizontalSpacing || sm.modifierType == ShotModifierType.HorizontalSpacingTotal)
			{
				if (indexOfSelectorModifier > -1)
				{
					List<BulletSpawn> selectedBullets = new List<BulletSpawn>();
					int nextSelectedBullet = 0;
					for (int i = 0; i < bs.Length; i++)
					{	
						if (nextSelectedBullet == selectorMod.indexesOfBulletsAffected.Length) continue;
						else if (bs[i].index != selectorMod.indexesOfBulletsAffected[nextSelectedBullet])
							continue;
						else nextSelectedBullet++;
						selectedBullets.Add(bs[i]);
					}

					BulletSpawn[] toEdit = selectedBullets.ToArray();
					toEdit = GetSortedBullets(toEdit, BulletSortMode.X, true);

					int bullets = toEdit.Length;
					bool even = (bullets % 2 == 0);
					int centerBullet = even ? (bullets / 2 - 1) : ((bullets - 1) / 2);

					// apply step
					float actualSpacing = 0;
					if (sm.modifierType == ShotModifierType.HorizontalSpacing)
						actualSpacing = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.horizontalSpacing, 608474 * indexOfSelectorModifier, ParameterOwner.Shot);
					else actualSpacing = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.xSpacingTotal, 434787 * indexOfSelectorModifier, ParameterOwner.Shot)/(float)bullets;
					for (int i = 0; i < bullets; i++)
					{
						float steps = centerBullet - i;
						if (even) steps += 0.5f;
						toEdit[i].bulletOrigin.x += actualSpacing * steps;
					}
					toEdit = GetSortedBulletsByIndex(toEdit, false);
					bs = GetSortedBulletsByIndex(bs, false);
					int nextRelevantBullet = 0;
					for (int i = 0; i < bs.Length; i++)
					{
						if (nextRelevantBullet == toEdit.Length) continue;
						else if (bs[i].index != toEdit[nextRelevantBullet].index) continue;
						
						bs[i] = toEdit[nextRelevantBullet];
						nextRelevantBullet++;
					}
				}
				else
				{
					bs = GetSortedBullets(bs, BulletSortMode.X, true);
					int bullets = bs.Length;
					bool even = (bullets % 2 == 0);
					int centerBullet = even ? (bullets / 2 - 1) : ((bullets - 1) / 2);

					// apply step
					float actualSpacing = 0;
					if (sm.modifierType == ShotModifierType.HorizontalSpacing)
						actualSpacing = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.horizontalSpacing, 608474 * indexOfSelectorModifier, ParameterOwner.Shot);
					else actualSpacing = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.xSpacingTotal, 434787 * indexOfSelectorModifier, ParameterOwner.Shot)/(float)bullets;
					for (int i = 0; i < bullets; i++)
					{
						float steps = centerBullet - i;
						if (even) steps += 0.5f;
						bs[i].bulletOrigin.x += actualSpacing * steps;
					}
				}

				reordered = true;
			}

			else if (sm.modifierType == ShotModifierType.VerticalSpacing || sm.modifierType == ShotModifierType.VerticalSpacingTotal)
			{
				if (indexOfSelectorModifier > -1)
				{
					List<BulletSpawn> selectedBullets = new List<BulletSpawn>();
					int nextSelectedBullet = 0;
					for (int i = 0; i < bs.Length; i++)
					{	
						if (nextSelectedBullet == selectorMod.indexesOfBulletsAffected.Length) continue;
						else if (bs[i].index != selectorMod.indexesOfBulletsAffected[nextSelectedBullet])
							continue;
						else nextSelectedBullet++;
						selectedBullets.Add(bs[i]);
					}

					BulletSpawn[] toEdit = selectedBullets.ToArray();
					toEdit = GetSortedBullets(toEdit, BulletSortMode.Y, true);

					int bullets = toEdit.Length;
					bool even = (bullets % 2 == 0);
					int centerBullet = even ? (bullets / 2 - 1) : ((bullets - 1) / 2);

					// apply step
					float actualSpacing = 0;
					if (sm.modifierType == ShotModifierType.VerticalSpacing)
						actualSpacing = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.verticalSpacing, 232807 * indexOfSelectorModifier, ParameterOwner.Shot);
					else actualSpacing = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.ySpacingTotal, 344875 * indexOfSelectorModifier, ParameterOwner.Shot)/(float)bullets;
					for (int i = 0; i < bullets; i++)
					{
						float steps = centerBullet - i;
						if (even) steps += 0.5f;
						toEdit[i].bulletOrigin.y += actualSpacing * steps;
					}
					toEdit = GetSortedBulletsByIndex(toEdit, false);
					bs = GetSortedBulletsByIndex(bs, false);
					int nextRelevantBullet = 0;
						
					for (int i = 0; i < bs.Length; i++)
					{
						if (nextRelevantBullet == toEdit.Length) continue;
						else if (bs[i].index != toEdit[nextRelevantBullet].index) continue;
						
						bs[i] = toEdit[nextRelevantBullet];
						nextRelevantBullet++;
					}
				}
				else
				{
					bs = GetSortedBullets(bs, BulletSortMode.Y, true);
					int bullets = bs.Length;
					bool even = (bullets % 2 == 0);
					int centerBullet = even ? (bullets / 2 - 1) : ((bullets - 1) / 2);

					// apply step
					float actualSpacing = 0;
					if (sm.modifierType == ShotModifierType.VerticalSpacing)
						actualSpacing = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.verticalSpacing, 232807 * indexOfSelectorModifier, ParameterOwner.Shot);
					else actualSpacing = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.ySpacingTotal, 344875 * indexOfSelectorModifier, ParameterOwner.Shot)/(float)bullets;	
					for (int i = 0; i < bullets; i++)
					{
						float steps = centerBullet - i;
						if (even) steps += 0.5f;
						bs[i].bulletOrigin.y += actualSpacing * steps;
					}
				}
				
				reordered = true;
			}

			if (reordered) return bs;

			#endregion

			#region all other modifiers

			int nextBullet = 0;
			for (int i = 0; i < bs.Length; i++)
			{
				// discard unaffected bullets when using a modifier
				if (indexOfSelectorModifier > -1)
				{
					if (nextBullet == selectorMod.indexesOfBulletsAffected.Length) continue;
					else if (bs[i].index != selectorMod.indexesOfBulletsAffected[nextBullet])
						continue;
					else nextBullet++;					
				}

				if (sm.modifierType == ShotModifierType.LookAtPoint)
				{
					bs[i].bulletOrigin.z = LookAtTarget(bs[i].bulletOrigin, emitterBullet.dynamicSolver.SolveDynamicVector2(sm.pointToLookAt, 65705 * indexOfSelectorModifier, ParameterOwner.Shot));
					bs[i].bulletOrigin.z += 720;
					bs[i].bulletOrigin.z = bs[i].bulletOrigin.z % 360;
					// display debug
					if (Mathf.Abs(bs[i].bulletOrigin.z) < 0.001f) bs[i].bulletOrigin.z = 0;
				}
				else if (sm.modifierType == ShotModifierType.LookAwayFromPoint)
				{
					bs[i].bulletOrigin.z = LookAwayFromTarget(bs[i].bulletOrigin, emitterBullet.dynamicSolver.SolveDynamicVector2(sm.pointToFleeFrom, 851816 * indexOfSelectorModifier, ParameterOwner.Shot));
					bs[i].bulletOrigin.z += 720;
					bs[i].bulletOrigin.z = bs[i].bulletOrigin.z % 360;
					// display debug
					if (Mathf.Abs(bs[i].bulletOrigin.z) < 0.001f) bs[i].bulletOrigin.z = 0;
				}
				else if (sm.modifierType == ShotModifierType.ResetCoordinates)
				{
					if (emitterBullet.dynamicSolver.SolveDynamicBool(sm.resetX, 881662 * indexOfSelectorModifier, ParameterOwner.Shot)) bs[i].bulletOrigin.x = 0;
					if (emitterBullet.dynamicSolver.SolveDynamicBool(sm.resetY, 893177 * indexOfSelectorModifier, ParameterOwner.Shot)) bs[i].bulletOrigin.y = 0;
					if (emitterBullet.dynamicSolver.SolveDynamicBool(sm.resetZ, 399941 * indexOfSelectorModifier, ParameterOwner.Shot)) bs[i].bulletOrigin.z = 0;
				}
				else if (sm.modifierType == ShotModifierType.GlobalTranslation)
				{
					Vector2 globalMovement = emitterBullet.dynamicSolver.SolveDynamicVector2(sm.globalMovement, 544121 * indexOfSelectorModifier, ParameterOwner.Shot);
					Vector3 gm = new Vector3(globalMovement.x, globalMovement.y, 0);
					bs[i].bulletOrigin += gm;
				}
				else if (sm.modifierType == ShotModifierType.LocalTranslation)
				{
					Vector2 localMovement = emitterBullet.dynamicSolver.SolveDynamicVector2(sm.localMovement, 146709 * indexOfSelectorModifier, ParameterOwner.Shot);
					Vector2 x = GetRelativeRight(bs[i]);
					Vector2 y = GetRelativeUp(bs[i]);
					Vector2 total = x * localMovement.x + y * localMovement.y;
					Vector3 totalV3 = new Vector3(total.x, total.y, 0);
					bs[i].bulletOrigin += totalV3;
				}
				else if (sm.modifierType == ShotModifierType.Rotation)
				{
					Vector3 gm = Vector3.forward * emitterBullet.dynamicSolver.SolveDynamicFloat(sm.rotationDegrees, 792624 * indexOfSelectorModifier, ParameterOwner.Shot);
					bs[i].bulletOrigin += gm;
					bs[i].bulletOrigin.z += 720;
					bs[i].bulletOrigin.z = bs[i].bulletOrigin.z % 360;
				}
				else if (sm.modifierType == ShotModifierType.RotateAroundPivot)
				{
					float layoutDegrees = emitterBullet.dynamicSolver.SolveDynamicFloat(sm.layoutDegrees, 94878 * indexOfSelectorModifier, ParameterOwner.Shot);
					float x = bs[i].bulletOrigin.x;
					float y = bs[i].bulletOrigin.y;
					float a = Mathf.Deg2Rad * layoutDegrees;
					float c = Mathf.Cos(a);
					float s = Mathf.Sin(a);

					x -= curPivot.x;
					y -= curPivot.y;
					float newX = x*c - y*s;
					float newY = x*s + y*c;
					newX += curPivot.x;
					newY += curPivot.y;

					float newZ = bs[i].bulletOrigin.z;
					newZ += layoutDegrees;
					newZ += 720;
					newZ = newZ % 360;

					bs[i].bulletOrigin = new Vector3(newX, newY, newZ);
				}
				else if (sm.modifierType == ShotModifierType.ScaleLayout)
				{
					Vector2 scale = emitterBullet.dynamicSolver.SolveDynamicVector2(sm.scale, 544562 * indexOfSelectorModifier, ParameterOwner.Shot);

					Vector3 o = bs[i].bulletOrigin;
					float newX = curPivot.x + (o.x-curPivot.x) * scale.x;
					float newY = curPivot.y + (o.y-curPivot.y) * scale.y;
					
					float newZ = o.z;
					bool flipX = scale.x < 0;
					bool flipY = scale.y < 0;
					if (flipY != flipX) newZ *= -1;
					if (flipY) newZ += 180;
					newZ += 720;
					newZ = newZ % 360;
					
					bs[i].bulletOrigin = new Vector3(newX, newY, newZ);
				}
				else if (sm.modifierType == ShotModifierType.FlipOrientation)
				{
					bool flipX = emitterBullet.dynamicSolver.SolveDynamicBool(sm.flipX, 828793 * indexOfSelectorModifier, ParameterOwner.Shot);
					bool flipY = emitterBullet.dynamicSolver.SolveDynamicBool(sm.flipY, 942865 * indexOfSelectorModifier, ParameterOwner.Shot);

					float newZ = bs[i].bulletOrigin.z;
					if (flipY != flipX) newZ *= -1;
					if (flipY) newZ += 180;
					newZ += 720;
					newZ = newZ % 360;
					bs[i].bulletOrigin = new Vector3(bs[i].bulletOrigin.x, bs[i].bulletOrigin.y, newZ);
				}
				else if (sm.modifierType == ShotModifierType.SetBulletParams)
				{
					BulletParams newBP = emitterBullet.dynamicSolver.SolveDynamicBullet(sm.bulletParams, 923317 * indexOfSelectorModifier, ParameterOwner.Shot);
					if (newBP)
					{
						bs[i].bulletParams = newBP;
						bs[i].useDifferentBullet = true;
					}
				}
			}

			#endregion

			return bs;
		}

		// Returns needed angle to look at Vector2(x,y).
		public static float LookAtTarget(Vector3 bulletSpawn, Vector2 target)
		{
			Vector2 bullet = new Vector2(bulletSpawn.x, bulletSpawn.y);
			Vector2 diff = target - bullet;
			if (diff == Vector2.zero) return bulletSpawn.z;

			Vector2 oldOrientation = new Vector2(Mathf.Cos(bulletSpawn.z * Mathf.Deg2Rad), Mathf.Sin(bulletSpawn.z * Mathf.Deg2Rad));
			float angle = Vector2.Angle(oldOrientation, diff);
			if (Vector3.Cross(oldOrientation, diff).z < 0) angle *= -1;
			return bulletSpawn.z + angle - 90;
			// -90 because "0 rotation" would look to the right in math, but gameplay main direction is Vector2.up
		}

		// Returns needed angle to look away from Vector2(x,y).
		public static float LookAwayFromTarget(Vector3 bulletSpawn, Vector2 target)
		{
			Vector2 bullet = new Vector2(bulletSpawn.x, bulletSpawn.y);
			Vector2 diff = target - bullet;
			if (diff == Vector2.zero) return bulletSpawn.z;

			Vector2 oldOrientation = new Vector2(Mathf.Cos(bulletSpawn.z * Mathf.Deg2Rad), Mathf.Sin(bulletSpawn.z * Mathf.Deg2Rad));
			float angle = Vector2.Angle(oldOrientation, diff);
			if (Vector3.Cross(oldOrientation, diff).z < 0) angle *= -1;
			return bulletSpawn.z + angle + 180 - 90;
			// +180 because it's basically the same as "look at", but in the opposite direction
			// -90 because "0 rotation" would look to the right in math, but gameplay main direction is Vector2.up
		}

		// Sorts bullets of a same shot, by X, Y or Z - used in spreading and spacing bullets
		public static BulletSpawn[] GetSortedBullets(BulletSpawn[] raw, BulletSortMode sortMode, bool descending)
		{
			if (raw == null) return null;

			int bulletCount = raw.Length;
			if (bulletCount < 2) return raw;

			List<BulletSpawn> listResult = new List<BulletSpawn>();
			List<BulletSpawn> rawList = new List<BulletSpawn>();
			for (int i = 0; i < bulletCount; i++)
				rawList.Add(raw[i]);

			for (int i = 0; i < bulletCount; i++)
			{
				int indexOfNext = 0;
				for (int j = 0; j < rawList.Count; j++)
				{
					// Sort mode : by X
					if (sortMode == BulletSortMode.X)
					{
						if (rawList[j].bulletOrigin.x == rawList[indexOfNext].bulletOrigin.x)
						{
							// tie-breaker : Y
							if (!descending && rawList[j].bulletOrigin.y < rawList[indexOfNext].bulletOrigin.y) indexOfNext = j;
							else if (descending && rawList[j].bulletOrigin.y > rawList[indexOfNext].bulletOrigin.y) indexOfNext = j;
						}
						else
						{
							if (!descending && rawList[j].bulletOrigin.x < rawList[indexOfNext].bulletOrigin.x) indexOfNext = j;
							else if (descending && rawList[j].bulletOrigin.x > rawList[indexOfNext].bulletOrigin.x) indexOfNext = j;
						}
					}

					// Sort mode : by Y
					else if (sortMode == BulletSortMode.Y)
					{
						if (rawList[j].bulletOrigin.y == rawList[indexOfNext].bulletOrigin.y)
						{
							// tie-breaker : X
							if (!descending && rawList[j].bulletOrigin.x < rawList[indexOfNext].bulletOrigin.x) indexOfNext = j;
							else if (descending && rawList[j].bulletOrigin.x > rawList[indexOfNext].bulletOrigin.x) indexOfNext = j;
						}
						else
						{
							if (!descending && rawList[j].bulletOrigin.y < rawList[indexOfNext].bulletOrigin.y) indexOfNext = j;
							else if (descending && rawList[j].bulletOrigin.y > rawList[indexOfNext].bulletOrigin.y) indexOfNext = j;
						}
						
					}

					// Sort mode : by Z
					else if (sortMode == BulletSortMode.Z)
					{
						// get angle values from 0 to 360
						float jz = rawList[j].bulletOrigin.z %360;
						float nz = rawList[indexOfNext].bulletOrigin.z %360;

						if (jz == nz)
						{
							// tie-breaker : X
							if (rawList[j].bulletOrigin.x == rawList[indexOfNext].bulletOrigin.x)
							{
								// tie-breaker's tie-breaker : Y
								if (!descending && rawList[j].bulletOrigin.y < rawList[indexOfNext].bulletOrigin.y) indexOfNext = j;
								else if (descending && rawList[j].bulletOrigin.y > rawList[indexOfNext].bulletOrigin.y) indexOfNext = j;
							}
							else
							{
								if (!descending && rawList[j].bulletOrigin.x < rawList[indexOfNext].bulletOrigin.x) indexOfNext = j;
								else if (descending && rawList[j].bulletOrigin.x > rawList[indexOfNext].bulletOrigin.x) indexOfNext = j;
							}
						}
						else
						{
							if (!descending && jz < nz) indexOfNext = j;
							else if (descending && jz > nz) indexOfNext = j;	
						}
					}
				}

				listResult.Add(rawList[indexOfNext]);
				rawList.RemoveAt(indexOfNext);
			}

			return listResult.ToArray();
		}

		// Sorts bullets of a same shot, by internal index - used in managing selector modifiers when spreading and spacing bullets
		public static BulletSpawn[] GetSortedBulletsByIndex(BulletSpawn[] raw, bool descending)
		{
			if (raw == null) return null;

			int bulletCount = raw.Length;
			if (bulletCount < 2) return raw;

			List<BulletSpawn> listResult = new List<BulletSpawn>();
			List<BulletSpawn> rawList = new List<BulletSpawn>();
			for (int i = 0; i < bulletCount; i++)
				rawList.Add(raw[i]);

			for (int i = 0; i < bulletCount; i++)
			{
				int indexOfNext = 0;
				for (int j = 0; j < rawList.Count; j++)
				{
					if (!descending && rawList[j].index < rawList[indexOfNext].index) indexOfNext = j;
					if (descending && rawList[j].index > rawList[indexOfNext].index) indexOfNext = j;
				}

				listResult.Add(rawList[indexOfNext]);
				rawList.RemoveAt(indexOfNext);
			}

			return listResult.ToArray();
		}

		// Returns the "self.right" vector of a BulletSpawn :
		public static Vector2 GetRelativeRight(BulletSpawn bs)
		{
			float z = bs.bulletOrigin.z;
			float x = 1;
			float y = 0;

			float cos = Mathf.Cos(z * Mathf.Deg2Rad);
			float sin = Mathf.Sin(z * Mathf.Deg2Rad);

			Vector2 result = new Vector2(x * cos - y * sin, x * sin + y * cos);
			// boils down to (cos, sin), but makes it way more explicit.

			return result.normalized;
		}

		// Returns the "self.up" vector of a BulletSpawn :
		public static Vector2 GetRelativeUp(BulletSpawn bs)
		{
			float z = bs.bulletOrigin.z;
			float x = 0;
			float y = 1;

			float cos = Mathf.Cos(z * Mathf.Deg2Rad);
			float sin = Mathf.Sin(z * Mathf.Deg2Rad);

			Vector2 result = new Vector2(x * cos - y * sin, x * sin + y * cos);
			// ^ boils down to (-sin,cos), but makes it way more explicit.

			return result.normalized;
		}

		// And their inverted counterparts
		public static Vector2 GetRelativeDown(BulletSpawn bs) { return -1 * GetRelativeUp(bs); }
		public static Vector2 GetRelativeLeft(BulletSpawn bs) { return -1 * GetRelativeRight(bs); }

		// Mirrors bullet spawns during runtime by applying a "scale*-1" mod.
		// Unused at the moment, but could be. (TODO ?)
		public static Vector3 Mirror(Vector3 coords, bool mirrorX, bool mirrorY)
		{
			Vector2 scale = Vector2.one;
			if (mirrorX) scale.x = -1;
			if (mirrorY) scale.y = -1;

			Vector2 pivot = Vector2.zero; // unused for now, could be later

			float newX = pivot.x + (coords.x-pivot.x) * scale.x;
			float newY = pivot.y + (coords.y-pivot.y) * scale.y;
			
			float newZ = coords.z;
			if (mirrorY != mirrorX) newZ *= -1;
			if (mirrorY) newZ += 180;
			newZ += 720;
			newZ = newZ % 360;
			
			return new Vector3(newX, newY, newZ);
		}

	}
}