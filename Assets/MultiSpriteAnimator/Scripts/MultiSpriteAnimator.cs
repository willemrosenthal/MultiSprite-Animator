using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PowerTools;
using System;
using UnityEditor;

namespace MultiSprite {

[RequireComponent (typeof(SpriteRenderer))]
public class MultiSpriteAnimator : MonoBehaviour {

	public MSAnimation defaultAnimation = null;
	public float timeScale = 1;

	bool playing;

	// current animation data
	MSAnimation currentAnimation;
	string animName; // current animaiton
	
	// parts used in multi sprite object
	List<Transform> parts;
	List<SpriteRenderer> sRend;
	List<Animator> sAnimator;

	#if POWER_TOOLS
		List<SpriteAnim> sAnim;
	#endif

	SpriteRenderer spriteRenderer;
	MSPlayback playback;

	// initialize
	void Start() {
		playback = new MSPlayback();
		spriteRenderer = GetComponent<SpriteRenderer>();

		parts = new List<Transform>();
		sRend = new List<SpriteRenderer>();

		#if POWER_TOOLS
			sAnimator = new List<Animator>();
			sAnim = new List<SpriteAnim>();
		#endif

		if (defaultAnimation != null)
			Play(defaultAnimation);
	}

	// plays an animaiton
	public void Play(MSAnimation animation, bool playIfNotAlreadyPlaying = true) {
		if (playIfNotAlreadyPlaying && animation.name == animName)
			return;

		// converts frames to frame class format
		animation.PrepareFrames();

		// sets the current animation to the new animation
		currentAnimation = animation;
			
		// prepare / create the sprites
		PrepareSprites();

		// prepare animation data
		playback.PrepareAnimationData(currentAnimation);

		// reset animation
		animName = animation.name;

		// starts animating each sprite
		KeyframeUpdateSprites();
	}

	public bool IsPlaying(MSAnimation animation = null) {
		if (animation == null)
			return playing;
		else if (animation.name == animName && playing)
			return true;
		return false;
	}

	void PrepareSprites() {
		// set up parts
		while (parts.Count < currentAnimation.totalSprites)
			CreateSprite();
		
		// hides unnessesary parts
		for (int i = currentAnimation.totalSprites; i < parts.Count; i++) {
			sRend[i].enabled = false;
		}
	}

	// creats a new sprite
	void CreateSprite() {
		GameObject g = new GameObject();
		g.name = "MS_sprite" + parts.Count;
		g.transform.parent = this.transform;
		sRend.Add(g.AddComponent<SpriteRenderer>());

		#if POWER_TOOLS
			sAnimator.Add(g.AddComponent<Animator>());
			sAnim.Add(g.AddComponent<SpriteAnim>());
		#endif

		parts.Add(g.transform);
	}

	// animates
	void Update() {
		if (currentAnimation == null)
			return;

		if (currentAnimation.loop || playback.time < currentAnimation.GetTotalTime()) {
			float deltaTime = Time.deltaTime * timeScale;

			playing = true;

			playback.IncrementTime(deltaTime);

			if (playback.time >= currentAnimation.GetTotalTime())
				playing = false;

			// if next frame has started
			if (playback.framePercentFPS > 1) {
				// advance the frame
				playback.AdvanceFrame();
				// updates sprites for the new keyframe
				KeyframeUpdateSprites();
			}
			
			UpdateSprites();
		}

		// sort order
		SortingOrder();

		if (spriteRenderer.flipX && Mathf.Sign(transform.localScale.x) > 0 || !spriteRenderer.flipX && Mathf.Sign(transform.localScale.x) < 0) {
			Vector3 localScale = transform.localScale;
			localScale.x *= -1;
			transform.localScale = localScale;
		}
	}

	void SortingOrder() {
		for (int i = 0; i < currentAnimation.totalSprites; i++) {
			string spriteLayer = currentAnimation.spriteLayer[i];
			if (spriteLayer != "Use Parent" && spriteLayer != "")
				sRend[i].sortingLayerName = currentAnimation.spriteLayer[i];
			else sRend[i].sortingLayerName = spriteRenderer.sortingLayerName;
			sRend[i].sortingOrder = spriteRenderer.sortingOrder + currentAnimation.GetSortOrder(playback.cFrameFPS, i);
		}
	}

	void UpdateSprites() {
		// loop through sprites
		float frameCurvePercent = MSCurves.GetCurve(playback.framePercentFPS, currentAnimation.GetFrameCurve(playback.cFrameFPS));
		for (int i = 0; i < currentAnimation.totalSprites; i++) {
			float curvedPercent = frameCurvePercent;
			if (currentAnimation.GetSpriteCurve(playback.cFrameFPS, i) >= 0)
				curvedPercent = MSCurves.GetCurve(playback.framePercentFPS, currentAnimation.GetSpriteCurve(playback.cFrameFPS, i));
			// need to change this to curves
			Vector2 spritePos = playback.GetUpdatedPosition(i, curvedPercent);
			if (currentAnimation.pixelPerfect)
				spritePos = playback.PixelPerfertSnap(spritePos, sRend[i].sprite);
			parts[i].localPosition = spritePos;  
			parts[i].localScale = playback.GetUpdatedScale(i, curvedPercent); 
			float rotation = playback.GetUpdatedRotation(i, curvedPercent);

			parts[i].localEulerAngles = Vector3.forward * rotation;
		}
	}


	void KeyframeUpdateSprites() {
		for (int i = 0; i < currentAnimation.totalSprites; i++) {
			// update sprite name
			sRend[i].transform.name = currentAnimation.spriteLable[i];

			// hide or show sprite if it should be hidden
			sRend[i].enabled = !currentAnimation.GetHidden(playback.cFrameFPS, i);
			
			// flip X
			sRend[i].flipX = currentAnimation.GetFlipX(playback.cFrameFPS, i);

			AnimationClip clip = null;

			#if POWER_TOOLS
				// get animation clip
				clip = currentAnimation.GetAnimation(playback.cFrameFPS, i);
			#endif

			// display image
			Sprite sprite = currentAnimation.GetSprite(playback.cFrameFPS, i);
			if ( sprite != null && clip == null) {
				#if POWER_TOOLS
					sAnimator[i].enabled = false;
				#endif
				sRend[i].sprite = sprite;
			}
			
			#if POWER_TOOLS
				if (clip != null && (!sAnim[i].IsPlaying(clip) || !sAnimator[i].enabled) ) {
					sAnimator[i].enabled = true;
					sAnim[i].Play(clip);
				}
			#endif
		}
	}
}
}
