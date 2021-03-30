using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	public class MicroActionTurnToTarget : MicroActionGeneric<float>
	{
		public MicroActionTurnToTarget(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.self.localEulerAngles.z;
			endValue = startValue + bullet.moduleHoming.GetAngleToTarget(inputValue);
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.Rotate((endValue-startValue) * deltaTime / totalTime);
		}
	}

	public class MicroActionHomingSpeedMultiply : MicroActionGeneric<float>
	{
		public MicroActionHomingSpeedMultiply(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleHoming.homingAngularSpeed;
			endValue = startValue * inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleHoming.homingAngularSpeed += (endValue-startValue) * deltaTime / totalTime;			
		}
	}

	public class MicroActionHomingSpeedSet : MicroActionGeneric<float>
	{
		public MicroActionHomingSpeedSet(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleHoming.homingAngularSpeed;
			endValue = inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleHoming.homingAngularSpeed = (Mathf.Lerp(startValue, endValue, GetRatio()));			
		}
	}
}