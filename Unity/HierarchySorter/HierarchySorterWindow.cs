using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;

public class HierarchySorterWindow : EditorWindow
{
	public const string WINDOW_TITLE = "HierarchySorterWindow";
	HierarchySorter sorterInstance;
	Editor settingsEditor;

	[MenuItem("Window/" + WINDOW_TITLE)]
	static void Init()
	{
		HierarchySorterWindow window = GetWindow<HierarchySorterWindow>();
		window.titleContent = new GUIContent(WINDOW_TITLE);
		window.Show();
	}

	void OnEnable()
	{
		if(sorterInstance == null)
		{
			sorterInstance = new HierarchySorter();
		}
		InitGUI();
	}

	void InitGUI()
	{
		rootVisualElement.Clear();
		DrawCurrentSettings(rootVisualElement);
	}

	private void DrawCurrentSettings(VisualElement parent)
	{
		VisualElement settingsParent = GUITemplates.GetTitledBoxElement(parent, "Settings");
		settingsParent.style.SetPadding(10, 10, 10, 10);
		ScrollView scrollView = new ScrollView();
		scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
		VisualElement settingsElement = GetSettingsInspectorElement();
		Button tmpB = new Button(() => HierarchySorter.SortHierarchy());
		tmpB.text = "Sort";
		tmpB.style.SetPadding(10, 10, 10, 10);

		scrollView.Add(settingsElement);
		settingsParent.Add(scrollView);
		parent.Add(GUITemplates.GetSpaceElement());
		parent.Add(tmpB);
	}

	private VisualElement GetSettingsInspectorElement()
	{
		settingsEditor = Editor.CreateEditor(HierarchySorterSettings.Instance);

		IMGUIContainer imGUIContainer = new IMGUIContainer();
		imGUIContainer.onGUIHandler += (() =>
		{
			EditorGUI.BeginChangeCheck();
			settingsEditor.OnInspectorGUI();
			EditorGUI.EndChangeCheck();
		});

		return imGUIContainer;
	}
}
