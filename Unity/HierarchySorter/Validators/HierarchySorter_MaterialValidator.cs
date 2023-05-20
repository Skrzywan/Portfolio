using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/HierarchySorter/MaterialValidator", fileName = "MaterialValidator_HierarchySorter")]
public class HierarchySorter_MaterialValidator : HierarchySorterValidator
{
	List<Material> materials = new List<Material>();

	public override object GetArguments()
	{
		return materials;
	}

	public override System.Type GetArgumentType()
	{
		return typeof(Material);
	}

	public override void SetArguments(object args)
	{
		materials = args as List<Material>;
	}

	public override bool Validate(GameObject go)
	{
		Renderer[] rends = go.GetComponentsInChildren<Renderer>();
		for(int i = 0; i < rends.Length; i++)
		{
			if(rends[i].sharedMaterials.Any(n => n != null && n == materials.Contains(n)))
			{
				return true;
			}
		}

		return false;
	}

	public override bool Validate(string prefabPath)
	{
		//List<string> labels = AssetDatabase.GetLabels(AssetDatabase.GUIDFromAssetPath(prefabPath)).Select(n => n.ToLower()).ToList();
		List<string> dirPaths = prefabPath.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).Select(n => n.ToLower()).ToList();
		List<string> directoryNames = GetArguments() as List<string>;
		if(directoryNames.Any(n => dirPaths.Contains(n.ToLower())))
		{
			return true;
		}
		return false;
	}

}