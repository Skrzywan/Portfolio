using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class OctTreeCreator : EditorWindow
{
	Label fileCountExtimate;
	Label sliderValIndicator;

	int levels = 3;
	[MenuItem("Window/OctoTree/Creator")]
	public static void Init()
	{
		OctTreeCreator ctc = GetWindow<OctTreeCreator>();
		ctc.Show();
	}

	private void OnEnable()
	{
		rootVisualElement.Clear();

		BuildPlayerWindow(rootVisualElement);
	}

	private void BuildPlayerWindow(VisualElement root)
	{
		//IntegerField levelsField = new IntegerField("levels");
		Box box = new Box();
		SliderInt levelsField = new SliderInt("levels", 1, 7, SliderDirection.Horizontal);
		fileCountExtimate = new Label();
		levelsField.tooltip = "Restricted to 7 lvls for your own sake";
		levelsField.SetValueWithoutNotify(levels);
		levelsField.RegisterValueChangedCallback(ev =>
		{
			levels = ev.newValue;
			sliderValIndicator.text = levels.ToString();
			RefreshEstimatedFilesCountLabel();
		});
		RefreshEstimatedFilesCountLabel();
		sliderValIndicator = new Label(levels.ToString());
		box.style.flexDirection = FlexDirection.Row;
		levelsField.style.flexGrow = 1;

		box.Add(levelsField);
		box.Add(sliderValIndicator);

		root.Add(box);
		root.Add(fileCountExtimate);

		Button butt = new Button(Create);
		butt.text = "Create";
		root.Add(butt);
	}

	private void RefreshEstimatedFilesCountLabel()
	{
		System.Numerics.BigInteger assetsCount = GetEstimatedFileCount(levels);
		fileCountExtimate.text = $"Estimated {assetsCount.ToString()} ({assetsCount * 3} with meta files)"; //IS: x3 cause directories generate metas too
		int filesPerSecond = 20;
		try
		{
			TimeSpan ts = new TimeSpan(0, 0, (int)(assetsCount / filesPerSecond));
			fileCountExtimate.tooltip = $"\nEstimated time {ts.ToString()} (Given {filesPerSecond} files per second)";
		} catch(Exception e)
		{
			Debug.LogError("Error while estimating time. Harmless");
			Debug.LogError(e);
		}
	}

	private System.Numerics.BigInteger GetEstimatedFileCount(int lvl)
	{
		System.Numerics.BigInteger estimatedFilesCount = 0;
		for(int i = 0; i <= lvl; i++)
		{
			estimatedFilesCount += System.Numerics.BigInteger.Pow(8, i);
		}
		return estimatedFilesCount;
	}

	void Create()
	{
		string targetLocation = EditorUtility.OpenFolderPanel("Select location", $"{Application.dataPath}/Resources/SO/", "Octree");

		OctreeNode main = CreateInstance<OctreeNode>();

		main.bounds = new Bounds(Vector3.zero, Vector3.one * 2000);

		targetLocation = $"Assets/{targetLocation.Substring(Application.dataPath.Length + 1, targetLocation.Length - (Application.dataPath.Length + 1))}";
		DateTime start = DateTime.Now;
		AssetDatabase.StartAssetEditing();
		try
		{
			AssetDatabase.CreateAsset(main, $"{targetLocation}/Octrre_Root.asset");
			AssetDatabase.SetLabels(main, new string[] { "OctreeNode" });

			main.Split(levels);

		} finally
		{
			AssetDatabase.StopAssetEditing();
		}
		Debug.Log($"<color=#79D679FF><b>Finished in {(DateTime.Now - start).TotalSeconds} seconds</b></color>");
		AssetDatabase.SaveAssets();
	}
}
