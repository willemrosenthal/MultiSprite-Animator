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
	float accelTime = 0.4f;

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

		// change animation speed based on velocity if moving
		mas.timeScale = velocity.magnitude / speed * 2;
	
		// moves him
		transform.position += (Vector3)velocity * Time.deltaTime;
	}


	
	void Animate() {
		if (velocity == Vector2.zero)
			return;

		// if x and y values are close, walk diagonally
		if (velocity.x != 0 && velocity.y != 0 && Mathf.Abs(velocity.normalized.x) + 0.35 > Mathf.Abs(velocity.normalized.y) && Mathf.Abs(velocity.normalized.x) - 0.35 < Mathf.Abs(velocity.normalized.y)) {
			if (velocity.y > 0)
				mas.Play(walkUpSide, mas.GetTime());
			else mas.Play(walkDownSide, mas.GetTime());
			FaceMovementDirection();
		}

		// if facing to the side
		else if (velocity.x != 0 && Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y)) {
			mas.Play(walkSide, mas.GetTime());
			FaceMovementDirection();
		}

		// if facing up
		else if (velocity.y > 0) 
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
