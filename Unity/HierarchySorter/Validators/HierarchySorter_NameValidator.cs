using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/HierarchySorter/NameValidator",fileName = "NameValidator_HierarchySorter")]
public class HierarchySorter_NameValidator : HierarchySorterValidator
{
	List<string> _names = new List<string>();

	public override object GetArguments()
	{
		return _names;
	}

	public override System.Type GetArgumentType()
	{
		return typeof(string);
	}

	public override void SetArguments(object args)
	{
		_names = args as List<string>;
		if(_names != null)
		{
			_names = _names.Select(n => n?.ToLower()).ToList(); 
		}
	}

	public override bool Validate(GameObject go)
	{
		return Validate(go?.name);
	}

	public override bool Validate(string str)
	{
		if(str != null)
		{
			string lowerStr = str.ToLower();
			bool retVal = _names.Any(n => lowerStr.ToLower().Contains(n));
			return retVal; 
		}
		return false;
	}
}