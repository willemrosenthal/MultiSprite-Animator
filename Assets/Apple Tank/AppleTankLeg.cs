using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiSprite;

public class AppleTankLeg : MonoBehaviour {

	// sprites
	public Sprite legSprite;

	// animation
	public MSAnimation  walk;

	public float kneeOffset = 0.5f;
	public float cogOffset = 0.5f;

	Transform joint;
	Transform knee;
	Transform foot;

	GameObject upperLeg;
	GameObject lowerLeg;
	SpriteRenderer upperLegSr;
	SpriteRenderer lowerLegSr;

	public MultiSpriteAnimator msa;

	void Start () {
		msa = GetComponent<MultiSpriteAnimator>();
		SetupAnimation();
		BuildLegs();
	}

	void BuildLegs() {
		upperLeg = new GameObject();
		upperLeg.transform.parent = transform.parent;
		upperLegSr = upperLeg.AddComponent<SpriteRenderer>();
		upperLeg.AddComponent<Billboard>();
		upperLeg.AddComponent<SortBasedOnDistToCam>();
		upperLegSr.sprite = legSprite;
		upperLegSr.sortingLayerName = "Gameplay";

		lowerLeg = new GameObject();
		lowerLeg.transform.parent = transform.parent;
		lowerLegSr = lowerLeg.AddComponent<SpriteRenderer>();
		lowerLeg.AddComponent<SortBasedOnDistToCam>();
		lowerLeg.AddComponent<Billboard>();
		lowerLegSr.sprite = legSprite;
		lowerLegSr.sortingLayerName = "Gameplay";
	}

	void SetupAnimation() {
		if (kneeOffset < 0)
			msa.Play(walk, walk.GetLength() * 0.5f);
		else msa.Play(walk);

		for (int i = 0; i < msa.GetParts().Count; i++) {
			msa.GetParts()[i].gameObject.AddComponent<Billboard>();
			msa.GetParts()[i].gameObject.AddComponent<SortBasedOnDistToCam>();
			if (msa.GetParts()[i].name == "mid_joint")
				knee = msa.GetParts()[i];
			if (msa.GetParts()[i].name == "top_joint")
				joint = msa.GetParts()[i];
			if (msa.GetParts()[i].name == "foot")
				foot = msa.GetParts()[i];
		}
	}

	void LateUpdate() {
		knee.transform.localPosition += kneeOffset * Vector3.forward;

		// legs
		upperLeg.transform.position = Vector3.Lerp(joint.transform.position, knee.transform.position, 0.5f) + Vector3.forward * Vector3.Distance(joint.transform.position, knee.transform.position) * 0.51f;
		lowerLeg.transform.position = Vector3.Lerp(foot.transform.position, knee.transform.position, 0.5f) + Vector3.forward * Vector3.Distance(foot.transform.position, knee.transform.position) * 0.51f;

		Vector3 upperLegRot = (Vector2)knee.transform.position - (Vector2)joint.transform.position;
		if (knee.transform.position.x > joint.transform.position.x) {
			upperLegRot *= -1;
		}
		upperLegRot = new Vector3(0,0,Vector2.Angle(Vector2.up, upperLegRot));

		Vector3 lowerLegRot = (Vector2)foot.transform.position - (Vector2)knee.transform.position;
		if (foot.transform.position.x > knee.transform.position.x) {
			lowerLegRot *= -1;
		}
		lowerLegRot = new Vector3(0,0,Vector2.Angle(Vector2.up, lowerLegRot));

		upperLeg.transform.localEulerAngles += upperLegRot;
		lowerLeg.transform.localEulerAngles += lowerLegRot;
	}
}
