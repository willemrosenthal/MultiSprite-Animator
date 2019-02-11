using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using PowerTools;
using UnityEditorInternal;

namespace MultiSprite {
	
public partial class MultiSpriteEditor: EditorWindow {

	MSAnimation _animOriginal = null;
	MSAnimation _anim = null;
	List<MSFrame> _frames = null;
	int frameIndex = 0;
	int spriteIndex = 0;
	string[] spriteLables;
	bool changesMade = false;
	bool popSave = false;

	[SerializeField] Object obj = null;

	// curves
	string[] frameCurves;

	// layers
	string[] gameLayers = null;
	List<int> spriteLayers; //System.Array.IndexOf (array, item)

	// this is used to see if spriteAnim exists
	System.Type asType;


	// animation playback
	[SerializeField] bool m_playing = false;
	float m_animTime = 0;
	double m_editorTimePrev = 0;

	bool scrubbing = false;
	int scrubFrame = 0;

	// animation preivew
	float m_previewSpeedScale = 1.0f;
	float m_previewScale = 1.0f;
	Vector2 m_previewOffset = Vector2.down;
	[SerializeField] bool m_previewloop = true;
	bool m_gameWasPlaying;

	[SerializeField] int pixelsInCheckerboard = 32;

	MSPlayback playback;

	Color debugColor;

	bool displayScaleAndRotate = false;

	bool displayAdvancedOptions = false;
	bool displayAnimationSettings = false;
	[SerializeField] bool drawDebugSpriteRects = false;
	int selectingSpriteWithClick = -1;

	// key commands
	bool rotateDragMode = false;

	[MenuItem("Window/Multisprite Animation")]
	public static void ShowWindow() {
		EditorWindow.GetWindow<MultiSpriteEditor>("MS Anim Editior");
	}

	void OnEnable() {
		CheckIfPowerToolsPluginIsInstalled();
		BuildGameLayers();
		OnSelectionChange();
	}

	void OnFocus() {
		OnSelectionChange();
		CheckIfPowerToolsPluginIsInstalled();
	}

	void OnDisable() {
		if (popSave)
			Revert();
	}

	void CheckIfPowerToolsPluginIsInstalled() {
		Assembly assembly = typeof(PowerToolsIntegration).Assembly;

		System.Type[] tps = assembly.GetTypes();
		for (int i = 0; i < tps.Length; i++) {
			if (tps[i].Name == "SpriteAnim") {
				asType = tps[i];
			}
		}

		if (asType != null)
			MSEditorUtils.AddDefineIfNecessary("POWER_TOOLS", BuildTargetGroup.Standalone);
		else MSEditorUtils.RemoveDefineIfNecessary("POWER_TOOLS", BuildTargetGroup.Standalone);
	}

	void BuildGameLayers() {
		string[] gll = GetSortingLayerNames();
		gameLayers = new string[gll.Length + 1];
		gameLayers[0] = "Use Parent";
		for (int i = 1; i < gameLayers.Length; i++) {
			gameLayers[i] = gll[i - 1];
		}
	}

	public string[] GetSortingLayerNames() {
		System.Type internalEditorUtilityType = typeof(InternalEditorUtility);
		PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
		return (string[])sortingLayersProperty.GetValue(null, new object[0]);
	}

	public MultiSpriteEditor() {
		EditorApplication.update += Update;
		Undo.undoRedoPerformed += OnUndoRedo;
	}

	void OnUndoRedo() {
		//if animation still selected, undo changes
		if (_anim != null) {
			int lastindex = frameIndex;
			NewAnimationSelected(false);
			Repaint();
			frameIndex = lastindex;
		}
	}

	void ChangeMade() {
		changesMade = true;
		popSave = true;
	}

	/// Unity event called when the selectd object changes
	void OnSelectionChange() {
		if (obj != null && obj != Selection.activeObject && obj is MSAnimation && Selection.activeObject is MSAnimation && popSave) {
			Revert();
		}
		
		if (Selection.activeObject is DefaultAsset)
			return;

			
		obj = Selection.activeObject;
		//if (obj) Selection.selectionChanged
		if ( (obj != _anim || _frames == null) && obj is MSAnimation ) {

			_anim = Selection.activeObject as MSAnimation;

			// creates a backed-up version of the animation pre editing
			_animOriginal = ScriptableObject.CreateInstance<MSAnimation>();
			_animOriginal.CopyDataFrom(_anim);
			
			NewAnimationSelected();
		}
		Repaint();
	}

	void NewAnimationSelected(bool sizeToFit = true) {
		_frames = _anim.ConvertForEditor();
		_animFrameList = null;
		frameIndex = 0;

		spriteLables = null;

		ConvertSpriteLayerForEditing();
		BuildFrameCurveArray();

		playback = new MSPlayback();
		playback.PrepareAnimationData(_anim);


		if (sizeToFit) {
			Rect previewRect = new Rect(0, 20, position.width-FRAME_PANEL_WIDTH-SPRITE_PANEL_WIDTH, position.height-20-TIMELINE_HEIGHT);
			previewRect.width -= 50;
			previewRect.height -= 50;
			LayoutTimelineSprite(previewRect, 0, true);
		}
		if (m_previewScale < 0)
			m_previewScale *= -1;
	}

	void BuildFrameCurveArray() {
		frameCurves = new string[MSCurves.curves.Length + 1];
		frameCurves[0] = "Use Frame Default";
		for (int i = 1; i < frameCurves.Length; i++) {
			frameCurves[i] = MSCurves.curves[i - 1];
		}
	}

	public void OnGUI() {
		GUI.SetNextControlName("none");
		// If no sprite selected, show editor	
		if ( _anim == null || _frames == null )
		{
			GUILayout.Space(10);
			GUILayout.Label("No animation selected", EditorStyles.centeredGreyMiniLabel);
			return;
		}
		BuildEditorGUI();
	}



	void BuildEditorGUI() {
		//
		// Toolbar
		// 
		GUILayout.BeginHorizontal( Styles.PREVIEW_BUTTON ); {// EditorStyles.toolbar );
			//LayoutToolbarDebugData();
			LayoutToolbarPlay();
			LayoutToolbarLoop();
			LayoutToolbarScaleSlider();
			LayoutToolbarResetPreviewOffset();
			LayoutToolbarAnimListName();
			LayoutToolbarTimeScaleSlider();
		}
		GUILayout.EndHorizontal();

		//
		// Preview
		//

		Rect lastRect = GUILayoutUtility.GetLastRect();

		//
		// Frames Panel
		//
		Rect infoPanelRect = new Rect(lastRect.xMin+position.width-FRAME_PANEL_WIDTH, lastRect.yMax, FRAME_PANEL_WIDTH, position.height-lastRect.yMax-TIMELINE_HEIGHT);
		LayoutInfoPanel(infoPanelRect);


		//
		// Sprites Panel
		//
		Rect spritesPanelRect = new Rect(lastRect.xMin+position.width-SPRITE_PANEL_WIDTH-FRAME_PANEL_WIDTH, lastRect.yMax, SPRITE_PANEL_WIDTH, position.height-lastRect.yMax-TIMELINE_HEIGHT);
		LayoutSpritePanel(spritesPanelRect);

		//
		// frame preview
		//

		Rect previewRect = new Rect(lastRect.xMin, lastRect.yMax, position.width-FRAME_PANEL_WIDTH-SPRITE_PANEL_WIDTH, position.height-lastRect.yMax-TIMELINE_HEIGHT);
		LayoutPreview( previewRect );

		//
		// Timeline
		//
		Rect timelineRect = new Rect(0, previewRect.yMax, position.width, TIMELINE_HEIGHT );
		LayoutTimeline(timelineRect);

	}

	void LayoutPreview( Rect rect ) {
		
		//
		// Draw checkerboard
		//
		Rect checkboardCoords = new Rect( Vector2.zero, rect.size / (pixelsInCheckerboard * m_previewScale) );
		checkboardCoords.center = new Vector2(-m_previewOffset.x,m_previewOffset.y) / (pixelsInCheckerboard * m_previewScale);
		GUI.DrawTextureWithTexCoords(rect, GetCheckerboardTexture(), checkboardCoords, false);

		// 
		// Position Slider
		//
		// Rect previewOffsetRect = rect;
		// previewOffsetRect.width = 20;
		// previewOffsetRect.height *= .8f;
		// previewOffsetRect.center = previewOffsetRect.center + Vector2.right * (rect.width - 20) + Vector2.up * previewOffsetRect.height * 0.1f;
		// m_previewOffset.y = GUI.VerticalSlider(previewOffsetRect, m_previewOffset.y, 200.0f, -100.0f);


		//
		// Draw sprite
		//
		bool playbackPlaymode = m_playing;
		if (scrubbing) {
			//scrubFrame = 0;
			// gets scrub frame
			if ( m_animTime < _anim.GetLength() && m_animTime >= 0 ) {
				while (_frames[scrubFrame].endTime < m_animTime) {
					scrubFrame++;
				}
				while (_frames[scrubFrame].startTime > m_animTime) {
					scrubFrame--;
				}
				playback.cFrame = scrubFrame;
				playback.frameTime = m_animTime - _frames[scrubFrame].startTime;
				playback.IncrementTime(0);
			}
			playbackPlaymode = true;
		}
		DrawAllFrameSprites(rect, playbackPlaymode);

		// DRAW ORIGIN POINT

		Rect originCoords = new Rect( Vector2.zero, (Vector2.one * 14));
		originCoords.center = rect.center + m_previewOffset;
		GUI.DrawTexture(originCoords, GetOriginTexture());

		//
		// Handle layout events
		//
		// LISTEN FOR KEY COMMANDS
		Event e = Event.current;
		switch (e.type) {
			case EventType.KeyDown: {
				if (Event.current.keyCode == (KeyCode.R)) {
					rotateDragMode = true;
				} 
				break;
			}
			case EventType.KeyUp: {
				if (Event.current.keyCode == (KeyCode.R)) {
					rotateDragMode = false;
				} 
				if (Event.current.keyCode == (KeyCode.LeftArrow) && e.command) {
					SwitchSelectedSprite(-1);
				} 
				if (Event.current.keyCode == (KeyCode.RightArrow) && e.command) {
					SwitchSelectedSprite(1);
				} 
				break;
			}
		}

		// turns this off for the next frame
		selectingSpriteWithClick = -1;

		if ( rect.Contains( e.mousePosition ) ) {
			if ( e.type == EventType.ScrollWheel ) {
				float scale = 1000.0f;
				while ( (m_previewScale/scale) < 1.0f || (m_previewScale/scale) > 10.0f ) {
					scale /= 10.0f;
				}
				m_previewScale -= e.delta.y * scale * 0.05f;
				m_previewScale = Mathf.Clamp(m_previewScale,0.1f,100.0f);
				Repaint();
				e.Use();
			}
			else if ( e.type == EventType.MouseDrag ) {
				if ( _frames.Count > frameIndex && _frames[frameIndex] != null && (spriteIndex < _frames[frameIndex].sprites.Count)) {

					if (( e.button == 1 || e.button == 2 ) && !rotateDragMode ) {
						m_previewOffset += e.delta;
						Repaint();
						e.Use();
					}

					// editing
					if (!m_playing && !scrubbing) {
						// rotation
						if (( e.button == 0 || e.button == 1 ) && rotateDragMode) {
							_frames[frameIndex].sprites[spriteIndex].rotation += e.delta.x + e.delta.y;
							Repaint();
							e.Use();
							ChangeMade();
						}
						// move all
						else if ( e.button == 0 && e.shift ) {
							Vector2 d = e.delta;
							d.y *= -1;
							for (int i = 0; i < _anim.totalSprites; i++ ) {
								_frames[frameIndex].sprites[i].position += d * (1f/_anim.pixelsPerUnit) / m_previewScale;
							}
							Repaint();
							e.Use();
							ChangeMade();
						}
						// move
						else if ( e.button == 0 ) {
							Vector2 d = e.delta;
							d.y *= -1;
							_frames[frameIndex].sprites[spriteIndex].position += d * (1f/_anim.pixelsPerUnit) / m_previewScale;
							Repaint();
							e.Use();
							ChangeMade();
						}
					}
				}
			}
			// select sprite
			else if (e.button == 0 && e.command && !rotateDragMode && !m_playing && !scrubbing) { //(e.type == EventType.ContextClick) || 
				if ( _frames.Count > frameIndex && _frames[frameIndex] != null && (spriteIndex < _frames[frameIndex].sprites.Count)) {
					// editing
					if (!m_playing) {
						ClickSelectSprite(e.mousePosition, e);
					}
				}
			}
		}

	}


	void ClickSelectSprite(Vector2 mousePos, Event e) {
		// see if pos is inside any boxes, from the topmost down
		for (int i = spritePositions.Length-1; i >= 0; i--) {
			//Debug.Log("searching... " + _anim.spriteLable[spritePositionIndexes[i]]);
			if (spritePositions[i].Contains(mousePos)) {
				//Debug.Log("got it " + _anim.spriteLable[spritePositionIndexes[i]]);
				GUI.FocusControl(null);
				selectingSpriteWithClick = spritePositionIndexes[i];
				if (e.type == EventType.MouseUp)
					SelectSprite(selectingSpriteWithClick);
				break;
			}
		}
	}

	void SelectSprite( int index ) {	
		spriteIndex = index;
		Repaint();	
	}

	/// Handles selection a single frame on timeline and list, and puts playhead at start
	void SelectFrame( int index ) {		
		frameIndex = index;
		m_animTime = _frames[index].startTime;
		if (_animFrameList.index != index)
			_animFrameList.index = index;
	}

	void TempSave() {
		_anim.ConvertToSavableFormat(_frames);
		ConvertSpriteLayerForSaving();
		//Debug.Log("TEMP SAVE COMPLETE");
	}

	void Save() {
		MakeSureFirstFrameHasSpriteAndAnimationReferences();
		
		TempSave();

		// brings the backup up-to-date with the edits
		_animOriginal.CopyDataFrom(_anim);
		
		// saving
		//var transforms = Selection.objects;
		//Debug.Log("SAVE! transforms selected");
		var so = new SerializedObject(obj); // unity sometimes crashes here.
		//Debug.Log("SAVE! so initialized");
		so.ApplyModifiedProperties();
		//Debug.Log("SAVE! so applied");

		EditorUtility.SetDirty(_anim);
		//Debug.Log("Saved all the way");

		Repaint();
	}

	void Revert() {
		_anim.CopyDataFrom(_animOriginal);
		obj = _anim;

		changesMade = false;
		popSave = false;

		Debug.Log("reset has occured");
		Repaint();
	}

	void ConvertSpriteLayerForEditing() {
		if (gameLayers == null)
			BuildGameLayers();
			
		spriteLayers = new List<int>();
		for (int i = 0; i < _anim.totalSprites; i++) {
			spriteLayers.Add(0);
		}

		// if we already have sprite layers saved
		if ( _anim.spriteLayer != null ) {
			// loop through each saved sprite layer string, and recod which it is set to
			for (int i = 0; i < _anim.spriteLayer.Length; i++) {
				spriteLayers[i] = System.Array.IndexOf (gameLayers, _anim.spriteLayer[i]);
			}
		}

	}

	void ConvertSpriteLayerForSaving() {
		if (gameLayers == null)
			BuildGameLayers();
		// creates a new list of layer for the sprites
		_anim.spriteLayer = new string[_anim.totalSprites];

		// gets and records the selected name for each layer
		for (int i = 0; i < spriteLayers.Count; i++) {
			if (spriteLayers[i] < 0 || spriteLayers[i] >= gameLayers.Length)
				spriteLayers[i] = 0;
			_anim.spriteLayer[i] = gameLayers[spriteLayers[i]];
		}
	}

	void MakeSureFirstFrameHasSpriteAndAnimationReferences() {
		for (int i = 0; i < _anim.totalSprites; i++) {
			Sprite s;
			AnimationClip c;

			// see if frame has a sprite
			s = _frames[0].sprites[i].sprite;

			// if PowerSprite Animator is installed
			if (asType != null) {
				// see if frame has a sprite
				c = _frames[0].sprites[i].animation;

				if (c != null || s != null)
					continue;
				
				// if this frame doesn't have a sprite or animation, go from the last frame backward till we find a sprite or animation
				if (c == null && s == null) {
					for (int n = _frames.Count - 1; n > 0; n--) {
						c = _frames[n].sprites[i].animation;
						s = _frames[n].sprites[i].sprite;
						if (s || c) break;
					}
					_frames[0].sprites[i].sprite = s;
					_frames[0].sprites[i].animation = c;
				}
			}

			// if PowerSprite Animator is not installed
			else {
				// if this frame doesn't have a sprite, go from the last frame backward till we find a sprite
				if (s == null) {
					for (int n = _frames.Count - 1; n > 0; n--) {
						s = _frames[n].sprites[i].sprite;
						if (s) break;
					}
					_frames[0].sprites[i].sprite = s;
				}
			}
		}
	}


	void Update() {
		if ( _anim != null && m_playing ) { //&& m_dragState != eDragState.Scrub
			// Update anim time if playing (and not scrubbing)
			float delta = (float)(EditorApplication.timeSinceStartup - m_editorTimePrev);

			float adjustedDelta = delta * m_previewSpeedScale;
			//m_animTime += adjustedDelta;

			playback.IncrementTime(adjustedDelta);

			m_animTime = playback.time;

			if (playback.framePercentFPS > 1)
				playback.AdvanceFrame(true);

			// stop playback if we aren't looping
			if ( m_animTime >= _anim.GetLength() && !m_previewloop) {
				m_playing = false;
				m_animTime = 0;
				playback.time = 0;
			}

			if (m_previewScale < 0)
				m_previewScale *= -1;

			Repaint();
		}
		// else if ( m_dragDropHovering || m_dragState != eDragState.None ) {
		// 	Repaint();
		// }

		//When going to Play, we need to clear the selection since references get broken.
		if ( m_gameWasPlaying != EditorApplication.isPlayingOrWillChangePlaymode ) {
			m_gameWasPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
			m_playing = false;
			m_animTime = 0;
		}

		m_editorTimePrev = EditorApplication.timeSinceStartup;

		if (changesMade) {
			Undo.RecordObject(_anim, "Animation Editor Change");
			//_anim.ConvertToSavableFormat(_frames);
			TempSave();
			changesMade = false;
		}
	}


	// Returns a usable texture that looks like a high-contrast checker board.
	static Texture2D GetCheckerboardTexture()
	{
		if (s_textureCheckerboard == null)
		{
			s_textureCheckerboard = new Texture2D(2, 2);
			s_textureCheckerboard.name = "[Generated] Checkerboard Texture";
			s_textureCheckerboard.hideFlags = HideFlags.DontSave;
			s_textureCheckerboard.filterMode = FilterMode.Point;
			s_textureCheckerboard.wrapMode = TextureWrapMode.Repeat;

			Color c0 = new Color(0.4f,0.4f,0.4f,1.0f);
			Color c1 = new Color(0.278f,0.278f, 0.278f, 1.0f);
			s_textureCheckerboard.SetPixel(0,0,c0);
			s_textureCheckerboard.SetPixel(1,1,c0);
			s_textureCheckerboard.SetPixel(0,1,c1);
			s_textureCheckerboard.SetPixel(1,0,c1);
			s_textureCheckerboard.Apply();
		}			
		return s_textureCheckerboard;
        
	}

	static Texture2D GetOriginTexture()
	{
		if (s_textureOrigin == null)
		{
			s_textureOrigin = new Texture2D(7, 7);
			s_textureOrigin.name = "[Generated] Origin Texture";
			s_textureOrigin.hideFlags = HideFlags.DontSave;
			s_textureOrigin.filterMode = FilterMode.Point;
			s_textureOrigin.wrapMode = TextureWrapMode.Clamp;

			Color white = Color.white;//Color.yellow;//new Color(1f,0.5f,0f,1.0f);
			Color black = Color.black;//new Color(0.4f,0.4f,1f,1.0f);
			Color c = Color.clear;//new Color(1f,0.5f,0f,1.0f);

			s_textureOrigin.SetPixel(0,0,c);
			s_textureOrigin.SetPixel(0,1,c);
			s_textureOrigin.SetPixel(0,2,black);
			s_textureOrigin.SetPixel(0,3,black);
			s_textureOrigin.SetPixel(0,4,black);
			s_textureOrigin.SetPixel(0,5,c);
			s_textureOrigin.SetPixel(0,6,c);

			s_textureOrigin.SetPixel(1,0,c);
			s_textureOrigin.SetPixel(1,1,c);
			s_textureOrigin.SetPixel(1,2,black);
			s_textureOrigin.SetPixel(1,3,white);
			s_textureOrigin.SetPixel(1,4,black);
			s_textureOrigin.SetPixel(1,5,c);
			s_textureOrigin.SetPixel(1,6,c);

			s_textureOrigin.SetPixel(2,0,black);
			s_textureOrigin.SetPixel(2,1,black);
			s_textureOrigin.SetPixel(2,2,black);
			s_textureOrigin.SetPixel(2,3,white);
			s_textureOrigin.SetPixel(2,4,black);
			s_textureOrigin.SetPixel(2,5,black);
			s_textureOrigin.SetPixel(2,6,black);

			s_textureOrigin.SetPixel(3,0,black);
			s_textureOrigin.SetPixel(3,1,white);
			s_textureOrigin.SetPixel(3,2,white);
			s_textureOrigin.SetPixel(3,3,white);
			s_textureOrigin.SetPixel(3,4,white);
			s_textureOrigin.SetPixel(3,5,white);
			s_textureOrigin.SetPixel(3,6,black);

			s_textureOrigin.SetPixel(4,0,black);
			s_textureOrigin.SetPixel(4,1,black);
			s_textureOrigin.SetPixel(4,2,black);
			s_textureOrigin.SetPixel(4,3,white);
			s_textureOrigin.SetPixel(4,4,black);
			s_textureOrigin.SetPixel(4,5,black);
			s_textureOrigin.SetPixel(4,6,black);

			s_textureOrigin.SetPixel(5,0,c);
			s_textureOrigin.SetPixel(5,1,c);
			s_textureOrigin.SetPixel(5,2,black);
			s_textureOrigin.SetPixel(5,3,white);
			s_textureOrigin.SetPixel(5,4,black);
			s_textureOrigin.SetPixel(5,5,c);
			s_textureOrigin.SetPixel(5,6,c);

			s_textureOrigin.SetPixel(6,0,c);
			s_textureOrigin.SetPixel(6,1,c);
			s_textureOrigin.SetPixel(6,2,black);
			s_textureOrigin.SetPixel(6,3,black);
			s_textureOrigin.SetPixel(6,4,black);
			s_textureOrigin.SetPixel(6,5,c);
			s_textureOrigin.SetPixel(6,6,c);
			s_textureOrigin.Apply();
		}			
		return s_textureOrigin;
	}

	static Texture2D GetEmptySpriteTexture() {
		if (s_textureMissingSprite == null) {
			int pix = missingTexturSize;
			s_textureMissingSprite = new Texture2D(pix, pix);
			s_textureMissingSprite.name = "[Generated] Missing Sprite Texture";
			s_textureMissingSprite.hideFlags = HideFlags.DontSave;
			s_textureMissingSprite.filterMode = FilterMode.Point;
			s_textureMissingSprite.wrapMode = TextureWrapMode.Clamp;

			Color white = Color.white;//Color.yellow;//new Color(1f,0.5f,0f,1.0f);
			Color blue = Color.blue;//new Color(0.4f,0.4f,1f,1.0f);
			blue.a = 0.5f;
			blue.g = 0.5f;
			
			for (int x = 0; x < pix; x++) {
				for (int y = 0; y < pix; y++) {
					if (x == 0 || y == 0 || y == pix-1 || x == pix-1 || (x == y) || (pix-x == y+1)) 
						s_textureMissingSprite.SetPixel(x,y,white);
					else s_textureMissingSprite.SetPixel(x,y,blue);
				}
			}

			s_textureMissingSprite.Apply();
		}			
		return s_textureMissingSprite;
	}

	static Texture2D GetPixelTexture(Color c) {
		Texture2D s_texturePixel = new Texture2D(1, 1);
		s_texturePixel.name = "[Generated] Pixel Texture";
		s_texturePixel.hideFlags = HideFlags.DontSave;
		s_texturePixel.filterMode = FilterMode.Point;
		s_texturePixel.wrapMode = TextureWrapMode.Clamp;

		s_texturePixel.SetPixel(0,0,c);
		s_texturePixel.Apply();
				
		return s_texturePixel;
	}

	
	static Texture2D GetSelectSpriteTexture(Color c) {
		if (s_textureSelectingSprite == null) {
			int pix = 40;
			s_textureSelectingSprite = new Texture2D(pix, pix);
			s_textureSelectingSprite.name = "[Generated] SelectSprite Texture";
			s_textureSelectingSprite.hideFlags = HideFlags.DontSave;
			s_textureSelectingSprite.filterMode = FilterMode.Point;
			s_textureSelectingSprite.wrapMode = TextureWrapMode.Clamp;
			
			Color white = Color.white;
			white.a = 0.15f;
			//Color c = Color.blue;
			
			s_textureSelectingSprite.SetPixel(0,0,c);

			for (int x = 0; x < pix; x++) {
				for (int y = 0; y < pix; y++) {
					if (x == 0 || y == 0 || y == pix-1 || x == pix-1) 
						s_textureSelectingSprite.SetPixel(x,y,c);
					else s_textureSelectingSprite.SetPixel(x,y,white);
				}
			}
			s_textureSelectingSprite.Apply();
		}
				
		return s_textureSelectingSprite;
	}

	static Texture2D GetSelectedSpriteTexture(Color c) {
		if (s_textureSelectedSprite == null) {
			int pix = 40;
			s_textureSelectedSprite = new Texture2D(pix, pix);
			s_textureSelectedSprite.name = "[Generated] SelectSprite Texture";
			s_textureSelectedSprite.hideFlags = HideFlags.DontSave;
			s_textureSelectedSprite.filterMode = FilterMode.Point;
			s_textureSelectedSprite.wrapMode = TextureWrapMode.Clamp;
			
			Color white = Color.white;
			white.a = 0.15f;
			white.g = 0.95f;
			white.b = 0.95f;
			//Color c = Color.blue;
			
			s_textureSelectedSprite.SetPixel(0,0,c);

			for (int x = 0; x < pix; x++) {
				for (int y = 0; y < pix; y++) {
					if (x == 0 || y == 0 || y == pix-1 || x == pix-1) 
						s_textureSelectedSprite.SetPixel(x,y,c);
					else s_textureSelectedSprite.SetPixel(x,y,white);
				}
			}
			s_textureSelectedSprite.Apply();
		}
				
		return s_textureSelectedSprite;
	}

	// STYLES
	class Styles
	{
		public static readonly GUIStyle PREVIEW_BUTTON = new GUIStyle("preButton");
		public static readonly GUIStyle PREVIEW_BUTTON_LOOP = new GUIStyle(Styles.PREVIEW_BUTTON) { padding = new RectOffset(0,0,2,0) };
		public static readonly GUIStyle PREVIEW_SLIDER = new GUIStyle("preSlider");
		public static readonly GUIStyle PREVIEW_SLIDER_THUMB = new GUIStyle("preSliderThumb");
		public static readonly GUIStyle PREVIEW_LABEL_BOLD = new GUIStyle("preLabel");
		public static readonly GUIStyle PREVIEW_LABEL_BOLD_SHADOW = new GUIStyle("preLabel") { normal = { textColor = Color.black }  };
		public static readonly GUIStyle PREVIEW_LABEL_SPEED = new GUIStyle("preLabel") { fontStyle = FontStyle.Normal, normal = { textColor = Color.gray }  };


		public static readonly GUIStyle DEBUG_LABEL = new GUIStyle("preLabel") { fontStyle = FontStyle.Normal, normal = { textColor = Color.black }  };

		public static readonly GUIStyle TIMELINE_KEYFRAME_BG = new GUIStyle("AnimationKeyframeBackground");
		#if UNITY_5_3 || UNITY_5_4
			public static readonly GUIStyle TIMELINE_ANIM_BG = new GUIStyle("AnimationCurveEditorBackground");
		#else
			public static readonly GUIStyle TIMELINE_ANIM_BG = new GUIStyle("CurveEditorBackground");
		#endif
		public static readonly GUIStyle TIMELINE_BOTTOMBAR_BG = new GUIStyle("ProjectBrowserBottomBarBg");

		public static readonly GUIStyle TIMELINE_EVENT_TEXT = EditorStyles.miniLabel;
		public static readonly GUIStyle TIMELINE_EVENT_TICK = new GUIStyle();

		public static readonly GUIStyle TIMELINE_EVENT_TOGGLE = new GUIStyle(EditorStyles.toggle) { font = EditorStyles.miniLabel.font, fontSize = EditorStyles.miniLabel.fontSize, padding = new RectOffset(15,0,3,0) };

		public static readonly GUIStyle INFOPANEL_LABEL_RIGHTALIGN = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };

		public static readonly GUIStyle SETTINGS_STYLE_FOLDOUT = new GUIStyle(EditorStyles.foldout) { normal = {textColor = Color.grey}, focused = {textColor = Color.grey}, active = {textColor = Color.grey} };

	}

	// Static list of content (built in unity icons and things)
	class Contents
	{
		public static readonly GUIContent PLAY = EditorGUIUtility.IconContent("PlayButton"); //
		public static readonly GUIContent PAUSE = EditorGUIUtility.IconContent("PauseButton");
		public static readonly GUIContent PREV = EditorGUIUtility.IconContent("Animation.PrevKey");
		public static readonly GUIContent NEXT = EditorGUIUtility.IconContent("Animation.NextKey");
		public static readonly GUIContent SPEEDSCALE = EditorGUIUtility.IconContent("SpeedScale");
		public static readonly GUIContent ZOOM = EditorGUIUtility.IconContent("ViewToolZoom");
		public static readonly GUIContent LOOP_OFF = EditorGUIUtility.IconContent("d_RotateTool");
		public static readonly GUIContent LOOP_ON = EditorGUIUtility.IconContent("d_RotateTool On");
		public static readonly GUIContent PLAY_HEAD = EditorGUIUtility.IconContent("me_playhead");
		public static readonly GUIContent EVENT_MARKER = EditorGUIUtility.IconContent("d_Animation.EventMarker");
		public static readonly GUIContent ANIM_MARKER = EditorGUIUtility.IconContent("blendKey");
		public static readonly GUIContent RECENTER_PREVIEW = EditorGUIUtility.IconContent("d_Toolbar Plus");


		public static readonly GUIContent ORIGIN = EditorGUIUtility.IconContent("sv_icon_dot1_sml"); //PlayButton sv_icon_dot1_pix16_gizmo
	}

	static readonly float TIMELINE_HEIGHT = 200;
	static readonly float FRAME_PANEL_WIDTH = 200;
	static readonly float SPRITE_PANEL_WIDTH = 200;

	static Texture2D s_textureCheckerboard;
	static Texture2D s_textureOrigin;
	static Texture2D s_textureMissingSprite;
	static Texture2D s_textureSelectingSprite;
	static Texture2D s_textureSelectedSprite;
	static int missingTexturSize = 19;

	void LayoutToolbarPlay()
	{
		EditorGUI.BeginChangeCheck();
		m_playing = GUILayout.Toggle( m_playing, m_playing ? Contents.PAUSE : Contents.PLAY, Styles.PREVIEW_BUTTON, GUILayout.Width(40) );
		if (EditorGUI.EndChangeCheck()) {

			// Clicked play
			if ( m_playing ){

				playback.ResetPlayback();
				playback.time = m_animTime;
				playback.cFrame = frameIndex;

				// If anim is at end, restart
				if ( m_animTime >= _anim.GetLength() ) {
					m_animTime = 0;
					playback.time = 0;
					playback.ResetPlayback();
				}
			}
		}
	}


	void LayoutToolbarLoop()
	{
		m_previewloop = GUILayout.Toggle( m_previewloop, m_previewloop ? Contents.LOOP_ON : Contents.LOOP_OFF, Styles.PREVIEW_BUTTON_LOOP, GUILayout.Width(25) );
	}

	void LayoutToolbarScaleSlider() {
		GUILayout.Space(5);
		m_previewScale = GUILayout.HorizontalSlider(m_previewScale, 0.1f, 5, Styles.PREVIEW_SLIDER, Styles.PREVIEW_SLIDER_THUMB, GUILayout.Width(50));
		GUILayout.Label(m_previewScale.ToString("0.0"), Styles.PREVIEW_LABEL_SPEED, GUILayout.Width(30));
	}

	void LayoutToolbarResetPreviewOffset() {
		if (GUILayout.Button(Contents.RECENTER_PREVIEW, Styles.PREVIEW_LABEL_BOLD, GUILayout.Width(15)) ) {
			m_previewOffset = Vector2.zero;
			m_previewScale = 1;
		}
	}

	void LayoutToolbarTimeScaleSlider() {
		if (GUILayout.Button(Contents.SPEEDSCALE, Styles.PREVIEW_LABEL_BOLD, GUILayout.Width(30)) )
			m_previewSpeedScale = 1;
		m_previewSpeedScale = GUILayout.HorizontalSlider(m_previewSpeedScale, 0, 4, Styles.PREVIEW_SLIDER, Styles.PREVIEW_SLIDER_THUMB, GUILayout.Width(50));
		GUILayout.Label(m_previewSpeedScale.ToString("0.00"), Styles.PREVIEW_LABEL_SPEED, GUILayout.Width(40));
	}

	void LayoutToolbarPlaybackTime() {
		GUILayout.Label(m_animTime.ToString("0.0"), Styles.PREVIEW_LABEL_SPEED, GUILayout.Width(40));
	}

	void LayoutToolbarAnimListName() {
		GUILayout.Space(10);
		if ( GUILayout.Button(_anim.name, new GUIStyle(Styles.PREVIEW_BUTTON) { stretchWidth = true, alignment = TextAnchor.MiddleLeft } ) )
		{
			Selection.activeObject = _anim;
			EditorGUIUtility.PingObject(_anim);
		}
	}

	
}
}
