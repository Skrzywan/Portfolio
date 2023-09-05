using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Generates more or less uniformaly distributed random points for given dataset <see href="https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf"/>
/// </summary>
public class PoissonDiskSamplingPointGenerator
{
	protected const int TIMEOUT_SECONDS = 10;
	protected System.Random random = new System.Random();

	protected LayerData layerData;
	protected HashSet<Vector2Int> indexesToCheck = new HashSet<Vector2Int>();
	protected HashSet<Vector2Int> checkedIndexes = new HashSet<Vector2Int>();
	internal bool debug;
	internal int maxPoints;

	List<Vector2Int> sampleIndexesCached = new List<Vector2Int>();
	internal CancellationToken cancelToken;

	[System.Serializable]
	public class LayerData
	{
		public float gridSize = 100;
		public float objectRadious = 500;
		public Vector3 center = Vector3.zero;
		public Vector2 boardSize = new Vector2(5000, 5000);
	}

	public static List<Vector3> Generate(int seed, LayerData layerData)
	{
		PoissonDiskSamplingPointGenerator generator = new PoissonDiskSamplingPointGenerator(seed, layerData);
		return generator.Generate();
	}

	public PoissonDiskSamplingPointGenerator(int seed, LayerData layerData)
    {
		random = new System.Random(seed);
		this.layerData = layerData;
    }

    public virtual List<Vector3> Generate()
	{
		DateTime startTime = DateTime.Now;
		int count = (int)((layerData.boardSize.x  * layerData.boardSize.y) / layerData.objectRadious) / 2;
		int listCount = (int)((layerData.boardSize.x / layerData.objectRadious) * (layerData.boardSize.y / layerData.objectRadious));

		List<Vector3> generatedPoints = new List<Vector3>(count);
		checkedIndexes.EnsureCapacity(count);
		indexesToCheck.EnsureCapacity(count);

		do
		{
			Vector3 point = Vector3.zero;
			if(generatedPoints.Count == 0)
			{
				point = GetStartPoint();
			} else
			{
				point = GetNextPoint(indexesToCheck);
			}

			generatedPoints.Add(point);


			var index = PositionToIndex(point);
			List<Vector2Int> borderIndexes = GetBorderIndexes(index);
			borderIndexes.RemoveAll(n => checkedIndexes.Contains(n));
			indexesToCheck.UnionWith(borderIndexes);

			var insideCells = GetIndexesInsideSample(index);
			for(int i = 0; i < insideCells.Count; i++)
			{
				indexesToCheck.Remove(insideCells[i]);
				checkedIndexes.Add(insideCells[i]);
			}

			if(((DateTime.Now - startTime).TotalSeconds > TIMEOUT_SECONDS))
			{
				Debug.LogError($"<b>TIMEOUT</b> in {(DateTime.Now - startTime).TotalSeconds} seconds generated {generatedPoints.Count} points, and checked {checkedIndexes.Count}  Left to check {indexesToCheck.Count}  borderIndexes {borderIndexes.Count}");
				break;
			}
		}
		while(indexesToCheck.Count > 0 && generatedPoints.Count < maxPoints && !cancelToken.IsCancellationRequested);
		if(debug)
		{
			//DrawBoard();
			DrawPoints(generatedPoints);
		}
		return generatedPoints;
	}

	private void DrawPoints(List<Vector3> generatedPoints)
	{
		for(int i = 0; i < generatedPoints.Count; i++)
		{
			DebugService.AddGizmo(new DebugService.GizmosToDraw()
			{
				color = Color.red,
				displayDuration = 10,
				start = generatedPoints[i],
				end = new Vector3(layerData.objectRadious / 2, layerData.objectRadious / 2, layerData.objectRadious / 2),
				type = DebugService.GizmosToDraw.Type.Sphere
			});
		}
	}

	private void DrawBoard()
	{
		int maxxIndex = (int)(layerData.boardSize.x / layerData.gridSize);
		int maxyIndex = (int)(layerData.boardSize.y / layerData.gridSize);
		for(int x = 0; x < maxxIndex; x++)
		{
			for(int y = 0; y < maxyIndex; y++)
			{
				Color tmpCol = Color.white;
				if(indexesToCheck.Contains(new Vector2Int(x,y)))
				{
					tmpCol *= 0.5f;
				}
				Vector3 position = IndexesToPosition(new Vector2Int(x,y));
				
				DebugService.AddWireCube(position,layerData.gridSize * Vector3.one, tmpCol, 10);
			}
		}
	}

	protected virtual Vector3 GetNextPoint(HashSet<Vector2Int> indexPool)
	{
		int indexToCheck = random.Next(indexPool.Count);
		var enumerator = indexPool.GetEnumerator();
		enumerator.MoveNext();
		for(int i = 0; i < indexToCheck; i++)
		{
			enumerator.MoveNext();
		}
		Vector2Int nextIndex = enumerator.Current;
		Vector3 position = IndexesToPosition(nextIndex);

		//var randRadious = (float)random.NextDouble() * layerData.objectRadious / 2;
		//var pointOnCirc = (float)random.NextDouble() * randRadious * 2 * Mathf.PI;

		//position.x += (randRadious * Mathf.Cos(pointOnCirc));
		//position.z += (randRadious * Mathf.Sin(pointOnCirc));

		return position;
	}

	public List<Vector2Int> GetBorderIndexes(Vector2Int index)
	{

		UnityEngine.Profiling.Profiler.BeginSample(".GetBorderIndexes");
		List<Vector2Int> borderIndexes = new List<Vector2Int>();
		int neiCellsCount = Mathf.RoundToInt(layerData.objectRadious / layerData.gridSize);
		int maxxIndex = (int)(layerData.boardSize.x / layerData.gridSize);
		int maxyIndex = (int)(layerData.boardSize.y / layerData.gridSize);

		int radiousSquared = (neiCellsCount * neiCellsCount);
		for(int i = -neiCellsCount; i <= neiCellsCount; i++)
		{
			for(int j = -neiCellsCount; j <= neiCellsCount; j++)
			{
				int distToCenter = ((i * i) + (j * j));
				if((distToCenter > radiousSquared - neiCellsCount) && (distToCenter < radiousSquared + neiCellsCount))
				{
					Vector2Int currPoint = new Vector2Int(index.x + i, index.y + j);
					
					if(currPoint.x > 0 && currPoint.x <= maxxIndex && currPoint.y > 0 && currPoint.y <= maxyIndex)
					{
						borderIndexes.Add(currPoint);
					}
				}
			}
		}

		UnityEngine.Profiling.Profiler.EndSample();
		return borderIndexes;
	}

	public List<Vector2Int> GetIndexesInsideSample(Vector2Int index)
	{
		UnityEngine.Profiling.Profiler.BeginSample(".GetIndexesInsideSample");
		sampleIndexesCached.Clear();
		int neiCellsCount = Mathf.RoundToInt(layerData.objectRadious / layerData.gridSize);

		int radiousSquared = (neiCellsCount * neiCellsCount) - neiCellsCount;
		for(int i = -neiCellsCount; i <= neiCellsCount; i++)
		{
			for(int j = -neiCellsCount; j <= neiCellsCount; j++)
			{
				int distToCenter = ((i * i) + (j * j));
				if((distToCenter < radiousSquared))
				{
					Vector2Int currPoint = new Vector2Int(index.x + i, index.y + j);
					sampleIndexesCached.Add(currPoint);
				}
			}
		}
		UnityEngine.Profiling.Profiler.EndSample();
		return sampleIndexesCached;
	}

	protected virtual Vector3 GetStartPoint()
	{
		//return layerData.center;
		float randomxOffset = RandomRange(-layerData.boardSize.x / 2, layerData.boardSize.x / 2);
		float randomZOffset = RandomRange(-layerData.boardSize.y / 2, layerData.boardSize.y / 2);
		Vector3 offset = new Vector3(randomxOffset, layerData.center.y, randomZOffset);
		return layerData.center + offset;
	}

	protected Vector2Int PositionToIndex(Vector3 position)
	{
		Vector3 localPosition = position - layerData.center;
		Vector3 posFromStart = localPosition + new Vector3(layerData.boardSize.x / 2,0, layerData.boardSize.y / 2);
		int x = (int)(posFromStart.x / layerData.gridSize);
		int y = (int)(posFromStart.z / layerData.gridSize);
		return new Vector2Int(x,y);
	}

	public Vector3 IndexesToPosition(Vector2Int indexes)
	{
		Vector3 startPosition = layerData.center - new Vector3(layerData.boardSize.x / 2, 0, layerData.boardSize.y / 2);
		Vector3 pos = startPosition + new Vector3(indexes.x * layerData.gridSize,0, indexes.y * layerData.gridSize);
		return pos;
	}

	protected float RandomRange(float min, float max)
	{
		float randomVal = (float)random.NextDouble();
		return min + (randomVal * (max - min));
	}

}