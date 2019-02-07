using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiSprite {
public class MSPlayback {

    MSAnimation animation;

    public int cFrame; // current frame
    public float framePercent; // percent to next frame
    public float frameTime; // time elapsed since last frmae change
    public float time; // total time elapsed in animation
    public float fpsTimer; // time since frame changed 
    float fps; // fps of the current animation
    public float framePercentFPS;
    public int cFrameFPS;

    float pixelUnit;


    public void PrepareAnimationData(MSAnimation newAnimation) {
        // gets ref to new animation
        animation = newAnimation;
        // sets up fps
        fps = 1f/(float)newAnimation.fps;
        // rests all data
        ResetPlayback();
        // gets time data
        float totalTime = 0;
        for (int i = 0; i < animation.GetTotalFrames(); i++) {
            newAnimation.SetFrameStartTime(i, totalTime);
            totalTime += animation.GetFrameTime(i);
            newAnimation.SetFrameEndTime(i, totalTime);
        }

        // caclulate pixel snap stuff
        if (animation.pixelPerfect)
            pixelUnit = 1f/animation.pixelsPerUnit;
    }

    public void ResetPlayback() {
        cFrame = 0;
        framePercent = 0;
        frameTime = 0;
        time = 0;
        cFrameFPS = 0;
        framePercentFPS = 0;
    }

    public void IncrementTime(float deltaTime) {
        time += deltaTime;
        frameTime += deltaTime;
        fpsTimer += deltaTime;
        framePercent = animation.GetFramePercent(cFrame, frameTime);
        
        if (animation.limitToFPS)
            FPSBasedFramePercent(); 
        else {
            framePercentFPS = framePercent;
            cFrameFPS = cFrame;
        }
    }

    public void AdvanceFrame(bool forceLoop = false) {
        // increments frame
        while (cFrame < animation.GetTotalFrames() && animation.GetFrameEndTime(cFrame) < time) {
            cFrame++;
        }

        // get frametime
        if (cFrame > 0)
            frameTime = time - animation.GetFrameEndTime(cFrame-1);
		
        // reached final frame
        if (cFrame == animation.GetTotalFrames()) {
            if (animation.loop || forceLoop) {
                LoopBack();
                return;
            }
            else {
                cFrame--;
                frameTime = animation.GetFrameTime(cFrame);
            }
        }

        // gets new frame percent
        framePercent = animation.GetFramePercent(cFrame, frameTime);
        
        // updates fps frame data
        framePercentFPS = framePercent;
        cFrameFPS = cFrame;
    }

    void FPSBasedFramePercent() {
        // animation plays at fps
        if(fpsTimer > fps) {
            framePercentFPS = framePercent;
            cFrameFPS = cFrame;
            fpsTimer -= fps;
        }
    }

    void LoopBack() {
        cFrame = 0;
        time -= animation.GetTotalTime();
        frameTime = time;
        AdvanceFrame();
    }

    public Vector2 GetUpdatedPosition(int sprite, float curvedPercent = -1, int _frame = -1) {
        if (_frame == -1) _frame = cFrameFPS;
        if (curvedPercent == -1) curvedPercent = framePercent;
        return Vector2.Lerp(animation.GetSpritePosition(_frame, sprite), animation.GetSpritePosition(_frame, sprite, true), curvedPercent);
    }
    public Vector2 GetUpdatedScale(int sprite, float curvedPercent = -1, int _frame = -1) {
        if (_frame == -1) _frame = cFrameFPS;
        if (curvedPercent == -1) curvedPercent = framePercent;
        return Vector2.Lerp(animation.GetSpriteScale(_frame, sprite), animation.GetSpriteScale(_frame, sprite, true), curvedPercent);
    }
    public float GetUpdatedRotation(int sprite, float curvedPercent = -1, int _frame = -1) {
        if (_frame == -1) _frame = cFrameFPS;
        if (curvedPercent == -1) curvedPercent = framePercent;
        return Mathf.Lerp(animation.GetSpriteRotation(_frame, sprite), animation.GetSpriteRotation(_frame, sprite, true), curvedPercent);
    }

    // not being used atm
    public int GetCurvedSortOrder(int sprite, float curvedPercent = -1, int _frame = -1) {
        if (_frame == -1) _frame = cFrameFPS;
        if (curvedPercent == -1) curvedPercent = framePercent;
        return (int)Mathf.Round(Mathf.Lerp((float)animation.GetSortOrder(_frame, sprite), (float)animation.GetSortOrder(_frame, sprite, true), curvedPercent));
    }

    public Vector2 PixelPerfertSnap(Vector2 spritePos, Sprite sprite) {
        // snap position
        spritePos.x = Mathf.Floor(spritePos.x * animation.pixelsPerUnit)/ animation.pixelsPerUnit;
        spritePos.y = Mathf.Floor(spritePos.y * animation.pixelsPerUnit)/ animation.pixelsPerUnit;

        // get how far off a pixel the pivot point is
        float pivotDiffX = (sprite.pivot.x - Mathf.Floor(sprite.pivot.x)) * pixelUnit;
        float pivotDiffY = (sprite.pivot.y - Mathf.Floor(sprite.pivot.y)) * pixelUnit;
        
        // apply that difference back to the position
        spritePos.x += pivotDiffX; 
        spritePos.y += pivotDiffY;

        // move off half pixels
        if (pivotDiffX >= 0.5f)
            spritePos.x -=  pixelUnit * 0.5f;
        if (pivotDiffY >= 0.5f)
            spritePos.y -=  pixelUnit * 0.5f;

        return spritePos;
    }


}
}
