using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/HierarchySorter/ComponentValidator ", fileName = "ComponentValidator _HierarchySorter")]
public class HierarchySorter_ComponentValidator : HierarchySorterValidator
{
	List<string> componentNames = new List<string>();

	public override object GetArguments()
	{
		return componentNames;
	}

	public override System.Type GetArgumentType()
	{
		return typeof(string);
	}

	public override void SetArguments(object args)
	{
		componentNames = args as List<string>;
		if(!componentNames.IsNullOrEmpty())
		{
			for(int i = 0; i < componentNames.Count; i++)
			{
				componentNames[i] = componentNames[i].ToLower();
			} 
		}
	}

	public override bool Validate(GameObject go)
	{
		List<Component> comps = go.GetComponentsInChildren<Component>().ToList();
		
		List<string> onGOComponentNames = comps.Select(n => n != null ? n.GetType().Name.ToLower() : "null").ToList();
		string containedComponents = componentNames.FirstOrDefault(n => onGOComponentNames.Contains(n.ToLower()));
		if(!string.IsNullOrWhiteSpace(containedComponents))
		{
			return true;
		}
		return false;
	}

	public override bool Validate(string prefabPath)
	{
		GameObject assetGo = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		return Validate(assetGo);
	}

	public override object ValidateArguments(object stringArguments)
	{
		List<string> argumentsList = stringArguments as List<string>;
		if(argumentsList != null)
		{
			for(int i = 0; i < argumentsList.Count; i++)
			{
				argumentsList[i] = argumentsList[i].Replace(" ","");
			}
			return argumentsList;
		}
		return stringArguments;
	}
}