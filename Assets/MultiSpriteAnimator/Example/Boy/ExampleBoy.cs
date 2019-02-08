using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiSprite;

public class ExampleBoy : MonoBehaviour {

	public MSAnimation walk;
	public MSAnimation idle;

	bool walking = false;
	float walkSpeed = 30;
	float walkTime = 2;
	float pauseTime = 1.5f;
	float timer;

	SpriteRenderer sr;
	MultiSpriteAnimator msa;

	void Start () {
		sr = GetComponent<SpriteRenderer>();
		msa = GetComponent<MultiSpriteAnimator>();
		msa.Play(idle);
	}
	
	void Update () {
		// if not walking
		if (!walking) {
			// play idle animation
			msa.Play(idle);

			// tick down timer untill we are done waiting
			timer -= Time.deltaTime;
			if (timer < 0) {
				walking = true;
				timer = walkTime;
			}
		}

		// if walking
		if (walking) {
			// play walk animation
			msa.Play(walk);
			msa.timeScale = 2.2f;

			// face walking direction
			if (walkSpeed > 0)
				sr.flipX = true;
			else sr.flipX = false;

			// move
			transform.position += Vector3.right * walkSpeed * Time.deltaTime;

			// tick down timer untill we are done walking
			timer -= Time.deltaTime;
			if (timer < 0) {
				walking = false;
				timer = pauseTime;
				walkSpeed *= -1;
			}
		}
	}
}
