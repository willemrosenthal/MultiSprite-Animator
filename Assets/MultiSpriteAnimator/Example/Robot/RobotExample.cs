using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiSprite;

public class RobotExample : MonoBehaviour {

	public MSAnimation walkSide;
	public MSAnimation walkDown;
	public MSAnimation walkUp;
	public MSAnimation walkUpSide;
	public MSAnimation walkDownSide;

	Vector2 velocity = Vector2.zero;
	Vector2 velocityAcceloration;

	Vector2 dir = Vector2.zero;

	float speed = 5;
	float accelTime = 2f;
	public float maxSpeedMultiplier = 3;

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

		if (dir != Vector2.zero)
			velocity = velocity.magnitude * dir.normalized;
		else velocity *= (1-Time.deltaTime) * 0.98f;
		
		// gets velocity
		velocity = Vector2.SmoothDamp( velocity, dir.normalized * speed * maxSpeedMultiplier, ref velocityAcceloration, accelTime, 3);

		// change animation speed based on velocity if moving
		mas.timeScale = velocity.magnitude / speed * maxSpeedMultiplier;
	
		// moves him
		transform.position += (Vector3)velocity * Time.deltaTime;
	}


	
	void Animate() {
		if (dir == Vector2.zero)
			return;

		// if x and y values are close, walk diagonally
		if (dir.x != 0 && dir.y != 0) {
			if (dir.y > 0)
				mas.Play(walkUpSide, mas.GetTime());
			else mas.Play(walkDownSide, mas.GetTime());
			FaceMovementDirection();
		}

		// if facing to the side
		else if (dir.x != 0) {
			mas.Play(walkSide, mas.GetTime());
			FaceMovementDirection();
		}

		// if facing up
		else if (dir.y > 0) 
			mas.Play(walkUp, mas.GetTime());

		// if facing down
		else mas.Play(walkDown, mas.GetTime());
		
	}

	void FaceMovementDirection() {
		// flip to face left or right
		if (velocity.x > 0 && !sr.flipX)
			sr.flipX = true;

		if (velocity.x < 0 && sr.flipX)
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

	}
}
