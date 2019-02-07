using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MultiSprite {

// THESE CLASSES NEED TO MATCH THE DATA IN MSAnimation. THEY ALLOW FOR EASY SORTING IN THE EDITOR
public class MSFrame {
	public float frameTime = 1;
	public float startTime;
	public float endTime;
	public string frameLable;
	public int frameCurve = 0;
	public List<MSFrameSprite> sprites;

	public MSFrame() {
		sprites = new List<MSFrameSprite>();
	}
}

public class MSFrameSprite {
	public AnimationClip animation = null;
	public Sprite sprite = null;
	public Vector2 position = Vector2.zero;
	public bool flipX = false;
	public Vector2 scale = Vector2.one;
	public float rotation = 0; 
	public int sortOrder = 0;
	public int curve = 0;
	public bool hide = false;

	// used in editor only
	public int originalIndex;
}

public class MSAnimation : ScriptableObject {

	public bool limitToFPS = false;
	public int fps = 20;
	[HideInInspector] public int totalSprites;
	public bool loop = true;
	public bool pixelPerfect = false;
	public int pixelsPerUnit = 32;

	[HideInInspector] public List<string> spriteLable = null;
	[HideInInspector] public string[] spriteLayer = null;
	[HideInInspector] [SerializeField] List<float> frameTime;
	[HideInInspector] [SerializeField] List<string> frameLable;
	[HideInInspector] [SerializeField] List<int> frameCurves;

	// sprite data
	[HideInInspector] [SerializeField] List<AnimationClip> animations;
	[HideInInspector] [SerializeField] List<Sprite> sprites;
	[HideInInspector] [SerializeField] List<Vector2> positions;
	[HideInInspector] [SerializeField] List<bool> flipX;
	[HideInInspector] [SerializeField] List<Vector2> scales;
	[HideInInspector] [SerializeField] List<float> rotations;
	[HideInInspector] [SerializeField] List<int> sortOrders;
	[HideInInspector] [SerializeField] List<int> curves;
	[HideInInspector] [SerializeField] List<bool> hide;

	// where we actually read data from
	[SerializeField] List<MSFrame> frames = null;

	// clear lists when initialized
	public MSAnimation() {
		InitializeLists();
		StartingFrame();
	}

	// clears lists
	public void InitializeLists() {
		if (spriteLable ==  null)
			spriteLable = new List<string>();

		spriteLayer = new string[1];

		frameTime = new List<float>();
		frameLable = new List<string>();
		frameCurves = new List<int>();

		animations = new List<AnimationClip>();
		sprites = new List<Sprite>();
		flipX = new List<bool>();
		positions = new List<Vector2>();
		scales = new List<Vector2>();
		rotations = new List<float>();
		sortOrders = new List<int>();
		curves = new List<int>();
		hide = new List<bool>();
	}

	void StartingFrame() {
		totalSprites = 1;
		spriteLable.Add("sprite 1");
		spriteLayer[0] = "Use Parent";
		frameTime.Add(0.5f);
		frameLable.Add("frame 0");
		frameCurves.Add(0);
		animations.Add(null);
		sprites.Add(null);
		flipX.Add(false);
		positions.Add(Vector2.zero);
		scales.Add(Vector2.one);
		rotations.Add(0);
		sortOrders.Add(0);
		curves.Add(0);
		hide.Add(false);
	}

	// convert to readable format for editor and animator
	public List<MSFrame> ConvertForEditor() {
		frames = new List<MSFrame>();

		// go through each frame
		for (int i = 0; i < frameTime.Count; i++) {
			// new frame
			frames.Add(new MSFrame());
			frames[i].frameTime = frameTime[i];
			frames[i].frameLable = frameLable[i];
			if (frameCurves.Count > i)
				frames[i].frameCurve = frameCurves[i];

			// new sprite list
			frames[i].sprites = new List<MSFrameSprite>();
			
			for (int s = 0; s < totalSprites; s++) {
				int slot = (i * totalSprites) + s;
				frames[i].sprites.Add(new MSFrameSprite());
				frames[i].sprites[s].animation = animations[slot];
				frames[i].sprites[s].sprite = sprites[slot];
				frames[i].sprites[s].position = positions[slot];
				frames[i].sprites[s].flipX = flipX[slot];
				frames[i].sprites[s].scale = scales[slot];
				frames[i].sprites[s].rotation = rotations[slot];
				frames[i].sprites[s].sortOrder = sortOrders[slot];
				frames[i].sprites[s].curve = curves[slot];
				frames[i].sprites[s].hide = hide[slot];
			}
		}
		return frames;
	}

	// converts editor format to savable list format
	public void ConvertToSavableFormat(List<MSFrame> frames) {
		InitializeLists();
		for (int i = 0; i < frames.Count; i++) {
			// frame level
			frameTime.Add(frames[i].frameTime);
			frameLable.Add(frames[i].frameLable);
			frameCurves.Add(frames[i].frameCurve);

			// populate frame lists
			for (int s = 0; s < totalSprites; s++) {
				animations.Add(frames[i].sprites[s].animation);
				sprites.Add(frames[i].sprites[s].sprite);
				positions.Add(frames[i].sprites[s].position);
				flipX.Add(frames[i].sprites[s].flipX);
				scales.Add(frames[i].sprites[s].scale);
				rotations.Add(frames[i].sprites[s].rotation);
				sortOrders.Add(frames[i].sprites[s].sortOrder);
				curves.Add(frames[i].sprites[s].curve);
				hide.Add(frames[i].sprites[s].hide);
			}
		}
	}


	public void PrepareFrames() {
		if (frames == null) {
			frames = ConvertForEditor();
		}
	}

	// returns the percent to next frame
	public float GetFramePercent (int currentframe, float currentFrameTime) {
		return currentFrameTime / frames[currentframe].frameTime;
	}

	public int GetTotalFrames() {
		return frames.Count;
	}

	// used in animating
	// POSITION
	public Vector2 GetSpritePosition(int currentframe, int currentSprite, bool next = false) {
		int f = currentframe;
		if (next) f = GetNextFrameNo(f); 
		return frames[f].sprites[currentSprite].position;
	}
	public Vector2 GetSpriteScale(int currentframe, int currentSprite, bool next = false) {
		int f = currentframe;
		if (next) f = GetNextFrameNo(f); 
		return frames[f].sprites[currentSprite].scale;
	}
	public float GetSpriteRotation(int currentframe, int currentSprite, bool next = false) {
		int f = currentframe;
		if (next) f = GetNextFrameNo(f); 
		return -frames[f].sprites[currentSprite].rotation;
	}
	public int GetSortOrder(int currentframe, int currentSprite, bool next = false) {
		int f = currentframe;
		if (next) f = GetNextFrameNo(f); 
		return frames[f].sprites[currentSprite].sortOrder;
	}



	public int GetNextFrameNo(int currentframe) {
		if (frames.Count > currentframe+1)
			return currentframe + 1;
		else if (loop)
			return 0;
		else return currentframe;
	}



	public bool GetHidden(int currentframe, int currentSprite) {
		return frames[currentframe].sprites[currentSprite].hide;
	}

	public bool GetFlipX(int currentframe, int currentSprite) {
		return frames[currentframe].sprites[currentSprite].flipX;
	}

	public int GetFrameCurve(int currentframe) {
		return frameCurves[currentframe];
	}

	public int GetSpriteCurve(int currentframe, int currentSprite) {
		return frames[currentframe].sprites[currentSprite].curve - 1;
	}

	public AnimationClip GetAnimation(int currentframe, int currentSprite) {
		return frames[currentframe].sprites[currentSprite].animation;
	}

	public Sprite GetSprite (int currentframe, int currentSprite) {
		return frames[currentframe].sprites[currentSprite].sprite;
	}

	public float GetLength() {
		float totalTime = 0;
		for (int i = 0; i < frames.Count; i++) {
			totalTime += frames[i].frameTime;
		}
		return totalTime;
	}

	public float GetFrameTime(int frameNo) {
		return frames[frameNo].frameTime;
	}

	public List<float> GetCumulativeFrameTimes() {
		List<float> frameTimes = new List<float>();
		float totalTime = 0;
		for (int i = 0; i < GetTotalFrames(); i++) {
			totalTime += GetFrameTime(i);
			frameTimes.Add(totalTime);
		}
		return frameTimes;
	}

	// modding frames
	public void SetFrameStartTime(int frameNo, float time) {
		frames[frameNo].startTime = time;
	}
	public void SetFrameEndTime(int frameNo, float time) {
		frames[frameNo].endTime = time;
	}
	public float GetTotalTime() {
		return frames[frames.Count-1].endTime;
	}
	public float GetFrameEndTime(int frameNo) {
		return frames[frameNo].endTime;
	}
	public MSFrame GetFrame(int frameNo) {
		return frames[frameNo];
	}

}

}