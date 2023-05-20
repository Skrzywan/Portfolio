using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HierarchySorterValidator : ScriptableObject
{
    public abstract bool Validate(GameObject go);
    public abstract bool Validate(string str);
    public abstract object GetArguments();
    public abstract void SetArguments(object args);
    public abstract Type GetArgumentType();
    public virtual object ValidateArguments(object arguments)
    {
        return arguments;
    }
}
