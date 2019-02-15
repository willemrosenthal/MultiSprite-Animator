using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MultiSprite
{

public partial class MultiSpriteEditor {

	// Timeline view's offset from left (in pixels)
	float m_timelineOffset = -TIMELINE_OFFSET_MIN;
	// Unit per second on timeline
	float m_timelineScale = 1000;
	float m_timelineAnimWidth = 1;

	eDragState m_dragState = eDragState.None;

	static readonly float TIMELINE_SCRUBBER_HEIGHT = 16;
	static readonly float TIMELINE_EVENT_HEIGHT = 0;//12;
	static readonly float TIMELINE_BOTTOMBAR_HEIGHT = 0;//24;
	static readonly float TIMELINE_OFFSET_MIN = -10;

    static readonly float SCRUBBER_INTERVAL_TO_SHOW_LABEL = 60.0f;
    static readonly float SCRUBBER_INTERVAL_WIDTH_MIN = 10.0f;
    static readonly float SCRUBBER_INTERVAL_WIDTH_MAX = 80.0f;

	static readonly Color COLOR_UNITY_BLUE = new Color(0.3f,0.5f,0.85f,1);

	static readonly float FRAME_RESIZE_RECT_WIDTH = 8;


	enum eDragState
	{
		None,
		Scrub,
		ResizeFrame,
		MoveFrame,
		SelectFrame,
		MoveEvent,
		SelectEvent,
		MoveNode,
		RotateNode
	}

	int m_resizeFrameId = 0;
	float m_timelineEventBarHeight = TIMELINE_EVENT_HEIGHT;


	void LayoutTimeline( Rect rect )
	{
		Event e = Event.current;

	

        m_timelineScale = Mathf.Clamp( m_timelineScale, 10, 10000 );

        //
        // Update timeline offset
        //
        m_timelineAnimWidth = m_timelineScale * _anim.GetLength();
        if ( m_timelineAnimWidth > rect.width/2.0f ) {
			m_timelineOffset = Mathf.Clamp( m_timelineOffset, rect.width - m_timelineAnimWidth - rect.width/2.0f, -TIMELINE_OFFSET_MIN );
        }
        else {
			m_timelineOffset = -TIMELINE_OFFSET_MIN;
        }

        //
        // Layout stuff
		//
		// Draw scrubber bar <--- timeline at top
		float elementPosY = rect.yMin;
		float elementHeight = TIMELINE_SCRUBBER_HEIGHT;
		LayoutScrubber( new Rect(rect) { yMin = elementPosY, height = elementHeight } );
		elementPosY += elementHeight;

		// Draw frames
		elementHeight = rect.height - ((elementPosY - rect.yMin) + m_timelineEventBarHeight + TIMELINE_BOTTOMBAR_HEIGHT);
		Rect rectFrames = new Rect(rect) { yMin = elementPosY, height = elementHeight };
		LayoutFrames(rectFrames);
		elementPosY += elementHeight;


		// Draw playhead (in front of events bar background, but behind events
		LayoutPlayhead(new Rect(rect) { height = rect.height - TIMELINE_BOTTOMBAR_HEIGHT } );

		// Draw bottom
		elementHeight = TIMELINE_BOTTOMBAR_HEIGHT;

		//
		// Handle events
		//
		if ( rect.Contains( e.mousePosition ) )
		{
			
			if ( e.type == EventType.ScrollWheel )
			{
				float scale = 10000.0f;
				while ( (m_timelineScale/scale) < 1.0f || (m_timelineScale/scale) > 10.0f ) {
                    scale /= 10.0f;
                }
                				
                float oldCursorTime = GuiPosToAnimTime(rect, e.mousePosition.x);

				m_timelineScale -= e.delta.y * scale * 0.05f;
				m_timelineScale = Mathf.Clamp(m_timelineScale,10.0f,10000.0f);

				// Offset to time at old cursor pos is same as at new position (so can zoom in/out of current cursor pos)
				m_timelineOffset += ( e.mousePosition.x - AnimTimeToGuiPos( rect, oldCursorTime ) );

				Repaint();
				e.Use();
			}
			else if ( e.type == EventType.MouseDrag ) 
			{
				if (  e.button == 1 || e.button == 2 )
				{					
					m_timelineOffset += e.delta.x;
					Repaint();
					e.Use();
				}
			}
		}

		if ( e.rawType == EventType.MouseUp && e.button == 0 && ( m_dragState == eDragState.SelectEvent || m_dragState == eDragState.SelectFrame ) )
		{
			m_dragState = eDragState.None;
			Repaint();
		}

	}

	void LayoutScrubber(Rect rect )
	{

        //
        // Calc time scrubber lines
        //
		float minUnitSecond = 1.0f/ 60f;//m_clip.frameRate;		
        float curUnitSecond = 1.0f;
        float curCellWidth = m_timelineScale;
        int intervalId;
		List<int> intervalScales = CreateIntervalSizeList(out intervalId);

        // get curUnitSecond and curIdx
        if ( curCellWidth < SCRUBBER_INTERVAL_WIDTH_MIN ) 
        {
            while ( curCellWidth < SCRUBBER_INTERVAL_WIDTH_MIN ) 
            {
                curUnitSecond = curUnitSecond * intervalScales[intervalId];
                curCellWidth = curCellWidth * intervalScales[intervalId];

                intervalId += 1;
                if ( intervalId >= intervalScales.Count ) 
                {
                    intervalId = intervalScales.Count - 1;
                    break;
                }
            }
        }
        else if ( curCellWidth > SCRUBBER_INTERVAL_WIDTH_MAX ) 
        {
            while ( (curCellWidth > SCRUBBER_INTERVAL_WIDTH_MAX) && 
                    (curUnitSecond > minUnitSecond) ) 
            {
                intervalId -= 1;
                if ( intervalId < 0 ) 
                {
                    intervalId = 0;
                    break;
                }

                curUnitSecond = curUnitSecond / intervalScales[intervalId];
                curCellWidth = curCellWidth / intervalScales[intervalId];
            }
        }

        // check if prev width is good to show
        if ( curUnitSecond > minUnitSecond ) 
        {
            int intervalIdPrev = intervalId - 1;
            if ( intervalIdPrev < 0 )
                intervalIdPrev = 0;
            float prevCellWidth = curCellWidth / intervalScales[intervalIdPrev];
            float prevUnitSecond = curUnitSecond / intervalScales[intervalIdPrev];
            if ( prevCellWidth >= SCRUBBER_INTERVAL_WIDTH_MIN ) {
                intervalId = intervalIdPrev;
                curUnitSecond = prevUnitSecond;
                curCellWidth = prevCellWidth;
            }
        }

        // get lod interval list
        int[] lodIntervalList = new int[intervalScales.Count+1];
        lodIntervalList[intervalId] = 1;
        for ( int i = intervalId-1; i >= 0; --i ) 
        {
            lodIntervalList[i] = lodIntervalList[i+1] / intervalScales[i];
        }
        for ( int i = intervalId+1; i < intervalScales.Count+1; ++i ) 
        {
            lodIntervalList[i] = lodIntervalList[i-1] * intervalScales[i-1];
        }

        // Calc width of intervals
        float[] lodWidthList = new float[intervalScales.Count+1];
        lodWidthList[intervalId] = curCellWidth;
        for ( int i = intervalId-1; i >= 0; --i ) 
        {
            lodWidthList[i] = lodWidthList[i+1] / intervalScales[i];
        }
        for ( int i = intervalId+1; i < intervalScales.Count+1; ++i ) 
        {
            lodWidthList[i] = lodWidthList[i-1] * intervalScales[i-1];
        }

        // Calc interval id to start from
        int idxFrom = intervalId;
        for ( int i = 0; i < intervalScales.Count+1; ++i ) 
        {
            if ( lodWidthList[i] > SCRUBBER_INTERVAL_WIDTH_MAX ) 
            {
                idxFrom = i;
                break;
            }
        }

        // NOTE: +50 here can avoid us clip text so early 
        int iStartFrom = Mathf.CeilToInt( -(m_timelineOffset + 50.0f)/curCellWidth );
        int cellCount = Mathf.CeilToInt( (rect.width - m_timelineOffset)/curCellWidth );

        // draw the scrubber bar
		GUI.BeginGroup(rect, EditorStyles.toolbar);

        for ( int i = iStartFrom; i < cellCount; ++i ) 
        {
            float x = m_timelineOffset + i * curCellWidth + 1;
            int idx = idxFrom;

            while ( idx >= 0 ) 
            {
                if ( i % lodIntervalList[idx] == 0 ) 
                {
                    float heightRatio = 1.0f - (lodWidthList[idx] / SCRUBBER_INTERVAL_WIDTH_MAX);

                    // draw scrubber bar
                    if ( heightRatio >= 1.0f ) 
                    {                           
                        DrawLine ( new Vector2(x, 0 ), 
                                   new Vector2(x, TIMELINE_SCRUBBER_HEIGHT), 
                                   Color.gray); 
                        DrawLine ( new Vector2(x+1, 0 ), 
                                   new Vector2(x+1, TIMELINE_SCRUBBER_HEIGHT), 
                                   Color.gray);
                    }
                    else
                    {
						DrawLine ( new Vector2(x, TIMELINE_SCRUBBER_HEIGHT * heightRatio ), 
                                   new Vector2(x, TIMELINE_SCRUBBER_HEIGHT ), 
                                   Color.gray);
                    }

                    // draw lable
                    if ( lodWidthList[idx] >= SCRUBBER_INTERVAL_TO_SHOW_LABEL ) 
                    {
						GUI.Label ( new Rect( x + 4.0f, -2, 50, 15 ), 
							ToTimelineLabelString(i*curUnitSecond, 60f), EditorStyles.miniLabel ); //m_clip.frameRate
                    }

                    //
                    break;
                }
                --idx;
            }
        }

        GUI.EndGroup();

        //
        // Scrubber events
        //

		Event e = Event.current;
		if ( rect.Contains( e.mousePosition ) )
		{
			if ( e.type == EventType.MouseDown ) 
			{
				if ( e.button == 0 )
				{
					// gert the scrub frame
					scrubFrame = frameIndex;
					m_animTime = GuiPosToAnimTime(rect, e.mousePosition.x);
					if (m_animTime > _frames[_frames.Count-1].endTime)
						m_animTime = _frames[_frames.Count-1].endTime;
					while (_frames[scrubFrame].endTime < m_animTime) {
						scrubFrame++;
					}
					while (_frames[scrubFrame].startTime > m_animTime) {
						scrubFrame--;
					}
					
					m_dragState = eDragState.Scrub;
					GUI.FocusControl("none");
					e.Use();
				}
			}
		}
		// dragging scrubber
		if ( m_dragState == eDragState.Scrub && e.button == 0 )
		{
			if ( e.type == EventType.MouseDrag )
			{
				scrubbing = true;
				m_playing = false;
				m_animTime = GuiPosToAnimTime(rect, e.mousePosition.x);
				if (m_animTime > _frames[_frames.Count-1].endTime)
					m_animTime = _frames[_frames.Count-1].endTime;
				e.Use();
			}
			else if ( e.type == EventType.MouseUp )
			{
				scrubbing = false;
				// snap to nearest frame
				if (m_animTime - _frames[scrubFrame].startTime < _frames[scrubFrame].endTime - m_animTime)
					m_animTime = _frames[scrubFrame].startTime;
				else {
					m_animTime = _frames[scrubFrame].endTime;
					if (_frames.Count -1 > scrubFrame) {
						scrubFrame++;
						m_animTime = _frames[scrubFrame].startTime;
					}
				}
				SelectFrame(scrubFrame);

				m_dragState = eDragState.None;
				e.Use();
			}
		}
	}


	void LayoutFrames(Rect rect)
	{
		Event e = Event.current;

		GUI.BeginGroup(rect, Styles.TIMELINE_ANIM_BG);

		//DrawRect( new Rect(0,0,rect.width,rect.height), new Color(0.3f,0.3f,0.3f,1));

		for ( int i = 0; i < _frames.Count; ++i ) { // NB: ignore final dummy keyframe
			// Calc time of next frame
			float prevFrameTime = 0;
			if (i > 0)
				prevFrameTime = _frames[i-1].endTime;
			LayoutFrame(rect, i, prevFrameTime, _frames[i].endTime);
		}

		// Draw rect over area that has no frames in it
		if ( m_timelineOffset > 0 )
		{			
			// Before frames start
			DrawRect( new Rect(0,0,m_timelineOffset,rect.height), new Color(0.4f,0.4f,0.4f,0.2f) );
			DrawLine( new Vector2(m_timelineOffset,0), new Vector2(m_timelineOffset,rect.height), new Color(0.4f,0.4f,0.4f) );
		}
		float endOffset = m_timelineOffset + (_anim.GetLength() * m_timelineScale);
		if ( endOffset < rect.xMax ) {
			// After frames end
			DrawRect( new Rect(endOffset,0,rect.width-endOffset,rect.height), new Color(0.4f,0.4f,0.4f,0.2f) );
		}

		GUI.EndGroup();


		if ( m_dragState == eDragState.None )
		{
			//
			// Check for unhandled mouse left click. It should deselect any selected frames
			//
			if ( e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition) )
			{				
				e.Use();
			}
			// Check for unhanlded drag, it should start a select
			if ( e.type == EventType.MouseDrag && e.button == 0 && rect.Contains(e.mousePosition) )
			{		
				m_dragState = eDragState.SelectFrame;
				e.Use();				
			}
		}
		else if ( m_dragState == eDragState.ResizeFrame )
		{
			// While resizing frame, show the resize cursor
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.ResizeHorizontal );
		}
		else if ( m_dragState == eDragState.MoveFrame )
		{
			// While moving frame, show the move cursor
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.MoveArrow );
		}
	}

	void LayoutFrame( Rect rect, int frameId, float startTime, float endTime )
	{
		float startOffset = m_timelineOffset + (startTime * m_timelineScale);
		float endOffset = m_timelineOffset + (endTime * m_timelineScale);

		// check if it's visible on timeline
		if ( startOffset > rect.xMax || endOffset < rect.xMin )
			return;
		//AnimFrame animFrame = m_frames[frameId];
		Rect frameRect = new Rect(startOffset, 0, endOffset-startOffset, rect.height);
		bool selected = false;
		if (frameId == frameIndex)
			selected = true;
		//highlight selected frames
		if ( selected ) {
			//frameRect.width = rect.width;
			DrawRect( frameRect, ColorAlpha(Color.grey, 0.3f) );
		}
		DrawLine( new Vector2(endOffset,0), new Vector2(endOffset,rect.height), new Color(0.4f,0.4f,0.4f) );
		LayoutTimelineSprite( frameRect, frameId );

		//
		// Frame clicking events
		//
		
		Event e = Event.current;
		
		if ( m_dragState == eDragState.None ) {
			// Move cursor (when selected, it can be dragged to move it)
			if ( selected )
			{	
				EditorGUIUtility.AddCursorRect( new Rect(frameRect) { xMin = frameRect.xMin+FRAME_RESIZE_RECT_WIDTH*0.5f, xMax = frameRect.xMax-FRAME_RESIZE_RECT_WIDTH*0.5f }, MouseCursor.MoveArrow );
			}

			//
			// Resize rect
			//
			Rect resizeRect = new Rect(endOffset-(FRAME_RESIZE_RECT_WIDTH*0.5f),0,FRAME_RESIZE_RECT_WIDTH,rect.height);
			EditorGUIUtility.AddCursorRect( resizeRect, MouseCursor.ResizeHorizontal );

			//
			// Check for Start Resizing frame
			//
			if ( e.type == EventType.MouseDown && e.button == 0 && resizeRect.Contains(e.mousePosition) )
			{
				// Start resizing the frame
				m_dragState = eDragState.ResizeFrame;
				m_resizeFrameId = frameId;
				GUI.FocusControl("none");
				e.Use();
			}

			//
			// Handle Frame Selection
			//
			if ( selected == false && e.type == EventType.MouseDown && e.button == 0 && frameRect.Contains(e.mousePosition) ) {
				// Started clicking unselected - start selecting
				m_dragState = eDragState.SelectFrame;
				SelectFrame(frameId);
				GUI.FocusControl("none");
				e.Use();
			}

		}
		else if ( m_dragState == eDragState.ResizeFrame )
		{
			// Check for resize frame by dragging mouse
			if ( e.type == EventType.MouseDrag && e.button == 0 && m_resizeFrameId == frameId )
			{	
				float newFrameLength = GuiPosToAnimTime(new Rect(0,0,position.width,position.height), e.mousePosition.x) - startTime;
				newFrameLength = Mathf.Max(newFrameLength, 1.0f / 60f);
				SetFrameLength(frameId, newFrameLength);
			

				e.Use();
				Repaint();				
			}

			// Check for finish resizing frame
			if ( e.type == EventType.MouseUp && e.button == 0 && m_resizeFrameId == frameId)
			{
				m_dragState = eDragState.None;
				//ApplyChanges();
				e.Use();			
			}
		}
		else if ( m_dragState == eDragState.SelectFrame ) {
			if ( e.type == EventType.MouseUp && e.button == 0 ) {
				m_dragState = eDragState.None;
				e.Use();
			}
		}
	}

	float FrameStartTime( int frameId) {
		return _frames[frameId].endTime - _frames[frameId].frameTime;
	}
	
	void LayoutTimelineSprite( Rect rect, int frameNo, bool useForFirstSpritePreview = false ) {
		float scale = 0.85f;

		float xMin = 0;
		float xMax = 0;
		float yMin = 0;
		float yMax = 0;
		Vector2 spriteOriginOffset = Vector2.zero;

		for (int i = 0; i < _anim.totalSprites; i++) {
			if (_frames[frameNo].sprites[i].hide) {
				continue;
			}

			Sprite sprite = FindSprite(i, frameNo);

			// continue if still no sprite exists
			if (sprite == null)
				continue;
			
			Vector2 spriteScale = _frames[frameNo].sprites[i].scale;
			
			float _xMin = ((float)_frames[frameNo].sprites[i].position.x * (float)sprite.pixelsPerUnit) - sprite.textureRect.width * 0.55f * spriteScale.x;
			float _xMax = ((float)_frames[frameNo].sprites[i].position.x * (float)sprite.pixelsPerUnit) + sprite.textureRect.width * 0.55f * spriteScale.x;
			float _yMin = ((float)_frames[frameNo].sprites[i].position.y * (float)sprite.pixelsPerUnit) - sprite.textureRect.height * 0.55f * spriteScale.y;
			float _yMax = ((float)_frames[frameNo].sprites[i].position.y * (float)sprite.pixelsPerUnit) + sprite.textureRect.height * 0.55f * spriteScale.y;


			if (_xMin < xMin)
				xMin = _xMin;
			if (_xMax > xMax)
				xMax = _xMax;
			if (_yMin < yMin)
				yMin = _yMin;
			if (_yMax > yMax)
				yMax = _yMax;
		}


		// caclulates origin offset
		spriteOriginOffset.x = Mathf.Abs(xMin - xMax) * 0.5f;
		spriteOriginOffset.y = Mathf.Abs(yMin - yMax) * 0.5f;

		spriteOriginOffset.x = Mathf.Abs(xMin) - spriteOriginOffset.x;
		spriteOriginOffset.y = Mathf.Abs(yMin) - spriteOriginOffset.y;

		spriteOriginOffset.y *= -1;

		Vector2 combinedSpriteSize = new Vector2(xMax - xMin, yMax - yMin);

		if ( combinedSpriteSize.x > 0 && combinedSpriteSize.y > 0 )
		{
			float widthScaled = rect.width / combinedSpriteSize.x;
			float heightScaled = rect.height / combinedSpriteSize.y;
			// Finds best fit for timeline window based on sprite size
			if ( widthScaled < heightScaled)
			{
				scale *= rect.width / combinedSpriteSize.x;
			}
			else 
			{
				scale *= rect.height / combinedSpriteSize.y;
			}
		}

		spriteOriginOffset *= scale;

		// if being used ot size the preview when opening a enw animation
		if (useForFirstSpritePreview) {
			m_previewScale = scale;
			m_previewOffset = spriteOriginOffset;
			return;
		}

		DrawAllFrameSprites(rect, false, frameNo, scale, true, spriteOriginOffset.x, spriteOriginOffset.y);
	}

	void LayoutPlayhead(Rect rect)
	{		
		float offset = rect.xMin + m_timelineOffset + (m_animTime * m_timelineScale);
		DrawLine( new Vector2(offset, rect.yMin), new Vector2(offset,rect.yMax),Color.red );
	}


	void LayoutReplaceFramesBox( Rect rect, int frameId, int numFrames ) {
		float time = frameId < _frames.Count ? _frames[frameId].startTime : _anim.GetLength();
		float startPosOnTimeline = m_timelineOffset + (time * m_timelineScale);
		int finalTimeId = frameId+numFrames;
		float finalTime = finalTimeId < _frames.Count ? _frames[finalTimeId].startTime : _anim.GetLength()+0.0f;
		float endPosOnTimeline = m_timelineOffset + (finalTime * m_timelineScale);

		Rect selectionRect = new Rect(rect){ xMin = Mathf.Max(rect.xMin,startPosOnTimeline), xMax = Mathf.Min(rect.xMax, endPosOnTimeline) };
		DrawRect(selectionRect,ColorAlpha(COLOR_UNITY_BLUE, 0.1f), ColorAlpha(COLOR_UNITY_BLUE, 0.6f));	
	}



    public static string ToTimelineLabelString( float seconds, float sampleRate ) 
    {
		return string.Format( "{0:0}:{1:00}",Mathf.FloorToInt(seconds),(seconds%1.0f)*100.0f );
    }

    List<int> CreateIntervalSizeList(out int intervalId)
    {
		List<int> intervalSizes = new List<int>();
        int tmpSampleRate = (int) 60f;//m_clip.frameRate;
        while ( true ) {
            int div = 0;
            if ( tmpSampleRate == 30 ) {
                div = 3;
            }
            else if ( tmpSampleRate % 2 == 0 ) {
                div = 2;
            }
            else if ( tmpSampleRate % 5 == 0 ) {
                div = 5;
            }
            else if ( tmpSampleRate % 3 == 0 ) {
                div = 3;
            }
            else {
                break;
            }
            tmpSampleRate /= div;
            intervalSizes.Insert(0,div);
        }
		intervalId = intervalSizes.Count;
        intervalSizes.AddRange( new int[] { 
                            5, 2, 3, 2,
                            5, 2, 3, 2,
                            } );
		return intervalSizes;
    }

    float GuiPosToAnimTime(Rect rect, float mousePosX)
    {
		float pos = mousePosX - rect.xMin;
		return ((pos-m_timelineOffset) / m_timelineScale );
    }

    float AnimTimeToGuiPos(Rect rect, float time)
    {
    	return rect.xMin + m_timelineOffset + (time*m_timelineScale);
    }

	// Returns the point- Set to m_frames.Length if should insert after final frame
	int MousePosToInsertFrameIndex(Rect rect)
	{
		if ( _frames.Count == 0 )
			return 0;

		// Find point between two frames closest to mouse cursor so we can show indicator
		float closest = float.MaxValue;
		float animTime = GuiPosToAnimTime(rect, Event.current.mousePosition.x);
		int closestFrame = 0;
		for ( ; closestFrame < _frames.Count+1; ++closestFrame )
		{
			// Loop through frames until find one that's further away than the last from the mouse pos
			// For final iteration it checks the end time of the last frame rather than start time
			float frameStartTime = closestFrame < _frames.Count ? _frames[closestFrame].startTime : _frames[closestFrame-1].endTime; 
			float diff = Mathf.Abs(frameStartTime-animTime);
			if ( diff > closest)
				break;
			closest = diff;
		}

		closestFrame = Mathf.Clamp(closestFrame-1, 0, _frames.Count);
		return closestFrame;
	}

	// Returns frame mouse is hovering over
	int MousePosToReplaceFrameIndex(Rect rect)
	{
		if ( _frames.Count == 0 )
			return 0;

		// Find point between two frames closest to mouse cursor so we can show indicator

		float animTime = GuiPosToAnimTime(rect, Event.current.mousePosition.x);
		int closestFrame = 0;
		while ( closestFrame < _frames.Count && _frames[closestFrame].endTime <= animTime )
			++closestFrame;
		closestFrame = Mathf.Clamp(closestFrame, 0, _frames.Count);
		return closestFrame;
	}

	static void DrawLine( Vector2 from, Vector2 to, Color color, float width = 0, bool snap = true )  {
		if ( (to - from).sqrMagnitude <= float.Epsilon )
			return;

		if ( snap )
		{
			from.x = Mathf.FloorToInt(from.x); 
			from.y = Mathf.FloorToInt(from.y);
			to.x = Mathf.FloorToInt(to.x); 
			to.y = Mathf.FloorToInt(to.y);
		}

		Color savedColor = Handles.color;
		Handles.color = color;

		if ( width > 1.0f ) 
			Handles.DrawAAPolyLine(width, new Vector3[] { from, to } );
		else 
			Handles.DrawLine( from, to );

		Handles.color = savedColor;
	}
	static void DrawRect( Rect rect, Color backgroundColor ) {
		EditorGUI.DrawRect(rect, backgroundColor);
	}
	static void DrawRect( Rect rect, Color backgroundColor, Color borderColor, float borderWidth = 1 ) {
		// draw background
		EditorGUI.DrawRect(rect, backgroundColor);

		// Draw border
		rect.width = rect.width - borderWidth;
		rect.height = rect.height - borderWidth;
		DrawLine(new Vector2(rect.xMin,rect.yMin), new Vector2(rect.xMin,rect.yMax), borderColor, borderWidth);
		DrawLine(new Vector2(rect.xMin,rect.yMax), new Vector2(rect.xMax,rect.yMax), borderColor, borderWidth);
		DrawLine(new Vector2(rect.xMax,rect.yMax), new Vector2(rect.xMax,rect.yMin), borderColor, borderWidth);
		DrawLine(new Vector2(rect.xMax,rect.yMin), new Vector2(rect.xMin,rect.yMin), borderColor, borderWidth);
	}
	
	void SetFrameLength(int frameId, float length) {
		if (  Mathf.Approximately( length, _frames[frameId].frameTime ) == false ) {
			_frames[frameId].frameTime = Mathf.Max(GetMinFrameTime(), SnapTimeToFrameRate(length));
			RecalcFrameTimes();
			ChangeMade();
		}
	}

	float GetMinFrameTime() {
		return 1.0f/60f;
	}

	/// Snaps a time to the closest sample time on the timeline
	float SnapTimeToFrameRate(float value) {
		return Mathf.Round(value * 60f) / 60f;
	}

	/// Update the times of all frames based on the lengths
	void RecalcFrameTimes() {    	
		float time = 0;
		foreach ( MSFrame frame in _frames ) {
			frame.startTime = time;
			time += frame.frameTime;
			frame.endTime = time;
		}
	}

}

}