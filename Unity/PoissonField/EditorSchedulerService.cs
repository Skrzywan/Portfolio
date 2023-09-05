using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR

using UnityEditor;
#endif


public class EditorSchedulerService
{
#if UNITY_EDITOR
	static Dictionary<Action, DateTime> scheduledActions = new Dictionary<Action, DateTime>();

	static EditorSchedulerService _instance;
	public static EditorSchedulerService Instance
	{
		get
		{
			if(_instance == null)
			{
				_instance = new EditorSchedulerService();
			}
			return _instance;
		}
	}

	EditorSchedulerService()
	{
		if(_instance == null)
		{
			_instance = this;
		}
		EditorApplication.update -= EditorUpdate;
		EditorApplication.update += EditorUpdate;
	}

	~EditorSchedulerService()
	{
		EditorApplication.update -= EditorUpdate;
	}

	public void EditorUpdate()
	{
		if(scheduledActions.Count > 0)
		{
			HashSet<Action> actionsToRemove = new HashSet<Action>();
			Dictionary<Action, DateTime> scheduledActionsCopy = new Dictionary<Action, DateTime>(scheduledActions);

			foreach(var item in scheduledActionsCopy.Keys)
			{
				if(scheduledActionsCopy[item] < DateTime.Now)
				{
					try
					{
						item?.Invoke();
					} catch(Exception e)
					{
						Debug.LogError(e);
					}
					actionsToRemove.Add(item);
				}
			}
			foreach(var item in actionsToRemove)
			{
				scheduledActions.Remove(item);
			} 
		}
	}

	public void ScheduleAction(Action action, float seconds)
	{
		DateTime targetTime = DateTime.Now + TimeSpan.FromSeconds(seconds);
		if(!scheduledActions.ContainsKey(action))
		{
			scheduledActions.Add(action, targetTime);
		} else
		{
			UpdateScheduledActionDeadline(action,seconds);
		}

	}

	internal void UpdateScheduledActionDeadline(Action action, float seconds)
	{
		DateTime targetTime = DateTime.Now + TimeSpan.FromSeconds(seconds);
		if(scheduledActions.ContainsKey(action))
		{
			scheduledActions[action] = targetTime;
		}
	}
#endif

}
