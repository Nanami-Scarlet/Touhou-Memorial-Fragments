using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	public class MicroActionTranslateGlobal : MicroActionGeneric<Vector2>
	{
		public MicroActionTranslateGlobal(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, Vector2 inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.self.position;
			endValue = startValue + inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.Translate((endValue-startValue) * deltaTime / totalTime, Space.World);
		}
	}

	public class MicroActionTranslateLocal : MicroActionGeneric<Vector2>
	{
		public MicroActionTranslateLocal(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, Vector2 inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.self.localPosition;
			endValue = startValue + inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.Translate((endValue-startValue) * deltaTime / totalTime, Space.Self);
		}
	}

	public class MicroActionPositionSetGlobal : MicroActionGeneric<Vector2>
	{
		public MicroActionPositionSetGlobal(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, Vector2 inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleMovement.GetGlobalPosition();
			endValue = inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.SetGlobalPosition(Vector2.Lerp(startValue, endValue, GetRatio()));			
		}
	}

	public class MicroActionPositionSetLocal : MicroActionGeneric<Vector2>
	{
		public MicroActionPositionSetLocal(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, Vector2 inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.self.localPosition;
			endValue = inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleMovement.SetLocalPosition(Vector2.Lerp(startValue, endValue, GetRatio()));			
		}
	}
}