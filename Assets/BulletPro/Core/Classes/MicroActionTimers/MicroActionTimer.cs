using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	// A non-serialized class that Bullet.cs uses to handle tiny timed functions, like coroutines.
	public class MicroActionTimer
	{
		protected Bullet bullet;

		protected float totalTime;
		protected float timeLeft;
		protected AnimationCurve curve;

		protected PatternCurveType involvedCurve;

		public MicroActionTimer(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, PatternCurveType curveType=PatternCurveType.None)
		{
			bullet = thisBullet;
			totalTime = lerpTime;
			timeLeft = lerpTime;
			curve = lerpCurve;
			involvedCurve = curveType;
		}

		public float GetRatio() { if (totalTime==0) return 1; return curve.Evaluate(1-(timeLeft/totalTime)); }
		public bool IsDone() { return timeLeft <= 0; }
		
		public void Update()
		{
			if (IsDone()) return;

			// prevent dividing by zero by forcing the lerp to resolve instantly
			if (totalTime <= 0)
			{
				totalTime = 1;
				timeLeft = 0;
				UpdateParameter(1);
				return;
			}

			float deltaTime = Time.deltaTime;
			float prevTimeLeft = timeLeft;
			timeLeft -= deltaTime;
			if (IsDone()) deltaTime += timeLeft;

			// applying the curve to deltaTime: take the base deltaTime and multiply it by dy/dx
			if (deltaTime != 0)
			{
				float dx = deltaTime / totalTime; // normalize it to the 0-1 range
				float dy = curve.Evaluate(1-(timeLeft/totalTime)) - curve.Evaluate(1-(prevTimeLeft/totalTime));
				deltaTime *= dy/dx;
			}

			UpdateParameter(deltaTime);
		}

		public virtual void UpdateParameter(float deltaTime) { }
	}

	public class MicroActionGeneric<T> : MicroActionTimer
	{
		protected T startValue, endValue;

		public MicroActionGeneric(Bullet thisBullet, float lerpTime, AnimationCurve lerpCurve, T inputValue, PatternCurveType curveType=PatternCurveType.None)
			: base(thisBullet, lerpTime, lerpCurve, curveType) { }

		public override void UpdateParameter(float deltaTime) { }
	}
}