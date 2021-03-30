using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	public class MicroActionSpeedMultiply : MicroActionGeneric<float>
	{
		public MicroActionSpeedMultiply(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleMovement.baseSpeed;
			endValue = startValue * inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.baseSpeed += (endValue-startValue) * deltaTime / totalTime;			
		}
	}

	public class MicroActionSpeedSet : MicroActionGeneric<float>
	{
		public MicroActionSpeedSet(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleMovement.baseSpeed;
			endValue = inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.baseSpeed = (Mathf.Lerp(startValue, endValue, GetRatio()));			
		}
	}

	public class MicroActionAngularSpeedMultiply : MicroActionGeneric<float>
	{
		public MicroActionAngularSpeedMultiply(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleMovement.baseAngularSpeed;
			endValue = startValue * inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.baseAngularSpeed += (endValue-startValue) * deltaTime / totalTime;			
		}
	}

	public class MicroActionAngularSpeedSet : MicroActionGeneric<float>
	{
		public MicroActionAngularSpeedSet(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleMovement.baseAngularSpeed;
			endValue = inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.baseAngularSpeed = (Mathf.Lerp(startValue, endValue, GetRatio()));			
		}
	}
}