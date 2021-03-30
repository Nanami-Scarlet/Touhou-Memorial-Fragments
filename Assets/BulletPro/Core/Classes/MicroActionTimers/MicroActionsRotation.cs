using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	public class MicroActionRotate : MicroActionGeneric<float>
	{
		public MicroActionRotate(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.self.localEulerAngles.z;
			endValue = startValue + inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.Rotate((endValue-startValue) * deltaTime / totalTime);
		}
	}

	public class MicroActionRotationSetGlobal : MicroActionGeneric<float>
	{
		public MicroActionRotationSetGlobal(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleMovement.GetGlobalRotation();
			endValue = (inputValue + 720) % 360;

			if (Mathf.Abs(startValue-endValue) > 180)
			{
				if (endValue > startValue) endValue -= 360;
				else endValue += 360;
			}			
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.SetGlobalRotation(Mathf.Lerp(startValue, endValue, GetRatio()));			
		}
	}

	public class MicroActionRotationSetLocal : MicroActionGeneric<float>
	{
		public MicroActionRotationSetLocal(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.self.localEulerAngles.z;
			endValue = (inputValue + 720) % 360;

			if (Mathf.Abs(startValue-endValue) > 180)
			{
				if (endValue > startValue) endValue -= 360;
				else endValue += 360;
			}
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.SetLocalRotation(Mathf.Lerp(startValue, endValue, GetRatio()));			
		}
	}
}