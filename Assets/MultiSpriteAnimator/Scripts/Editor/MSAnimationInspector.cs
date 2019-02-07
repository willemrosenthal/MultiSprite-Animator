using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace MultiSprite {
[CustomEditor(typeof(MSAnimation))]
public class MSAnimationInspector : Editor {

	public override void OnInspectorGUI () {
		GUILayout.Space (10);

		if (GUILayout.Button("Open MultiSprite Editor")) {
			EditorWindow.GetWindow<MultiSpriteEditor>("MS Anim Editior");
		}
	}
}
}