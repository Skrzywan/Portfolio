using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;

public class HierarchySorter
{
	static HierarchySorterSettings Settings => HierarchySorterSettings.Instance;


	[MenuItem("Hierarchy/Show Settings")]
	public static void PingSettingsInEditor()
	{
		EditorGUIUtility.PingObject(Settings);
		Selection.activeObject = Settings;
	}

	[MenuItem("Hierarchy/Sort")]
	public static void SortHierarchy()
	{
		for(int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
		{
			var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
			List<GameObject> transformParentsList = GetGameObjectsToSort(scene);
			Dictionary<HierarchySorterSettings.SortData, List<GameObject>> sortedGos = AssignFolders(transformParentsList);

			MoveGameObjectsToAssignedFolders(sortedGos, scene); 
		}
	}

	private static void MoveGameObjectsToAssignedFolders(Dictionary<HierarchySorterSettings.SortData, List<GameObject>> sortedGos, UnityEngine.SceneManagement.Scene scene)
	{
		foreach(KeyValuePair<HierarchySorterSettings.SortData, List<GameObject>> pair in sortedGos)
		{
			GameObject newFolder = GetFolder(pair.Key.hierarchyFolderName, scene);
			for(int i = 0; i < pair.Value.Count; i++)
			{
				pair.Value[i].transform.SetParent(newFolder.transform);
			}
		}
	}

	private static Dictionary<HierarchySorterSettings.SortData, List<GameObject>> AssignFolders(List<GameObject> rendParentsList)
	{
		Dictionary<HierarchySorterSettings.SortData, List<GameObject>> sortedGos = new();
		for(int i = 0; i < Settings.sortDatas.Count; i++)
		{
			if(Settings.sortDatas[i].enabled)
			{
				for(int j = rendParentsList.Count - 1; j >= 0; j--)
				{
					if(DoesMatchSortData(rendParentsList[j], Settings.sortDatas[i]))
					{
						if(!sortedGos.ContainsKey(Settings.sortDatas[i]))
						{
							sortedGos.Add(Settings.sortDatas[i], new List<GameObject>());
						}
						Debug.Log($"<b>{rendParentsList[j]?.name}</b> belongs in <b>{Settings.sortDatas[i].hierarchyFolderName}</b>", rendParentsList[j].gameObject);
						sortedGos[Settings.sortDatas[i]].Add(rendParentsList[j]);
						rendParentsList.RemoveAt(j);
					}
				} 
			}
		}

		return sortedGos;
	}

	private static List<GameObject> GetGameObjectsToSort(UnityEngine.SceneManagement.Scene scene = default)
	{
		Transform[] transforms = null;
		if(scene.isLoaded)
		{
			transforms = scene.GetRootGameObjects().SelectMany(n => n.GetComponentsInChildren<Transform>()).ToArray();
		} else
		{
			transforms = GameObject.FindObjectsOfType<Transform>();
		}
		HashSet<GameObject> transParentsHashSet = new HashSet<GameObject>();

		for(int i = 0; i < transforms.Length; i++)
		{
			if(PrefabUtility.IsPartOfPrefabInstance(transforms[i].gameObject))
			{
				GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(transforms[i].gameObject);
				if(prefabRoot != null)
				{
					transParentsHashSet.Add(prefabRoot);
				}
			} else
			{
				LODGroup[] parentLOD = transforms[i].GetComponentsInParent<LODGroup>();
				if(parentLOD != null)
				{
					parentLOD = parentLOD.OrderBy(n => n.transform.GetComponentsInParent<Transform>().Length).ToArray();
					LODGroup lODGroup = parentLOD.FirstOrDefault();
					if(lODGroup != null)
					{
						transParentsHashSet.Add(lODGroup?.gameObject);
					} else
					{
						transParentsHashSet.Add(transforms[i]?.gameObject);
					}

				} else
				{
					transParentsHashSet.Add(transforms[i]?.gameObject);
				}
			}
		}
		List<GameObject> rendParentsList = transParentsHashSet.ToList();
		return rendParentsList;
	}


	static private GameObject GetFolder(string hierarchyFolderName, UnityEngine.SceneManagement.Scene scene)
	{
		string[] hierarchyPaths = hierarchyFolderName.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar});
		GameObject lastFolder = null;
		for(int i = 0; i < hierarchyPaths.Length; i++)
		{
			GameObject folder = FindOrCreateFolder(hierarchyPaths[i], lastFolder?.transform, scene);
			if(lastFolder != null && folder.transform.parent != lastFolder)
			{
				folder.transform.SetParent(lastFolder.transform);
			}
			lastFolder = folder;
		}

		return lastFolder;
	}

	private static GameObject FindOrCreateFolder(string folderName, Transform parent = null, UnityEngine.SceneManagement.Scene scene = default)
	{
		folderName = folderName.Trim();
		GameObject goToRet = null;
		if(parent == null)
		{
			if(scene.isLoaded)
			{
				GameObject[] rootGos = scene.GetRootGameObjects();
				GameObject child = null;
				for(int i = 0; i < rootGos.Length; i++)
				{
					child = rootGos[i].transform.FindChildRecursive(folderName)?.gameObject;
					if(child != null)
					{
						goToRet = child;
						break;
					}
				}
			} else
			{
				goToRet = GameObject.Find(folderName);
			}
		} else
		{
			goToRet = parent.FindChildRecursive(folderName)?.gameObject;
		}
		if(goToRet == null)
		{
			goToRet = new GameObject(folderName);
			if(scene.isLoaded)
			{
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(goToRet,scene);
			}
			goToRet.transform.position = Vector3.zero;
			goToRet.transform.rotation = Quaternion.identity;
		}

		return goToRet;
	}

	static bool DoesMatchSortData(GameObject go, HierarchySorterSettings.SortData sortData)
	{
		//string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
		bool conditionsMet = false;
		for(int i = 0; i < sortData.validators.Count; i++)
		{
			if(sortData.validators[i].validator.GetArgumentType() == typeof(string))
			{
				sortData.validators[i].validator.SetArguments(sortData.validators[i].stringArguments);
			} else
			{
				sortData.validators[i].validator.SetArguments(sortData.validators[i].objectArguments);
			}
			bool isValid = sortData.validators[i].validator.Validate(go);
			if(isValid)
			{
				conditionsMet = true;
				if(sortData.logicStatement == HierarchySorterSettings.SortData.LogicStatement.AnyConditionMatches)
				{
					return true;
				}
			} else
			{
				return false;
			}
		}

		return conditionsMet;
	}

}
