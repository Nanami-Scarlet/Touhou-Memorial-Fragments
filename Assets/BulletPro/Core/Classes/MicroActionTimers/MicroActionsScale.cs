using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	public class MicroActionScaleMultiply : MicroActionGeneric<float>
	{
		public MicroActionScaleMultiply(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleMovement.baseScale;
			endValue = startValue * inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.baseScale += (endValue-startValue) * deltaTime / totalTime;			
		}
	}

	public class MicroActionScaleSet : MicroActionGeneric<float>
	{
		public MicroActionScaleSet(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleMovement.baseScale;
			endValue = inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.baseScale = (Mathf.Lerp(startValue, endValue, GetRatio()));			
		}
	}
}