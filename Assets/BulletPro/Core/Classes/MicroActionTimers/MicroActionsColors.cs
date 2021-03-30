using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	public class MicroActionColorBase : MicroActionGeneric<Color>
	{
		public MicroActionColorBase(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, Color inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleRenderer.startColor;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleRenderer.startColor = Color.Lerp(startValue, endValue, GetRatio());
		}
	}

	public class MicroActionColorReplace : MicroActionColorBase
	{
		public MicroActionColorReplace(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, Color inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			endValue = inputValue;
		}
	}

	public class MicroActionColorAdd : MicroActionColorBase
	{
		public MicroActionColorAdd(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, Color inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			endValue = startValue + inputValue;
		}
	}

	public class MicroActionColorMultiply : MicroActionColorBase
	{
		public MicroActionColorMultiply(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, Color inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			endValue = startValue * inputValue;
		}
	}

	public class MicroActionColorOverlay : MicroActionColorBase
	{
		public MicroActionColorOverlay(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, Color inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			float finalAlpha = startValue.a + inputValue.a*(1-startValue.a);
			endValue = startValue * (1-inputValue.a) + inputValue * inputValue.a;
			endValue.a = finalAlpha;
		}
	}

	public class MicroActionGradientSet : MicroActionGeneric<Gradient>
	{
		public MicroActionGradientSet(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, Gradient inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			bullet.moduleRenderer.nextColorEvolution = inputValue;
			bullet.moduleRenderer.isSwitchingGradient = true;
			bullet.moduleRenderer.ratioToNextGradient = 0f;
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleRenderer.ratioToNextGradient = GetRatio();
			if (IsDone())
			{
				bullet.moduleRenderer.isSwitchingGradient = false;
				bullet.moduleRenderer.colorEvolution = bullet.moduleRenderer.nextColorEvolution;
			}
		}
	}

	public class MicroActionAlphaMultiply : MicroActionGeneric<float>
	{
		public MicroActionAlphaMultiply(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleRenderer.startColor.a;
			endValue = Mathf.Clamp01(startValue * inputValue);
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleRenderer.startColor += new Color(
				bullet.moduleRenderer.startColor.r,
				bullet.moduleRenderer.startColor.g,
				bullet.moduleRenderer.startColor.b,
				(endValue-startValue) * deltaTime / totalTime);
		}
	}

	public class MicroActionAlphaAdd : MicroActionGeneric<float>
	{
		public MicroActionAlphaAdd(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleRenderer.startColor.a;
			endValue = Mathf.Clamp01(startValue + inputValue);
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleRenderer.startColor += new Color(
				bullet.moduleRenderer.startColor.r,
				bullet.moduleRenderer.startColor.g,
				bullet.moduleRenderer.startColor.b,
				(endValue-startValue) * deltaTime / totalTime);
		}
	}

	public class MicroActionAlphaSet : MicroActionGeneric<float>
	{
		public MicroActionAlphaSet(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			startValue = bullet.moduleRenderer.startColor.a;
			endValue = Mathf.Clamp01(inputValue);
		}

		public override void UpdateParameter(float deltaTime)
		{
			bullet.moduleRenderer.startColor = new Color(
				bullet.moduleRenderer.startColor.r,
				bullet.moduleRenderer.startColor.g,
				bullet.moduleRenderer.startColor.b,
				(Mathf.Lerp(startValue, endValue, GetRatio())));
		}
	}
}