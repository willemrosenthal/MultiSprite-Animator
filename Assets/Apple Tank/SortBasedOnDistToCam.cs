using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SortBasedOnDistToCam : MonoBehaviour {

	SpriteRenderer sr;
	void Awake () {
		sr = GetComponent<SpriteRenderer> ();
	}

	void Update () {
		float distToCam = Vector3.Distance(transform.position, Camera.main.transform.position);
		sr.sortingOrder = (int)distToCam * -1;
	}
}
