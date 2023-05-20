using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Grab Snaffles and try to throw them through the opponent's goal!
 * Move towards a Snaffle and use your team id to determine where you need to throw it.
 **/
//TODO: wykrywanie kolizji
//TODO: targetowanie tych najblizszych mojej bramki
//Poprawione rzycanie flipendo (dystans pilki od wizarda)
class Player
{
	static void Main (string[] args)
	{
		Game g = new Game ();
		g.Loop ();
	}



}

public class Game
{
	List<Entity> buldgers;
	List<Entity> snaffles;
	List<Entity> enemies;
	List<Entity> wizards;
	List<Entity> allEntities;
	List<int> deadEntities;


	public readonly Vector2 firstGoal = new Vector2 (0, 3750);
	public readonly Vector2 secondGoal = new Vector2 (16000, 3750);
	public const int goalWidth = 1500;

	public Vector2 myGoal;
	public Vector2 enemyGoal;

	int manaPoints = 0;

	public Game ()
	{
		buldgers = new List<Entity> ();
		snaffles = new List<Entity> ();
		enemies = new List<Entity> ();
		wizards = new List<Entity> ();
		allEntities = new List<Entity> ();
		deadEntities = new List<int> ();
	}


	public void Loop ()
	{
		int myTeamId = int.Parse (Console.ReadLine ()); // if 0 you need to score on the right of the map, if 1 you need to score on the left
		if (myTeamId == 0) {
			myGoal = firstGoal;
			enemyGoal = secondGoal;
		} else {
			myGoal = secondGoal;
			enemyGoal = firstGoal;
		}
		Entity[] closestSnaffles = new Entity[2];

		string tmpCommand;
		// game loop
		while (true) {
			LoopInput ();

			closestSnaffles = ChooseSnaffles ();

			for (int i = 0; i < 2; i++) {

				string command = "";
				if (wizards [i].hasSnaffle) {
					command = ThrowSnaffle (i);
					//Console.Error.WriteLine (i + " 0 " + command);

				} else {

					command = "MOVE " + (closestSnaffles [i].position + closestSnaffles [i].velocity - wizards [i].velocity).OutputString () + " " + 150;
					//Console.Error.WriteLine (i + " 1 " + command);

					tmpCommand = Accio (wizards [i]);
					if (tmpCommand != "") {
						command = tmpCommand;
					}

					//					tmpCommand = StopSnaffles ();
					//					if (tmpCommand != "") {
					//						command = tmpCommand;
					//					}

					//					tmpCommand = AvoidBuldgers (i);
					//					if (tmpCommand != "") {
					//						command = tmpCommand;
					//					}
					//Console.Error.WriteLine (i + " 2 " + command);


				}

				tmpCommand = CastFlippendo (i);
				if (tmpCommand != "") {
					command = tmpCommand;
				}
				//Console.Error.WriteLine (i + " 3 " + command);


				//Console.Error.WriteLine (i + " 4 " + command);

				//Console.Error.WriteLine (manaPoints);
				Console.WriteLine (command);
			}
			manaPoints++;

		}
	}

	float GetFlipendoStrength (Entity wizard, Entity target)
	{
		return Math.Min (6000 / ((float)Math.Pow (Vector2.Distance (wizard.position + wizard.velocity, target.position + target.velocity) / 1000, 2)), 1000);
	}

	//TODO: flippendo rzucane jest w następnej turze - dodać przewidywanie pozycji w następnej turze - bez tego mogę rzuca piłki nie trafiając do bramki (w następnej turze nie będzie już między mną a bramką)
	string CastFlippendo (int i)
	{
		if (manaPoints > 20) {

			for (int j = 0; j < snaffles.Count; j++) {
				float str = GetFlipendoStrength (wizards [i], snaffles [j]);

				if (!snaffles [j].willBePetrified && Vector2.Distance (snaffles [j], wizards [i]) > 600 && str > 300) {

					Vector2 dotVect = ((snaffles [j].position + snaffles [j].velocity) - (wizards [i].position + wizards [i].velocity)).normalized;
					Vector2 direction = new Vector2 (dotVect);

					float dist = Math.Abs (enemyGoal.x - snaffles [j].position.x);
					if ((dist > 1000) || Vector2.Distance (snaffles [j].position + snaffles [j].velocity, wizards [i].position + wizards [i].velocity) > 500) {

						if (Entity.IsInPlayableArea ((int)(snaffles [j].position + (snaffles [j].velocity * 3)).x)) {
							//Console.Error.WriteLine (i + " " + direction + " " + (enemyGoal - wizards [i].position).normalized + " " + (snaffles [i].position - wizards [i].position));

							direction /= direction.x;

							direction *= dist;


							Vector2 currVel = new Vector2 (snaffles [j].velocity);
							snaffles [j].velocity = direction.normalized * str;
							direction += snaffles [j].position;
							if (IsFacingEnemyGoal (snaffles [j]) && Vector2.Dot (dotVect.normalized, (enemyGoal - wizards [i].position).normalized) > 0.5f) {

								if (Math.Abs (direction.y) - enemyGoal.y < goalWidth) {

									for (int k = 0; k < allEntities.Count; k++) {
										if (allEntities [k].entityType != Entity.EntityType.WIZZARD && allEntities [k] != snaffles [j]) {
											if (Vector2.Distance (allEntities [k], wizards [i]) < 6000) {
												//snaffles [j].velocity = throwDirection;
												if (snaffles [j].WillCollide (allEntities [k])) {
													return "";
												}
											}
										}
									}

									//Console.Error.WriteLine (snaffles [j].index + "  pos in X " + (snaffles [j].position + (snaffles [j].velocity * 3)) + "  velo " + snaffles [j].velocity + "   faces " + IsFacingEnemyGoal (snaffles [j]) + "   str " + str);

									snaffles [j].velocity = currVel;
									manaPoints -= 20;
									snaffles [j].willBePetrified = true;


									return "FLIPENDO " + snaffles [j].index;
								}
							} else {

								//return BounceSnaffle (i, j, currVel, str);
							}
							snaffles [j].velocity = currVel;
						}
					} else {
						//Console.Error.WriteLine (i + " nono " + snaffles [j].SimplePositionInXTurns (3));
					}
				}
			}
		}


		return "";
	}

	string BounceSnaffle (int i, int j, Vector2 currVel, float str)
	{
		Vector2 potVel = (snaffles [j].position - wizards [i].position).normalized * str;
		snaffles [j].velocity = potVel + currVel;

		if (Vector2.Distance (snaffles [j].position + snaffles [j].velocity, wizards [i].velocity + wizards [i].position) > 500 && str > 500 && WillTargetGoalAfterBounce (snaffles [j], enemyGoal)) {
			Console.Error.WriteLine (wizards [i].index + " " + snaffles [j].index + " " + currVel + " " + snaffles [j].velocity);
			snaffles [j].velocity = currVel;

			manaPoints -= 20;
			snaffles [j].willBePetrified = true;

			return "FLIPENDO " + snaffles [j].index;
		}
		return "";
	}

	string ThrowSnaffle (int i)
	{
		float dist = float.MaxValue;
		float curDist = 0;
		Vector2 targetPosition = GetClosestGoalPosition (wizards [i]);
		Entity closest = null;

		for (int k = 0; k < snaffles.Count; k++) {
			curDist = Vector2.Distance (wizards [i], snaffles [k]);
			if (curDist < dist) {
				dist = curDist;
				closest = snaffles [k];
			}
		}

		float strength = 500;				
		Vector2 throwPosition = enemyGoal;
		//					if (Vector2.Distance (targetPosition, wizards [i].position) < 2000) {
		//						strength = 500;
		//					}
		Vector2 throwDirection = throwPosition - (wizards [i].position + wizards [i].velocity);
		Vector2 target = new Vector2 (enemyGoal);
		bool check = false;
		for (int j = 0; j < allEntities.Count; j++) {

			target.y = wizards [i].position.y + wizards [i].velocity.y;
			if (closest.WillCollide (allEntities [j]) && closest.WillCollideStatic (allEntities [j])) {
				check = true;
				break;
			}
		}
		if (check) {
			target = enemyGoal;
			for (int j = 0; j < allEntities.Count; j++) {
				if (allEntities [j].entityType != Entity.EntityType.WIZZARD && allEntities [j] != closest) {
					if (Vector2.Distance (allEntities [j], wizards [i]) < 5000) {
						closest.velocity = throwDirection;
						//Console.Error.WriteLine ("Check " + allEntities [j].index + " " + allEntities [j].entityType);
						if (closest.WillCollide (allEntities [j]) || closest.WillCollideStatic (allEntities [j])) {

							target = enemyGoal + new Vector2 (0, -goalWidth);

							throwDirection = target - closest.position;
							//Console.Error.WriteLine ("Collision " + wizards [i].index + " " + allEntities [j].index + "  target " + target);

							do {
								if (closest.WillCollide (allEntities [j]) || closest.WillCollideStatic (allEntities [j])) {
									//Console.Error.WriteLine ("Checking collision " + wizards [i].index + " " + allEntities [j].index);

									target.y += 500;
									throwDirection = (target - closest.position).normalized * 500 + wizards [i].velocity;
									closest.velocity = throwDirection;
									//Console.Error.WriteLine ("Coll with " + allEntities [j].index + " change To " + target + "   dir " + throwDirection);
								} else {
									target.y -= (wizards [i].velocity.y * 2);
									return "THROW " + target.OutputString () + " " + strength;
								}
							} while (target.y > enemyGoal.y + goalWidth);
							//Console.Error.WriteLine (wizards [i].index + " " + allEntities [j].index + "   target " + target + "   wid" + (enemyGoal.y - goalWidth));
						}
					} else {
						//Console.Error.WriteLine (wizards [i].index + " " + allEntities [j].index + "   dist " + Vector2.Distance (allEntities [j], wizards [i]));

					}
				}
			}
		}
		return "THROW " + target.OutputString () + " " + strength;
	}

	Entity[] ChooseSnaffles ()
	{
		Entity[] closestSnaffles = new Entity[2];
		List<Entity> targeted = new List<Entity> ();
		float dist = float.MaxValue;
		float curDist = 0;
		for (int i = 0; i < closestSnaffles.Length; i++) {
			dist = float.MaxValue;
			curDist = 0;
			for (int j = 0; j < snaffles.Count; j++) {
				if (Entity.IsInPlayableArea (snaffles [j].position + snaffles [j].velocity * 5) || snaffles.Count == 1) {

					curDist = Vector2.Distance (wizards [i].position + (wizards [i].velocity), snaffles [j].position + (snaffles [j].velocity)) - (Vector2.Dot (wizards [i].velocity, snaffles [j].velocity) * 50);
					if (curDist < dist) {
						dist = curDist;
						closestSnaffles [i] = snaffles [j];
					}
				} else {
					Console.Error.WriteLine (snaffles [j].index + " " + (snaffles [j].position + snaffles [j].velocity * 5));
				}
			}
			if (closestSnaffles [i] == null) {
				for (int j = 0; j < snaffles.Count; j++) {

					curDist = Vector2.Distance (wizards [i].position + (wizards [i].velocity), snaffles [j].position + (snaffles [j].velocity)) - (Vector2.Dot (wizards [i].velocity, snaffles [j].velocity) * 50);
					if (curDist < dist) {
						dist = curDist;
						closestSnaffles [i] = snaffles [j];
					}
				}
				Console.Error.WriteLine (closestSnaffles [i].index + " ");

			}
		}


		dist = float.MaxValue;
		curDist = 0;

		if (closestSnaffles [0] == closestSnaffles [1]) {
			targeted.Add (closestSnaffles [0]);

			if (Vector2.Distance (wizards [0], closestSnaffles [0]) < Vector2.Distance (wizards [1], closestSnaffles [1])) {

				for (int j = 0; j < snaffles.Count; j++) {
					if ((Entity.IsInPlayableArea (snaffles [j].position + snaffles [j].velocity * 5) && !targeted.Contains (snaffles [j])) || snaffles.Count == 1) {
						curDist = Vector2.Distance (wizards [1].position, snaffles [j].position + (snaffles [j].velocity));
						if (curDist < dist) {
							dist = curDist;
							closestSnaffles [1] = snaffles [j];
						}
					}
				}
			} else {

				for (int j = 0; j < snaffles.Count; j++) {
					if ((Entity.IsInPlayableArea (snaffles [j].position + snaffles [j].velocity * 5) && !targeted.Contains (snaffles [j])) || snaffles.Count == 1) {

						curDist = Vector2.Distance (wizards [0].position, snaffles [j].position + (snaffles [j].velocity));
						if (curDist < dist) {
							dist = curDist;
							closestSnaffles [0] = snaffles [j];
						}
					}
				}
			}
		}

		return closestSnaffles;
	}

	string AvoidBuldgers (int i)
	{
		Vector2 direction = null;
		if (manaPoints >= 5) {
			float distance = 0;
			for (int j = 0; j < buldgers.Count; j++) {
				distance = Vector2.Distance (buldgers [j].position, wizards [i].position);
				int turnsToCollision = wizards [i].TurnsUntilCollision (buldgers [j]);

				if (!buldgers [j].willBePetrified && /* distance < 6000 &&*/ Vector2.Dot ((wizards [i].position - buldgers [j].position).normalized, buldgers [j].velocity.normalized) > 0.2f &&
				    Vector2.Dot (wizards [i].velocity.normalized, buldgers [j].velocity.normalized) < 0f && (turnsToCollision < 6 && turnsToCollision > 0)) {
					//					if (distance < 4000 && manaPoints >= 5) {
					//						manaPoints -= 5;
					//						buldgers [j].willBePetrified = true;
					//						return "OBLIVIATE " + buldgers [j].index;
					//					} 
					//				else {
					direction = buldgers [j].velocity.GetPerpedicular ();
					direction.Normalize ();
					if (Vector2.Distance (wizards [i], wizards [(i + 1) % 2]) < 5000 && Vector2.Dot (wizards [(i + 1) % 2].position - wizards [i].position, direction) > 0.5f) {
						direction *= (-1);
					}
					//direction -= wizards [i].velocity.normalized;
					direction *= 500;

					return "MOVE " + (wizards [i].position + direction).OutputString () + " " + 150 + " AVOID";
					//				}
				}
			}
		}
		return "";
	}


	string StopSnaffles ()
	{
		if (manaPoints >= 10) {

			for (int j = 0; j < snaffles.Count; j++) {
				if (!snaffles [j].willBePetrified && Vector2.Dot (snaffles [j].velocity.normalized, (myGoal - snaffles [j].position).normalized) > 0.1f) {

					Vector2 nextTurnPos = snaffles [j].position + (snaffles [j].velocity * 3);
					if (!Entity.IsInPlayableArea ((int)nextTurnPos.x) && Entity.IsInPlayableArea ((int)(snaffles [j].position + snaffles [j].velocity * 2).x) && IsFacingGoal (snaffles [j], myGoal)) {
						//Console.Error.WriteLine (" next turn " + nextTurnPos + " " + snaffles [j].index + "  vel " + (snaffles [j].velocity));

						bool throwSna = true;
						for (int k = 0; k < enemies.Count; k++) {
							if (Vector2.Dot (enemies [k].velocity, snaffles [j].position - enemies [k].position) > 0.5f && Vector2.Distance (enemies [k], snaffles [j]) < (enemies [k].velocity * 2).GetLength ()) {
								throwSna = false;
							}
						}
						if (throwSna) {

							manaPoints -= 10;
							snaffles [j].willBePetrified = true;
							return "PETRIFICUS " + snaffles [j].index;
						}
					} else {
						//Console.Error.WriteLine (" next turn " + nextTurnPos + " " + snaffles [j].index + "  vel " + (snaffles [j].velocity));
					}

					//					float dist = Math.Abs (snaffles [j].position.x - myGoal.x);
					//					Console.Error.WriteLine (snaffles [j].index + " " + IsFacingGoal (snaffles [j], myGoal) + "  dist - " + dist + "  vel - " + snaffles [j].velocity + "  dot - " + Vector2.Dot ((new Vector2 (myGoal.x, snaffles [j].position.y)).normalized, snaffles [j].velocity.normalized));
					//
					//					if (IsFacingGoal (snaffles [j], myGoal) && /*dist > Math.Abs (snaffles [j].velocity.x) && */Math.Abs (snaffles [j].velocity.x) > 400
					//					    && Math.Abs (myGoal.x - snaffles [j].position.x) < 4000 && Entity.IsInPlayableArea ((int)(snaffles [j].position + (snaffles [j].velocity * 2)).x)) {
					//						Console.Error.WriteLine (Math.Abs (myGoal.x - snaffles [j].position.x));
					//						manaPoints -= 10;
					//						snaffles [j].willBePetrified = true;
					//						return "PETRIFICUS " + snaffles [j].index;
					//					}

				}
			}
			//			for (int i = 0; i < enemies.Count; i++) {
			//				for (int j = 0; j < snaffles.Count; j++) {
			//					if (!snaffles [j].willBePetrified && Vector2.Distance (snaffles [j], enemies [i]) < 200 && Math.Abs (myGoal.x - snaffles [j].position.x) < 4000
			//					    && Math.Abs (myGoal.x - snaffles [j].position.x) > 700 && Entity.IsInPlayableArea ((int)(snaffles [j].position + (snaffles [j].velocity * 2)).x)
			//					    && snaffles [j].velocity.GetLength () < 500) {
			//						manaPoints -= 10;
			//						snaffles [j].willBePetrified = true;
			//						return "PETRIFICUS " + snaffles [j].index;
			//					}
			//				}
			//			}
		}
		return "";
	}

	bool IsFacingEnemyGoal (Entity ent)
	{
		Vector2 vel = ent.velocity.normalized;
		vel /= vel.x;
		float dist = enemyGoal.x - ent.position.x;
		vel *= dist;
		//Console.Error.WriteLine (ent.index + "  fir " + vel + " " + dist);
		vel += ent.position;

		//Console.Error.WriteLine (ent.index + "  se " + vel);


		return (Math.Abs (enemyGoal.y - vel.y) <= goalWidth);
	}

	bool IsFacingGoal (Entity ent, Vector2 goal)
	{
		Vector2 vel = ent.velocity.normalized;
		vel /= vel.x;
		float dist = goal.x - ent.position.x;
		vel *= dist;

		vel += ent.position;
		return (Math.Abs (goal.y - vel.y) <= goalWidth);
	}

	bool IsFacingGoal (Vector2  entPos, Vector2 entVelo, Vector2 goal)
	{
		float dist = goal.x - entPos.x;
		if (dist * entVelo.x > 0) {
			//dist = Math.Abs (dist);
			Vector2 vel = entVelo.normalized;
			//Console.Error.WriteLine ("Bef dir " + entVelo + " " + (goal.x - entPos.x));

			vel /= vel.x;
			vel *= dist;
			//Console.Error.WriteLine ("After dir " + vel + " " + dist);

			vel += entPos;
			//Console.Error.WriteLine ("plus pos " + vel);

			return (Math.Abs (goal.y - vel.y) <= goalWidth && Math.Sign (8000 - goal.x) == Math.Sign (entVelo.x));
		}
		return false;
	}

	Vector2 GetWallHitPoint (Entity tmpEnt)
	{
		float distToWall = 0;
		if (tmpEnt.velocity.y > 0) {
			distToWall = 7500 - tmpEnt.position.y - tmpEnt.radious;
		} else {
			distToWall = -tmpEnt.position.y + tmpEnt.radious;

		}

		Vector2 vel = tmpEnt.velocity.normalized;
		//Console.Error.WriteLine (tmpEnt.index + " distToWall " + distToWall + " " + vel + "  real vel " + tmpEnt.velocity + "   pos " + tmpEnt.position);
		vel /= vel.y;
		vel *= distToWall;
		//Console.Error.WriteLine (tmpEnt.index + " vel after prze " + (vel + tmpEnt.position) + " " + vel);

		return vel + tmpEnt.position;
	}

	string Accio (Entity wizard)
	{
		if (manaPoints >= 20) {
			//float dist = float.MaxValue;
			for (int k = 0; k < enemies.Count; k++) {
				for (int j = 0; j < snaffles.Count; j++) {
					if (Vector2.Distance (snaffles [j].position + snaffles [j].velocity, wizard.position + wizard.velocity) > 1500 && !snaffles [j].willBePetrified) {
						float str = AccioStrength (wizard, snaffles [j]);
						//Console.Error.WriteLine (wizard.index + " " + snaffles [j].index + " " + str);
						if (str >= 400 && snaffles [j].velocity.GetLength () < 600 && Vector2.Distance (myGoal, snaffles [j].position) < 8000 && Vector2.Dot (snaffles [j].velocity, wizard.position - snaffles [j].position) <= 0 && //((myGoal.x - 8000) * snaffles [j].velocity.x >= 0) &&
						    Vector2.Dot (snaffles [j].velocity, wizard.position - snaffles [j].position) <= 0 && Math.Abs (wizard.position.x - 8000) < Math.Abs (snaffles [j].position.x - 8000)) {
							manaPoints -= 20;
							snaffles [j].willBePetrified = true;
							return "ACCIO " + snaffles [j].index;
						} else {
							//							Console.Error.WriteLine (snaffles [j].index + "  str " + str + "  dist " + (Vector2.Distance (myGoal, snaffles [j].position)) + "    dir " + ((myGoal.x - 8000) * snaffles [j].velocity.x >= 0)
							//							+ "  dot " + Vector2.Dot (snaffles [j].velocity, wizard.position - snaffles [j].position) + "    distFromCen " + (Math.Abs (wizard.position.x - 8000) < Math.Abs (snaffles [j].position.x - 8000))); 
						}
					}
				}
			}
		}
		return "";
	}

	float AccioStrength (Entity wizard, Entity target)
	{
		return Math.Min (3000 / ((float)Math.Pow (Vector2.Distance (wizard.position, target.position) / 1000, 2)), 1000);

	}

	bool WillTargetGoalAfterBounce (Entity tmpEnt, Vector2 goal)
	{
		Vector2 bouncePoint = GetWallHitPoint (tmpEnt);
		Vector2 newDir = new Vector2 ();
		if (Entity.IsInPlayableArea ((int)bouncePoint.x)) {
			float newX = tmpEnt.position.x - bouncePoint.x;
			newDir.x = tmpEnt.position.x - (2 * newX);
			newDir.y = tmpEnt.position.y;
			if (Entity.IsInPlayableArea ((int)newDir.x)) {
				//Console.Error.WriteLine (tmpEnt.index + " boPo " + bouncePoint + "   newDir " + newDir + "  newX " + newX + "   tmpEnt.position " + tmpEnt.position);

				newDir = newDir - bouncePoint;
				//Console.Error.WriteLine (tmpEnt.index + "    NEW newDir " + newDir);

				return IsFacingGoal (bouncePoint, newDir, goal);
			}
		}
		return false;
	}

	Vector2 GetClosestGoalPosition (Entity entity)
	{
		int x = (int)enemyGoal.x;
		int y = (int)enemyGoal.y;
		if (entity.position.y > enemyGoal.y - goalWidth && entity.position.y < enemyGoal.y + goalWidth) {
			y = (int)entity.position.y;
		} else {
			if (entity.position.y <= enemyGoal.y - goalWidth) {
				y = (int)enemyGoal.y - goalWidth;

			}
			if (entity.position.y >= enemyGoal.y + goalWidth) {
				y = (int)enemyGoal.y + goalWidth;
			}
		}

		return new Vector2 (x, y);
	}

	public void LoopInput ()
	{
		wizards.Clear ();
		enemies.Clear ();
		snaffles.Clear ();
		buldgers.Clear ();
		allEntities.Clear ();


		int entities = int.Parse (Console.ReadLine ()); // number of entities still in game
		for (int i = 0; i < entities; i++) {
			string[] inputs = Console.ReadLine ().Split (' ');
			int entityId = int.Parse (inputs [0]); // entity identifier

			string entityType = inputs [1]; // "WIZARD", "OPPONENT_WIZARD" or "SNAFFLE" (or "BLUDGER" after first league)

			int x = int.Parse (inputs [2]); // position
			int y = int.Parse (inputs [3]); // position
			int vx = int.Parse (inputs [4]); // velocity
			int vy = int.Parse (inputs [5]); // velocity
			int state = int.Parse (inputs [6]); // 1 if the wizard is holding a Snaffle, 0 otherwise
			Entity tmpEnt = new Entity (entityId);
			allEntities.Add (tmpEnt);
			Entity.EntityType tmpType = GetEntityType (entityType);
			tmpEnt.entityType = tmpType;
			switch (tmpType) {
			case Entity.EntityType.WIZZARD:
				wizards.Add (tmpEnt);
				break;
			case Entity.EntityType.OPPONENT_WIZARD:
				enemies.Add (tmpEnt);
				break;
			case Entity.EntityType.SNAFFLE:
				snaffles.Add (tmpEnt);
				break;
			case Entity.EntityType.BLUDGER:
				buldgers.Add (tmpEnt);
				break;
			default:
				break;
			}
			tmpEnt.SetRadius ();
			tmpEnt.position = new Vector2 (x, y);
			tmpEnt.velocity = new Vector2 (vx, vy);
			if (tmpEnt.entityType == Entity.EntityType.SNAFFLE) {
				//Console.Error.WriteLine (tmpEnt.velocity + " " + tmpEnt.index);
			}
			tmpEnt.hasSnaffle = state == 1;
		}
	}

	void RemoveEntity (Entity tmpEnt)
	{
		deadEntities.Add (tmpEnt.index);

		switch (tmpEnt.entityType) {
		case Entity.EntityType.WIZZARD:
			wizards.Remove (tmpEnt);
			break;
		case Entity.EntityType.OPPONENT_WIZARD:
			enemies.Remove (tmpEnt);
			break;
		case Entity.EntityType.SNAFFLE:
			snaffles.Remove (tmpEnt);
			break;
		case Entity.EntityType.BLUDGER:
			buldgers.Remove (tmpEnt);
			break;
		default:
			break;
		}
	}

	public Entity.EntityType GetEntityType (string type)
	{
		switch (type) {
		case "WIZARD":
			return Entity.EntityType.WIZZARD;
		case "OPPONENT_WIZARD":
			return Entity.EntityType.OPPONENT_WIZARD;
		case "SNAFFLE":
			return Entity.EntityType.SNAFFLE;
		default:
			return Entity.EntityType.BLUDGER;
		}
	}
}

public class Entity
{
	public enum EntityType
	{
		WIZZARD,
		OPPONENT_WIZARD,
		SNAFFLE,
		BLUDGER}

	;


	public EntityType entityType;
	public int index;
	public bool hasSnaffle;
	public bool willBePetrified;
	public Vector2 position;
	public Vector2 velocity;
	public Entity target;

	public float radious;

	public Entity (int index)
	{
		this.index = index;
		willBePetrified = false;
	}


	public void SetRadius ()
	{
		switch (entityType) { 
		case EntityType.BLUDGER:
			radious = 200;
			break;
		case EntityType.SNAFFLE:
			radious = 150;
			break;
		case EntityType.WIZZARD:
			radious = 400;
			break;
		case EntityType.OPPONENT_WIZARD:
			radious = 400;
			break;
		default:
			break;
		}
	}

	public static bool IsInPlayableArea (int tmpX)
	{
		return (tmpX > 1 && tmpX < 15999);
	}

	public static bool IsInPlayableArea (float tmpX)
	{
		return IsInPlayableArea ((int)tmpX);
	}

	public static bool IsInPlayableArea (Vector2  tmpX)
	{
		return IsInPlayableArea ((int)tmpX.x);
	}

	public bool IsInPlayableArea ()
	{
		return (position.x > 1 && position.x < 15999);
	}

	public bool IsOnCollisionCourse (Entity other)
	{
		Vector2 direction = other.position - position;
		if (Vector2.Dot (direction, velocity) > 0.85f) {
			return true;
		}
		return false;
	}

	public Vector2 SimplePositionInXTurns (int turns)
	{
		Vector2 retVal = new Vector2 (position);
		Vector2 tmpVelocity = new Vector2 ();
		tmpVelocity.x = velocity.x;
		tmpVelocity.y = velocity.y;
		for (int i = 1; i < turns; i++) {
			retVal += tmpVelocity;


			switch (entityType) {
			case EntityType.WIZZARD:
				tmpVelocity += tmpVelocity.normalized * 112.5f;
				break;
			case EntityType.SNAFFLE:
				tmpVelocity *= 0.75f;
				break;
			case EntityType.BLUDGER:
				tmpVelocity += tmpVelocity.normalized * 135f;
				break;
			case EntityType.OPPONENT_WIZARD:
				tmpVelocity += tmpVelocity.normalized * 112.5f;
				break;
			default:
				break;
			}

		}
		return retVal;
	}

	public Vector2 SimpleSnafflePositionUntilStop ()
	{
		Vector2 retVal = new Vector2 (position);
		Vector2 tmpVelocity = new Vector2 ();
		tmpVelocity.x = velocity.x;
		tmpVelocity.y = velocity.y;

		do {
			retVal += tmpVelocity;
			tmpVelocity *= 0.75f;

		} while(tmpVelocity.GetLength () > 0.25f);
		return retVal;
	}

	public int TurnsUntilCollision (Entity other, int maxTurns = 10)
	{
		Vector2 myPos = null;
		Vector2 otherPos = null;
		for (int i = 0; i < maxTurns; i++) {
			myPos = SimplePositionInXTurns (i);
			otherPos = other.SimplePositionInXTurns (i);
			if (Vector2.Distance (myPos, otherPos) < (radious + other.radious) + 400) {
				return i;
			}
		}
		return -1;
	}

	float collisionCheckStep = 25;
	int radiousIncreseOverLoop = 50;

	public bool WillCollide (Entity other)
	{
		Vector2 futurePosition = new Vector2 (position);
		Vector2 vel = velocity.normalized * collisionCheckStep;
		int loops = 0;

		float prevDist = float.MaxValue;
		float dist = 0;
		while (Entity.IsInPlayableArea ((int)futurePosition.x) && futurePosition.y > 0 && futurePosition.y < 7500) {
			dist = Vector2.Distance (futurePosition, other.position);
			if (dist > prevDist) {
				//Console.Error.WriteLine (other.index + " nocollision " + futurePosition);
				return false;
			}
			prevDist = dist;
			if ((radious + other.radious) + (radiousIncreseOverLoop * loops) >= Vector2.Distance (futurePosition, other.position + (other.velocity * loops))) {

				return true;
			}

			loops++;
			futurePosition += vel;
		}

		return false;
	}

	public bool WillCollideStatic (Entity other)
	{
		Vector2 futurePosition = new Vector2 (position);
		Vector2 vel = velocity.normalized * collisionCheckStep;
		int loops = 0;
		float prevDist = float.MaxValue;
		float dist = 0;
		while (Entity.IsInPlayableArea ((int)futurePosition.x) && futurePosition.y > 0 && futurePosition.y < 7500) {
			dist = Vector2.Distance (futurePosition, other.position);
			if (dist > prevDist) {
				return false;
			}
			prevDist = dist;

			if ((radious + other.radious) + (radiousIncreseOverLoop * loops) > Vector2.Distance (futurePosition, other.position)) {
				return true;
			}
			loops++;
			futurePosition += vel;
		}

		return false;
	}
}

public class Vector2
{
	public float x;
	public float y;
	public const double radToDegrees = 180 / Math.PI;
	public const double degreesToRad = Math.PI / 180;

	public void Normalize ()
	{
		float sum = GetLength ();
		if (sum != 0) {
			x /= sum;
			y /= sum;
		}
	}

	public Vector2 normalized {
		get {
			Vector2 ret = new Vector2 (x, y);
			ret.Normalize ();
			return ret;
		}
	}

	public Vector2 ()
	{
		x = 0;
		y = 0;
	}

	public Vector2 (float x, float y)
	{
		this.x = x;
		this.y = y;
	}

	public Vector2 (int x, int y)
	{
		this.x = (float)x;
		this.y = (float)y;
	}

	public Vector2 (Vector2 vector)
	{
		this.x = vector.x;
		this.y = vector.y;
	}

	public static Vector2 operator + (Vector2 v1, Vector2 v2)
	{
		return new Vector2 (v1.x + v2.x, v1.y + v2.y);
	}

	public static Vector2 operator - (Vector2 v1, Vector2 v2)
	{
		return new Vector2 (v1.x - v2.x, v1.y - v2.y);
	}

	public static Vector2 operator * (Vector2 v1, float variable)
	{
		return new Vector2 (v1.x * variable, v1.y * variable);
	}

	public static Vector2 operator / (Vector2 v1, float variable)
	{
		if (variable == 0)
			return new Vector2 ();
		return new Vector2 (v1.x / variable, v1.y / variable);
	}

	public static bool operator == (Vector2 v1, Vector2 v2)
	{
		return (v1.x == v2.x && v1.y == v2.y);
	}

	public static bool operator != (Vector2 v1, Vector2 v2)
	{
		return (v1.x != v2.x || v1.y != v2.y);
	}

	override public string ToString ()
	{
		return "x = " + x + " , y = " + y;

	}

	public string OutputString ()
	{
		return Convert.ToInt32 (Math.Min (16000, Math.Max (0, x))).ToString () + " " + Convert.ToInt32 (Math.Min (7500, Math.Max (0, y))).ToString ();

	}

	public float GetLength ()
	{
		return (float)Math.Sqrt (Math.Pow (x, 2) + Math.Pow (y, 2));
	}

	public static float Distance (Vector2 first, Vector2 target)
	{
		Vector2 connectVectors = first - target;
		return connectVectors.GetLength ();
	}

	public static float Distance (Entity first, Entity target)
	{
		Vector2 connectVectors = first.position - target.position;
		return connectVectors.GetLength ();
	}

	public override bool Equals (object obj)
	{
		return ReferenceEquals (this, obj);
	}

	public override int GetHashCode ()
	{
		return base.GetHashCode ();
	}

	public static Vector2 GetLineComponents (Vector2 first, Vector2 second)
	{
		float a = (first.y - second.y) / (first.x - second.x);
		float b = first.y - a * first.x;
		return new Vector2 (a, b);
	}


	//Angle between point1,point2 and point3
	public static int Angle (Vector2 point1, Vector2 point2, Vector2 point3)
	{
		//double radToDegrees = 180 / Math.PI;

		float a = Vector2.Distance (point1, point2);
		float b = Vector2.Distance (point2, point3);
		float c = Vector2.Distance (point1, point3);
		double cosine = ((Math.Pow (a, 2) + Math.Pow (b, 2) - Math.Pow (c, 2)) / (2 * a * b));

		return (int)((Math.Acos (cosine) * radToDegrees) + 0.5f);
	}
	//Angle between point1,point2
	public static int WorldAngle (Vector2 point1, Vector2 point2)
	{
		//double radToDegrees = 180 / Math.PI;

		Vector2 point3 = new Vector2 (point1.x, point2.y);

		float a = Vector2.Distance (point1, point2);
		float b = Vector2.Distance (point2, point3);
		float c = Vector2.Distance (point1, point3);
		double cosine = ((Math.Pow (a, 2) + Math.Pow (b, 2) - Math.Pow (c, 2)) / (2 * a * b));

		return (int)((Math.Acos (cosine) * radToDegrees) + 0.5f);
	}

	public static float Dot (Vector2 vec1, Vector2 vec2)
	{
		Vector2 tmpV1 = vec1.normalized;
		Vector2 tmpV2 = vec2.normalized;
		return (tmpV1.x * tmpV2.x) + (tmpV1.y * tmpV2.y);
	}

	public Vector2 GetPerpedicular ()
	{
		return new Vector2 (y, -x);
	}

}