using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiSprite;

public class AppleTank : MonoBehaviour {

	Vector2 controllDir;

	public float moveDir;
	float turnVelocity;
	float turnTime = 0.3f;

	float maxSpeed = 12;
	float accel = 2f;
	float speed;

    float meterPerSec = 2f;

	// parts
	public Transform bodyRotationTransform;
	public MultiSpriteAnimator body;
	public AppleTankLeg leftLeg;
	public AppleTankLeg rightLeg;
	SpriteRenderer[] childenSR = null;
	
	SpriteRenderer spriteRenderer;
	void Start() {
		spriteRenderer = GetComponent<SpriteRenderer>();

		body.GetParts()[0].gameObject.AddComponent<SortBasedOnDistToCam>();
	}

	void Update () {
		// turns
		moveDir = Mathf.SmoothDampAngle(moveDir, ControllsAngle(), ref turnVelocity, turnTime, Mathf.Infinity);

		// movement
		if (controllDir != Vector2.zero && speed < maxSpeed) {
			speed += accel * Time.deltaTime;
			if (speed > maxSpeed)
				speed = maxSpeed;
		}
		else if (controllDir == Vector2.zero && speed > 0) {
			speed -= accel * Time.deltaTime * 3;
			if (speed < 0)
				speed = 0;
		}

		// moves in direction
		Vector2 moveAmmount = Math.DegreeToVector2(moveDir + 90) * speed * Time.deltaTime;
		moveAmmount.y *= -1;
		transform.position += (Vector3)moveAmmount;

		// only roates as fast as we are moving
		Vector3 rotChange =  new Vector3(0,moveDir,0) - bodyRotationTransform.localEulerAngles;;
		while(rotChange.y > 180)
			rotChange.y -= 360;
		while(rotChange.y < -180)
			rotChange.y += 360;
		bodyRotationTransform.localEulerAngles += rotChange * (speed/maxSpeed); //new Vector3(0,rotChange,0);

		// plays animation at appropriate speed
		body.timeScale = leftLeg.msa.timeScale = rightLeg.msa.timeScale = (1f/meterPerSec) * speed;
	}

	void LateUpdate() {
		if (childenSR == null)
			GetAllChildren();
		foreach (SpriteRenderer sr in childenSR)
			sr.sortingOrder += spriteRenderer.sortingOrder;
	}

	float ControllsAngle() {
		controllDir = Vector2.zero;
		// left
		if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
			controllDir.x = -1;

		// right
		else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
			controllDir.x = 1;

		// up
		if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
			controllDir.y = 1;

		// down
		if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
			controllDir.y = -1;

		if (controllDir == Vector2.zero)
			return moveDir;

		float angle = Vector2.Angle(Vector2.down, controllDir);
		if (controllDir.x > 0)
			angle *= -1;
			
		return angle;
	}


	void GetAllChildren() {
		childenSR = GetComponentsInChildren<SpriteRenderer>();
	}
}
