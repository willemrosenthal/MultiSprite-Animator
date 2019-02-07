using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiSprite;

public class ExampleKnight : MonoBehaviour {

	public MSAnimation walkAnimation;
	public MSAnimation punchAnimation;

	float walkTime = 3;
	float timer;

	MultiSpriteAnimator mas;
	SpriteRenderer sr;

	void Start () {
		mas = GetComponent<MultiSpriteAnimator>();
		sr = GetComponent<SpriteRenderer>();
	}
	
	void Update () {
		// plays walk animation if not currently playing the punch animaiton
		if (!mas.IsPlaying(punchAnimation)) {
			timer += Time.deltaTime;
			mas.Play(walkAnimation);
		}
		
		// after a time, play the punch animaiton
		if (timer > walkTime) {
			mas.Play(punchAnimation);
			timer = 0;
		}

	}

	// all sprites in the animation use the layer and sort order of the sprite renderer attached here. This allows for easy z sorting
	void LateUpdate() {
		sr.sortingOrder = (int)Camera.main.WorldToScreenPoint (transform.position).y * -1;
	}
}
