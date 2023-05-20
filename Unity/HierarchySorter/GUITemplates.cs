using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class GUITemplates
{
	private const int HEADER_FONT_SIZE = 20;

	public static void DrawHeader(string headerText)
	{
#if UNITY_EDITOR

		EditorGUI.indentLevel = 0;
		GUIContent content = new GUIContent(headerText);
		GUIStyle style = new GUIStyle(GUI.skin.label);
		style.fontStyle = FontStyle.Bold;
		style.fontSize = 16;
		style.CalcSize(content);

		EditorGUILayout.LabelField(content, style, GUILayout.Height(style.fontSize + 6));
		EditorGUI.indentLevel = 1;
#else
		Debug.LogError($"{nameof(DrawHeader)} works only in editor");
#endif
	}

	public static Label GetHeaderElement(string xmlName, string text)
	{
		Label label = new Label(text);
		label.style.fontSize = HEADER_FONT_SIZE;
		label.name = xmlName;

		label.style.marginTop = 5;
		label.style.marginBottom = 5;
		label.style.marginLeft = 1;
		label.style.marginRight = 10;

		return label;
	}

	public static VisualElement GetSpaceElement()
	{
		VisualElement spaceElement = new VisualElement();
		spaceElement.name = "SpaceElement";
#if UNITY_EDITOR

		spaceElement.style.paddingTop = EditorGUIUtility.singleLineHeight / 2;
		spaceElement.style.paddingBottom = EditorGUIUtility.singleLineHeight / 2;
#else

		spaceElement.style.paddingTop = 8;
		spaceElement.style.paddingBottom = 8;
#endif
		return spaceElement;
	}

	/// <summary>
	/// Tworzy box z tytulem i kontenerem na elementy
	/// </summary>
	/// <param name="parent">Obiekt do ktorego ma byc zaparentowany dany box</param>
	/// <param name="title">Tytul wyswietlany u gory boxa</param>
	/// <returns>kontener na nowe obiekty wewnatrz wygenerowanego boxa</returns>
	public static VisualElement GetTitledBoxElement(VisualElement parent, string title)
	{
		Box box = new Box();
		box.Add(GetHeaderElement(title, title));
		box.name = title;

		box.style.marginTop = 5;
		box.style.marginBottom = 5;
		box.style.marginLeft = 5;
		box.style.marginRight = 5;
		box.style.display = DisplayStyle.Flex;
		box.style.whiteSpace = WhiteSpace.NoWrap;

		VisualElement container = new VisualElement();
		container.style.marginLeft = 15;
		container.name = title + "_Container";
		container.style.flexGrow = 1;
		box.Add(container);
		parent?.Add(box);
		return container;
	}

}
