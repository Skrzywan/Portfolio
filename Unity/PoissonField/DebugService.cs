using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DebugService
{

	[System.Serializable]
	public struct GizmosToDraw
	{
		public int key;
		public enum Type { Line, Box, Sphere }
		public Type type;
		public Vector3 start;
		public Vector3 end;
		public Color color;
		public float displayDuration;
	}
	static List<GizmosToDraw> gizmosToDraws = new List<GizmosToDraw>();
#if UNITY_EDITOR

	static DebugService()
	{
		SceneView.duringSceneGui -= SceneView_duringSceneGui;
		SceneView.duringSceneGui += SceneView_duringSceneGui;
	}

	private static void SceneView_duringSceneGui(SceneView obj)
	{
		RemoveExpiredGizmos();
		for(int i = 0; i < gizmosToDraws.Count; i++)
		{
			Handles.color = gizmosToDraws[i].color;
			switch(gizmosToDraws[i].type)
			{
				case GizmosToDraw.Type.Line:
					Handles.DrawLine(gizmosToDraws[i].start, gizmosToDraws[i].end);
					break;
				case GizmosToDraw.Type.Box:
					Handles.DrawWireCube(gizmosToDraws[i].start, gizmosToDraws[i].end);
					break;
				case GizmosToDraw.Type.Sphere:
					Handles.DrawWireDisc(gizmosToDraws[i].start, Vector3.up, gizmosToDraws[i].end.x);
					break;
				default:
					break;
			}
		}
	}

#endif

	public static int AddGizmo(Vector3 start, Vector3 end, Color color, float displayForSeconds)
	{

		GizmosToDraw gizmoToDraw = new GizmosToDraw
		{
			type = GizmosToDraw.Type.Line,
			start = start,
			end = end,
			color = color,
			displayDuration = displayForSeconds
		};

		return AddGizmo(gizmoToDraw);
	}

	public static int AddGizmo(GizmosToDraw gizmosToDraw)
	{
#if UNITY_EDITOR
		if(gizmosToDraw.key == 0)
		{
			int key = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
			gizmosToDraw.key = key;
		}
		gizmosToDraw.displayDuration += (float)EditorApplication.timeSinceStartup;
		gizmosToDraws.Add(gizmosToDraw);
		//Debug.Log($"Draw line {gizmosToDraw.start.ToShortString()} => {gizmosToDraw.end.ToShortString()}");
		return gizmosToDraw.key;
#else
		return 0;
#endif
	}


	private static void RemoveExpiredGizmos()
	{
#if UNITY_EDITOR
		gizmosToDraws.RemoveAll(n => n.displayDuration < EditorApplication.timeSinceStartup);
#endif
	}

	public static void ShowEditorNotification(string text)
	{
#if UNITY_EDITOR

		UnityEditor.SceneView.lastActiveSceneView?.ShowNotification(new GUIContent(text));

		System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
		EditorWindow gameview = EditorWindow.GetWindow(T);
		gameview?.ShowNotification(new GUIContent(text));
#endif
	}

	internal static void DisableGizmosForSeconds(float seconds)
	{

#if UNITY_EDITOR
		if(AreGizmosEnabled())
		{
			if(SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.drawGizmos)
			{
				EditorSchedulerService.Instance.ScheduleAction(EnableGizmos, seconds);
			}
		}
		SetGizmos(false);
		EditorSchedulerService.Instance.UpdateScheduledActionDeadline(EnableGizmos, seconds);
#endif
	}

	public static void EnableGizmos()
	{
		SetGizmos(true);
	}

	public static void SetGizmos(bool value)
	{
#if UNITY_EDITOR
		if(SceneView.lastActiveSceneView != null)
		{
			SceneView.lastActiveSceneView.drawGizmos = value;
		}
		if(SceneView.currentDrawingSceneView != null)
		{
			SceneView.currentDrawingSceneView.drawGizmos = value;
		}
#endif

	}

	public static bool AreGizmosEnabled()
	{
#if UNITY_EDITOR

		if(SceneView.lastActiveSceneView != null)
		{
			return SceneView.lastActiveSceneView.drawGizmos;
		}
		if(SceneView.currentDrawingSceneView != null)
		{
			return SceneView.currentDrawingSceneView.drawGizmos;
		}
#endif
		return false;
	}

	internal static int AddWireCube(Vector3 position, Vector3 vector3, Color color, float displayForSeconds)
	{
		GizmosToDraw gizmoToDraw = new GizmosToDraw
		{
			type = GizmosToDraw.Type.Box,
			start = position,
			end = vector3,
			color = color,
			displayDuration = displayForSeconds
		};
		return AddGizmo(gizmoToDraw);
	}

	internal static int RemoveGizmo(int key)
	{
		return gizmosToDraws.RemoveAll(n => n.key == key);
	}
}
