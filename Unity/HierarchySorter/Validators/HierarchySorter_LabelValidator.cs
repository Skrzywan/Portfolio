using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/HierarchySorter/Labelalidator",fileName = "LabelValidator_HierarchySorter")]
public class HierarchySorter_LabelValidator : HierarchySorterValidator
{
	List<string> _labels = new List<string>();
	public override object GetArguments()
	{
		return _labels;
	}

	public override System.Type GetArgumentType()
	{
		return typeof(string);
	}

	public override void SetArguments(object args)
	{
		_labels = args as List<string>;
		if(_labels != null)
		{
			_labels = _labels.Select(n => n?.ToLower()).ToList(); 
		}
	}

	public override bool Validate(GameObject go)
	{
		string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
		return Validate(prefabPath);
	}

	public override bool Validate(string prefabPath)
	{
		List<string> labels = AssetDatabase.GetLabels(AssetDatabase.GUIDFromAssetPath(prefabPath)).Select(n => n.ToLower()).ToList();
		List<string> assetLabels = GetArguments() as List<string>;
		if(assetLabels.Any(n => labels.Contains(n.ToLower())))
		{
			return true;
		}
		return false;
	}
}