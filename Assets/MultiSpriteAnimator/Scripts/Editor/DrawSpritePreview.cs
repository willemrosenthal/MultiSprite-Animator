using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace MultiSprite
{
public partial class MultiSpriteEditor {

	[SerializeField] bool m_ignorePivot = false;

	List<Vector2> combinedSpriteSizes;

	Rect[] spritePositions;
	int[] spritePositionIndexes;

	bool _frameMode;

	void DrawAllFrameSprites(Rect rect, bool playingAnimation, int frameNo = -1, float scaleMod = 1, bool frameMode = false, float spriteOffsetx = 0, float spriteOffsety = 0) {
		Sprite sprite = null;
		Vector2 spritePos = Vector2.zero;
		Vector2 spriteScale = Vector2.one;
		float spriteRotation;
		bool flipX = false;
		int _cFrame = frameIndex;
		_frameMode = frameMode;
		float frameCurvePercent;

		float viewScaling = scaleMod * m_previewScale;
		//float previewScale = m_previewScale;

		Vector2 spriteOffset = new Vector2(spriteOffsetx, spriteOffsety);

		Vector2 previewOffset = m_previewOffset;

		float framePercent = playback.framePercent;


		if (playingAnimation) {
			_cFrame = playback.cFrame;
		}

		if (m_playing && playingAnimation && !scrubbing) {
			framePercent = playback.framePercentFPS;
			_cFrame = playback.cFrameFPS;
		}

		if (frameMode) {
			playingAnimation = false;
			previewOffset = Vector2.zero;
			viewScaling = scaleMod;
			//previewScale = 1;
		}
		else {
			spritePositions = new Rect[_anim.totalSprites];
			spritePositionIndexes = new int[_anim.totalSprites];
		}
		if (!playingAnimation && frameNo != -1)
			_cFrame = frameNo;
			
		// create temporary sprite list so we can re-order it
		List<MSFrameSprite> tempSpriteList = new List<MSFrameSprite>();
		for (int i = 0; i < _anim.totalSprites; i++) {
			tempSpriteList.Add(_frames[_cFrame].sprites[i]);
			tempSpriteList[i].originalIndex = i;
		}
		// order sprites for drawing
		tempSpriteList.Sort( (a,b) => a.sortOrder.CompareTo(b.sortOrder));

		int oi; // original inxed;
	
		frameCurvePercent = MSCurves.GetCurve(framePercent, _frames[_cFrame].frameCurve);

		for (int i = 0; i < _anim.totalSprites; i++) {
			if (tempSpriteList[i].hide) {
				continue;
			}
			// log original index
			oi = tempSpriteList[i].originalIndex;

			sprite = FindSprite(oi, _cFrame);

			// // continue if still no sprite exists
			// if (sprite == null)
			// 	continue;

			// display empty sprite if no sprite is found
			if (sprite == null) {
				sprite = Sprite.Create(GetEmptySpriteTexture(), new Rect(0,0,missingTexturSize,missingTexturSize), new Vector2(0.5f, 0.5f), _anim.pixelsPerUnit);
				_frames[_cFrame].sprites[oi].sprite = sprite;
			}
			
			// get all the sprite data
			if (!playingAnimation) {
				spritePos = tempSpriteList[i].position;
				spriteScale = tempSpriteList[i].scale;
				spriteRotation = tempSpriteList[i].rotation;
			}
			else {
				float curvedPercent = frameCurvePercent;
				if (tempSpriteList[i].curve > 0)
					curvedPercent = MSCurves.GetCurve(framePercent, tempSpriteList[i].curve - 1);
				spritePos = playback.GetUpdatedPosition(oi, curvedPercent, _cFrame); //_frames[_cFrame].sprites[i].position;
				spriteScale = playback.GetUpdatedScale(oi, curvedPercent, _cFrame); //_frames[_cFrame].sprites[i].scale;
				spriteRotation = -playback.GetUpdatedRotation(oi, curvedPercent, _cFrame); //_frames[_cFrame].sprites[i].rotation;
			}

			if (_anim.pixelPerfect) {
				spritePos = playback.PixelPerfertSnap(spritePos, sprite);
			}

			
			flipX = tempSpriteList[i].flipX;

			spritePos.y *= -1;
			
			LayoutFrameSpriteTexture(rect, sprite, spriteScale * scaleMod, flipX ,spriteRotation, previewOffset + spriteOffset, spritePos, viewScaling, scaleMod, false, true, frameMode, i, oi);
		}
	}

	// This layout just draws the sprite using gui tools - PreviewRenderUtility  is broken in Unity 2017 so this is necessary
	void LayoutFrameSpriteTexture(Rect rect, Sprite sprite, Vector2 scale, bool flipX, float spriteRotation, Vector2 offset, Vector2 spritePos, float viewScale, float scaleMod, bool useTextureRect, bool clipToRect, bool frameMode, int sortedIndex, int originalIndex) {
		// Calculate the pivot offset
		Vector2 pivotOffset = Vector2.zero;
		if ( useTextureRect == false && m_ignorePivot == false ) {
			pivotOffset = ((sprite.rect.size*0.5f) - sprite.pivot) * viewScale;
			pivotOffset.y = -pivotOffset.y;
		}

		spritePos *= viewScale;

		//if (frameMode)
		//	spritePos *= viewScale;

		Rect spriteRectOriginal = (useTextureRect ? sprite.textureRect : sprite.rect);
		Rect texCoords = new Rect( spriteRectOriginal.x/sprite.texture.width, spriteRectOriginal.y/sprite.texture.height, spriteRectOriginal.width/ sprite.texture.width, spriteRectOriginal.height/sprite.texture.height );

		Rect spriteRect = new Rect(Vector2.zero, spriteRectOriginal.size * viewScale);
		spriteRect.width *= scale.x * (1f/scaleMod);
		spriteRect.height *= scale.y * (1f/scaleMod);
		spriteRect.center = rect.center + offset + pivotOffset + (spritePos * _anim.pixelsPerUnit);

		Vector2 pivotPointShift = Vector2.zero;

		if (flipX) {
			spriteRect.width *= -1;
			pivotPointShift = Vector2.left * spriteRect.width;
		}
		

		Matrix4x4 matrixBackup = GUI.matrix;
		GUIUtility.RotateAroundPivot(spriteRotation, spriteRect.center + pivotPointShift - pivotOffset);

		Rect savedSpriteRect = spriteRect;

		if ( clipToRect )
		{
			// If the sprite doesn't fit in the rect, it needs to be cropped, and have it's uv's scaled to compensate (This is way more complicated than it should be!)
			Vector2 croppedRectOffset = new Vector2(Mathf.Max(spriteRect.xMin,rect.xMin), Mathf.Max(spriteRect.yMin,rect.yMin));
			Vector2 croppedRectSize = new Vector2(Mathf.Min(spriteRect.xMax, rect.xMax), Mathf.Min(spriteRect.yMax, rect.yMax)) - croppedRectOffset;
			Rect croppedRect = new Rect( croppedRectOffset, croppedRectSize );
			texCoords.x += ((croppedRect.xMin-spriteRect.xMin)/spriteRect.width)*texCoords.width;
			texCoords.y += ((spriteRect.yMax-croppedRect.yMax)/spriteRect.height)*texCoords.height;
			texCoords.width *= (1.0f-(spriteRect.width-croppedRect.width)/spriteRect.width);
			texCoords.height *= (1.0f-(spriteRect.height-croppedRect.height)/spriteRect.height);

			if (flipX)
				croppedRect.center += pivotPointShift - pivotOffset.x * Vector2.right;

			savedSpriteRect = croppedRect;
			// Draw the texture
			GUI.DrawTextureWithTexCoords(croppedRect, sprite.texture, texCoords, true);
		}
		else 
		{
			//if (flipX) spriteRect.width *= -1;
			// Draw the texture
			GUI.DrawTextureWithTexCoords(spriteRect, sprite.texture, texCoords, true);
		}
		GUI.matrix = matrixBackup;

		if (!frameMode) {
			Rect r = savedSpriteRect;
			r.center -= pivotPointShift;
			r.width = Mathf.Abs(r.width);
			spritePositions[sortedIndex] = r;
			spritePositionIndexes[sortedIndex] = originalIndex;
			
			if (drawDebugSpriteRects || selectingSpriteWithClick > -1) {
				if (originalIndex == selectingSpriteWithClick)
					GUI.DrawTexture(r, GetSelectedSpriteTexture(new Color(1, 0, 0, 0.8f)), ScaleMode.StretchToFill, true);
				else GUI.DrawTexture(r, GetSelectSpriteTexture(new Color(0, 1, 1, 0.6f)), ScaleMode.StretchToFill, true);
			}
		}
	}

	// finds the sprite image to display for this frame
	Sprite FindSprite(int spriteNo, int frameNo) {
		Sprite s;
		// step1 see if frame has a sprite
		s = GetSprite(spriteNo, frameNo, frameNo);
		
		if (s == null) {
			// look for previous sprite
			for (int i = frameNo - 1; i >= 0; i--) {
				s = GetSprite(spriteNo, i, frameNo);
				if (s) break;
			}
			// if previous sprite can't be found, look into the future
			if (s == null) {
				for (int i = _frames.Count - 1; i > frameNo; i--) {
					s = GetSprite(spriteNo, i, frameNo);
					if (s) break;
				}
			}
		}
		return s;
	}

	// grabs a sprite form animation or image
	Sprite GetSprite(int s, int f, int realFrame) {
		Sprite sprite = null;
		float animTime = m_animTime;
		if (_frameMode)
			animTime = _frames[realFrame].startTime;
		// first get one form the animation
		if (_frames[f].sprites[s].animation != null) {
			float animLength =  _frames[f].sprites[s].animation.length;
			ConvertAnimationToReadableFormat(_frames[f].sprites[s].animation);
			while (_frames[f].sprites[s].animation.isLooping && animTime > animLength) {
				animTime -= animLength;
			}
			float clipTime = _frames[f].startTime;
			if (f <= realFrame)
				clipTime = animTime - _frames[f].startTime;
			if (f > realFrame) {
				clipTime = (_frames[_frames.Count-1].endTime - clipTime) + animTime;
			}
			sprite = GetSpriteAtTime(clipTime);
		}

		// next look for it as an image
		if (!sprite)
			sprite = _frames[f].sprites[s].sprite;

		return sprite;
	}


	// used for reading animation data.

	class AnimFrame {
		public float m_time = 0;
		public float m_length = 0;
		public Sprite m_sprite = null;
		public float EndTime {get{ return m_time+m_length; } }
	}

	List<AnimFrame> m_frames;
	[SerializeField] EditorCurveBinding m_curveBinding = new EditorCurveBinding();
	static readonly string PROPERTYNAME_SPRITE = "m_Sprite";

	void ConvertAnimationToReadableFormat(AnimationClip m_clip) {
		// Find curve binding for the sprite. This property works for both UI anims and sprite anims :D
		m_curveBinding = System.Array.Find( AnimationUtility.GetObjectReferenceCurveBindings(m_clip), item=>item.propertyName == PROPERTYNAME_SPRITE ); 
		if ( m_curveBinding.isPPtrCurve ) {
			// Convert frames from ObjectReferenceKeyframe (struct with time & sprite) to our easier to use list of AnimFrame
			ObjectReferenceKeyframe[] objRefKeyframes = AnimationUtility.GetObjectReferenceCurve(m_clip, m_curveBinding );
			m_frames = new List<AnimFrame>(	
				System.Array.ConvertAll<ObjectReferenceKeyframe, AnimFrame>( objRefKeyframes, keyframe => 
				{ 
					return new AnimFrame() { m_time = keyframe.time, m_sprite = keyframe.value as Sprite }; 
				} ));			
		}
	}

	AnimFrame GetFrameAtTime(float time)
	{
		if ( m_frames == null || m_frames.Count == 0 )
			return null;
		int frame = m_frames.FindIndex( item => item.m_time > time );
		if ( frame <= 0 || frame > m_frames.Count  ) 
			frame = m_frames.Count;
		frame--;
		return m_frames[frame];
	}


	Sprite GetSpriteAtTime(float time)
	{   
		AnimFrame frame = GetFrameAtTime(time);
		return ( frame != null ) ? frame.m_sprite as Sprite : null;
	}




}
}