using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace MultiSprite {
[CustomEditor(typeof(MultiSpriteAnimator))]
public class MSAnimatorInspector : Editor {


	public bool foldoutOptions = false;
	int totalScripts;

	MultiSpriteAnimator msa;

	void OnEnable() {
		msa = (MultiSpriteAnimator)target;
		totalScripts = msa.componentsToAddToEachSprite.Count;
	}

	class Styles {
		public static readonly GUIStyle PREVIEW_LABEL_BOLD = new GUIStyle("preLabel");
	}
	class Contents {
		public static readonly GUIContent RemoveButton = EditorGUIUtility.IconContent("d_Toolbar Minus"); //
		public static readonly GUIContent AddButton = EditorGUIUtility.IconContent("d_Toolbar Plus");
	}

	public override void OnInspectorGUI () {
		GUILayout.Space (10);

		DrawDefaultInspector ();

		GUILayout.Space (10);
		foldoutOptions = EditorGUILayout.Foldout(foldoutOptions, "More Options");
		if (foldoutOptions) {
			EditorGUI.indentLevel += 1;
			msa.materialForSprites = (Material)EditorGUILayout.ObjectField("Sprite Material", msa.materialForSprites, typeof(Material));

			GUILayout.Space (15);
			EditorGUILayout.LabelField ("Attach scripts to each sprite:");
			EditorGUI.indentLevel += 2;
			if (msa.componentsToAddToEachSprite != null && msa.componentsToAddToEachSprite.Count > 0) {
				for (int i = 0; i < msa.componentsToAddToEachSprite.Count; i++) {
					GUILayout.BeginHorizontal ();

					
					EditorGUILayout.LabelField ("Name:", GUILayout.MaxWidth (80));

					MonoScript s = null;
					s = EditorGUILayout.ObjectField(s, typeof(MonoScript), false, GUILayout.MaxWidth (140)) as MonoScript;

					if (s != null)
						msa.componentsToAddToEachSprite[i] = s.name;
					msa.componentsToAddToEachSprite[i] = (string)GUILayout.TextField(msa.componentsToAddToEachSprite[i]); //

					if (GUILayout.Button(Contents.RemoveButton, GUILayout.Width(30), GUILayout.Height(15))) {
						msa.componentsToAddToEachSprite.RemoveAt(i);
						break;
					}

					GUILayout.EndHorizontal ();

				}
			}
			GUILayout.BeginHorizontal ();
				GUILayout.Space(45);
				if (GUILayout.Button(Contents.AddButton, GUILayout.Height(20)))
					msa.componentsToAddToEachSprite.Add("");
			GUILayout.EndHorizontal ();

			if (totalScripts != msa.componentsToAddToEachSprite.Count) {
				totalScripts = msa.componentsToAddToEachSprite.Count;
				Repaint();
			}
		}
	}
}
}