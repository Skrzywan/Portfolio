#define PROFILE

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace CodinGame
{

	class CodinGameMain
	{
		static byte playerCount = 3;
		static List<Unit> units = new List<Unit> ();
		static List<Looter> looters = new List<Looter> ();
		static List<Tanker> tankers = new List<Tanker> ();
		static List<Tanker> deadTankers = new List<Tanker> ();
		static List<Wreck> wrecks = new List<Wreck> ();
		//List<List<? : Unit>> unitsByType;
		static List<Player> players = new List<Player> ();
		static HashSet<SkillEffect> skillEffects = new HashSet<SkillEffect> ();


		static void Main (string[] args)
		{
			GameVars.Init ();
			Simulation.Init ();
			for (byte i = 0; i < playerCount; i++)
			{
				players.Add (new Player (i));
			}
			// game loop
			while (true)
			{
				ClearContainers ();
				InitInput ();
				GameVars.timeFrame = DateTime.Now;

				//Debug.Log ("new wrecks.Count " + wrecks.Count);
				Simulation s = Simulation.GetSimulation (units, wrecks, skillEffects, 0, players, null);
				s.Simulate ();
				s.PrintResult ();
				Debug.Log (Simulation.sID + " simulations");
				Simulation.Clear ();
				GameVars.initLoop = false;
#if PROFILE

				Debug.PrintProfiler ();
				Debug.Clear (); 
#endif
				Debug.Log ((DateTime.Now - GameVars.timeFrame).TotalMilliseconds + " ms");
				//Console.WriteLine (players[0].getReaper ().wantedThrustTarget+" "+ players[0].getReaper ().wantedThrustTarget);
				//Console.WriteLine ("WAIT");
				//Console.WriteLine ("WAIT");
			}
		}

		private static void ClearContainers ()
		{
			wrecks.Clear ();
			units.Clear ();
			looters.Clear ();
			tankers.Clear ();
			deadTankers.Clear ();
			skillEffects.Clear ();
		}

		private static void InitInput ()
		{

			int myScore = int.Parse (Console.ReadLine ());
			int enemyScore1 = int.Parse (Console.ReadLine ());
			int enemyScore2 = int.Parse (Console.ReadLine ());
			int myRage = int.Parse (Console.ReadLine ());
			int enemyRage1 = int.Parse (Console.ReadLine ());
			int enemyRage2 = int.Parse (Console.ReadLine ());
			int unitCount = int.Parse (Console.ReadLine ());

			players[0].score = (byte)myScore;
			players[0].rage = (byte)myRage;
			players[1].score = (byte)myScore;
			players[1].rage = (byte)enemyRage1;
			players[1].score = (byte)myScore;
			players[2].rage = (byte)enemyRage2;


			for (int i = 0; i < unitCount; i++)
			{
				string[] inputs = Console.ReadLine ().Split (' ');
				int unitId = int.Parse (inputs[0]);
				int unitType = int.Parse (inputs[1]);
				int player = int.Parse (inputs[2]);
				float mass = float.Parse (inputs[3]);
				int radius = int.Parse (inputs[4]);
				int x = int.Parse (inputs[5]);
				int y = int.Parse (inputs[6]);
				int vx = int.Parse (inputs[7]);
				int vy = int.Parse (inputs[8]);
				int extra = int.Parse (inputs[9]);
				int extra2 = int.Parse (inputs[10]);

				switch (unitType)
				{
					case 0:
						Reaper reaper = Reaper.InitReaper (players[player], x, y);
						reaper.vx = vx;
						reaper.vy = vy;
						units.Add (reaper);
						break;
					case 1:
						Destroyer d = Destroyer.InitDestroyer (players[player], x, y);
						d.vx = vx;
						d.vy = vy;
						units.Add (d);

						break;
					case 2:
						Doof doof = Doof.InitDoof (players[player], x, y);
						doof.vx = vx;
						doof.vy = vy;
						units.Add (doof);
						break;
					case 3:
						Tanker t = Tanker.InitTanker (extra2, null, x, y);
						t.vx = vx;
						t.vy = vy;
						t.water = extra;
						units.Add (t);
						tankers.Add (t);
						break;
					case 4:
						Wreck wr = new Wreck (x, y, extra, GameVars.TANKER_MAX_RADIUS);
						wr.radius = radius;
						wrecks.Add (wr);

						break;
					default:
						break;
				}
			}
		}
	}

	class Debug
	{
		public static void Log (object log)
		{

#if PROFILE
			Console.Error.WriteLine (log);
#endif
		}

		static Dictionary<string, float> profiler = new Dictionary<string, float> ();
		static Stack<KeyValuePair<string, DateTime>> profilerStart = new Stack<KeyValuePair<string, DateTime>> ();
		public static void ProfilerStart (string sampleName)
		{
#if PROFILE
			profilerStart.Push (new KeyValuePair<string, DateTime> (sampleName, DateTime.Now));
#endif
		}

		public static void ProfilerEnd ()
		{
#if PROFILE
			KeyValuePair<string, DateTime> fifo = profilerStart.Pop ();
			float totalMilliseconds = (float)(DateTime.Now - fifo.Value).TotalMilliseconds;

			if (profiler.ContainsKey (fifo.Key))
			{
				profiler[fifo.Key] += totalMilliseconds;
			} else
			{
				profiler.Add (fifo.Key, totalMilliseconds);
			}
#endif
		}

		public static void PrintProfiler ()
		{
#if PROFILE
			Log ("===Profiler===");
			foreach (var item in profiler.Keys)
			{
				Log (item + " " + profiler[item]);
			}
#endif
		}

		public static void Clear ()
		{
			ClearProfiler ();
		}

		public static void ClearProfiler ()
		{
			profiler.Clear ();
			profilerStart.Clear ();
		}
	}

	class Simulation
	{
		private const int MAX_DEPTH = 4;
		private const int MAX_COLLISION_DEPTH = 3;
		public static float[] directionAngles = { -1, 0, 60, 120, 180, 240, 300 };
		public static Point[] directions;
		public int depth;
		public Simulation (List<Unit> tmpUnits, List<Wreck> tmpWreck, HashSet<SkillEffect> tmpSkillEffects, int depth, List<Player> prevPlayers, Simulation parentSim = null)
		{
			InitSim (tmpUnits, tmpWreck, tmpSkillEffects, depth, prevPlayers, parentSim);
		}

		public Simulation ()
		{
		}

		private void InitSim (List<Unit> tmpUnits, List<Wreck> tmpWreck, HashSet<SkillEffect> tmpSkillEffects, int depth, List<Player> prevPlayers, Simulation parentSim)
		{
			units.Clear ();
			reapers.Clear ();
			doofTargets.Clear ();

			looters.Clear ();
			tankers.Clear ();
			wrecks.Clear ();

			players.Clear ();
			skillEffects.Clear ();
			childSimulations.Clear ();
			destroyers.Clear ();

			this.depth = depth;
			parentSimulation = parentSim;
			for (int i = 0; i < playerCount; i++)
			{
				players.Add (new Player (prevPlayers[i]));
			}
			for (int i = 0; i < tmpUnits.Count; i++)
			{
				int index = tmpUnits[i].getPlayerIndex ();
				if (tmpUnits[i].type == 3)
				{
					//Debug.ProfilerStart ("InitUnit3");

					Tanker t = Tanker.InitTanker (tmpUnits[i] as Tanker, players[index]);
					////Debug.ProfilerEnd ();
					////Debug.ProfilerStart ("InitUnitValuesTanker");

					t.move ((int)tmpUnits[i].x, (int)tmpUnits[i].y);
					units.Add (t);
					tankers.Add (t);
					//Debug.ProfilerEnd ();
				} else
				{

					if (tmpUnits[i].type == 0)
					{
						//Debug.ProfilerStart ("InitUnit0");
						Reaper reaper = Reaper.InitReaper (tmpUnits[i] as Reaper, players[index]);
						////Debug.ProfilerEnd ();
						////Debug.ProfilerStart ("InitUnitValuesReaper");

						units.Add (reaper);
						reapers.Add (reaper);
						doofTargets.Add (reaper);
						//Debug.ProfilerEnd ();
					} else
					{
						if (tmpUnits[i].type == 1)
						{
							//Debug.ProfilerStart ("InitUnit1");

							Destroyer d = Destroyer.InitDestroyer (tmpUnits[i] as Destroyer, players[index]);
							////Debug.ProfilerEnd ();
							////Debug.ProfilerStart ("InitUnitValuesDestroyer");
							destroyers.Add (d);
							units.Add (d);
							//doofTargets.Add (d);
							//Debug.ProfilerEnd ();
						} else
						if (tmpUnits[i].type == 2)
						{
							//Debug.ProfilerStart ("InitUnit2");

							Doof doof = Doof.InitDoof (tmpUnits[i] as Doof, players[index]);
							////Debug.ProfilerEnd ();
							////Debug.ProfilerStart ("InitUnitValuesDoof");

							//doofTargets.Add (doof);
							units.Add (doof);
							//Debug.ProfilerEnd ();
						}
					}
				}

				//Debug.ProfilerEnd ();

				if (depth > 0)
				{
					if (tmpUnits[i].wantedThrustTarget != null)
					{
						units[i].wantedThrustTarget = new Point (tmpUnits[i].wantedThrustTarget);
					} else
					{
						units[i].wantedThrustTarget = new Point ();
					}
					units[i].wantedThrustPower = tmpUnits[i].wantedThrustPower;
				}
			}

			//Debug.Log ("tmpWreck.Count "+tmpWreck.Count);
			for (int i = 0; i < tmpWreck.Count; i++)
			{
				wrecks.Add (new Wreck (tmpWreck[i]));
			}
			HashSet<SkillEffect>.Enumerator tmpEnum = tmpSkillEffects.GetEnumerator ();
			while (tmpEnum.MoveNext ())
			{
				skillEffects.Add (new SkillEffect (tmpEnum.Current));
			}
		}

		public static void InitDirections ()
		{
			directions = new Point[directionAngles.Length];
			for (int i = 0; i < directionAngles.Length; i++)
			{
				float degrees = GameVars.DEG_TO_RAD * directionAngles[i];
				Point result = new Point (GameVars.MAX_THRUST, 0);
				result.x = result.x * (float)Math.Cos (degrees) - result.y * (float)Math.Sin (degrees);
				result.y = result.x * (float)Math.Sin (degrees) + result.y * (float)Math.Cos (degrees);
				directions[i] = result.normalized;
			}
		}

		public Point GetDirection (int i)
		{
			return directions[i];
		}

		int loopCountMulti = 2;
		int loopCount = 0;

		int playerCount = 3;
		List<Unit> units = new List<Unit> ();
		List<Reaper> reapers = new List<Reaper> ();
		List<Unit> doofTargets = new List<Unit> ();

		List<Looter> looters = new List<Looter> ();
		List<Tanker> tankers = new List<Tanker> ();
		List<Wreck> wrecks = new List<Wreck> ();
		List<Destroyer> destroyers = new List<Destroyer> ();


		//List<List<? : Unit>> unitsByType;
		List<Player> players = new List<Player> ();
		HashSet<SkillEffect> skillEffects = new HashSet<SkillEffect> ();
		List<Simulation> childSimulations = new List<Simulation> ();
		Simulation parentSimulation;
		static int sendLog = -1;
		internal void Simulate ()
		{
			Simulation best = PrepareChildren ();
			//for (int i = 0; i < childSimulations.Count; i++)
			//{
			//	if (childSimulations[i].scoreCache != best.scoreCache)
			//	{
			//		childSimulations[i] = Simulation.GetSimulation (best.units, best.wrecks, best.skillEffects, best.depth, best.players, this);
			//		childSimulations[i].Mutate (); 
			//	}
			//}
			if (depth <= MAX_DEPTH && best != null)
			{
				for (int i = 0; i < childSimulations.Count; i++)
				{
					if (CanContinue ())
					{
						childSimulations[i].Simulate ();
					}
				}
				//best.Simulate ();
			}
			//Debug.Log ((DateTime.Now - GameVars.timeFrame).TotalMilliseconds + " ms");

		}

		static bool CanContinue ()
		{
			if (!GameVars.initLoop)
			{
				float totalMiliseconds = (float)(DateTime.Now - GameVars.timeFrame).TotalMilliseconds;
				return totalMiliseconds < GameVars.MILLISECONDS_LIMIT;
			}
			return true;
		}


		private Simulation PrepareChildren ()
		{
			float bestScore = float.MinValue;
			Simulation bestSim = null;
			Simulation s = null;
			if (depth < MAX_DEPTH)
			{
				for (int i = 0; i < directionAngles.Length; i++)
				{
					if (CanContinue ())
					{
						s = Simulation.GetSimulation (units, wrecks, skillEffects, depth + 1, players, this);
						childSimulations.Add (s);

						s.PrepareSimStage (i, 0);

					}
				}

			}
			for (int i = 0; i < childSimulations.Count; i++)
			{
				if (CanContinue ())
				{
					s = childSimulations[i];
					s.Play ();
					float score = s.Score ();
					if (score > bestScore)
					{
						bestScore = score;
						bestSim = s;
					}
				}
			}

			return bestSim;
		}

		private void PrepareSimStage (int i, int j)
		{
			for (int k = 0; k < units.Count; k++)
			{
				if (units[k].getPlayerIndex () != 0 || units[k].type == 4)
				{
					if (i == 0) //IS: skoro ich uzyje znowu i sie nie zmienia to nie ma sensu ponowanie liczyc
					{
						SimpleSim (units[k]);
					}
				} else
				{
					int direction = 0;
					if (units[k].type == 0)
					{
						direction = i;
					} else
					{
						if (units[k].type == 2)
							direction = j;
					}
					SimulateUnit (units[k], direction);
				}
			}

		}

		public bool Play ()
		{
			// Apply skill effects
			foreach (SkillEffect effect in skillEffects)
			{
				effect.apply (units);
			}

			// Apply thrust for tankers
			foreach (Tanker tank in tankers)
			{
				tank.play ();
			}

			// Apply wanted thrust for looters
			foreach (Player player in players)
			{
				foreach (Looter looter in player.looters)
				{
					if (looter != null && looter.wantedThrustTarget != null)
					{
						looter.thrust (looter.wantedThrustTarget, looter.wantedThrustPower);
					}
				}
			}

			if (!CanContinue ())
			{
				return false;
			}

			float t = 0.0f;

			// Play the round. Stop at each collisions and play it. Reapeat until t > 1.0

			if (depth < MAX_COLLISION_DEPTH)
			{

				for (int i = 0; i < units.Count; i++)
				{
					Collision collision = getNextCollision (units[i]);

					if (collision.a != null && collision.b != null)
					{
						float tmpDelta = collision.t;
						units[i].move (tmpDelta);
						playCollision (collision);
					}

				}
			}

			if (!CanContinue ())
			{
				return false;
			}
			// No more collision. Move units until the end of the round
			float delta = 1.0f - t;
			for (int i = 0; i < units.Count; i++)
			{
				units[i].move (delta);
			}


			for (int i = 0; i < tankers.Count; i++)
			{
				FillTankers (units, tankers, tankers[i]);
			}

			for (int i = wrecks.Count - 1; i >= 0; i--)
			{
				bool alive = wrecks[i].harvest (players, skillEffects);
				if (!alive)
				{
					wrecks.RemoveAt (i);
				}
			}

			// Round values and apply friction
			adjust ();

			// Generate rage
			if (GameVars.LOOTER_COUNT >= 3)
			{
				for (int i = 0; i < players.Count; i++)
				{
					players[i].rage = (byte)Math.Min (GameVars.MAX_RAGE, players[i].rage + players[i].getDoof ().sing ());
				}
			}

			for (int i = 0; i < units.Count; i++)
			{
				while (units[i].mass >= GameVars.REAPER_SKILL_MASS_BONUS)
				{
					units[i].mass -= GameVars.REAPER_SKILL_MASS_BONUS;
				}
			}

			// Remove dead skill effects
			HashSet<SkillEffect> effectsToRemove = new HashSet<SkillEffect> ();
			foreach (SkillEffect effect in skillEffects)
			{
				if (effect.duration <= 0)
				{
					effectsToRemove.Add (effect);
				}
			}

			skillEffects.RemoveWhere (n => effectsToRemove.Contains (n));

			return true;
		}

		private void adjust ()
		{
			for (int i = 0; i < units.Count; i++)
			{
				units[i].adjust (skillEffects);
			}
		}

		void spawnTanker (Player player)
		{
			TankerSpawn spawn = player.tankers.Dequeue ();

			float angle = (player.index + spawn.angle) * (float)Math.PI * 2.0f / ((float)playerCount);

			float cos = (float)Math.Cos (angle);
			float sin = (float)Math.Sin (angle);

			if (GameVars.SPAWN_WRECK)
			{
				// Spawn a wreck directly
				int tmpCos = (int)(cos * GameVars.WATERTOWN_RADIUS);
				int tmpSin = (int)(sin * GameVars.WATERTOWN_RADIUS);
				float tmpSize = GameVars.TANKER_RADIUS_BASE + spawn.size * GameVars.TANKER_RADIUS_BY_SIZE;
				Wreck wreck = new Wreck (tmpCos, tmpSin, spawn.size, tmpSize);
				wreck.player = player;

				wrecks.Add (wreck);

				return;
			}

			Tanker tanker = Tanker.InitTanker (spawn.size, player);

			float distance = GameVars.TANKER_SPAWN_RADIUS + tanker.radius;

			bool safe = false;
			while (!safe)
			{
				float tmpCosDist = cos * distance;
				float tmpSinDist = sin * distance;
				tanker.move ((int)tmpCosDist, (int)tmpSinDist);

				safe = true;//units.stream ().allMatch (u->tanker.distance (u) > tanker.radius + u.radius);
				for (int i = 0; i < units.Count; i++)
				{
					if (tanker.Distance (units[i]) > tanker.radius + units[i].radius)
					{
						safe = false;
						break;
					}
				}
				distance += GameVars.TANKER_MIN_RADIUS;
			}

			tanker.thrust (GameVars.WATERTOWN, GameVars.TANKER_START_THRUST);

			units.Add (tanker);
			tankers.Add (tanker);
		}

		private static void FillTankers (List<Unit> unitsToRemove, List<Tanker> tankerToRemove, Tanker tanker)
		{

			float distance = tanker.Distance (GameVars.WATERTOWN);
			bool full = tanker.isFull ();

			if (distance <= GameVars.WATERTOWN_RADIUS && !full)
			{
				// A non full tanker in watertown collect some water
				tanker.water += 1;
				tanker.mass += GameVars.TANKER_MASS_BY_WATER;
			} else if (distance >= GameVars.TANKER_SPAWN_RADIUS + tanker.radius && full)
			{
				// Remove too far away and not full tankers from the game
				unitsToRemove.Remove (tanker);
				tankerToRemove.Remove (tanker);
			}
		}

		// Play a collision
		void playCollision (Collision collision)
		{
			if (collision.b == null)
			{
				collision.a.bounce ();
			} else
			{
				Tanker dead = collision.dead ();
				if (dead != null)
				{
					tankers.Remove (dead);
					units.Remove (dead);

					Wreck wreck = dead.die ();

					// If a tanker is too far away, there's no wreck
					if (wreck != null)
					{
						wrecks.Add (wreck);
					}
				} else
				{
					collision.a.bounce (collision.b);
				}
			}
		}

		private Collision getNextCollision (Unit unit)
		{
			//////Debug.ProfilerStart ("getNextCollision");
			Collision result = GameVars.NULL_COLLISION;
			// Test collision with map border first

			Collision collision = unit.getCollision ();
			//////Debug.ProfilerEnd ();

			if (collision.t < result.t)
			{
				result = collision;
			}
			//////Debug.ProfilerStart ("loop");
			bool beginCheck = false;
			for (int j = 0; j < units.Count; ++j)
			{
				if (beginCheck && units[j] != unit)
				{
					collision = unit.getCollision (units[j]);

					if (collision.t < result.t)
					{
						result = collision;
					}
				} else
				{
					beginCheck = true;
				}
			}
			//////Debug.ProfilerEnd ();
			return result;
		}

		void SimpleSim (Unit u)
		{
			switch (u.type)
			{
				case 0:
					SimpleReaperSim (u as Reaper);
					break;
				case 1:
					SimpleDestroyerSim (u as Destroyer);
					break;
				case 2:
					SimpleDOOFSim (u as Doof);
					break;
				case 3:
					SimpleTankerSim (u as Tanker);
					break;
				default:
					break;
			}
		}

		void SimulateUnit (Unit u, int direction)
		{
			switch (u.type)
			{
				case 0:
					MultipleSimUnit (u, direction);
					break;
				case 1:
					//DestroyerSim (u as Destroyer,direction);
					SimpleDestroyerSim (u as Destroyer);
					break;
				case 2:
					//MultipleSimUnit (u, direction);
					SimpleDOOFSim (u as Doof);
					break;
				case 3:
					SimpleTankerSim (u as Tanker);
					break;
				default:
					break;
			}
		}

		private void MultipleSimUnit (Unit tmpUnit, int direction)
		{
			if (directionAngles[direction] == -1)
			{
				tmpUnit.SetUpWantedVars (tmpUnit, 0);
			} else
			{
				tmpUnit.SetUpWantedVars (tmpUnit + (GetDirection (direction) * 1000), GameVars.MAX_THRUST);
			}
		}


		void SimpleReaperSim (Reaper reaper)
		{
			float minDist = float.MaxValue;
			Point closest = reaper;

			for (int i = 0; i < wrecks.Count; i++)
			{
				float tmpDist = (float)wrecks[i].SqrtDistance (reaper);
				if (tmpDist < minDist)
				{
					minDist = tmpDist;
					closest = wrecks[i];
				}
			}

			reaper.SetUpWantedVars (closest, GameVars.MAX_THRUST);
		}

		void SimpleDestroyerSim (Destroyer destroyer)
		{
			float minDist = float.MaxValue;
			Point closest = destroyer;

			for (int i = 0; i < tankers.Count; i++)
			{
				float tmpDist = (float)tankers[i].SqrtDistance (destroyer);
				if (tmpDist < minDist)
				{
					minDist = tmpDist;
					closest = tankers[i];
				}
			}

			destroyer.SetUpWantedVars (closest, GameVars.MAX_THRUST);
		}

		void SimpleTankerSim (Tanker tanker)
		{
			Point closest = null;

			if (tanker.Magnitude () > GameVars.WATERTOWN_RADIUS)
			{
				closest = tanker + tanker.GetDirection () * GameVars.TANKER_START_THRUST;
			} else
			{
				closest = tanker * 2;
			}
			tanker.SetUpWantedVars (closest, GameVars.TANKER_START_THRUST);
		}

		void SimpleDOOFSim (Doof doof)
		{
			float minDist = float.MaxValue;
			Unit closest = doof;

			for (int i = 0; i < doofTargets.Count; i++)
			{
				if (doofTargets[i].getPlayerIndex () != doof.getPlayerIndex ())
				{
					float tmpDist = (float)doofTargets[i].SqrtDistance (doof);
					if (tmpDist < minDist)
					{
						minDist = tmpDist;
						closest = doofTargets[i];
					}
				}
			}
			Point velocity = new Point (closest.vx, closest.vy);
			velocity *= Math.Min (minDist / GameVars.MAX_THRUST,1);
			doof.SetUpWantedVars (closest + velocity, GameVars.MAX_THRUST);
		}

		public float scoreCache = 0;
		public float Score ()
		{
			float score = 0;
			if (childSimulations.Count > 0)
			{
				score = float.MinValue;
				for (int i = 0; i < childSimulations.Count; i++)
				{
					float tmpS = childSimulations[i].Score ();
					score = Math.Max (tmpS, score);
				}
				//Debug.Log ("Score "+ score+" "+ (players[0].getReaper ().wantedThrustTarget != null ? players[0].getReaper().wantedThrustTarget.ToString() : "null"));
			} else
			{
				//////Debug.ProfilerStart ("GetIndividualScore");
				score = GetIndividualScore ();
				//////Debug.ProfilerEnd ();
				//score = GetScoreToDestroyer();

			}
			scoreCache = score;

			return score;
		}

		private float GetScoreToDestroyer ()
		{
			Unit reap = players[0].getReaper ();
			Unit dest = players[0].getDestroyer ();

			return -(float)reap.Distance (dest);
		}

		private float GetIndividualScore ()
		{
			float score = players[0].score * 24000;
			Unit tmpUnit = players[0].getReaper ();
			Point closest = tmpUnit;
			float minDist = float.MaxValue;
			for (int i = 0; i < wrecks.Count; i++)
			{
				float tmpDist = (float)tmpUnit.Distance (wrecks[i]);
				//Debug.Log (tmpDist+ "  to  "+wrecks[i]);
				if (minDist > tmpDist)
				{
					closest = wrecks[i];
					minDist = tmpDist;
				}
			}
			//Debug.Log ("wrecks.Count "+ wrecks.Count+" "+minDist);

			if (tmpUnit.Equals (closest))
			{
				score -= 12000;
			} else
			{
				score -= minDist;
			}

			closest = tmpUnit;
			minDist = float.MaxValue;
			for (int i = 0; i < destroyers.Count; i++)
			{
				float tmpDist = (float)tmpUnit.Distance (destroyers[i]);
				//Debug.Log (tmpDist+ "  to  "+wrecks[i]);
				if (minDist > tmpDist)
				{
					closest = destroyers[i];
					minDist = tmpDist;
				}
			}

			if (tmpUnit.Equals (closest))
			{
				score -= 6000;
			} else
			{
				score -= minDist / 2;
			}
			score += players[0].rage * 50;
			//tmpUnit = players[0].getDestroyer ();
			//score -= (float)tmpUnit.Distance (tmpUnit.wantedThrustTarget) / 2;
			//if (tmpUnit.wantedThrustTarget.Equals (tmpUnit))
			//{
			//	score -= 2000;
			//}
			//if (GameVars.GAME_VERSION >= 3)
			//{
			//	tmpUnit = players[0].getDoof ();
			//	score -= (float)tmpUnit.Distance (tmpUnit.wantedThrustTarget) / 2;
			//	if (tmpUnit.wantedThrustTarget.Equals (tmpUnit))
			//	{
			//		score -= 2000;
			//	}
			//}
			for (int i = 1; i < players.Count; i++)
			{
				score -= players[i].score * 6000;
				//tmpUnit = players[i].getReaper ();
				//score += (float)tmpUnit.Distance (tmpUnit.wantedThrustTarget);

				//tmpUnit = players[i].getDestroyer ();
				//score += (float)tmpUnit.Distance (tmpUnit.wantedThrustTarget);
				//if (GameVars.GAME_VERSION >= 3)
				//{
				//	tmpUnit = players[i].getDoof ();
				//	score += (float)tmpUnit.Distance (tmpUnit.wantedThrustTarget);
				//}
			}
			return score;
		}

		internal void PrintResult ()
		{
			Simulation bestSim = null;
			float maxScore = float.MinValue;
			for (int i = 0; i < childSimulations.Count; i++)
			{
				float tmpScore = childSimulations[i].Score ();
				//Debug.Log (tmpScore + "  tarPos " +( childSimulations[i].players[0].getReaper ().wantedThrustTarget )+"  pos "+( players[0].getReaper ()));

				if (tmpScore > maxScore)
				{
					bestSim = childSimulations[i];
					maxScore = tmpScore;
				}
			}
			if (bestSim != null)
			{

				Player player = bestSim.players[0];
				Console.WriteLine (player.getReaper ().wantedThrustTarget + " " + player.getReaper ().wantedThrustPower);
				Console.WriteLine (player.getDestroyer ().wantedThrustTarget + " " + player.getDestroyer ().wantedThrustPower);
				Console.WriteLine (player.getDoof ().wantedThrustTarget + " " + player.getDoof ().wantedThrustPower);
			} else
			{
				Console.WriteLine ("WAIT");
				Console.WriteLine ("WAIT");
				Console.WriteLine ("WAIT");
			}

		}
		static Queue<Simulation> sims = new Queue<Simulation> ();
		static Queue<Simulation> usedSims = new Queue<Simulation> ();

		internal static Simulation GetSimulation (List<Unit> units, List<Wreck> wrecks, HashSet<SkillEffect> skillEffects, int v, List<Player> players, Simulation parentSim)
		{
			sID++;
			Simulation retVal = null;
			if (sims.Count > 0)
			{
				retVal = sims.Dequeue ();
				retVal.InitSim (units, wrecks, skillEffects, v, players, parentSim);
			} else
			{
				retVal = new Simulation (units, wrecks, skillEffects, v, players, parentSim);
			}
			usedSims.Enqueue (retVal);

			return retVal;
		}

		public static void InitSimQueue (int count)
		{
			for (int i = 0; i < count; i++)
			{
				Simulation retVal = new Simulation ();
				sims.Enqueue (retVal);

			}
		}
		public static int sID = 0;
		public static void Clear ()
		{
			while (usedSims.Count > 0)
			{
				sims.Enqueue (usedSims.Dequeue ());
			}
			sID = 0;
			Reaper.Clear ();
			Destroyer.Clear ();
			Doof.Clear ();
			Tanker.Clear ();
			Collision.Clear ();
		}

		internal static void Init ()
		{
			InitDirections ();
			InitSimQueue (5000);
		}
	}

	class Point
	{
		public float x;
		public float y;

		public Point ()
		{

		}
		public Point (Point p)
		{
			x = p.x;
			y = p.y;
		}

		public Point (int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public Point (float x, float y)
		{
			this.x = x;
			this.y = y;
		}

		public float Distance (Point p)
		{
			if (p == null)
			{
				return 0;
			}
			return (float)Math.Sqrt ((this.x - p.x) * (this.x - p.x) + (this.y - p.y) * (this.y - p.y));
		}

		public float SqrtDistance (Point p)
		{
			if (p == null)
			{
				return 0;
			}
			return (this.x - p.x) * (this.x - p.x) + (this.y - p.y) * (this.y - p.y);
		}

		// Move the point to x and y
		public void move (int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		// Move the point to x and y
		public void move (float x, float y)
		{
			this.x = x;
			this.y = y;
		}

		// Move the point to an other point for a given distance
		public void moveTo (Point p, float distance)
		{
			float d = Distance (p);

			if (d < GameVars.EPSILON)
			{
				return;
			}

			float dx = p.x - x;
			float dy = p.y - y;
			float coef = distance / d;

			this.x += (int)Math.Round (dx * coef);
			this.y += (int)Math.Round (dy * coef);
		}

		public bool isInRange (Point p, float range)
		{
			return p != this && Distance (p) <= range;
		}

		public void Normalize ()
		{
			float sum = x + y;
			x /= sum;
			y /= sum;
		}

		public Point normalized
		{
			get
			{
				float sum = Math.Abs (x) + Math.Abs (y);
				float tmpx = x / sum;
				float tmpy = y / sum;
				return new Point (tmpx, tmpy);
			}
		}

		public float Magnitude ()
		{
			return (float)Math.Sqrt ((this.x) * (this.x) + (this.y) * (this.y));
		}

		public override string ToString ()
		{
			return (int)x + " " + (int)y;
		}
		public string DebugString ()
		{
			return "x = " + x + " ; y = " + y;
		}

		#region Overriden Operators

		public static Point operator * (Point first, Point second)
		{
			return new Point (first.x * second.x, first.y * second.y);
		}

		public static Point operator / (Point first, Point second)
		{
			return new Point (first.x / second.x, first.y / second.y);
		}

		public static Point operator + (Point first, Point second)
		{
			return new Point (first.x + second.x, first.y + second.y);
		}

		public static Point operator - (Point first, Point second)
		{
			return new Point (first.x - second.x, first.y - second.y);
		}

		public static Point operator * (Point first, int second)
		{
			return new Point (first.x * second, first.y * second);
		}

		public static Point operator / (Point first, int second)
		{
			return new Point (first.x / second, first.y / second);
		}

		public static Point operator + (Point first, int second)
		{
			return new Point (first.x + second, first.y + second);
		}

		public static Point operator - (Point first, int second)
		{
			return new Point (first.x - second, first.y - second);
		}


		public static Point operator * (Point first, float second)
		{
			return new Point (first.x * second, first.y * second);
		}

		public static Point operator / (Point first, float second)
		{
			return new Point (first.x / second, first.y / second);
		}

		public static Point operator + (Point first, float second)
		{
			return new Point (first.x + second, first.y + second);
		}

		public static Point operator - (Point first, float second)
		{
			return new Point (first.x - second, first.y - second);
		}

		public bool Equals (Point second)
		{

			return (second != null && x == second.x && y == second.y);
		}

		#endregion

	}

	class Wreck : Point
	{
		public int id;
		public float radius;
		public int water;
		public bool known;
		public Player player;

		public bool alive = true;

		public Wreck (int x, int y, int water, float radius) : base (x, y)
		{
			this.radius = radius;
			this.water = water;
		}

		public Wreck (float x, float y, int water, float radius) : base (x, y)
		{
			this.radius = radius;
			this.water = water;
		}
		public Wreck (Wreck wreck) : this (wreck.x, wreck.y, wreck.water, wreck.radius)
		{
			known = wreck.known;
			player = wreck.player;
		}

		// Reaper harvesting
		public bool harvest (List<Player> players, HashSet<SkillEffect> skillEffects)
		{
			foreach (Player p in players)
			{
				if (isInRange (p.getReaper (), radius) && !p.getReaper ().isInDoofSkill (skillEffects))
				{
					//Debug.Log ("GainPoint "+p.index);
					p.score += 1;
					water -= 1;
				}

			}
			alive = water > 0;
			return alive;
		}
	}

	class Unit : Point
	{
		public byte type;
		public int id;
		public float vx;
		public float vy;
		public float radius;
		public float mass;
		public float friction;
		public bool known;
		public byte playerIndex;

		public Point wantedThrustTarget;
		public short wantedThrustPower;

		public Unit ()
		{
		}

		public Unit (byte type, int x, int y) : base (x, y)
		{
			SetUp (type);
		}

		public void SetUp (byte type)
		{
			this.type = type;
			known = false;
		}

		public Unit (byte type, float x, float y) : base (x, y)
		{
			SetUp (type);
		}

		public Unit (Unit tmpUnit)
		{
			SetUp (tmpUnit);
		}

		public void SetUp (Unit tmpUnit)
		{
			SetUp (tmpUnit.type);
			move (tmpUnit.x, tmpUnit.y);
			id = tmpUnit.id;
			vx = tmpUnit.vx;
			vy = tmpUnit.vy;
			radius = tmpUnit.radius;
			mass = tmpUnit.mass;
			friction = tmpUnit.friction;
			known = tmpUnit.known;
		}

		public void move (float t)
		{
			x += (int)(vx * t);
			y += (int)(vy * t);
		}

		public float Speed ()
		{
			return (float)Math.Sqrt (vx * vx + vy * vy);
		}
		public float SQRTSpeed ()
		{
			return vx * vx + vy * vy;
		}

		public void SetUpWantedVars (Point p, short power)
		{
			wantedThrustPower = power;
			wantedThrustTarget = p;
		}

		public void thrust (Point p, short power)
		{
			float distance = Distance (p);

			// Avoid a division by zero
			if (Math.Abs (distance) <= GameVars.EPSILON)
			{
				return;
			}

			wantedThrustPower = power;
			wantedThrustTarget = p;

			float coef = (((float)power) / mass) / distance;
			vx += (p.x - this.x) * coef;
			vy += (p.y - this.y) * coef;
		}

		public bool isInDoofSkill (HashSet<SkillEffect> skillEffects)
		{
			HashSet<SkillEffect>.Enumerator tmpEnum = skillEffects.GetEnumerator ();

			while (tmpEnum.MoveNext ())
			{
				if (isInRange (tmpEnum.Current, tmpEnum.Current.radius + radius))
				{
					return true;
				}
			}

			return false;
		}

		public void adjust (HashSet<SkillEffect> skillEffects)
		{
			if (!isInDoofSkill (skillEffects))
			{
				vx = vx * (1 - friction);
				vy = vy * (1 - friction);
			}
		}

		public int TurnsToStop ()
		{
			float speed = Speed ();
			int counter = 0;
			while (speed > GameVars.EPSILON)
			{
				counter++;
				speed = GetSpeedNextTurn (speed);
			}
			return counter;
		}

		public float DistToStop ()
		{
			float speed = Speed ();
			float dist = 0;
			while (speed > GameVars.EPSILON)
			{
				speed = GetSpeedNextTurn (speed);
				dist += speed;

			}
			return dist;
		}


		public float GetSpeedNextTurn (float speed)
		{
			return Math.Max (speed * (1 - GetFriction ()), 0);
		}

		public Point GetStopPosition ()
		{
			float distToStop = (float)DistToStop ();
			Point point = (GetDirection () * distToStop);
			point.x += x;
			point.y += y;
			return point;
		}

		public float GetFriction ()
		{
			switch (type)
			{
				case 0:
					return GameVars.REAPER_FRICTION;
				case 4:
					return GameVars.TANKER_FRICTION;
				default:
					break;
			}
			return 0;
		}

		// Search the next collision with the map border
		public Collision getCollision ()
		{
			// Check instant collision
			float distToEdge = SqrtDistance (GameVars.WATERTOWN) + (radius * radius);
			if (distToEdge >= GameVars.SQRT_MAP_RADIUS)
			{
				return Collision.InitCollision (0, this);
			}

			//float a = vx * vx + vy * vy;
			Point nextPosition = GetWantedPosition ();

			if (nextPosition.SqrtDistance (GameVars.WATERTOWN) > GameVars.SQRT_MAP_RADIUS)
			{
				return Collision.InitCollision (0, this);
			}

			//// We are not moving, we can't reach the map border
			//if (vx == 0&& vy == 0)
			//{
			//	return GameVars.NULL_COLLISION;
			//}

			//// Search collision with map border
			//// Resolving: sqrt((x + t*vx)^2 + (y + t*vy)^2) = MAP_RADIUS - radius <=> t^2*(vx^2 + vy^2) + t*2*(x*vx + y*vy) + x^2 + y^2 - (MAP_RADIUS - radius)^2 = 0
			//// at^2 + bt + c = 0;
			//// a = vx^2 + vy^2
			//// b = 2*(x*vx + y*vy)
			//// c = x^2 + y^2 - (MAP_RADIUS - radius)^2


			//float b = 2 * (x * vx + y * vy);
			//float c = x * x + y * y - (GameVars.MAP_RADIUS - radius) * (GameVars.MAP_RADIUS - radius);
			//float delta = b * b - 4 * a * c;

			//if (delta <= 0)
			//{
			//	return GameVars.NULL_COLLISION;
			//}

			//float t = (-b + (float)Math.Sqrt (delta)) / (2 * a);

			//if (t <= 0)
			//{

			//	return GameVars.NULL_COLLISION;
			//}

			return GameVars.NULL_COLLISION;
		}

		private Point GetWantedPosition ()
		{
			Point dir = GetVelocity ();
			Point wantedDir = GetWantedDirection () * GameVars.MAX_THRUST;
			Point nextPosition = this + wantedDir + dir;
			return nextPosition;
		}

		// Search the next collision with an other unit
		public Collision getCollision (Unit u)
		{
			if (this is Tanker)
			{
				return GameVars.NULL_COLLISION;
			}
			// Check instant collision
			float tmpR = radius + u.radius;
			if (Distance (u) <= (tmpR))
			{
				return Collision.InitCollision (0, this, u);
			}

			// Both units are motionless
			if (vx == 0 && vy == 0 && u.vx == 0 && u.vy == 0)
			{

				return GameVars.NULL_COLLISION;
			}

			// Change referencial
			// Unit u is not at point (0, 0) with a speed vector of (0, 0)
			float x2 = x - u.x;
			float y2 = y - u.y;
			float vx2 = vx - u.vx;
			float vy2 = vy - u.vy;

			// Resolving: sqrt((x + t*vx)^2 + (y + t*vy)^2) = radius <=> t^2*(vx^2 + vy^2) + t*2*(x*vx + y*vy) + x^2 + y^2 - radius^2 = 0
			// at^2 + bt + c = 0;
			// a = vx^2 + vy^2
			// b = 2*(x*vx + y*vy)
			// c = x^2 + y^2 - radius^2 

			float a = vx2 * vx2 + vy2 * vy2;

			if (a <= 0)
			{

				return GameVars.NULL_COLLISION;
			}

			float b = 2 * (x2 * vx2 + y2 * vy2);
			float c = x2 * x2 + y2 * y2 - tmpR * tmpR;
			float delta = b * b - 4 * a * c;

			if (delta < 0)
			{

				return GameVars.NULL_COLLISION;
			}

			float t = (-b - (float)Math.Sqrt (delta)) / (2 * a);

			if (t <= 0)
			{

				return GameVars.NULL_COLLISION;
			}
			return Collision.InitCollision (t, this, u);
		}

		// Bounce between 2 units
		public void bounce (Unit u)
		{
			float mcoeff = (mass + u.mass) / (mass * u.mass);
			float nx = x - u.x;
			float ny = y - u.y;
			float nxnysquare = nx * nx + ny * ny;
			float dvx = vx - u.vx;
			float dvy = vy - u.vy;
			float product = (nx * dvx + ny * dvy) / (nxnysquare * mcoeff);
			float fx = nx * product;
			float fy = ny * product;
			float m1c = 1 / mass;
			float m2c = 1 / u.mass;

			vx -= fx * m1c;
			vy -= fy * m1c;
			u.vx += fx * m2c;
			u.vy += fy * m2c;

			fx = fx * GameVars.IMPULSE_COEFF;
			fy = fy * GameVars.IMPULSE_COEFF;

			// Normalize vector at min or max impulse
			float impulse = (float)Math.Sqrt (fx * fx + fy * fy);
			float coeff = 1;
			if (impulse > GameVars.EPSILON && impulse < GameVars.MIN_IMPULSE)
			{
				coeff = GameVars.MIN_IMPULSE / impulse;
			}

			fx = fx * coeff;
			fy = fy * coeff;

			vx -= fx * m1c;
			vy -= fy * m1c;
			u.vx += fx * m2c;
			u.vy += fy * m2c;

			float diff = (Distance (u) - radius - u.radius) / 2;
			if (diff <= 0)
			{
				// Unit overlapping. Fix positions.
				moveTo (u, diff - GameVars.EPSILON);
				u.moveTo (this, diff - GameVars.EPSILON);
			}
		}

		// Bounce with the map border
		public void bounce ()
		{
			float mcoeff = 1 / mass;
			float nxnysquare = x * x + y * y;
			float product = (x * vx + y * vy) / (nxnysquare * mcoeff);
			float fx = x * product;
			float fy = y * product;

			vx -= fx * mcoeff;
			vy -= fy * mcoeff;

			fx = fx * GameVars.IMPULSE_COEFF;
			fy = fy * GameVars.IMPULSE_COEFF;

			// Normalize vector at min or max impulse
			float impulse = (float)Math.Sqrt (fx * fx + fy * fy);
			float coeff = 1;
			if (impulse > GameVars.EPSILON && impulse < GameVars.MIN_IMPULSE)
			{
				coeff = GameVars.MIN_IMPULSE / impulse;
			}

			fx = fx * coeff;
			fy = fy * coeff;
			vx -= fx * mcoeff;
			vy -= fy * mcoeff;

			float diff = Distance (GameVars.WATERTOWN) + radius - GameVars.MAP_RADIUS;
			if (diff >= 0)
			{
				// Unit still outside of the map, reposition it
				moveTo (GameVars.WATERTOWN, diff + GameVars.EPSILON);
			}
		}

		public Point GetDirection ()
		{
			Point dir = new Point ((float)vx, (float)vy);
			return dir.normalized;
		}

		public Point GetVelocity ()
		{
			return new Point (vx, vy);
		}

		public Point GetSQRVelocity ()
		{
			return new Point (vx * vx, vy * vy);
		}

		public Point GetWantedDirection ()
		{
			Point dir = wantedThrustTarget - this;
			return dir.normalized;
		}

		public virtual int getExtraInput ()
		{
			return -1;
		}

		public virtual int getExtraInput2 ()
		{
			return -1;
		}

		public int getPlayerIndex ()
		{
			return playerIndex;
		}
	}

	class Tanker : Unit
	{
		public int water;
		public int size;
		public Player player;
		public bool killed;

		public Tanker () : base (GameVars.TYPE_TANKER, 0, 0)
		{

		}

		public static Tanker InitTanker (int size, Player player, int x, int y)
		{
			Tanker t = GetTanker ();
			t.SetUp (GameVars.TYPE_TANKER);
			t.move (x, y);
			t.player = player;
			t.size = size;
			if (player != null)
			{
				t.playerIndex = player.index;
			}

			t.water = GameVars.TANKER_EMPTY_WATER;
			t.mass = GameVars.TANKER_EMPTY_MASS + GameVars.TANKER_MASS_BY_WATER * t.water;
			t.friction = GameVars.TANKER_FRICTION;
			t.radius = GameVars.TANKER_RADIUS_BASE + GameVars.TANKER_RADIUS_BY_SIZE * t.size;
			return t;
		}

		public static Tanker InitTanker (int size, Player player)
		{
			Tanker t = GetTanker ();
			t.SetUp (GameVars.TYPE_TANKER);
			t.move (0, 0);
			t.player = player;
			t.size = size;

			t.water = GameVars.TANKER_EMPTY_WATER;
			t.mass = GameVars.TANKER_EMPTY_MASS + GameVars.TANKER_MASS_BY_WATER * t.water;
			t.friction = GameVars.TANKER_FRICTION;
			t.radius = GameVars.TANKER_RADIUS_BASE + GameVars.TANKER_RADIUS_BY_SIZE * t.size;
			return t;
		}
		static Queue<Tanker> tankerPool = new Queue<Tanker> ();
		static Queue<Tanker> usedTankerPool = new Queue<Tanker> ();

		private static Tanker GetTanker ()
		{
			Tanker retVal = null;
			if (tankerPool.Count > 0)
			{
				retVal = tankerPool.Dequeue ();

			} else
			{
				retVal = new Tanker ();

			}
			usedTankerPool.Enqueue (retVal);

			return retVal;
		}

		public static void Clear ()
		{
			while (usedTankerPool.Count > 0)
			{
				tankerPool.Enqueue (usedTankerPool.Dequeue ());
			}
		}

		public static Tanker InitTanker (Tanker t, Player p)
		{
			Tanker tmpTanker = GetTanker ();
			tmpTanker.Setup (t, p);
			return tmpTanker;
		}

		private void Setup (Tanker t, Player p)
		{
			SetUp (t);
			vx = t.vx;
			vy = t.vy;

			water = t.water;
			size = t.size;
			player = p;
			killed = t.killed;
			radius = t.radius;
			mass = t.mass;
			friction = t.friction;
		}

		public Wreck die ()
		{
			// Don't spawn a wreck if our center is outside of the map
			if (Distance (GameVars.WATERTOWN) >= GameVars.MAP_RADIUS)
			{
				return null;
			}

			return new Wreck (x, y, water, radius);
		}

		public bool isFull ()
		{
			return water >= size;
		}

		public void play ()
		{
			if (isFull ())
			{
				// Try to leave the map
				thrust (GameVars.WATERTOWN, (short)-GameVars.TANKER_THRUST);
			} else if (Distance (GameVars.WATERTOWN) > GameVars.WATERTOWN_RADIUS)
			{
				// Try to reach watertown
				thrust (GameVars.WATERTOWN, GameVars.TANKER_THRUST);
			}
		}


		public override int getExtraInput ()
		{
			return water;
		}

		public override int getExtraInput2 ()
		{
			return size;
		}
	}

	class Looter : Unit
	{
		public byte skillCost;
		public float skillRange;
		public bool skillActive;

		public Player player;

		public string message;
		public Action attempt;
		public SkillResult skillResult;

		public Looter (byte type, Player player, int x, int y)
		{
			SetUp (type, player, x, y);
		}

		public void SetUp (byte type, Player player, int x, int y)
		{
			SetUp (type);
			move (x, y);
			this.player = player;
			playerIndex = player.index;
			radius = GameVars.LOOTER_RADIUS;
		}

		public Looter ()
		{

		}

		public Looter (byte type, Player player, float x, float y)
		{
			SetUp (type, player, (int)x, (int)y);
		}

		public Looter (Looter l, Player p)
		{
			SetUp (l, p);
		}

		private void SetUp (Looter l, Player p)
		{
			SetUp (l);
			radius = GameVars.LOOTER_RADIUS;
			skillCost = l.skillCost;
			skillRange = l.skillRange;
			skillActive = l.skillActive;

			player = p;
			wantedThrustTarget = l.wantedThrustTarget;
			wantedThrustPower = l.wantedThrustPower;

			message = l.message;
			attempt = l.attempt;
			skillResult = l.skillResult;
		}

		public SkillEffect skill (Point p)
		{
			if (player.rage < skillCost)
				throw new NoRageException ();
			if (Distance (p) > skillRange)
				throw new TooFarException ();

			player.rage -= skillCost;
			return skillImpl (p);
		}

		public SkillEffect skillImpl (Point p)
		{
			throw new NotImplementedException ();
		}

		public int getPlayerIndex ()
		{
			return player.index;
		}

	}

	[Serializable]
	internal class TooFarException : Exception
	{
		public TooFarException ()
		{
		}

		public TooFarException (string message) : base (message)
		{
		}

		public TooFarException (string message, Exception innerException) : base (message, innerException)
		{
		}

		protected TooFarException (SerializationInfo info, StreamingContext context) : base (info, context)
		{
		}
	}

	[Serializable]
	internal class NoRageException : Exception
	{
		public NoRageException ()
		{
		}

		public NoRageException (string message) : base (message)
		{
		}

		public NoRageException (string message, Exception innerException) : base (message, innerException)
		{
		}

		protected NoRageException (SerializationInfo info, StreamingContext context) : base (info, context)
		{
		}
	}

	class Reaper : Looter
	{
		public Reaper ()
		{

		}
		public static Reaper InitReaper (Player player, int x, int y)
		{
			Reaper tmpR = GetReaper ();
			tmpR.SetUpReaper (player, x, y);
			return tmpR;
		}

		public void SetUpReaper (Player player, int x, int y)
		{
			SetUp (GameVars.LOOTER_REAPER, player, x, y);
			SetUpReaper (player);
		}

		private void SetUpReaper (Player player)
		{
			playerIndex = player.index;
			player.looters[GameVars.LOOTER_REAPER] = this;
			mass = GameVars.REAPER_MASS;
			friction = GameVars.REAPER_FRICTION;
			skillCost = GameVars.REAPER_SKILL_COST;
			skillRange = GameVars.REAPER_SKILL_RANGE;
			skillActive = GameVars.REAPER_SKILL_ACTIVE;
		}

		public static Reaper InitReaper (Player player, float x, float y)
		{
			Reaper tmpR = GetReaper ();

			tmpR.SetUpReaper (player, x, y);
			return tmpR;
		}

		private void SetUpReaper (Player player, float x, float y)
		{
			SetUp (GameVars.LOOTER_REAPER, player, (int)x, (int)y);
			SetUpReaper (player);
		}

		public static Reaper InitReaper (Reaper r, Player p)
		{
			Reaper tmpR = GetReaper ();

			tmpR.SetUpReaperVel (r, p);
			return tmpR;
		}

		private void SetUpReaperVel (Reaper r, Player p)
		{

			SetUpReaper (p, (int)r.x, (int)r.y);
			vx = r.vx;
			vy = r.vy;
		}

		static Queue<Reaper> reaperPool = new Queue<Reaper> ();
		static Queue<Reaper> usedReaperPool = new Queue<Reaper> ();

		private static Reaper GetReaper ()
		{
			Reaper retVal = null;
			if (reaperPool.Count > 0)
			{
				retVal = reaperPool.Dequeue ();
			} else
			{
				retVal = new Reaper ();

			}
			usedReaperPool.Enqueue (retVal);

			return retVal;
		}

		public static void Clear ()
		{
			while (usedReaperPool.Count > 0)
			{
				reaperPool.Enqueue (usedReaperPool.Dequeue ());
			}
		}

		SkillEffect skillImpl (Point p)
		{
			return new ReaperSkillEffect (GameVars.TYPE_REAPER_SKILL_EFFECT, p.x, p.y, GameVars.REAPER_SKILL_RADIUS, GameVars.REAPER_SKILL_DURATION, GameVars.REAPER_SKILL_ORDER, this);
		}
	}

	class Destroyer : Looter
	{

		public Destroyer () { }

		public static Destroyer InitDestroyer (Player player, int x, int y)
		{
			Destroyer tmpD = GetDestroyer ();
			tmpD.SetUpDest (player, x, y);
			return tmpD;
		}

		static Queue<Destroyer> destPool = new Queue<Destroyer> ();
		static Queue<Destroyer> usedDestPool = new Queue<Destroyer> ();

		private static Destroyer GetDestroyer ()
		{
			Destroyer retVal = null;
			if (destPool.Count > 0)
			{
				retVal = destPool.Dequeue ();
			} else
			{
				retVal = new Destroyer ();

			}
			usedDestPool.Enqueue (retVal);

			return retVal;
		}

		public static void Clear ()
		{
			while (usedDestPool.Count > 0)
			{
				destPool.Enqueue (usedDestPool.Dequeue ());
			}
		}

		private void SetUpDest (Player player, int x, int y)
		{
			SetUp (GameVars.LOOTER_DESTROYER, player, x, y);
			playerIndex = player.index;
			player.looters[GameVars.LOOTER_DESTROYER] = this;
			mass = GameVars.DESTROYER_MASS;
			friction = GameVars.DESTROYER_FRICTION;
			skillCost = GameVars.DESTROYER_SKILL_COST;
			skillRange = GameVars.DESTROYER_SKILL_RANGE;
			skillActive = GameVars.DESTROYER_SKILL_ACTIVE;
		}

		public static Destroyer InitDestroyer (Destroyer d, Player p)
		{
			Destroyer tmpD = InitDestroyer (p, (int)d.x, (int)d.y);
			tmpD.vx = d.vx;
			tmpD.vy = d.vy;

			return tmpD;
		}

		public SkillEffect skillImpl (Point p)
		{
			return new DestroyerSkillEffect (GameVars.TYPE_DESTROYER_SKILL_EFFECT, p.x, p.y, GameVars.DESTROYER_SKILL_RADIUS, GameVars.DESTROYER_SKILL_DURATION,
					GameVars.DESTROYER_SKILL_ORDER, this);
		}
	}

	class Doof : Looter
	{
		public Doof () { }

		public static Doof InitDoof (Player player, int x, int y)
		{
			Doof tmpd = GetDoof ();
			tmpd.SetUpDoof (player, x, y);
			return tmpd;
		}

		private void SetUpDoof (Player player, int x, int y)
		{
			SetUp (GameVars.LOOTER_DOOF, player, x, y);
			player.looters[GameVars.LOOTER_DOOF] = this;
			playerIndex = player.index;
			mass = GameVars.DOOF_MASS;
			friction = GameVars.DOOF_FRICTION;
			skillCost = GameVars.DOOF_SKILL_COST;
			skillRange = GameVars.DOOF_SKILL_RANGE;
			skillActive = GameVars.DOOF_SKILL_ACTIVE;
		}

		public static Doof InitDoof (Doof d, Player p)
		{
			return InitDoof (p, (int)d.x, (int)d.y);
		}

		static Queue<Doof> doofPool = new Queue<Doof> ();
		static Queue<Doof> usedDoofPool = new Queue<Doof> ();

		private static Doof GetDoof ()
		{
			Doof retVal = null;
			if (doofPool.Count > 0)
			{
				retVal = doofPool.Dequeue ();
			} else
			{
				retVal = new Doof ();

			}
			usedDoofPool.Enqueue (retVal);

			return retVal;
		}


		public static void Clear ()
		{
			while (usedDoofPool.Count > 0)
			{
				doofPool.Enqueue (usedDoofPool.Dequeue ());
			}
		}

		public SkillEffect skillImpl (Point p)
		{
			return new DoofSkillEffect (GameVars.TYPE_DOOF_SKILL_EFFECT, p.x, p.y, GameVars.DOOF_SKILL_RADIUS, GameVars.DOOF_SKILL_DURATION, GameVars.DOOF_SKILL_ORDER, this);
		}

		// With flame effects! Yeah!
		public int sing ()
		{
			return (int)Math.Floor (Speed () * GameVars.DOOF_RAGE_COEF);
		}
	}

	class Player
	{
		public byte score;
		public byte index;
		public byte rage;
		public Looter[] looters;
		public bool dead;
		public Queue<TankerSpawn> tankers;

		public Player (byte index)
		{
			this.index = index;

			looters = new Looter[GameVars.LOOTER_COUNT];
		}

		public Player (Player p) : this (p.index)
		{
			rage = p.rage;
			for (int i = 0; i < p.looters.Length; i++)
			{
				looters[i] = p.looters[i];
			}
			score = p.score;
			dead = p.dead;
		}

		public void kill ()
		{
			dead = true;
		}

		public Reaper getReaper ()
		{
			return (Reaper)looters[GameVars.LOOTER_REAPER];
		}

		public Destroyer getDestroyer ()
		{
			return (Destroyer)looters[GameVars.LOOTER_DESTROYER];
		}

		public Doof getDoof ()
		{
			return (Doof)looters[GameVars.LOOTER_DOOF];
		}
	}

	class TankerSpawn
	{
		public int size;
		public float angle;

		public TankerSpawn (int size, float angle)
		{
			this.size = size;
			this.angle = angle;
		}
	}

	class Collision
	{
		public float t;
		public Unit a;
		public Unit b;

		public Collision ()
		{

		}

		public Collision (float t)
		{
			this.t = t;
		}

		public static Collision InitCollision (float t)
		{
			Collision c = GetCollision ();
			c.SetUpColl (t, null, null);
			return c;
		}

		public static Collision InitCollision (float t, Unit a)
		{
			Collision c = GetCollision ();
			c.SetUpColl (t, a, null);
			return c;
		}

		public static Collision InitCollision (float t, Unit a, Unit b)
		{
			Collision c = GetCollision ();
			c.SetUpColl (t, a, b);
			return c;
		}

		private void SetUpColl (float t, Unit a, Unit b)
		{
			this.t = t;
			this.a = a;
			this.b = b;
		}

		public Tanker dead ()
		{

			if (a is Destroyer && b is Tanker)// && b.mass < GameVars.REAPER_SKILL_MASS_BONUS)
			{
				return (Tanker)b;
			}

			if (b is Destroyer && a is Tanker)// && a.mass < GameVars.REAPER_SKILL_MASS_BONUS)
			{
				return (Tanker)a;
			}

			return null;
		}

		public static Queue<Collision> freeCollisions = new Queue<Collision> (1000);
		public static Queue<Collision> usedCollisions = new Queue<Collision> (1000);

		public static Collision GetCollision ()
		{
			Collision colToRet = null;
			if (freeCollisions.Count > 0)
			{
				colToRet = freeCollisions.Dequeue ();
			} else
			{
				colToRet = new Collision ();
			}
			usedCollisions.Enqueue (colToRet);
			return colToRet;
		}
		public static void Clear () {
			while (usedCollisions.Count > 0) {
				freeCollisions.Enqueue (usedCollisions.Dequeue());
			}
		}
	}

	class SkillEffect : Point
	{
		public int id;
		public byte type;
		public float radius;
		public int duration;
		public int order;
		public bool known;
		public Looter looter;

		public SkillEffect (byte type, int x, int y, float radius, int duration, int order, Looter looter) : this (type, (float)x, (float)y, radius, duration, order, looter)
		{
		}

		public void Init (byte type, float radius, int duration, int order, Looter looter)
		{
			this.type = type;
			this.radius = radius;
			this.duration = duration;
			this.looter = looter;
			this.order = order;
		}

		public SkillEffect (byte type, float x, float y, float radius, int duration, int order, Looter looter) : base (x, y)
		{
			Init (type, radius, duration, order, looter);
		}

		public SkillEffect (SkillEffect s) : this (s.type, s.x, s.y, s.radius, s.duration, s.order, s.looter)
		{
		}

		public void apply (List<Unit> units)
		{
			duration -= 1;
			applyImpl (units.FindAll (u => isInRange (u, radius + u.radius)));
		}

		public virtual void applyImpl (List<Unit> units)
		{
			throw new NotImplementedException ();
		}
	}

	class ReaperSkillEffect : SkillEffect
	{

		public ReaperSkillEffect (byte type, int x, int y, float radius, int duration, int order, Reaper reaper) : base (type, x, y, radius, duration, order, reaper)
		{
		}

		public ReaperSkillEffect (byte type, float x, float y, float radius, int duration, int order, Reaper reaper) : base (type, x, y, radius, duration, order, reaper)
		{
		}

		public override void applyImpl (List<Unit> units)
		{
			// Increase mass
			units.ForEach (u => u.mass += GameVars.REAPER_SKILL_MASS_BONUS);
		}
	}

	class DestroyerSkillEffect : SkillEffect
	{

		public DestroyerSkillEffect (byte type, int x, int y, float radius, int duration, int order, Destroyer destroyer) : base (type, x, y, radius, duration, order, destroyer)
		{
		}

		public DestroyerSkillEffect (byte type, float x, float y, float radius, int duration, int order, Destroyer destroyer) : base (type, x, y, radius, duration, order, destroyer)
		{
		}

		public override void applyImpl (List<Unit> units)
		{
			// Push units
			for (int i = 0; i < units.Count; i++)
			{
				units[i].thrust (this, (short)-GameVars.DESTROYER_NITRO_GRENADE_POWER);
			}
		}
	}

	class DoofSkillEffect : SkillEffect
	{

		public DoofSkillEffect (byte type, int x, int y, float radius, int duration, int order, Doof doof) : base (type, x, y, radius, duration, order, doof)
		{
		}

		public DoofSkillEffect (byte type, float x, float y, float radius, int duration, int order, Doof doof) : base (type, x, y, radius, duration, order, doof)
		{
		}

		public override void applyImpl (List<Unit> units)
		{
			// Nothing to do now
		}
	}


	class SkillResult
	{
		public static int OK = 0;
		public static int NO_RAGE = 1;
		public static int TOO_FAR = 2;
		public Point target;
		public int code;

		public SkillResult (int x, int y)
		{
			target = new Point (x, y);
			code = OK;
		}

		public int getX ()
		{
			return (int)target.x;
		}

		public int getY ()
		{
			return (int)target.y;
		}
	}


	class GameVars
	{
		static public bool SPAWN_WRECK = false;
		static public int LOOTER_COUNT = 3;
		static public bool REAPER_SKILL_ACTIVE = true;
		static public bool DESTROYER_SKILL_ACTIVE = true;
		static public bool DOOF_SKILL_ACTIVE = true;
		static public int GAME_VERSION = 3;
		static public int MAX_SIMULATION_DEPTH = 6;

		public static void Init ()
		{
			switch (GAME_VERSION)
			{
				case 0:
					SPAWN_WRECK = true;
					LOOTER_COUNT = 1;
					REAPER_SKILL_ACTIVE = false;
					DESTROYER_SKILL_ACTIVE = false;
					DOOF_SKILL_ACTIVE = false;
					break;
				case 1:
					LOOTER_COUNT = 2;
					REAPER_SKILL_ACTIVE = false;
					DESTROYER_SKILL_ACTIVE = false;
					DOOF_SKILL_ACTIVE = false;
					break;
				case 2:
					LOOTER_COUNT = 3;
					REAPER_SKILL_ACTIVE = false;
					DOOF_SKILL_ACTIVE = false;
					break;
				default: break;
			}
		}

		static public float MAP_RADIUS = 6000;
		static public byte TANKERS_BY_PLAYER;
		static public byte TANKERS_BY_PLAYER_MIN = 1;
		static public byte TANKERS_BY_PLAYER_MAX = 3;

		static public float WATERTOWN_RADIUS = 3000;

		static public short TANKER_THRUST = 500;
		static public float TANKER_EMPTY_MASS = 2.5f;
		static public float TANKER_MASS_BY_WATER = 0.5f;
		static public float TANKER_FRICTION = 0.40f;
		static public float TANKER_RADIUS_BASE = 400.0f;
		static public float TANKER_RADIUS_BY_SIZE = 50.0f;
		static public int TANKER_EMPTY_WATER = 1;
		static public int TANKER_MIN_SIZE = 4;
		static public int TANKER_MAX_SIZE = 10;
		static public float TANKER_MIN_RADIUS = TANKER_RADIUS_BASE + TANKER_RADIUS_BY_SIZE * TANKER_MIN_SIZE;
		static public float TANKER_MAX_RADIUS = TANKER_RADIUS_BASE + TANKER_RADIUS_BY_SIZE * TANKER_MAX_SIZE;
		static public float TANKER_SPAWN_RADIUS = 8000;
		static public short TANKER_START_THRUST = 2000;

		static public short MAX_THRUST = 300;
		static public int MAX_RAGE = 300;
		static public byte WIN_SCORE = 50;

		static public float REAPER_MASS = 0.5f;
		static public float REAPER_FRICTION = 0.2f;
		static public byte REAPER_SKILL_DURATION = 3;
		static public byte REAPER_SKILL_COST = 30;
		static public byte REAPER_SKILL_ORDER = 0;
		static public float REAPER_SKILL_RANGE = 2000;
		static public float REAPER_SKILL_RADIUS = 1000;
		static public float REAPER_SKILL_MASS_BONUS = 10;

		static public float DESTROYER_MASS = 1.5f;
		static public float DESTROYER_FRICTION = 0.30f;
		static public int DESTROYER_SKILL_DURATION = 1;
		static public byte DESTROYER_SKILL_COST = 60;
		static public byte DESTROYER_SKILL_ORDER = 2;
		static public float DESTROYER_SKILL_RANGE = 2000;
		static public float DESTROYER_SKILL_RADIUS = 1000;
		static public short DESTROYER_NITRO_GRENADE_POWER = 1000;

		static public float DOOF_MASS = 1;
		static public float DOOF_FRICTION = 0.25f;
		static public float DOOF_RAGE_COEF = 1 / 100;
		static public byte DOOF_SKILL_DURATION = 3;
		static public byte DOOF_SKILL_COST = 30;
		static public byte DOOF_SKILL_ORDER = 1;
		static public float DOOF_SKILL_RANGE = 2000;
		static public float DOOF_SKILL_RADIUS = 1000;

		static public float LOOTER_RADIUS = 400;
		static public byte LOOTER_REAPER = 0;
		static public byte LOOTER_DESTROYER = 1;
		static public byte LOOTER_DOOF = 2;

		static public byte TYPE_TANKER = 3;
		static public byte TYPE_WRECK = 4;
		static public byte TYPE_REAPER_SKILL_EFFECT = 5;
		static public byte TYPE_DOOF_SKILL_EFFECT = 6;
		static public byte TYPE_DESTROYER_SKILL_EFFECT = 7;

		static public float EPSILON = 0.00001f;
		static public float MIN_IMPULSE = 30;
		static public float IMPULSE_COEFF = 0.5f;

		// Global first free id for all elements on the map 
		static public int GLOBAL_ID = 0;

		// Center of the map
		static public Point WATERTOWN = new Point (0, 0);

		// The null collision 
		static public Collision NULL_COLLISION = new Collision (1 + EPSILON);
		public static float DEG_TO_RAD = 0.0174532925f;
		internal static DateTime timeFrame;
		internal static float SQRT_MAP_RADIUS = 3600000000;
		internal static float MILLISECONDS_LIMIT = 40;
		public static bool initLoop = true;
	}
}