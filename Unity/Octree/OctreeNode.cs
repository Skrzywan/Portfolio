using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
#endif
using UnityEngine;

[CreateAssetMenu(fileName = "OctreeNode", menuName = "SO/OctreeNode")]
public class OctreeNode : ScriptableObject
{
	public Bounds bounds;
	public List<Vector3> verts = new List<Vector3>(9);
	public List<OctreeNodeChild> childrenReferences;
	public OctreeNode parent;
	public int level;
	public int myChildID;

	public OctreeNode GetLowestNode(Vector3 position)
	{
		for(int i = 0; i < childrenReferences.Count; i++)
		{
			if(childrenReferences[i].bounds.Contains(position))
			{
				if(childrenReferences[i].node == null)
				{
					childrenReferences[i].LoadAsync();
					return this;
				} else
				{
					return childrenReferences[i].node.GetLowestNode(position);
				}
			}
		}
		return this;
	}

#if UNITY_EDITOR

	public void Split(int maxDepth)
	{
		if(maxDepth <= level)
		{
			return;
		}

		Split(false);

		for(int i = 0; i < childrenReferences.Count; i++)
		{
			childrenReferences[i].node.Split(maxDepth);
		}
	}

	public void Split(bool saveAssets = true)
	{
		if(childrenReferences == null)
		{
			childrenReferences = new List<OctreeNodeChild>();
		}
		childrenReferences.Clear();
		for(int i = 0; i < 8; i++)
		{
			CreateChildNode(i);
		}
		if(saveAssets)
		{
			AssetDatabase.SaveAssets();
		}
	}

	private void CreateChildNode( int childID)
	{
		string assetPath = PrepareAndGetAssetPathForChild(childID);

		OctreeNodeChild newChild = new OctreeNodeChild();
		newChild.path = assetPath;

		childrenReferences.Add(newChild);
		Vector3 newBoundsCenter = GetCenterForSplitBounds(childID);
		newChild.bounds = new Bounds(newBoundsCenter, bounds.size / 4);

		newChild.node = CreateInstance<OctreeNode>();
		newChild.node.bounds = newChild.bounds;
		newChild.node.level = level + 1;
		newChild.node.myChildID = childID;

		//OctreeCreatorUtils.CreateNewAsset(newChild.node, newChild.path);
		Debug.Log($"CreateFile");
		AssetDatabase.CreateAsset(newChild.node, newChild.path);
		AssetDatabase.SetLabels(newChild.node, new string[] { "OctreeNode" });
	}

	private string PrepareAndGetAssetPathForChild(int childID)
	{
		string childDirName = $"level_{level}_children";
		string path = AssetDatabase.GetAssetPath(this);
		path = Path.GetDirectoryName(path);

		AssetDatabase.CreateFolder(path, $"{childDirName}_{childID}");
		string childFolder = $"{path}\\{childDirName}_{childID}";
		string assetPath = $"{childFolder}\\level_{level + 1}_node_{childID}.asset";
		return assetPath;
	}

	private Vector3 GetCenterForSplitBounds(int i)
	{
		Vector3 newCenter = bounds.center;
		Vector3 quaterSize = bounds.size / 4;
		switch(i)
		{
			case 0:
				quaterSize = Vector3.Scale(quaterSize, new Vector3(1, 1, 1));
				break;
			case 1:
				quaterSize = Vector3.Scale(quaterSize, new Vector3(-1, 1, 1));
				break;
			case 2:
				quaterSize = Vector3.Scale(quaterSize, new Vector3(1, -1, 1));
				break;
			case 3:
				quaterSize = Vector3.Scale(quaterSize, new Vector3(-1, -1, 1));
				break;
			case 4:
				quaterSize = Vector3.Scale(quaterSize, new Vector3(1, 1, -1));
				break;
			case 5:
				quaterSize = Vector3.Scale(quaterSize, new Vector3(-1, 1, -1));
				break;
			case 6:
				quaterSize = Vector3.Scale(quaterSize, new Vector3(1, -1, -1));
				break;
			case 7:
				quaterSize = Vector3.Scale(quaterSize, new Vector3(-1, -1, -1));
				break;
			default:
				break;
		}
		return newCenter + quaterSize;
	}
#endif

}

[System.Serializable]
public class OctreeNodeChild
{
	public Bounds bounds;
	public OctreeNode node;
	public string path;
	public ResourceRequest request;
	public Action OnResourceLoaded;


	public void LoadAsync()
	{
		if(request == null)
		{
			request = Resources.LoadAsync<ScriptableObject>(path);
			request.completed += ResourceLoaded;
		}
	}

	void ResourceLoaded(AsyncOperation ao)
	{
		node = request.asset as OctreeNode;
		request = null;
		OnResourceLoaded?.Invoke();
	}
}


#if UNITY_EDITOR
[CustomEditor(typeof(OctreeNode)), CanEditMultipleObjects]
public class OctreeNodeEditor : Editor
{
	public override void OnInspectorGUI()
	{
		OctreeNode myScript = target as OctreeNode;
		List<OctreeNode> myScripts = targets.Select(n => n as OctreeNode).ToList();

		base.OnInspectorGUI();

		if(GUILayout.Button("Split"))
		{
			myScripts.ForEach(n => n.Split());
		}
	}
}
#endif