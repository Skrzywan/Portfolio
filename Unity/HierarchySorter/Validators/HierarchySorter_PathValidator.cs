using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/HierarchySorter/PathValidator", fileName = "PathValidator_HierarchySorter")]
public class HierarchySorter_PathValidator : HierarchySorterValidator
{
	List<string> dirNames = new List<string>();

	public override object GetArguments()
	{
		return dirNames;
	}

	public override System.Type GetArgumentType()
	{
		return typeof(string);
	}

	public override void SetArguments(object args)
	{
		dirNames = args as List<string>;
		if(dirNames != null)
		{
			dirNames = dirNames.Select(n => n?.ToLower()).ToList();
		}
	}

	public override bool Validate(GameObject go)
	{
		string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
		return Validate(prefabPath);
	}

	public override bool Validate(string prefabPath)
	{

		if(prefabPath != null)
		{
			//List<string> labels = AssetDatabase.GetLabels(AssetDatabase.GUIDFromAssetPath(prefabPath)).Select(n => n.ToLower()).ToList();
			List<string> dirPaths = prefabPath.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).Select(n => n.ToLower()).ToList();
			List<string> directoryNames = dirNames;
			bool pathContainsArgument = dirNames.Any(m => prefabPath.Contains(m));
			if(/*directoryNames.Any(n => dirPaths.Any(m => m.Contains(n.ToLower()))) ||*/ pathContainsArgument)
			{
				return true;
			}
		}
		return false;
	}
}