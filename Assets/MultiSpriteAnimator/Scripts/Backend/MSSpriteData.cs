using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MultiSprite {

[Serializable]
public class MSSpriteData : ScriptableObject {
	public AnimationClip animation = null;
	public Vector2 position = Vector2.zero;
	public Vector2 scale = Vector2.one;
	public float rotation = 0; 
	public int sortOrder = 0;
	public string curve = "";
	public bool hide = false;
}

}