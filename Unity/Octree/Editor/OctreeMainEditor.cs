using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(OctreeMain))]
public class OctreeMainEditor : Editor
{
	GUIContent buttonContent;

	private void OnEnable()
	{
		buttonContent = new GUIContent("Open Octree Creator","If you want to create new octree files, use creator window");
	}

	public override void OnInspectorGUI()
	{
		if(GUILayout.Button(buttonContent))
		{
			EditorWindow.GetWindow<OctTreeCreator>();
		}
		OctreeMain myScript = target as OctreeMain;
		base.OnInspectorGUI();
	}
}
#endif
