using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ZSort : MonoBehaviour {

	SpriteRenderer sr;
	void Awake () {
		sr = GetComponent<SpriteRenderer> ();
	}

	void LateUpdate () {
		if (!Application.isPlaying)
			sr = GetComponent<SpriteRenderer> ();
		if (sr)
			sr.sortingOrder = (int)Camera.main.WorldToScreenPoint (transform.position).y * -1;
	}
}
