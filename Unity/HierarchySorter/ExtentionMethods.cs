using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public static class ExtentionMethods
{
    public static Color Brighten(this Color color, float value)
    {
        color += Color.white * value;
        return color;
    }

    public static bool IsNullOrEmpty(this ICollection collection)
    {
        return collection == null || collection.Count == 0;
	}

    public static void SetPadding(this IStyle style, float top, float bottom, float left, float right)
    {
        style.paddingTop = top;
		style.paddingBottom = bottom;
		style.paddingLeft = left;
        style.paddingRight = right;
	}

    public static Transform FindChildRecursive(this Transform transform, string childName)
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            if(transform.GetChild(i).name == childName)
            {
                return transform.GetChild(i);
            } else
            {
                var child = transform.FindChildRecursive(childName);
                if(child != null)
                {
                    return child;
				}
			}
        }
        return null;
    }
}
