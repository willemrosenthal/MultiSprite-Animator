using UnityEngine;
using UnityEditor;

namespace MultiSprite {
public class MSAnimationAsset
{
	[MenuItem("Assets/Create/MS Animation")]
	public static void CreateAsset ()
	{
		ScriptableObjectUtility.CreateAsset<MSAnimation> ("MSA");
	}
}
}