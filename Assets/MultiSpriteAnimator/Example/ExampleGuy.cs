using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiSprite;

public class ExampleGuy : MonoBehaviour {

	public MSAnimation idleDown;
	public MSAnimation idleSide;
	public MSAnimation idleUp;

	public MSAnimation walkSide;
	public MSAnimation walkDown;
	public MSAnimation walkUp;

	Vector2 velocity = Vector2.zero;
	Vector2 velocityAcceloration;

	Vector2 dir = Vector2.zero;
	Vector2 lastFacing;

	float speed = 5;
	float accelTime = 0.25f;

	MultiSpriteAnimator mas;
	SpriteRenderer sr;

	void Start () {
		mas = GetComponent<MultiSpriteAnimator>();
		sr = GetComponent<SpriteRenderer>();
	}
	
	void Update () {
		// what buttons are being pressed
		GetInput();

		// plays appropraite animation
		Animate();
		
		// gets velocity
		velocity = Vector2.SmoothDamp( velocity, dir.normalized * speed, ref velocityAcceloration, accelTime, Mathf.Infinity);

		if (velocity.magnitude < 0.1)
			velocity = Vector2.zero;

		// change animation speed based on velocity if moving
		if (velocity != Vector2.zero)
			mas.timeScale = velocity.magnitude / speed * 2;

		// playback at normal speed if not moving
		else mas.timeScale = 1;
		
		// moves him
		transform.position += (Vector3)velocity * Time.deltaTime;
	}


	
	void Animate() {
		// if not moving
		if (velocity == Vector2.zero) { 
			// if facing to the side
			if (lastFacing.x != 0) 
				mas.Play(idleSide);

			// if facing up
			else if (lastFacing.y > 0) 
				mas.Play(idleUp);

			// if facing down
			else mas.Play(idleDown);
		}

		// if moving
		else {
			// if facing to the side
			if (dir.x != 0) 
				mas.Play(walkSide);

			// if facing up
			else if (dir.y > 0) 
				mas.Play(walkUp);

			// if facing down
			else mas.Play(walkDown);
		}

		// flip to face left or right
		if (lastFacing.x > 0 && !sr.flipX)
			sr.flipX = true;

		if (lastFacing.x < 0 && sr.flipX)
			sr.flipX = false;
	}


	// all sprites in the animation use the layer and sort order of the sprite renderer attached here. This allows for easy z sorting
	void LateUpdate() {
		sr.sortingOrder = (int)Camera.main.WorldToScreenPoint (transform.position).y * -1;
	}



	void GetInput() {
		dir = Vector2.zero;
		// left
		if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
			dir.x = -1;

		// right
		if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
			dir.x = 1;

		// up
		if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
			dir.y = 1;

		// down
		if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
			dir.y = -1;

		// saves last direction we input
		if (dir != Vector2.zero)
			lastFacing = dir;
	}
}
