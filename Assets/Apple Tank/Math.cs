using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Math {

	public static float PlusOrMinus () {
		return 1 - (Mathf.Round(Random.value) * 2);
	}
	
	// easing
	public static float EaseElastic (float timePercent) {
		float t = timePercent;
		float b = 0f;
		float c = 1f;
		float d = 1f;
		
		float ts = (t/=d)*t;
		float tc = ts*t;
		return b+c*(56*tc*ts + -175*ts*ts + 200*tc + -100*ts + 20*t);
	}

	public static float PercentBetween(float lowEnd, float highEnd, float val) {
		return (val - lowEnd) / (highEnd - lowEnd);
	}

	public static Vector2 DegreeToVector2(float degree) {
	    return RadianToVector2(degree * Mathf.Deg2Rad);
	}

	public static Vector2 RadianToVector2(float radian) {
		return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
	}

	public static float SinWave(float amplitude = 1, float frequency = 1, float initial = 0, float sinTime = -1) {
		if (sinTime == -1)
			sinTime = Time.time;
		return initial + amplitude * Mathf.Sin (frequency * sinTime);
	}

	public static float EaseElasticBig (float timePercent) {
		float t = timePercent;
		float b = 0f;
		float c = 1f;
		float d = 1f;
		
		float ts = (t/=d)*t;
		float tc = ts*t;
		return b+c*(96*tc*ts + -295*ts*ts + 330*tc + -160*ts + 30*t);
	}

	public static float EaseBound (float timePercent) {
		float t = timePercent;
		float b = 0f;
		float c = 1f;
		float d = 1f;

		float ts = (t/=d)*t;
		float tc = ts*t;

		return b+c*(0.499999999999996f*tc*ts + -5f*ts*ts + 11f*tc + -12f*ts + 6.5f*t);
	}
}
