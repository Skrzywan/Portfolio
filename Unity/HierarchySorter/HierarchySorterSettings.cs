using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

//[CreateAssetMenu(menuName = "SO/HierarchySorterSettings")]
public class HierarchySorterSettings : ScriptableObject
{

	[System.Serializable]
	public class SortData
	{
		/// <summary>
		/// For a nice label in Unity Inspector window
		/// </summary>
		[HideInInspector] public string name;
		/// <summary>
		/// AnyConditionMatches = or
		/// AllConditionsMustMatch = and
		/// </summary>
		[Tooltip("move to folder: \n" +
			"AnyConditionMatches - if any of listed conditions is met\n" +
			"AllConditionsMustMatch - only of all listed conditions are met at once")]
		public enum LogicStatement { AnyConditionMatches, AllConditionsMustMatch }
		public bool enabled = true;
		public LogicStatement logicStatement;
		public string hierarchyFolderName = "";
		public List<ValidatorArguments> validators = new();
	}


	[System.Serializable]
	public class ValidatorArguments
	{
		public HierarchySorterValidator validator;
		public List<string> stringArguments;
		public List<UnityEngine.Object> objectArguments;
	}


	public List<SortData> sortDatas = new List<SortData>();


	private void OnValidate()
	{
		for(int i = 0; i < sortDatas.Count; i++)
		{
			string[] path = sortDatas[i].hierarchyFolderName.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
			if(!path.IsNullOrEmpty())
			{
				sortDatas[i].name = path[^1];
			}
			for(int j = 0; j < sortDatas[i].validators.Count; j++)
			{
				ValidatorArguments valArgs = sortDatas[i].validators[j];
				if(valArgs.validator != null)
				{

					valArgs.objectArguments = valArgs.validator.ValidateArguments(valArgs.objectArguments) as List<UnityEngine.Object>;
					valArgs.stringArguments = valArgs.validator.ValidateArguments(valArgs.stringArguments) as List<string>;
				}
			}
		}
	}

	public const string SO_LOCATION = "Assets/Resources/SO/Editor/";
	public const string SO_NAME = "HierarchySorterSettings.asset";
	static HierarchySorterSettings _instance;
	public static HierarchySorterSettings Instance
	{
		get
		{
			if(_instance == null)
			{
				_instance = AssetDatabase.LoadAssetAtPath<HierarchySorterSettings>($"{SO_LOCATION}{SO_NAME}");
				if(_instance == null)
				{
					Debug.Log($"Settings asset is empty. Creating new at path: {SO_LOCATION}{SO_NAME}");
					_instance = new HierarchySorterSettings();
					if(!Directory.Exists(SO_LOCATION))
					{
						Directory.CreateDirectory(SO_LOCATION);
					}
					AssetDatabase.CreateAsset(_instance, $"{SO_LOCATION}{SO_NAME}");
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
			}
			return _instance;
		}
	}

	[CustomPropertyDrawer(typeof(ValidatorArguments))]
	public class ValidatorArgumentsDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			UnityEngine.Profiling.Profiler.BeginSample(".ValidatorArgumentsDrawer.OnGUI");

			EditorGUI.BeginProperty(position, label, property);

			SerializedProperty validator = property.FindPropertyRelative(nameof(ValidatorArguments.validator));
			SerializedProperty argsProp = GetUsedArgsProp(property);
			EditorGUI.BeginChangeCheck();

			Rect validatorRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(validatorRect, validator);
			Rect arrayRect = new Rect(position.x, position.y + validatorRect.height, position.width, position.height - validatorRect.height);
			if(argsProp != null)
			{
				EditorGUI.PropertyField(arrayRect, argsProp);
			}

			EditorGUI.EndChangeCheck();
			EditorGUI.EndProperty();

			UnityEngine.Profiling.Profiler.EndSample();
		}

		SerializedProperty GetUsedArgsProp(SerializedProperty property)
		{

			UnityEngine.Profiling.Profiler.BeginSample(".ValidatorArgumentsDrawer.GetUsedArgsProp");

			SerializedProperty validator = property.FindPropertyRelative(nameof(ValidatorArguments.validator));
			SerializedProperty stringArgs = property.FindPropertyRelative(nameof(ValidatorArguments.stringArguments));
			SerializedProperty objectArgs = property.FindPropertyRelative(nameof(ValidatorArguments.objectArguments));

			if(validator.objectReferenceValue != null)
			{
				HierarchySorterValidator validatorObj = validator.objectReferenceValue as HierarchySorterValidator;

				Type genericType = validatorObj.GetArgumentType();
				if(genericType != null)
				{
					if(genericType == typeof(string))
					{
						UnityEngine.Profiling.Profiler.EndSample();
						return stringArgs;
					} else
					{
						if(genericType.IsSubclassOf(typeof(UnityEngine.Object)))
						{
							UnityEngine.Profiling.Profiler.EndSample();
							return objectArgs;
						}
					}
				}
			}

			UnityEngine.Profiling.Profiler.EndSample();
			return null;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			UnityEngine.Profiling.Profiler.BeginSample("ValidatorArgumentsDrawer.GetPropertyHeight");

			SerializedProperty validator = property.FindPropertyRelative(nameof(ValidatorArguments.validator));
			SerializedProperty argsProp = GetUsedArgsProp(property);
			if(validator != null && argsProp != null)
			{

				float height = EditorGUI.GetPropertyHeight(validator, true) + EditorGUI.GetPropertyHeight(argsProp, true);
				height = Mathf.Max(height, 25);

				UnityEngine.Profiling.Profiler.EndSample();
				return height;
			} else
			{
				return 16;
			}
		}
	}
}
