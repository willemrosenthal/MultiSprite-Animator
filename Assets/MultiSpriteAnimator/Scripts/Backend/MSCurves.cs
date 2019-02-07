using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiSprite
{
public static class MSCurves {

	public static string[] curves = {
		"Linear",
		"Sin In-Out",
		"Sin In",
		"Sin Out"
	};

	public static float GetCurve(float percent, int curve) {
		switch (curves[curve]) {
			case "Linear": {
				return percent;
			}
			case "Sin In-Out": {
				return SinInOut(percent);
			}
			case "Sin In": {
				return SinIn(percent);
			}
			case "Sin Out": {
				return SinOut(percent);
			}
		}
		return percent;
	}

	public static float Linear(float percent) {
		return percent;
	}

	public static float SinInOut (float percent) {
		return -0.5f * (Mathf.Cos(Mathf.PI * percent) - 1);
	}

	public static float SinIn (float percent) {
		return -1 * Mathf.Cos(percent * (Mathf.PI/2)) + 1;
	}

	public static float SinOut (float percent) {
		return Mathf.Sin(percent * (Mathf.PI/2));
	}

}
}
