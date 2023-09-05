using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class PDS_MonoInit : MonoBehaviour
{
	[SerializeField] int seed = 0;
	[SerializeField] int maxPoints = int.MaxValue;
	[SerializeField] PoissonDiskSamplingPointGenerator.LayerData layerData;
	CancellationTokenSource cancellationToken;

	void Generate()
	{
		cancellationToken = new CancellationTokenSource();

		PoissonDiskSamplingPointGenerator pdspd = new PoissonDiskSamplingPointGenerator(seed, layerData);
		pdspd.cancelToken = cancellationToken.Token;
		pdspd.debug = true;
		pdspd.maxPoints = maxPoints;

		pdspd.Generate();

		cancellationToken.Dispose();
		cancellationToken = null;
	}

	private void GenerateAsync()
	{
		Task tmpTask = Task.Run(Generate);
		tmpTask.ContinueWith(t => {
			if(t.IsCanceled)
			{
				Debug.Log($"Canceled"); 
			}
			cancellationToken.Dispose();
			cancellationToken = null;
			});
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(PDS_MonoInit))]
	public class PDS_MonoGeneratorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			PDS_MonoInit myScript = target as PDS_MonoInit;
			base.OnInspectorGUI();

			if(GUILayout.Button("Generate"))
			{
				myScript.Generate();
			}
			if(myScript.cancellationToken != null)
			{
				if(GUILayout.Button("Cancel running task"))
				{
					myScript.cancellationToken.Cancel();
				}
			} else
			{
				if(GUILayout.Button("Generate async"))
				{
					myScript.GenerateAsync();
				}
			}
		}
	}

#endif
}
