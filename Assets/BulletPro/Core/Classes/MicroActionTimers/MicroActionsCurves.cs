using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	public class MicroActionCurvePeriodSet : MicroActionGeneric<float>
	{
		public MicroActionCurvePeriodSet(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			if (involvedCurve == PatternCurveType.Alpha)
			{
				if (bullet.moduleRenderer.alphaOverLifetime.periodIsLifespan)
				{
					bullet.moduleRenderer.alphaOverLifetime.periodIsLifespan = false;
					bullet.moduleRenderer.alphaOverLifetime.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleRenderer.alphaOverLifetime.period;
			}
			else if (involvedCurve == PatternCurveType.Color)
			{
				if (bullet.moduleRenderer.colorOverLifetime.periodIsLifespan)
				{
					bullet.moduleRenderer.colorOverLifetime.periodIsLifespan = false;
					bullet.moduleRenderer.colorOverLifetime.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleRenderer.colorOverLifetime.period;
			}
			else if (involvedCurve == PatternCurveType.Homing)
			{
				if (bullet.moduleHoming.homingOverLifetime.periodIsLifespan)
				{
					bullet.moduleHoming.homingOverLifetime.periodIsLifespan = false;
					bullet.moduleHoming.homingOverLifetime.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleHoming.homingOverLifetime.period;
			}
			else if (involvedCurve == PatternCurveType.Speed)
			{
				if (bullet.moduleMovement.speedOverLifetime.periodIsLifespan)
				{
					bullet.moduleMovement.speedOverLifetime.periodIsLifespan = false;
					bullet.moduleMovement.speedOverLifetime.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.speedOverLifetime.period;
			}
			else if (involvedCurve == PatternCurveType.AngularSpeed)
			{
				if (bullet.moduleMovement.angularSpeedOverLifetime.periodIsLifespan)
				{
					bullet.moduleMovement.angularSpeedOverLifetime.periodIsLifespan = false;
					bullet.moduleMovement.angularSpeedOverLifetime.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.angularSpeedOverLifetime.period;
			}
			else if (involvedCurve == PatternCurveType.Scale)
			{
				if (bullet.moduleMovement.scaleOverLifetime.periodIsLifespan)
				{
					bullet.moduleMovement.scaleOverLifetime.periodIsLifespan = false;
					bullet.moduleMovement.scaleOverLifetime.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.scaleOverLifetime.period;
			}
			else if (involvedCurve == PatternCurveType.AnimX)
			{
				if (bullet.moduleMovement.moveXFromAnim.periodIsLifespan)
				{
					bullet.moduleMovement.moveXFromAnim.periodIsLifespan = false;
					bullet.moduleMovement.moveXFromAnim.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.moveXFromAnim.period;
			}
			else if (involvedCurve == PatternCurveType.AnimY)
			{
				if (bullet.moduleMovement.moveYFromAnim.periodIsLifespan)
				{
					bullet.moduleMovement.moveYFromAnim.periodIsLifespan = false;
					bullet.moduleMovement.moveYFromAnim.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.moveYFromAnim.period;
			}
			else if (involvedCurve == PatternCurveType.AnimAngle)
			{
				if (bullet.moduleMovement.rotateFromAnim.periodIsLifespan)
				{
					bullet.moduleMovement.rotateFromAnim.periodIsLifespan = false;
					bullet.moduleMovement.rotateFromAnim.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.rotateFromAnim.period;
			}
			else if (involvedCurve == PatternCurveType.AnimScale)
			{
				if (bullet.moduleMovement.scaleFromAnim.periodIsLifespan)
				{
					bullet.moduleMovement.scaleFromAnim.periodIsLifespan = false;
					bullet.moduleMovement.scaleFromAnim.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.scaleFromAnim.period;
			}

			endValue = inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			float newPeriod = (Mathf.Lerp(startValue, endValue, GetRatio()));

			if (involvedCurve == PatternCurveType.Alpha)
				bullet.moduleRenderer.alphaOverLifetime.period = newPeriod;
			else if (involvedCurve == PatternCurveType.Color)
				bullet.moduleRenderer.colorOverLifetime.period = newPeriod;
			else if (involvedCurve == PatternCurveType.Homing)
				bullet.moduleHoming.homingOverLifetime.period = newPeriod;
			else if (involvedCurve == PatternCurveType.Speed)
				bullet.moduleMovement.speedOverLifetime.period = newPeriod;
			else if (involvedCurve == PatternCurveType.AngularSpeed)				
				bullet.moduleMovement.angularSpeedOverLifetime.period = newPeriod;
			else if (involvedCurve == PatternCurveType.Scale)				
				bullet.moduleMovement.scaleOverLifetime.period = newPeriod;
			else if (involvedCurve == PatternCurveType.AnimX)				
				bullet.moduleMovement.moveXFromAnim.period = newPeriod;
			else if (involvedCurve == PatternCurveType.AnimY)				
				bullet.moduleMovement.moveYFromAnim.period = newPeriod;
			else if (involvedCurve == PatternCurveType.AnimAngle)				
				bullet.moduleMovement.rotateFromAnim.period = newPeriod;
			else if (involvedCurve == PatternCurveType.AnimScale)
				bullet.moduleMovement.scaleFromAnim.period = newPeriod;
		}
	}

	public class MicroActionCurvePeriodMultiply : MicroActionGeneric<float>
	{
		public MicroActionCurvePeriodMultiply(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			if (involvedCurve == PatternCurveType.Alpha)
			{
				if (bullet.moduleRenderer.alphaOverLifetime.periodIsLifespan)
				{
					bullet.moduleRenderer.alphaOverLifetime.periodIsLifespan = false;
					bullet.moduleRenderer.alphaOverLifetime.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleRenderer.alphaOverLifetime.period;
			}
			else if (involvedCurve == PatternCurveType.Color)
			{
				if (bullet.moduleRenderer.colorOverLifetime.periodIsLifespan)
				{
					bullet.moduleRenderer.colorOverLifetime.periodIsLifespan = false;
					bullet.moduleRenderer.colorOverLifetime.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleRenderer.colorOverLifetime.period;
			}
			else if (involvedCurve == PatternCurveType.Homing)
			{
				if (bullet.moduleHoming.homingOverLifetime.periodIsLifespan)
				{
					bullet.moduleHoming.homingOverLifetime.periodIsLifespan = false;
					bullet.moduleHoming.homingOverLifetime.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleHoming.homingOverLifetime.period;
			}
			else if (involvedCurve == PatternCurveType.Speed)
			{
				if (bullet.moduleMovement.speedOverLifetime.periodIsLifespan)
				{
					bullet.moduleMovement.speedOverLifetime.periodIsLifespan = false;
					bullet.moduleMovement.speedOverLifetime.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.speedOverLifetime.period;
			}
			else if (involvedCurve == PatternCurveType.AngularSpeed)
			{
				if (bullet.moduleMovement.angularSpeedOverLifetime.periodIsLifespan)
				{
					bullet.moduleMovement.angularSpeedOverLifetime.periodIsLifespan = false;
					bullet.moduleMovement.angularSpeedOverLifetime.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.angularSpeedOverLifetime.period;
			}
			else if (involvedCurve == PatternCurveType.Scale)
			{
				if (bullet.moduleMovement.scaleOverLifetime.periodIsLifespan)
				{
					bullet.moduleMovement.scaleOverLifetime.periodIsLifespan = false;
					bullet.moduleMovement.scaleOverLifetime.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.scaleOverLifetime.period;
			}
			else if (involvedCurve == PatternCurveType.AnimX)
			{
				if (bullet.moduleMovement.moveXFromAnim.periodIsLifespan)
				{
					bullet.moduleMovement.moveXFromAnim.periodIsLifespan = false;
					bullet.moduleMovement.moveXFromAnim.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.moveXFromAnim.period;
			}
			else if (involvedCurve == PatternCurveType.AnimY)
			{
				if (bullet.moduleMovement.moveYFromAnim.periodIsLifespan)
				{
					bullet.moduleMovement.moveYFromAnim.periodIsLifespan = false;
					bullet.moduleMovement.moveYFromAnim.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.moveYFromAnim.period;
			}
			else if (involvedCurve == PatternCurveType.AnimAngle)
			{
				if (bullet.moduleMovement.rotateFromAnim.periodIsLifespan)
				{
					bullet.moduleMovement.rotateFromAnim.periodIsLifespan = false;
					bullet.moduleMovement.rotateFromAnim.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.rotateFromAnim.period;
			}
			else if (involvedCurve == PatternCurveType.AnimScale)
			{
				if (bullet.moduleMovement.scaleFromAnim.periodIsLifespan)
				{
					bullet.moduleMovement.scaleFromAnim.periodIsLifespan = false;
					bullet.moduleMovement.scaleFromAnim.period = bullet.moduleLifespan.lifespan;
				}
				startValue = bullet.moduleMovement.scaleFromAnim.period;
			}

			endValue = inputValue * startValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			float periodGain = (endValue - startValue) * deltaTime / totalTime;

			if (involvedCurve == PatternCurveType.Alpha)
				bullet.moduleRenderer.alphaOverLifetime.period += periodGain;
			else if (involvedCurve == PatternCurveType.Color)
				bullet.moduleRenderer.colorOverLifetime.period += periodGain;				
			else if (involvedCurve == PatternCurveType.Homing)
				bullet.moduleHoming.homingOverLifetime.period += periodGain;				
			else if (involvedCurve == PatternCurveType.Speed)
				bullet.moduleMovement.speedOverLifetime.period += periodGain;				
			else if (involvedCurve == PatternCurveType.AngularSpeed)				
				bullet.moduleMovement.angularSpeedOverLifetime.period += periodGain;				
			else if (involvedCurve == PatternCurveType.Scale)				
				bullet.moduleMovement.scaleOverLifetime.period += periodGain;				
			else if (involvedCurve == PatternCurveType.AnimX)				
				bullet.moduleMovement.moveXFromAnim.period += periodGain;				
			else if (involvedCurve == PatternCurveType.AnimY)				
				bullet.moduleMovement.moveYFromAnim.period += periodGain;				
			else if (involvedCurve == PatternCurveType.AnimAngle)				
				bullet.moduleMovement.rotateFromAnim.period += periodGain;				
			else if (involvedCurve == PatternCurveType.AnimScale)
				bullet.moduleMovement.scaleFromAnim.period += periodGain;
		}
	}

	public class MicroActionCurveRawTimeSet : MicroActionGeneric<float>
	{
		public MicroActionCurveRawTimeSet(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			if (involvedCurve == PatternCurveType.Alpha)
				startValue = bullet.moduleRenderer.alphaOverLifetime.timeSinceLive;
			else if (involvedCurve == PatternCurveType.Color)
				startValue = bullet.moduleRenderer.colorOverLifetime.timeSinceLive;
			else if (involvedCurve == PatternCurveType.Homing)
				startValue = bullet.moduleHoming.homingOverLifetime.timeSinceLive;
			else if (involvedCurve == PatternCurveType.Speed)
				startValue = bullet.moduleMovement.speedOverLifetime.timeSinceLive;
			else if (involvedCurve == PatternCurveType.AngularSpeed)
				startValue = bullet.moduleMovement.angularSpeedOverLifetime.timeSinceLive;
			else if (involvedCurve == PatternCurveType.Scale)
				startValue = bullet.moduleMovement.scaleOverLifetime.timeSinceLive;
			else if (involvedCurve == PatternCurveType.AnimX)
				startValue = bullet.moduleMovement.moveXFromAnim.timeSinceLive;
			else if (involvedCurve == PatternCurveType.AnimY)
				startValue = bullet.moduleMovement.moveYFromAnim.timeSinceLive;
			else if (involvedCurve == PatternCurveType.AnimAngle)
				startValue = bullet.moduleMovement.rotateFromAnim.timeSinceLive;
			else if (involvedCurve == PatternCurveType.AnimScale)
				startValue = bullet.moduleMovement.scaleFromAnim.timeSinceLive;
			
			endValue = inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			float newRawTime = (Mathf.Lerp(startValue, endValue, GetRatio()));

			if (involvedCurve == PatternCurveType.Alpha)
				bullet.moduleRenderer.alphaOverLifetime.SetRawTime(newRawTime);
			else if (involvedCurve == PatternCurveType.Color)
				bullet.moduleRenderer.colorOverLifetime.SetRawTime(newRawTime);				
			else if (involvedCurve == PatternCurveType.Homing)
				bullet.moduleHoming.homingOverLifetime.SetRawTime(newRawTime);			
			else if (involvedCurve == PatternCurveType.Speed)
				bullet.moduleMovement.speedOverLifetime.SetRawTime(newRawTime);				
			else if (involvedCurve == PatternCurveType.AngularSpeed)				
				bullet.moduleMovement.angularSpeedOverLifetime.SetRawTime(newRawTime);				
			else if (involvedCurve == PatternCurveType.Scale)				
				bullet.moduleMovement.scaleOverLifetime.SetRawTime(newRawTime);				
			else if (involvedCurve == PatternCurveType.AnimX)				
				bullet.moduleMovement.moveXFromAnim.SetRawTime(newRawTime);				
			else if (involvedCurve == PatternCurveType.AnimY)				
				bullet.moduleMovement.moveYFromAnim.SetRawTime(newRawTime);				
			else if (involvedCurve == PatternCurveType.AnimAngle)				
				bullet.moduleMovement.rotateFromAnim.SetRawTime(newRawTime);				
			else if (involvedCurve == PatternCurveType.AnimScale)
				bullet.moduleMovement.scaleFromAnim.SetRawTime(newRawTime);
		}
	}

	public class MicroActionCurveRatioSet : MicroActionGeneric<float>
	{
		public MicroActionCurveRatioSet(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, float inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, inputValue, curveType)
		{
			if (involvedCurve == PatternCurveType.Alpha)
				startValue = bullet.moduleRenderer.alphaOverLifetime.GetRatio();
			else if (involvedCurve == PatternCurveType.Color)
				startValue = bullet.moduleRenderer.colorOverLifetime.GetRatio();
			else if (involvedCurve == PatternCurveType.Homing)
				startValue = bullet.moduleHoming.homingOverLifetime.GetRatio();
			else if (involvedCurve == PatternCurveType.Speed)
				startValue = bullet.moduleMovement.speedOverLifetime.GetRatio();
			else if (involvedCurve == PatternCurveType.AngularSpeed)
				startValue = bullet.moduleMovement.angularSpeedOverLifetime.GetRatio();
			else if (involvedCurve == PatternCurveType.Scale)
				startValue = bullet.moduleMovement.scaleOverLifetime.GetRatio();
			else if (involvedCurve == PatternCurveType.AnimX)
				startValue = bullet.moduleMovement.moveXFromAnim.GetRatio();
			else if (involvedCurve == PatternCurveType.AnimY)
				startValue = bullet.moduleMovement.moveYFromAnim.GetRatio();
			else if (involvedCurve == PatternCurveType.AnimAngle)
				startValue = bullet.moduleMovement.rotateFromAnim.GetRatio();
			else if (involvedCurve == PatternCurveType.AnimScale)
				startValue = bullet.moduleMovement.scaleFromAnim.GetRatio();
			
			endValue = inputValue;
		}

		public override void UpdateParameter(float deltaTime)
		{
			float newRatio = (Mathf.Lerp(startValue, endValue, GetRatio()));

			if (involvedCurve == PatternCurveType.Alpha)
				bullet.moduleRenderer.alphaOverLifetime.SetRatio(newRatio);
			else if (involvedCurve == PatternCurveType.Color)
				bullet.moduleRenderer.colorOverLifetime.SetRatio(newRatio);				
			else if (involvedCurve == PatternCurveType.Homing)
				bullet.moduleHoming.homingOverLifetime.SetRatio(newRatio);			
			else if (involvedCurve == PatternCurveType.Speed)
				bullet.moduleMovement.speedOverLifetime.SetRatio(newRatio);				
			else if (involvedCurve == PatternCurveType.AngularSpeed)				
				bullet.moduleMovement.angularSpeedOverLifetime.SetRatio(newRatio);				
			else if (involvedCurve == PatternCurveType.Scale)				
				bullet.moduleMovement.scaleOverLifetime.SetRatio(newRatio);				
			else if (involvedCurve == PatternCurveType.AnimX)				
				bullet.moduleMovement.moveXFromAnim.SetRatio(newRatio);				
			else if (involvedCurve == PatternCurveType.AnimY)				
				bullet.moduleMovement.moveYFromAnim.SetRatio(newRatio);				
			else if (involvedCurve == PatternCurveType.AnimAngle)				
				bullet.moduleMovement.rotateFromAnim.SetRatio(newRatio);				
			else if (involvedCurve == PatternCurveType.AnimScale)
				bullet.moduleMovement.scaleFromAnim.SetRatio(newRatio);
		}
	}
}