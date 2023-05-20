#define SHOW_DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SpringChallenge2020
{
	public class Program
	{
		private static void Main()
		{
			Game game = new Game();
			game.Init();

			// game loop
			while(true)
			{
				game.Update();
				game.Solve();
				Console.WriteLine(game.Output.ToString());
			}
		}

		public class Game
		{
			Random randomGen;
			public Tile[,] Map = null;
			public readonly StringBuilder Output = new StringBuilder();

			public int myPoints;
			int mapWidth;
			int mapHeight;

			public int opponentPoints;
			public int Turn;
			public List<Unit> Units = new List<Unit>();

			public List<Unit> MyUnits => Units.Where(u => u.isMine).ToList();
			public List<Unit> OpponentUnits => Units.Where(u => !u.isMine).ToList();
			Dictionary<int, Position> lastTargets = new Dictionary<int, Position>();
			//public Position MyHq => MyTeam == Team.Red ? (0, 0) : (11, 11);
			//public Position OpponentHq => MyTeam == Team.Red ? (11, 11) : (0, 0);

			public List<Position> MyPositions = new List<Position>();
			public List<Position> OpponentPositions = new List<Position>();
			public List<Position> NeutralPositions = new List<Position>();

			public void Init()
			{
				randomGen = new Random();
				string[] inputs;
				inputs = Console.ReadLine().Split(' ');
				mapWidth = int.Parse(inputs[0]); // size of the grid
				mapHeight = int.Parse(inputs[1]); // top left corner is (x=0, y=0)
				Map = new Tile[mapWidth, mapHeight];

				InitWallPlacements(mapWidth, mapHeight);

				InitFloorTileNeighbours(mapWidth, mapHeight);
			}

			private void InitWallPlacements(int width, int height)
			{
				for(var y = 0; y < height; y++)
				{
					string row = Console.ReadLine(); // one line of the grid: space " " is floor, pound "#" is wall
					for(var x = 0; x < width; x++)
					{
						Map[x, y] = new Tile
						{
							position = (x, y)
						};
						if(row[x] == '#')
						{
							Map[x, y].isWall = true;
						} else
						{
							Map[x, y].pointValue = -1;
						}
					}
				}
			}

			private void InitFloorTileNeighbours(int width, int height)
			{
				for(int x = 0; x < width; x++)
				{
					for(int y = 0; y < height; y++)
					{
						if(!Map[x, y].isWall)
						{
							if(!Map[Math.Max(0, x - 1), y].isWall)
							{
								Map[x, y].left = Map[Math.Max(0, x - 1), y];
								Map[Math.Max(0, x - 1), y].right = Map[x, y];

								Map[x, y].neighbour.Add(Map[Math.Max(0, x - 1), y]);
								Map[Math.Max(0, x - 1), y].neighbour.Add(Map[x, y]);
							}

							if(!Map[x, Math.Max(0, y - 1)].isWall)
							{
								Map[x, y].up = Map[x, Math.Max(0, y - 1)];
								Map[x, Math.Max(0, y - 1)].down = Map[x, y];

								Map[x, y].neighbour.Add(Map[x, Math.Max(0, y - 1)]);
								Map[x, Math.Max(0, y - 1)].neighbour.Add(Map[x, y]);
							}

							if(!Map[x, Math.Min(height - 1, y + 1)].isWall)
							{
								Map[x, y].down = Map[x, Math.Min(height - 1, y - 1)];
								Map[x, Math.Min(height - 1, y - 1)].up = Map[x, y];

								Map[x, y].neighbour.Add(Map[x, Math.Min(height - 1, y - 1)]);
								Map[x, Math.Min(height - 1, y - 1)].neighbour.Add(Map[x, y]);
							}

							if(!Map[Math.Min(width - 1, x + 1), y].isWall)
							{
								Map[x, y].right = Map[Math.Min(width - 1, x + 1), y];
								Map[Math.Min(width - 1, x + 1), y].left = Map[x, y];

								Map[x, y].neighbour.Add(Map[Math.Min(width - 1, x + 1), y]);
								Map[Math.Min(width - 1, x + 1), y].neighbour.Add(Map[x, y]);
							}
						}
					}
				}
			}

			public void Update()
			{
				Units.Clear();

				MyPositions.Clear();
				OpponentPositions.Clear();
				NeutralPositions.Clear();

				Output.Clear();

				// --------------------------------------

				string[] inputs = Console.ReadLine().Split(' ');
				myPoints = int.Parse(inputs[0]);
				opponentPoints = int.Parse(inputs[1]);
				int visiblePacCount = int.Parse(Console.ReadLine()); // all your pacs and enemy pacs in sight

				for(int i = 0; i < visiblePacCount; i++)
				{
					inputs = Console.ReadLine().Split(' ');

					Unit unit = new Unit();
					unit.id = int.Parse(inputs[0]); // pac number (unique within a team)
					unit.isMine = inputs[1] != "0"; // true if this pac is yours
					int x = int.Parse(inputs[2]); // position in the grid
					int y = int.Parse(inputs[3]); // position in the grid
					unit.position = (x, y);

					unit.typeId = inputs[4]; // unused in wood leagues
					unit.speedTurnsLeft = int.Parse(inputs[5]); // unused in wood leagues
					unit.abilityCooldown = int.Parse(inputs[6]); // unused in wood leagues
					Map[x, y].pointValue = 0;
					Units.Add(unit);
				}

				int visiblePelletCount = int.Parse(Console.ReadLine()); // all pellets in sight
				for(int i = 0; i < visiblePelletCount; i++)
				{
					inputs = Console.ReadLine().Split(' ');
					int x = int.Parse(inputs[0]);
					int y = int.Parse(inputs[1]);
					Map[x, y].pointValue = int.Parse(inputs[2]); // amount of points this pellet is worth
				}

				// Debug
				PrintDebug();
			}

			[Conditional("SHOW_DEBUG")]
			public void PrintDebug()
			{
				Debug.Log($"Turn: {Turn}");
				Debug.Log($"My points: {myPoints}");
				Debug.Log($"Opponent points: {opponentPoints}");

				Debug.Log("=====");
				foreach(var u in Units)
					Debug.Log(u);
			}

			/***
			 * -----------------------------------------------------------
			 * TODO Solve
			 * -----------------------------------------------------------
			 */
			public void Solve()
			{
				// Make sur the AI doesn't timeout

				MoveUnits();

				Turn++;
			}

			public void MoveUnits()
			{
				Debug.Log($"MoveUnits  {MyUnits.Count}");
				// Rush center
				for(int i = 0; i < MyUnits.Count; i++)
				{
					Debug.Log($"Move my Unit  {MyUnits[i].id}");
					Position target = Position.INVALID_POSITION;
					int id = MyUnits[i].id;

					if(lastTargets.ContainsKey(id))
					{
						if(lastTargets[id].Dist(MyUnits[i].position) > 0)
						{
							target = lastTargets[id];
						}
					} else
					{
						lastTargets.Add(id, Position.INVALID_POSITION);
					}

					Tile tmpTarget = FindMoreValuablePoint(MyUnits[i].position);
					if(tmpTarget != null)
					{
						target = tmpTarget.position;
						tmpTarget.pointValue = 0;
					}

					if(target == Position.INVALID_POSITION)
					{
						int newX;
						int newY;
						int tries = 0;
						int maxTries = 100;

						do
						{
							newX = randomGen.Next(0, mapWidth - 1);
							newY = randomGen.Next(0, mapHeight - 1);
							tries++;
							Debug.Log($" try (Map[{newX}, {newY}].isWall {Map[newX, newY].isWall} || Map[{newX}, {newY}].pointValue == {Map[newX, newY].pointValue})");

						} while(!IsTargetPositionValid(newX, newY) && tries < maxTries);

						target = (newX, newY);
					}
					Move(id, target);
					lastTargets[id] = target;
				}
			}

			private bool IsTargetPositionValid(int newX, int newY)
			{
				return (!Map[newX, newY].isWall && Map[newX, newY].pointValue != 0);
			}

			private Tile FindMoreValuablePoint(Position unitPosition)
			{
				Tile toReturn = null;
				float bestValuable = float.MaxValue;
				for(int y = 0; y < mapHeight; y++)
				{
					for(int x = 0; x < mapWidth; x++)
					{
						if(Map[x,y].pointValue > 0)
						{
							float value = unitPosition.Dist(Map[x, y].position) / Map[x, y].pointValue;
							if(value < bestValuable)
							{
								toReturn = Map[x, y];
								bestValuable = value;
							}
						}

					}
				}
				return toReturn;
			}

			public void Move(int id, Position position)
			{
				// TODO: Handle map change
				string moveCommand = $"MOVE {id} {position.x} {position.y}|";
				Output.Append(moveCommand);
				Debug.Log(moveCommand);
			}


			float CalculateOutcome()
			{
				int score = 0;
				return score;
			}
			// TODO: Handle Build command
		}

		public class Action
		{
			enum ActionType { MOVE, TRAIN, WAIT }
			ActionType action;
			public Position position;
			public override string ToString()
			{
				return $"{action.ToString()} {position.ToOutputString()}";
			}
		}

		public class Unit : Entity
		{
			public int id;
			public int Level;
			public string typeId;
			public int abilityCooldown;
			public int speedTurnsLeft;

			public override string ToString() => $"Unit => {base.ToString()} Id: {id} Level: {Level}";
		}

		public class Entity
		{
			public bool isMine;

			public Position position;
			public int X => position.x;
			public int Y => position.y;

			public override string ToString() => $"isMine: {isMine} Position: {position}";
		}

		public class Tile
		{
			public int pointValue;
			public bool isWall;

			public Position position;
			public int X => position.x;
			public int Y => position.y;

			public Tile left;
			public Tile right;
			public Tile up;
			public Tile down;

			public HashSet<Tile> neighbour = new HashSet<Tile>();
		}

		public class Position
		{
			public static Position INVALID_POSITION => new Position(-1,-1);
			public int x;
			public int y;

			public Position() { }

			public Position(int x, int y)
			{
				this.x = x;
				this.y = y;
			}

			public static implicit operator Position(ValueTuple<int, int> cell) => new Position
			{
				x = cell.Item1,
				y = cell.Item2
			};

			public override string ToString() => $"({x},{y})";
			public string ToOutputString() => $"({x} {y})";

			public static bool operator ==(Position obj1, Position obj2) => obj1.Equals(obj2);

			public static bool operator !=(Position obj1, Position obj2) => !obj1.Equals(obj2);

			public override bool Equals(object obj) => !(obj is null) && Equals((Position)obj);

			protected bool Equals(Position other) => !(other is null) && x == other.x && y == other.y;

			public float Dist(Position p) => Math.Abs(x - p.x) + Math.Abs(y - p.y);
		}

		static class Debug
		{
			public static void Log(string log)
			{
				Console.Error.WriteLine(log);
			}

			internal static void Log(Object o)
			{
				Log(o.ToString());
			}
		}
	}
}