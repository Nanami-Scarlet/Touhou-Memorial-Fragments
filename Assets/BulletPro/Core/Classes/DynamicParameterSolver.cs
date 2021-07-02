using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
    // A struct that stores result from looking at the settings in a DynamicParameter.
    public struct DynamicParameterSettingsResult
    {
        public DynamicParameterSorting sorting;
        // for from-to or blend
        public int indexOfFrom, indexOfTo, indexOfChosenBlendElement;
        public float interpolationValue;
    }

    public enum ParameterOwner { Bullet, Shot, Pattern, None }

    // Everything the bullet "remembers" from the shot and pattern that issued it. Used for dynamic parameter solving.
	public struct InheritedSpawnData
	{
        // For BulletHierarchy-based solving
		public Vector2 positionInShot; // from 0 to 1, (0,0) being the lower-left corner
		public float rotationAtSpawn; // in degrees
		public float shotIndexInPattern;
		public float shotTimeInPattern;
		public float patternIndexInEmitter;

        // For shared-random-based solving
		public float randomSeed;
        public int patternID, shotID, bulletID; // refers to EmissionParams.uniqueIndex
        public Dictionary<int, float> randomValues;

        // debug
        //public List<float> outputs;

		// Resets the struct with impossible values.
		public void Flush()
		{
			positionInShot = Vector2.one * -1f;
			rotationAtSpawn = 0f;
			shotIndexInPattern = -1f;
			shotTimeInPattern = -1f;
			patternIndexInEmitter = -1;

			randomSeed = -1f;
            patternID = 0;
            shotID = 0;
            bulletID = 0;
            randomValues.Clear();
		}

        // The solving hash is unique per type of calculation.
        // (example: period of color curve of a certain bullet fired by a certain shot and a certain pattern)
        // If the hash doesn't exist yet, we generate a pseudorandom value then store it. 
        public float Randomize(int solvingHash)
        {
            if (randomValues.ContainsKey(solvingHash)) return randomValues[solvingHash];
            
            // Calculating result based on hash :
            float result = randomSeed;
            for (int i=0; i < 23; i++) // just below the 24 bits float precision
            {
                if ((solvingHash & (1 << i)) != 0)
                    result += 1f / (float)(1 << i);
            }
            result = result % 1f;
            
            randomValues[solvingHash] = result;

            // debug
            //if (outputs == null) outputs = new List<float>();
            //outputs.Add(result);

            return result;
        }
	}

    // Passing pattern and shot information as an argument when a bullet is firing (as what it fires isn't contained in InheritedData)
    public struct TempEmissionData
    {
        public int patternID, shotID;
        public float shotTimeInPattern, shotIndexInPattern, patternIndexInEmitter;
    }

    // A struct that exists in every Bullet component to retrieve floats from DynamicFloats, strings from DynamicStrings, and so on.
    // It gets bullet info when it spawns, and uses it to calculate a dynamic parameter's interpoation value. 
    public class DynamicParameterSolver
    {
        public Bullet bullet;
        public InheritedSpawnData inheritedData;
        public TempEmissionData tempEmissionData;

        public void Awake()
        {
            inheritedData.randomValues = new Dictionary<int, float>();
        }

        // Initialization is made at bullet birth (just one line before bullet.ApplyBulletParams())

        // Called at bullet death
        public void Die()
        {
            inheritedData.Flush();
        }

        // Called every time a parameter is not fixed to retrieve an interpolation float (from 0 to 1) that decides which value to take.
        // OperationID is a fully random constant positive int (from 0 to 2^25) ensuring every operation is stored under a different hash.
        float SolveInterpolationValue(InterpolationValue val, int operationID, ParameterOwner owner = ParameterOwner.Bullet)
        {
            float result = 0;

            if (val.interpolationFactor == InterpolationFactor.Random)
            {
                if (!val.shareValueBetweenInstances) result = Random.value;
                else if (owner == ParameterOwner.None) result = Random.value;
                else if (bullet == null) result = Random.value;
                else // sharing random seed with a common parent
                {
                    // Regarding the owner : Shots and Patterns use the seed of their emitter bullet.
                    // A solving hash is generated, unique to be EmissionParam order of this hierarchy tree.
                    // XOR operations ensure its almost-uniqueness (2^24), homogeneity, and retrievability.
                    int relativeToEmitter = 0;
                    int solvingHash = operationID;
                    if (owner == ParameterOwner.Shot)
                    {
                        relativeToEmitter = -2;
                        solvingHash ^= tempEmissionData.shotID;
                        if (val.relativeTo > 0) solvingHash ^= tempEmissionData.patternID;
                        if (val.relativeTo == 1)
                        {
                            if (val.differentValuesPerShot)
                            {
                                // identifying the pattern : we pick some random number to avoid redundancy with multiples
                                int x = (int)tempEmissionData.shotIndexInPattern + 1;
                                solvingHash ^= 104729 * x; // a random high prime number
                            }
                        }
                    }
                    else if (owner == ParameterOwner.Pattern)
                    {
                        relativeToEmitter = -1;
                        solvingHash ^= tempEmissionData.patternID;
                    }
                    relativeToEmitter += val.relativeTo;
                    Bullet relevantBullet = bullet;
                    // if (relativeToEmitter < 0) : nothing more, keep same relevant bullet and randomize with already formed hash
                    
                    //float randomSeed = relevantBullet.dynamicSolver.inheritedData.randomSeed; // unused as of 2019-11-28

                    if (relativeToEmitter > -1)
                    {
                        // the bullet whose seed we'll use isn't necessarily the bullet whose inheritedData is relevant for the loop
                        Bullet seedOwner = relevantBullet;

                        while (relativeToEmitter > -1)
                        {
                            // checking a bullet
                            solvingHash ^= relevantBullet.dynamicSolver.inheritedData.bulletID;
                            if (relevantBullet.subEmitter)
                            {
                                seedOwner = relevantBullet.subEmitter;
                                //randomSeed = seedOwner.dynamicSolver.inheritedData.randomSeed; unused as of 2019-11-28
                            }
                            relativeToEmitter--;
                            if (relativeToEmitter < 0) break;

                            // checking a shot
                            solvingHash ^= relevantBullet.dynamicSolver.inheritedData.shotID;
                            relativeToEmitter--;
                            if (relativeToEmitter < 0)
                            {
                                // identifying the shot : using shot index to modify hash
                                int x = (int)relevantBullet.dynamicSolver.inheritedData.shotIndexInPattern + 1;
                                solvingHash ^= 104729 * x; // a random high prime number
                                break;
                            }

                            // checking a pattern
                            solvingHash ^= relevantBullet.dynamicSolver.inheritedData.patternID;
                            relativeToEmitter--;
                            if (relativeToEmitter < 0)
                            {
                                // identifying the pattern : we pick some random number to avoid redundancy with multiples
                                int x = (int)relevantBullet.dynamicSolver.inheritedData.patternIndexInEmitter + 1;
                                solvingHash ^= 104729 * x; // a random high prime number                                
                                break;
                            }

                            if (!relevantBullet.subEmitter) break;
                            else relevantBullet = relevantBullet.subEmitter;

                            // TODO (small perf improvement for later) : store seed and IDs of *all* hierarchy tree in the same inheritedData so that said bullet doesn't have to be still alive while generating this value
                        }

                        // store actual seed user into the old relevantBullet var
                        relevantBullet = seedOwner;
                    }
                    
                    result = relevantBullet.dynamicSolver.inheritedData.Randomize(solvingHash);
                }
            }

            else if (val.interpolationFactor == InterpolationFactor.GlobalParameter)
            {
                if (string.IsNullOrEmpty(val.parameterName)) result = 0;
                else result = BulletGlobalParamManager.instance.GetSlider01(val.parameterName);
            }
            
            else // meaning, if BulletHierarchy
            {
                if (owner == ParameterOwner.None) result = 0;
                else
                {
                    int relativeToEmitter = 0;
                    if (owner == ParameterOwner.Shot)
                        relativeToEmitter = -2;
                    else if (owner == ParameterOwner.Pattern)
                        relativeToEmitter = -1;
                    relativeToEmitter += val.relativeTo;
                    Bullet relevantBullet = bullet;
                    bool useTempData = relativeToEmitter < 0;

                    if (!useTempData)
                    {
                        // find out which bullet holds relevant data and whether we should check the bullet itself, or its shot, or pattern
                        while (relativeToEmitter > 2)
                        {
                            relativeToEmitter -= 3;
                            
                            if (!relevantBullet.subEmitter) break;
                            else relevantBullet = relevantBullet.subEmitter;
                        }

                        // given the object we should check, check it and retrieve the "result" value
                        if (relativeToEmitter % 3 == 0) // checking bullet
                        {
                            BulletInterpolationFactor bif = val.interpolationFactorFromBullet;
                            if (bif == BulletInterpolationFactor.CustomParameter)
                            {
                                if (string.IsNullOrEmpty(val.parameterName)) result = 0;
                                else result = relevantBullet.moduleParameters.GetFloat(val.parameterName);
                            }
                            else if (bif == BulletInterpolationFactor.TimeSinceAlive)
                                result = val.WrapValue(relevantBullet.timeSinceAlive);
                            else if (bif == BulletInterpolationFactor.Rotation)
                            {
                                result = relevantBullet.dynamicSolver.inheritedData.rotationAtSpawn;
                                result = ((result + 360f - val.wrapPoint) % 360f) * 0.0027777778f; // equals 1/360
                                if (val.countClockwise) result = 1f - result;
                            }
                            else // position in shot
                            {
                                if (val.sortMode == BulletPositionSortMode.Horizontal) result = relevantBullet.dynamicSolver.inheritedData.positionInShot.x;
                                else if (val.sortMode == BulletPositionSortMode.Vertical) result = relevantBullet.dynamicSolver.inheritedData.positionInShot.y;
                                else if (val.sortMode == BulletPositionSortMode.Radial)
                                {
                                    Vector2 pos = relevantBullet.dynamicSolver.inheritedData.positionInShot;
                                    float dist = Vector2.Distance(pos, new Vector2(val.centerX, val.centerY));
                                    result = dist / val.radius;
                                }
                                else // texture
                                {
                                    Vector2 pos = relevantBullet.dynamicSolver.inheritedData.positionInShot;
                                    float w = (float)val.repartitionTexture.width;
                                    float h = (float)val.repartitionTexture.height;
                                    int x = (int)(pos.x * w);
                                    int y = (int)(pos.y * h);
                                    result = val.repartitionTexture.GetPixel(x, y).grayscale;
                                }

                                if (val.sortDirection == BulletSortDirection.Descending)
                                    result = 1f - result;
                            }
                        }
                        else if (relativeToEmitter % 3 == 1) // checking shot
                        {
                            if (val.interpolationFactorFromShot == ShotInterpolationFactor.SpawnOrder)
                                result = val.WrapValue(relevantBullet.dynamicSolver.inheritedData.shotIndexInPattern);
                            else result = val.WrapValue(relevantBullet.dynamicSolver.inheritedData.shotTimeInPattern);
                        }
                        else // checking pattern
                        {
                            if (val.interpolationFactorFromPattern == PatternInterpolationFactor.PatternIndexInEmitter)
                                result = val.WrapValue(relevantBullet.dynamicSolver.inheritedData.patternIndexInEmitter);
                            else if (val.interpolationFactorFromPattern == PatternInterpolationFactor.TotalShotsFired)
                            {
                                int patternIndex = (int)relevantBullet.dynamicSolver.inheritedData.patternIndexInEmitter;
                                float shots = 0;
                                if (relevantBullet.modulePatterns.patternRuntimeInfo != null)
                                    shots = relevantBullet.modulePatterns.patternRuntimeInfo[patternIndex].shotsShotSinceLive;
                                result = val.WrapValue(shots);
                            }
                            else if (val.interpolationFactorFromPattern == PatternInterpolationFactor.TimePlayed)
                            {
                                int patternIndex = (int)relevantBullet.dynamicSolver.inheritedData.patternIndexInEmitter;
                                float time = 0;
                                if (relevantBullet.modulePatterns.patternRuntimeInfo != null)
                                    time = relevantBullet.modulePatterns.patternRuntimeInfo[patternIndex].timeSinceLive;
                                result = val.WrapValue(time);
                            }
                        }
                    }
                    else
                    {
                        if (relativeToEmitter == -2) // shot relative to itself
                        {
                            if (val.interpolationFactorFromShot == ShotInterpolationFactor.SpawnOrder)
                                result = val.WrapValue(tempEmissionData.shotIndexInPattern);
                            else result = val.WrapValue(tempEmissionData.shotTimeInPattern);
                        }
                        else // shot relative to pattern, or pattern relative to itself
                        {
                            if (val.interpolationFactorFromPattern == PatternInterpolationFactor.PatternIndexInEmitter)
                                result = val.WrapValue(tempEmissionData.patternIndexInEmitter);
                            else if (val.interpolationFactorFromPattern == PatternInterpolationFactor.TotalShotsFired)
                            {
                                int patternIndex = (int)inheritedData.patternIndexInEmitter;
                                float shots = 0;
                                if (bullet.modulePatterns.patternRuntimeInfo != null)
                                    shots = bullet.modulePatterns.patternRuntimeInfo[patternIndex].shotsShotSinceLive;
                                result = val.WrapValue(shots);
                            }
                            else if (val.interpolationFactorFromPattern == PatternInterpolationFactor.TimePlayed)
                            {
                                int patternIndex = (int)inheritedData.patternIndexInEmitter;
                                float time = 0;
                                if (bullet.modulePatterns.patternRuntimeInfo != null)
                                    time = bullet.modulePatterns.patternRuntimeInfo[patternIndex].timeSinceLive;
                                result = val.WrapValue(time);
                            }
                        }
                    }
                }
            }

            return val.repartitionCurve.Evaluate(result);
        }

        // Looks at settings and checks whether it must return a from-to or blended value.
        DynamicParameterSettingsResult SolveSettings(IDynamicParameter dynParameter, DynamicParameterSettings settings, int operationID, ParameterOwner owner)
        {
            DynamicParameterSettingsResult result = new DynamicParameterSettingsResult();
            result.sorting = settings.valueType;

            float interpVal = SolveInterpolationValue(settings.interpolationValue, operationID, owner);
            result.interpolationValue = interpVal;

            if (settings.valueType == DynamicParameterSorting.FromGradient)
                return result;

            if (settings.valueType == DynamicParameterSorting.FromAToB)
            {
                result.indexOfFrom = settings.indexOfFrom;
                result.indexOfTo = settings.indexOfTo;
                return result; 
            }

            // the rest implies that val.settings.valueType == DynamicParameterSorting.Blend
            
            // error handling for incomplete lists
            bool abort = false;
            if (settings.indexOfBlendedChildren == null) abort = true;
            else if (settings.indexOfBlendedChildren.Length == 0) abort = true;
            if (abort)
            {
                result.sorting = DynamicParameterSorting.Fixed;
                return result;
            }
            
            // shortcuts for simpler values that don't require the whole calcs
            if (settings.indexOfBlendedChildren.Length == 1 || interpVal == 0f)
            {
                result.indexOfChosenBlendElement = settings.indexOfBlendedChildren[0];
                return result;
            }
            if (interpVal == 1f)
            {
                result.indexOfChosenBlendElement = settings.indexOfBlendedChildren[settings.indexOfBlendedChildren.Length-1];
                return result;
            }

            // weight calculation and return value
            float totalWeights = 0;
            for (int i = 0; i < settings.indexOfBlendedChildren.Length; i++)
                totalWeights += dynParameter.GetSettingsInTree(settings.indexOfBlendedChildren[i]).weight;
            totalWeights = 1 / totalWeights; // inverting now to save divisions later
            float currentSum = 0;
            for (int i = 0; i < settings.indexOfBlendedChildren.Length; i++)
            {
                currentSum += dynParameter.GetSettingsInTree(settings.indexOfBlendedChildren[i]).weight;
                if (currentSum * totalWeights < interpVal) continue;
                result.indexOfChosenBlendElement = settings.indexOfBlendedChildren[i];
                return result;
            }

            // if somehow that did not reach total weight (but it should), fallback to the last value
            result.indexOfChosenBlendElement = settings.indexOfBlendedChildren[settings.indexOfBlendedChildren.Length-1];
            return result;
        }

        #region one solver function per type (supporting from-to)

        public float SolveDynamicFloat(DynamicFloat dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetFloat(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetFloat(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (if set back to fixed because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // from-to
            else if (settingsSolved.sorting == DynamicParameterSorting.FromAToB)
            {
                var fromVal = SolveDynamicFloat(dynParameter, operationID, owner, settingsSolved.indexOfFrom);
                var toVal = SolveDynamicFloat(dynParameter, operationID, owner, settingsSolved.indexOfTo);
                return Mathf.Lerp(fromVal, toVal, settingsSolved.interpolationValue);
            }

            // blend
            else return SolveDynamicFloat(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public float SolveDynamicSlider01(DynamicSlider01 dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetSlider01(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetSlider01(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // from-to
            else if (settingsSolved.sorting == DynamicParameterSorting.FromAToB)
            {
                var fromVal = SolveDynamicSlider01(dynParameter, operationID, owner, settingsSolved.indexOfFrom);
                var toVal = SolveDynamicSlider01(dynParameter, operationID, owner, settingsSolved.indexOfTo);
                return Mathf.Lerp(fromVal, toVal, settingsSolved.interpolationValue);
            }

            // blend
            else return SolveDynamicSlider01(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public int SolveDynamicInt(DynamicInt dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetInt(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetInt(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // from-to
            else if (settingsSolved.sorting == DynamicParameterSorting.FromAToB)
            {
                var fromVal = SolveDynamicInt(dynParameter, operationID, owner, settingsSolved.indexOfFrom);
                var toVal = SolveDynamicInt(dynParameter, operationID, owner, settingsSolved.indexOfTo);
                return Mathf.RoundToInt(Mathf.Lerp((float)fromVal, (float)toVal, settingsSolved.interpolationValue));
            }

            // blend
            else return SolveDynamicInt(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public Color SolveDynamicColor(DynamicColor dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetColor(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetColor(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // from-to
            else if (settingsSolved.sorting == DynamicParameterSorting.FromAToB)
            {
                var fromVal = SolveDynamicColor(dynParameter, operationID, owner, settingsSolved.indexOfFrom);
                var toVal = SolveDynamicColor(dynParameter, operationID, owner, settingsSolved.indexOfTo);
                return Color.Lerp(fromVal, toVal, settingsSolved.interpolationValue);
            }

            // from gradient
            else if (settingsSolved.sorting == DynamicParameterSorting.FromGradient)
                return val.gradientValue.Evaluate(settingsSolved.interpolationValue);

            // blend
            else return SolveDynamicColor(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public Vector2 SolveDynamicVector2(DynamicVector2 dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetVector2(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetVector2(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // from-to
            else if (settingsSolved.sorting == DynamicParameterSorting.FromAToB)
            {
                var fromVal = SolveDynamicVector2(dynParameter, operationID, owner, settingsSolved.indexOfFrom);
                var toVal = SolveDynamicVector2(dynParameter, operationID, owner, settingsSolved.indexOfTo);
                return Vector2.Lerp(fromVal, toVal, settingsSolved.interpolationValue);
            }

            // blend
            else return SolveDynamicVector2(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public Vector3 SolveDynamicVector3(DynamicVector3 dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetVector3(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetVector3(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // from-to
            else if (settingsSolved.sorting == DynamicParameterSorting.FromAToB)
            {
                var fromVal = SolveDynamicVector3(dynParameter, operationID, owner, settingsSolved.indexOfFrom);
                var toVal = SolveDynamicVector3(dynParameter, operationID, owner, settingsSolved.indexOfTo);
                return Vector3.Lerp(fromVal, toVal, settingsSolved.interpolationValue);
            }

            // blend
            else return SolveDynamicVector3(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public Vector4 SolveDynamicVector4(DynamicVector4 dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetVector4(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetVector4(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // from-to
            else if (settingsSolved.sorting == DynamicParameterSorting.FromAToB)
            {
                var fromVal = SolveDynamicVector4(dynParameter, operationID, owner, settingsSolved.indexOfFrom);
                var toVal = SolveDynamicVector4(dynParameter, operationID, owner, settingsSolved.indexOfTo);
                return Vector4.Lerp(fromVal, toVal, settingsSolved.interpolationValue);
            }

            // blend
            else return SolveDynamicVector4(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        #endregion
        
        #region one solver function per type (not supporting from-to)
        
        public bool SolveDynamicBool(DynamicBool dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetBool(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetBool(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // blend
            else return SolveDynamicBool(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public int SolveDynamicEnum(DynamicEnum dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetInt(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetInt(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // blend
            else return SolveDynamicEnum(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public string SolveDynamicString(DynamicString dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetString(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetString(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // blend
            else return SolveDynamicString(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public AnimationCurve SolveDynamicAnimationCurve(DynamicAnimationCurve dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetAnimationCurve(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetAnimationCurve(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // blend
            else return SolveDynamicAnimationCurve(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public Gradient SolveDynamicGradient(DynamicGradient dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetGradient(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetGradient(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // blend
            else return SolveDynamicGradient(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public Rect SolveDynamicRect(DynamicRect dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetRect(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetRect(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // blend
            else return SolveDynamicRect(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public Object SolveDynamicObjectReference(DynamicObjectReference dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // equal to param
            if (val.settings.valueType == DynamicParameterSorting.EqualToParameter)
            {
                UserMadeParameterType paramType = val.settings.parameterType;

                // global param
                if (paramType == UserMadeParameterType.GlobalParameter)
                    return BulletGlobalParamManager.instance.GetObjectReference(val.settings.parameterName);

                // bullet hierarchy
                Bullet paramHolder = bullet;
                int parents = val.settings.relativeTo;
                while (parents > 0)
                {
                    paramHolder = paramHolder.subEmitter;
                    // in case of an error (hierarchy element missing), fallback to fixed value
                    if (paramHolder == null) return val.defaultValue;
                    parents--;
                }
                return paramHolder.moduleParameters.GetObjectReference(val.settings.parameterName);
            }
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // blend
            else return SolveDynamicObjectReference(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public BulletParams SolveDynamicBullet(DynamicBullet dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // can't be equal to param
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // blend
            else return SolveDynamicBullet(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public ShotParams SolveDynamicShot(DynamicShot dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // can't be equal to param
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // blend
            else return SolveDynamicShot(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        public PatternParams SolveDynamicPattern(DynamicPattern dynParameter, int operationID, ParameterOwner owner, int valueIndexInTree = 1)
        {
            var val = dynParameter[valueIndexInTree];
            
            // fixed
            if (val.settings.valueType == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // can't be equal to param
            
            DynamicParameterSettingsResult settingsSolved = SolveSettings(dynParameter, val.settings, operationID, owner);

            // fixed (because of an error)
            if (settingsSolved.sorting == DynamicParameterSorting.Fixed)
                return val.defaultValue;

            // blend
            else return SolveDynamicPattern(dynParameter, operationID, owner, settingsSolved.indexOfChosenBlendElement);
        }

        #endregion

        #region toolbox, composite types

        public BulletCurve SolveDynamicBulletCurve(DynamicBulletCurve dynCurve, int operationID, ParameterOwner owner)
        {
            BulletCurve result = new BulletCurve();
            result.enabled = dynCurve.enabled;
            result.wrapMode = (WrapMode)SolveDynamicEnum(dynCurve.wrapMode, 19526852 ^ operationID, owner);
            result.periodIsLifespan = SolveDynamicBool(dynCurve.periodIsLifespan, 12566916 ^ operationID, owner);
            result.period = SolveDynamicFloat(dynCurve.period, 26726495 ^ operationID, owner);
            result.curve = SolveDynamicAnimationCurve(dynCurve.curve, 18208695 ^ operationID, owner);

            return result;
        }

        public SolvedPatternParams SolvePatternParams(PatternParams pp) // operation ID not needed since it's only done once and dispatches IDs later
        {
            SolvedPatternParams result = new SolvedPatternParams();
    		
            if (!pp)
            {
                result.containsNullPattern = true;
                return result;
            }

            if (pp.patternTags == null) result.patternTags = new string[0];
            else
            {
                result.patternTags = new string[pp.patternTags.Length];
                if (result.patternTags.Length > 0)
                    for (int i = 0; i < result.patternTags.Length; i++)
                        result.patternTags[i] = SolveDynamicString(pp.patternTags[i], 4788713 * i, ParameterOwner.Pattern);
            }
            result.playAtStart = pp.playAtBulletBirth;

            if (pp.instructionLists == null) return result;
            if (pp.instructionLists.Length == 0) return result;

            result.instructionLists = new SolvedInstructionList[pp.instructionLists.Length];
            for (int i = 0; i < pp.instructionLists.Length; i++)
            {
                SolvedInstructionList sil = new SolvedInstructionList();

                PatternInstruction[] pil = pp.instructionLists[i].instructions;
                if (pil == null)
                {
                    result.instructionLists[i] = sil;
                    continue;
                }
                sil.instructions = new SolvedInstruction[pil.Length];
                if (pil.Length > 0)
                    for (int j = 0; j < pil.Length; j++)
                        sil.instructions[j] = SolvePatternInstruction(pil[j], j);

                result.instructionLists[i] = sil;
            }

            return result;            
        }

        public SolvedInstruction SolvePatternInstruction(PatternInstruction pi, int operationIDMultiplier)
        {
            SolvedInstruction result = new SolvedInstruction();

            // preparing the rerolls, if any
            int totalChannels = 4;
            result.rerollFrequency = new RerollFrequency[totalChannels];
            result.rerollLoopDepth = new int[totalChannels];
            result.useComplexRerollSequence = new bool[totalChannels];
		    result.checkEveryNLoops = new int[totalChannels];
		    result.loopSequence = new int[totalChannels];

            DynamicParameterSettings[] settingsToCheck = new DynamicParameterSettings[totalChannels];

            for (int i = 0; i < totalChannels; i++)
            {
                result.rerollFrequency[i] = RerollFrequency.OnlyOncePerPattern;
                settingsToCheck[i] = pi.waitTime[1].settings; // dummy values
            }

            PatternInstructionType pit = pi.instructionType;
            ParameterOwner owner = ParameterOwner.Pattern;

            // will tell number of rerollable dynamic params in this instruction
            int lastActiveChannel = 0;
            if (pi.canBeDoneOverTime && pi.instructionTiming == InstructionTiming.Progressively)
            {
                result.instructionDuration = SolveDynamicFloat(pi.instructionDuration, (int)pit + 48723 * operationIDMultiplier, owner);
                settingsToCheck[1] = pi.instructionDuration[1].settings;
                
                result.operationCurve = SolveDynamicAnimationCurve(pi.operationCurve, (int)pit + 77491 * operationIDMultiplier, owner);
                settingsToCheck[2] = pi.operationCurve[1].settings;

                lastActiveChannel = 2;
            }

            result.enabled = pi.enabled;
            result.instructionType = pit;
            result.endless = pi.endless;
            result.curveAffected = pi.curveAffected;
            result.instructionTiming = pi.instructionTiming;

            // Main
            if (pit == PatternInstructionType.Shoot)
            {
                result.shot = SolveDynamicShot(pi.shot, 125107 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.shot[1].settings;
            }
            else if (pit == PatternInstructionType.Wait)
            {
                result.waitTime = SolveDynamicFloat(pi.waitTime, 121546 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.waitTime[1].settings;
            }
            else if (pit == PatternInstructionType.BeginLoop)
            {
                result.iterations = SolveDynamicInt(pi.iterations, 11599 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.iterations[1].settings;
            }
            else if (pit == PatternInstructionType.PlayAudio)
            {
                result.audioClip = SolveDynamicObjectReference(pi.audioClip, 116146 * operationIDMultiplier, owner) as AudioClip;
                settingsToCheck[0] = pi.audioClip[1].settings;
            }
            else if (pit == PatternInstructionType.PlayVFX)
            {
                result.vfxPlayType = pi.vfxPlayType;
                result.vfxToPlay = SolveDynamicObjectReference(pi.vfxToPlay, 93741 * operationIDMultiplier, owner) as ParticleSystem;
                settingsToCheck[0] = pi.vfxToPlay[1].settings;
            }

            // Transform things: translate, rotate, or speed
            else if (pit == PatternInstructionType.TranslateGlobal
                || pit == PatternInstructionType.SetWorldPosition)
            {
                result.globalMovement = SolveDynamicVector2(pi.globalMovement, (int)pit + 54252 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.globalMovement[1].settings;
            }
            else if (pit == PatternInstructionType.TranslateLocal
                || pit == PatternInstructionType.SetLocalPosition)
            {
                result.localMovement = SolveDynamicVector2(pi.localMovement, (int)pit + 38553 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.localMovement[1].settings;
            }
            else if (pit == PatternInstructionType.Rotate
                || pit == PatternInstructionType.SetWorldRotation
                || pit == PatternInstructionType.SetLocalRotation)
            {
                result.rotation = SolveDynamicFloat(pi.rotation, (int)pit + 17379 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.rotation[1].settings;
            }
            else if (pit == PatternInstructionType.SetSpeed
                || pit == PatternInstructionType.SetAngularSpeed
                || pit == PatternInstructionType.SetHomingSpeed)
            {
                result.speedValue = SolveDynamicFloat(pi.speedValue, (int)pit + 74121 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.speedValue[1].settings;
            }
            else if (pit == PatternInstructionType.SetScale)
            {
                result.scaleValue = SolveDynamicFloat(pi.scaleValue, 167982 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.scaleValue[1].settings;
            }
            else if (pit == PatternInstructionType.TurnToTarget)
            {
                result.turnIntensity = SolveDynamicFloat(pi.turnIntensity, 33413 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.turnIntensity[1].settings;
            }

            // Homing stuff, and tags
            else if (pit == PatternInstructionType.ChangeTarget)
            {
                result.preferredTarget = (PreferredTarget)SolveDynamicEnum(pi.preferredTarget, 65847 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.preferredTarget[1].settings;
            }
            else if (pit == PatternInstructionType.ChangeHomingTag
                || pit == PatternInstructionType.ChangeCollisionTag)
            {
                result.collisionTagAction = pi.collisionTagAction;
                result.collisionTag = SolveDynamicString(pi.collisionTag, 91143 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.collisionTag[1].settings;
            }
            else if (pit == PatternInstructionType.PlayPattern
                || pit == PatternInstructionType.PausePattern
                || pit == PatternInstructionType.StopPattern
                || pit == PatternInstructionType.RebootPattern)
            {
                result.patternControlTarget = pi.patternControlTarget;
                result.patternTag = SolveDynamicString(pi.patternTag, 84792 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.patternTag[1].settings;
            }

            // Curves
            else if (pit == PatternInstructionType.SetCurveValue)
            {
                result.newCurveValue = SolveDynamicAnimationCurve(pi.newCurveValue, 56857 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.newCurveValue[1].settings;
            }
            else if (pit == PatternInstructionType.SetWrapMode)
            {
                result.newWrapMode = (WrapMode)SolveDynamicEnum(pi.newWrapMode, 48772 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.newWrapMode[1].settings;
            }
            else if (pit == PatternInstructionType.SetPeriod)
            {
                result.newPeriodType = pi.newPeriodType;
                result.newPeriodValue = SolveDynamicFloat(pi.newPeriodValue, 47711 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.newPeriodValue[1].settings;
            }
            else if (pit == PatternInstructionType.SetCurveRawTime)
            {
                result.curveRawTime = SolveDynamicFloat(pi.curveRawTime, 71193 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.curveRawTime[1].settings;
            }
            else if (pit == PatternInstructionType.SetCurveRatio)
            {
                result.curveTime = SolveDynamicSlider01(pi.curveTime, 101956 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.curveTime[1].settings;
            }

            // Colors
            else if (pit == PatternInstructionType.SetColor 
                || pit == PatternInstructionType.AddColor
                || pit == PatternInstructionType.MultiplyColor
                || pit == PatternInstructionType.OverlayColor)
            {
                result.color = SolveDynamicColor(pi.color, (int)pit + 28712 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.color[1].settings;
            }
            else if (pit == PatternInstructionType.SetLifetimeGradient)
            {
                result.gradient = SolveDynamicGradient(pi.gradient, 85479 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.gradient[1].settings;
            }
            else if (pit == PatternInstructionType.SetAlpha)
            {
                result.alpha = SolveDynamicSlider01(pi.alpha, 45777 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.alpha[1].settings;                
            }
            else if (pit == PatternInstructionType.AddAlpha)
            {
                result.alpha = SolveDynamicSlider01(pi.alpha, 83033 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.alpha[1].settings;                
            }

            // Multiplications
            else if (pit == PatternInstructionType.MultiplySpeed 
                || pit == PatternInstructionType.MultiplyAngularSpeed
                || pit == PatternInstructionType.MultiplyHomingSpeed
                || pit == PatternInstructionType.MultiplyAlpha
                || pit == PatternInstructionType.MultiplyPeriod
                || pit == PatternInstructionType.MultiplyScale)
            {
                result.factor = SolveDynamicFloat(pi.factor, (int)pit + 48979 * operationIDMultiplier, owner);
                settingsToCheck[0] = pi.factor[1].settings;   
            }

            // Reroll settings
            for (int i = 0; i < lastActiveChannel+1; i++)
            {
                if (settingsToCheck[i].valueType != DynamicParameterSorting.Fixed)
                {
                    result.rerollFrequency[i] = settingsToCheck[i].interpolationValue.rerollFrequency;
                    
                    // loop depth
                    if (result.rerollFrequency[i] == RerollFrequency.AtCertainLoops)
                    {
                        result.rerollLoopDepth[i] = settingsToCheck[i].interpolationValue.loopDepth;
                        result.useComplexRerollSequence[i] = settingsToCheck[i].interpolationValue.useComplexRerollSequence;
                        result.checkEveryNLoops[i] = settingsToCheck[i].interpolationValue.checkEveryNLoops;
                        result.loopSequence[i] = settingsToCheck[i].interpolationValue.loopSequence;
                    }
                }
                else result.rerollFrequency[i] = RerollFrequency.OnlyOncePerPattern;
            }

            return result;
        }

        #endregion
    }
}