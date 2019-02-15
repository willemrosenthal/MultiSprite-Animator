using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MultiSprite
{

public partial class MultiSpriteEditor
{


	bool lockScale = true;

	// FRAME LIST
	ReorderableList _animFrameList = null;
	Vector2 f_scrollPosition = Vector2.zero;

	void BuildReorderableFrameList()
	{
		// if (_anim.frames == null)
		// 	Debug.LogWarning("animation missing frames");

		_animFrameList = new ReorderableList( _frames, typeof(MSFrame),true,true,true,true);
		_animFrameList.drawHeaderCallback = (Rect rect) => { 
			EditorGUI.LabelField(rect,"Frames"); 
			EditorGUI.LabelField(new Rect(rect){x=rect.width-37,width=45},"Length"); 
		};
		_animFrameList.drawElementCallback = LayoutFrameListFrame;
		_animFrameList.onSelectCallback = (ReorderableList list) => {
			SelectFrame(_animFrameList.index); 
		};

		_animFrameList.onAddCallback = (ReorderableList list) => {
			AddFrame();
			list.index = _frames.Count-1;
			SelectFrame(list.index);
		};
		_animFrameList.onRemoveCallback = (ReorderableList list) => {
			_frames.RemoveAt(list.index);
			if (list.index > 0)
				list.index--;
			SelectFrame(list.index);
			playback.PrepareAnimationData(_anim);
		};

		_animFrameList.onChangedCallback = (ReorderableList list) => {
			ChangeMade();
			Repaint();
		};

		_animFrameList.onReorderCallback = (ReorderableList list) => {
			playback.PrepareAnimationData(_anim);
		};
	}

	void AddFrame() {
		_frames.Add(new MSFrame());
		_frames[_frames.Count - 1].frameLable = "frame " + (_frames.Count - 1);
		// adds right number of sprites to the new frame
		int newFrameIndex = _frames.Count - 1;
		for (int i = 0; i < _anim.totalSprites; i++) {
			_frames[newFrameIndex].sprites.Add(new MSFrameSprite());
			// copy values from previous frame
			if (newFrameIndex > 0) {
				_frames[newFrameIndex].sprites[i].curve = _frames[frameIndex].sprites[i].curve;
				_frames[newFrameIndex].sprites[i].flipX = _frames[frameIndex].sprites[i].flipX;
				_frames[newFrameIndex].sprites[i].hide = _frames[frameIndex].sprites[i].hide;
				_frames[newFrameIndex].sprites[i].position = _frames[frameIndex].sprites[i].position;
				_frames[newFrameIndex].sprites[i].rotation = _frames[frameIndex].sprites[i].rotation;
				_frames[newFrameIndex].sprites[i].scale = _frames[frameIndex].sprites[i].scale;
				_frames[newFrameIndex].sprites[i].sortOrder = _frames[frameIndex].sprites[i].sortOrder;
			}
		}
		playback.PrepareAnimationData(_anim);	
	}


	void AddSprite() {
		_anim.totalSprites++;
		_anim.spriteLable.Add("sprite " + _anim.totalSprites);
		spriteLayers.Add(0);
		// adds sprite to each frame
		for (int i = 0; i < _frames.Count; i++) {
			_frames[i].sprites.Add(new MSFrameSprite());
			if (_frames[i].sprites.Count > 1)
				_frames[i].sprites[_frames[i].sprites.Count -1].curve = _frames[i].sprites[spriteIndex].curve;
		}
		//ChangeMade();
		SelectSprite(_anim.totalSprites-1);
		Save();
		Repaint();
	}

	void RemoveSprite(int index) {
		_anim.totalSprites--;
		_anim.spriteLable.RemoveAt(index);
		spriteLayers.RemoveAt(index);
		// removes sprite from each frame
		for (int i = 0; i < _frames.Count; i++) {
			_frames[i].sprites.RemoveAt(index);
		}
		if (spriteIndex == index && spriteIndex > 0)
			spriteIndex--;
		Save();
		_frames = _anim.ConvertForEditor();
		Repaint();
	}

	void SwitchSelectedSprite(int shirDir = 1) {
		spriteIndex += shirDir;
		while (spriteIndex < 0)
			spriteIndex += _anim.totalSprites;
		while (spriteIndex >= _anim.totalSprites)
			spriteIndex -= _anim.totalSprites;	
		GUI.FocusControl(null);
		Repaint();
	}

	int f;
	void LayoutInfoPanel( Rect rect )
	{
		GUILayout.BeginArea(rect, EditorStyles.inspectorFullWidthMargins);
		GUILayout.Space(20);

		// Animation length
		EditorGUILayout.LabelField( string.Format("Length: {0:0.00} sec", _anim.GetLength()), new GUIStyle(EditorStyles.miniLabel){normal = { textColor = Color.gray }});

		AddRemoveSpriteUI();

		GUILayout.Space(15);

		// divider line
		DividerLine();
		
		GUILayout.Space(10);

		//
		// Animation Settings
		//
		displayAnimationSettings = EditorGUILayout.Foldout(displayAnimationSettings, "Animation Settings", Styles.SETTINGS_STYLE_FOLDOUT);
		if ( displayAnimationSettings ) {
			EditorGUI.indentLevel++;
			_anim.loop= EditorGUILayout.Toggle( "Looping", _anim.loop );

			EditorGUI.BeginChangeCheck();

				_anim.limitToFPS = EditorGUILayout.Toggle( "Limit FPS", _anim.limitToFPS);
				if (_anim.limitToFPS)
					_anim.fps = EditorGUILayout.IntField("FPS", _anim.fps);

			if ( EditorGUI.EndChangeCheck() ) {
				playback.PrepareAnimationData(_anim);
			}

			EditorGUI.BeginChangeCheck();
			_anim.pixelPerfect = EditorGUILayout.Toggle( "Pixel Perfect Snapping", _anim.pixelPerfect );
			_anim.pixelsPerUnit = EditorGUILayout.IntField( "Pixel Per Unit", _anim.pixelsPerUnit );
			if ( EditorGUI.EndChangeCheck() ) {
				playback = new MSPlayback();
				playback.PrepareAnimationData(_anim);
				Repaint();
			}
			EditorGUI.indentLevel--;
			//GUILayout.Space(15);
		}


	

		GUILayout.Space(7);


		EditorGUI.BeginChangeCheck();

		// FRAME LIST
		f_scrollPosition = EditorGUILayout.BeginScrollView(f_scrollPosition,false,false);
		

		if (_animFrameList == null)
			BuildReorderableFrameList();
		_animFrameList.DoLayoutList();

		

		EditorGUILayout.EndScrollView();

		// curve for all frames
		if (spriteIndex < _frames[frameIndex].sprites.Count) {
			DividerLine();
			GUILayout.Space(5); 
			GUILayout.Label("Frame " + frameIndex + ":  " +_frames[frameIndex].frameLable); //, EditorStyles.boldLabel);
			//GUILayout.Space(2); 

			GUILayout.BeginHorizontal();
			GUILayout.Label("Defualt Curve");
			_frames[frameIndex].frameCurve = EditorGUILayout.Popup(_frames[frameIndex].frameCurve, MSCurves.curves);
			GUILayout.EndHorizontal();

			bool showSetAllSpriteButton = false;

			for (int i = 0; i < _anim.totalSprites; i++ ) {
				if (_frames[frameIndex].sprites[i].curve != 0)
					showSetAllSpriteButton = true;
			}

			if (showSetAllSpriteButton) {
				if (GUILayout.Button("All frame sprites use default")) {
					for (int i = 0; i < _anim.totalSprites; i++) {
						_frames[frameIndex].sprites[i].curve = 0;
					}
					Repaint();
					ChangeMade();
				}
			}

		}

		if ( EditorGUI.EndChangeCheck() ) {
			ChangeMade();
			playback.PrepareAnimationData(_anim);
			Repaint();
		}

		GUILayout.Space(10);

		if (popSave) {
			DividerLine();
			GUILayout.Space(10); 
			if (GUILayout.Button("Save")) {
				Save();
				changesMade = false;
				popSave = false;
			}
			if (GUILayout.Button("Revert") &&
				EditorUtility.DisplayDialog("Revert all changes?",
				"Are you sure you want to revert to the last save?", "Revert", "Cancel")) {
				//_anim = Selection.activeObject as MSAnimation;
				Revert();
				NewAnimationSelected();
			}
		}
		GUILayout.Space(10);

		
		GUILayout.EndArea();
	}

	void LayoutSpritePanel( Rect rect )
	{
		if (spriteIndex >= _anim.totalSprites || spriteIndex < 0)
			spriteIndex = 0;

		Rect lineRect = rect;
		lineRect.width = 1;
		lineRect.position = rect.position + Vector2.right * rect.width;
		EditorGUI.DrawRect(lineRect, new Color ( 0.5f,0.5f,0.5f, 1 ) );

		GUILayout.BeginArea(rect, EditorStyles.inspectorFullWidthMargins);
		GUILayout.Space(20);

		// Animation length
		// vertical divider
		//EditorStyles.popup.stretchWidth = true;
		//EditorStyles.popup.fixedWidth = 80;
		// EditorStyles.popup.fixedHeight = 20;
		// EditorGUIUtility.labelWidth = 110f;
		EditorGUILayout.LabelField( "total sprites: " + _anim.totalSprites, new GUIStyle(EditorStyles.miniLabel){normal = { textColor = Color.gray }}); //"total sprites: " + _anim.totalSprites, new GUIStyle(EditorStyles.miniLabel){normal = { textColor = Color.gray }});

		// SPRITE LIST LIST
		if (frameIndex < _frames.Count && _frames[frameIndex].sprites != null) {

			// current sprite
			//GUILayout.Label(spriteLables[spriteIndex]);
			if (frameIndex < _frames.Count && _frames[frameIndex].sprites != null) {
				SpriteSelect();
			}
			GUILayout.Space(10);

			// divider line
			DividerLine();

			GUILayout.Space(5);

			EditorGUI.BeginChangeCheck();

			// REORDERABLE LIST
			/*
			s_scrollPosition = EditorGUILayout.BeginScrollView(s_scrollPosition,false,false);

			if (_frameSpriteList == null)
				BuildReorderableSpriteList();
			_frameSpriteList.DoLayoutList();

			EditorGUILayout.EndScrollView();
			*/

			// SPRITE DATA
			if (spriteIndex < _frames[frameIndex].sprites.Count) {
				// animation
				if (asType != null) {
					EditorGUILayout.LabelField("Animation Clip");
					_frames[frameIndex].sprites[spriteIndex].animation = EditorGUILayout.ObjectField((AnimationClip)_frames[frameIndex].sprites[spriteIndex].animation, typeof(AnimationClip), false) as AnimationClip;
				}
				// sprites
				EditorGUILayout.LabelField("Sprite");
				_frames[frameIndex].sprites[spriteIndex].sprite = EditorGUILayout.ObjectField((Sprite)_frames[frameIndex].sprites[spriteIndex].sprite, typeof(Sprite), false) as Sprite;

				GUILayout.Space(10);
				DividerLine();
				GUILayout.Space(10);

				// position
				_frames[frameIndex].sprites[spriteIndex].position = EditorGUILayout.Vector2Field("Position", _frames[frameIndex].sprites[spriteIndex].position);
				
				GUILayout.Space(5);
				// sort
				_frames[frameIndex].sprites[spriteIndex].sortOrder = EditorGUILayout.IntField("Sort Order", _frames[frameIndex].sprites[spriteIndex].sortOrder);
				// flip
				_frames[frameIndex].sprites[spriteIndex].flipX = EditorGUILayout.Toggle("Flip X", _frames[frameIndex].sprites[spriteIndex].flipX);
				// hide
				_frames[frameIndex].sprites[spriteIndex].hide = EditorGUILayout.Toggle("Hide", _frames[frameIndex].sprites[spriteIndex].hide);


				if ( EditorGUI.EndChangeCheck() ) {
					ChangeMade();
					Repaint();
				}

				GUILayout.Space(10);

				//
				// MOVE OPTIONS
				//
				displayScaleAndRotate = EditorGUILayout.Foldout(displayScaleAndRotate, "Scale, Rotation, Curve, Sort", Styles.SETTINGS_STYLE_FOLDOUT);
				if ( displayScaleAndRotate ) {
					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					// rotation
					EditorGUILayout.LabelField("Rotation");
					_frames[frameIndex].sprites[spriteIndex].rotation = EditorGUILayout.Slider(_frames[frameIndex].sprites[spriteIndex].rotation, -180f, 180f);

					// scale
					bool scalesMatch = _frames[frameIndex].sprites[spriteIndex].scale.y == _frames[frameIndex].sprites[spriteIndex].scale.x;

					if (scalesMatch) lockScale = EditorGUILayout.Toggle("Lock Scale", lockScale);
					else lockScale = EditorGUILayout.Toggle("Lock Scale", lockScale, new GUIStyle(EditorStyles.miniLabel){normal = { textColor = Color.gray }});
				
					if (!lockScale || !scalesMatch) {
						_frames[frameIndex].sprites[spriteIndex].scale = EditorGUILayout.Vector2Field("Scale", _frames[frameIndex].sprites[spriteIndex].scale);
					}
					else {
						_frames[frameIndex].sprites[spriteIndex].scale.x = EditorGUILayout.FloatField("Scale", _frames[frameIndex].sprites[spriteIndex].scale.x);
						_frames[frameIndex].sprites[spriteIndex].scale.y = _frames[frameIndex].sprites[spriteIndex].scale.x;
					}
					// layer
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(15);
					GUILayout.Label("Sort Layer"); //, new GUIStyle(){fixedWidth = 50});
					spriteLayers[spriteIndex] = EditorGUILayout.Popup(spriteLayers[spriteIndex], gameLayers);
					EditorGUILayout.EndHorizontal();
					// curve
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(15);
					GUILayout.Label("Sprite Curve");//, new GUIStyle(){fixedWidth = 50});
					_frames[frameIndex].sprites[spriteIndex].curve = EditorGUILayout.Popup(_frames[frameIndex].sprites[spriteIndex].curve, frameCurves); //frameCurves
					EditorGUILayout.EndHorizontal();

					if ( EditorGUI.EndChangeCheck() ) {
						ChangeMade();
						Repaint();
					}
					GUILayout.Space(15);
					EditorGUI.indentLevel--;
				}
			

				//
				// Experimental featuers OPTIONS
				//
				displayExperimentalFeatures = EditorGUILayout.Foldout(displayExperimentalFeatures, "Experimental Featuers", Styles.SETTINGS_STYLE_FOLDOUT);
				if ( displayExperimentalFeatures ) {
					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					
					GUILayout.BeginHorizontal();
					GUILayout.Space(15);
					GUILayout.Label( "Sprite Z Position: ", new GUIStyle(EditorStyles.label) { fixedHeight = 15, fontSize = 10});
					_frames[frameIndex].sprites[spriteIndex].zPos = EditorGUILayout.FloatField( _frames[frameIndex].sprites[spriteIndex].zPos, new GUIStyle(EditorStyles.numberField) { fixedHeight = 15, fontSize = 10 });
					GUILayout.EndHorizontal();
					

					if ( EditorGUI.EndChangeCheck() ) {
						ChangeMade();
						Repaint();
					}
					GUILayout.Space(15);
					EditorGUI.indentLevel--;
				}
			
			}
			//
			// Editor Settings
			//
			displayAdvancedOptions = EditorGUILayout.Foldout(displayAdvancedOptions, "Editor Settings", Styles.SETTINGS_STYLE_FOLDOUT);
			if ( displayAdvancedOptions ) {
				EditorGUI.indentLevel++;

				drawDebugSpriteRects = EditorGUILayout.Toggle( "Show Sprite Rects", drawDebugSpriteRects );

				pixelsInCheckerboard = EditorGUILayout.IntSlider(pixelsInCheckerboard, 2, 512);

				GUILayout.Space(15);
				EditorGUI.indentLevel--;
			}
		}
		
		GUILayout.EndArea();
	}

	void DividerLine(int height = 1) {
		Rect lineRect = EditorGUILayout.GetControlRect(false, height );;
		lineRect.height = height;
		EditorGUI.DrawRect(lineRect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
	}

	void SpriteSelect() {
		// CHOOSE SPRITE FROM DROPDOWN
		if (spriteLables == null || spriteLables.Length != _anim.spriteLable.Count) {
			spriteLables = new string[_anim.spriteLable.Count];
			for (int i = 0; i < spriteLables.Length; i++) {
				spriteLables[i] = _anim.spriteLable[i];
			}
		}

		//GUILayout.Label("Sprite", new GUIStyle(EditorStyles.label) { fixedWidth = 80 });
		spriteIndex = EditorGUILayout.Popup(  spriteIndex, spriteLables, new GUIStyle(EditorStyles.popup) { fixedHeight = 20, fontSize = 10 });
		//spriteIndex = EditorGUILayout.Popup("Sprite",spriteIndex, spriteLables,new GUIStyle(EditorStyles.popup){normal = { height = 20 }}   );
		GUILayout.Space(8);
		EditorGUI.BeginChangeCheck();
		_anim.spriteLable[spriteIndex] = EditorGUILayout.TextField(_anim.spriteLable[spriteIndex]+""); 
		if ( EditorGUI.EndChangeCheck() ) {
			spriteLables = null;
			//ChangeMade();
			Repaint();
		}
	}

	void AddRemoveSpriteUI() {
		// new sprite
		if (GUILayout.Button("New Sprite")) {
			AddSprite();
		}
		// // delete sprite
		if (GUILayout.Button("Delete Sprite") &&
			EditorUtility.DisplayDialog("Delete Sprite?",
			"Are you sure you want to delete " + _anim.spriteLable[spriteIndex]
			+ "?", "Delete", "Cancel")) {
			RemoveSprite(spriteIndex);
		}
	}

	void LayoutFrameListFrame(Rect rect, int index, bool isActive, bool isFocused ) {
		if ( _frames == null || index < 0 || index >= _frames.Count )
			return;

		if (_frames[index] == null)
			Debug.Log("NULL FRAME " + index);

		MSFrame frame = _frames[index];

		EditorGUI.BeginChangeCheck();
		rect = new Rect(rect) { height = rect.height-4, y = rect.y+2 };

		// frame ID
		float xOffset = rect.x;
		float width = Styles.INFOPANEL_LABEL_RIGHTALIGN.CalcSize(new GUIContent(index.ToString())).x;
		EditorGUI.LabelField(new Rect(rect){x=xOffset,width=width},index.ToString(), Styles.INFOPANEL_LABEL_RIGHTALIGN );

		// Frame Sprite
		xOffset += width+5;
		width = (rect.xMax-5-(28 * 2))-xOffset;

		// Sprite thingy
		Rect spriteFieldRect = new Rect(rect){x=xOffset,width=width,height=16};
		frame.frameLable = EditorGUI.TextField(spriteFieldRect, frame.frameLable); //(spriteFieldRect, frame.m_sprite, typeof(Sprite), false ) as Sprite;
		//frame.m_sprite = EditorGUI.ObjectField(spriteFieldRect, frame.m_sprite, typeof(Sprite), false ) as Sprite;

		// Frame length (in samples)
		xOffset += width+5;
		width = 28 * 2;
		GUI.SetNextControlName("FrameLen");
		frame.frameTime = EditorGUI.FloatField( new Rect(rect){x=xOffset,width=width}, frame.frameTime );


		if ( EditorGUI.EndChangeCheck() ) {
			ChangeMade();
		}
	}

}

}