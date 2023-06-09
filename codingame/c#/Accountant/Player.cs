using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

class Player
{


	static void Main (string[] args)
	{
		Game game = new Game ();

		// game loop
		while (true) {

			string output = game.Loop ();

			Console.WriteLine (output); // MOVE x y or SHOOT id
		}
	}

	#region classes ################################################################################################################################################################################################


	public class Game
	{
		List<Enemy> enemies;
		List<Data> data;
		Wolff player;

		public Game ()
		{
			enemies = new List<Enemy> ();
			data = new List<Data> ();
			player = new Wolff ();
		}

		public void LoopInput ()
		{
			string[] inputs = Console.ReadLine ().Split (' ');
			int x = int.Parse (inputs [0]);
			int y = int.Parse (inputs [1]);
			player.position = new Vector2 (x, y);
			int dataCount = int.Parse (Console.ReadLine ());
			Data currData = null;

			//Reset objects relevancy
			for (int i = 0; i < enemies.Count; i++) {
				enemies [i].alive = false;
			}
			for (int i = 0; i < data.Count; i++) {
				data [i].status = false;
			}

			for (int i = 0; i < dataCount; i++) {
				inputs = Console.ReadLine ().Split (' ');

				int dataId = int.Parse (inputs [0]);
				currData = GetData (dataId);

				int dataX = int.Parse (inputs [1]);
				int dataY = int.Parse (inputs [2]);
				currData.position = new Vector2 (dataX, dataY);
				currData.status = true;
			}
			data.RemoveAll (n => !n.status);
			int enemyCount = int.Parse (Console.ReadLine ());

			Enemy currEnemy = null;
			for (int i = 0; i < enemyCount; i++) {
				inputs = Console.ReadLine ().Split (' ');

				int enemyId = int.Parse (inputs [0]);
				currEnemy = GetEnemy (enemyId);

				int enemyX = int.Parse (inputs [1]);
				int enemyY = int.Parse (inputs [2]);
				currEnemy.position = new Vector2 (enemyX, enemyY);

				int enemyLife = int.Parse (inputs [3]);
				currEnemy.health = enemyLife;
				currEnemy.alive = true;
				currEnemy.target = GetClosestData (currEnemy.position);
			}
			enemies.RemoveAll (n => !n.alive);
		}

		#region LOOP ################################################################################################################################################################################################

		public string Loop ()
		{
			string output = "";
			LoopInput ();

			output = Move ();
			if (output.Length > 0) {
				return output;
			}
			float minDist = float.MaxValue;
			float dist = 0;
			Enemy closest = null;

			for (int i = 0; i < enemies.Count; i++) {

				if (enemies [i].alive) {
					dist = Vector2.Distance (player.position, enemies [i].GetNextTurnPosition ());
					if (dist < minDist) {
						minDist = dist;
						closest = enemies [i];
					}
				}
			}
			Vector2 newPosition = new Vector2 ();
			if (closest != null) {

				if (dist > player.deathRadious + Enemy.movementSpeed) {

					float desiredDist = 0;
					int bullets = 1;
					do {
						desiredDist = closest.DistanceToTakeDown (bullets++);
					} while(desiredDist < player.deathRadious);
					if (closest.HowManyBulletsCanTake (player.position) > bullets) {
						Vector2 nextTurnPosition = closest.GetNextTurnPosition ();
						Vector2 dirToPlayer = (player.position - nextTurnPosition).normalized;
						newPosition = nextTurnPosition + (dirToPlayer * desiredDist);
						Console.Error.WriteLine ("Go to Enemy " + closest.ID + " " + dist + " " + (player.deathRadious + Enemy.movementSpeed));
						return "MOVE " + newPosition.OutputString () + " " + bullets + " " + closest.HowManyBulletsCanTake (player.position);
					}
				}

				return "SHOOT " + closest.ID;
			}


			return "MOVE 8000 4500";
		}

		#endregion

		string Move ()
		{
			Vector2 direction = new Vector2 ();

			List<Enemy> dangerousEnemies = new List<Enemy> ();
			List<Data> dangerousData = new List<Data> ();
			for (int i = 0; i < enemies.Count; i++) {
				Console.Error.WriteLine ("141 Enemy " + enemies [i].ID + " nextTurnPosition- " + enemies [i].GetNextTurnPosition () + " targetID- " + enemies [i].target.ID);
				float dist = Vector2.Distance (enemies [i].GetNextTurnPosition (), player.position);
				if (dist < player.deathRadious) {

					dangerousEnemies.Add (enemies [i]);
					direction += (player.position - enemies [i].position).normalized;

					Console.Error.WriteLine ("Dangerous enemy " + (player.position - enemies [i].position).normalized + " " + enemies [i]);

				}
			}
			//			for (int i = 0; i < data.Count; i++) {
			//				if (Vector2.Distance (data [i].position, player.position) < player.deathRadious) {
			//					dangerousData.Add (data [i]);
			//
			//					direction += (player.position - data [i].position).normalized;
			//					Console.Error.WriteLine ("Data " + direction);
			//
			//				}
			//			}
			Vector2 newPosition = player.position + (direction.normalized * Wolff.movementSpeed);
			Vector2 direction2 = new Vector2 ();

			for (int i = 0; i < enemies.Count; i++) {
				//Console.Error.WriteLine (Vector2.Distance (enemies [i].position, newPosition) + " " + enemies [i].ID);
				float dist = Vector2.Distance (enemies [i].GetNextTurnPosition (), newPosition);
				if (dist < player.deathRadious) {

					dangerousEnemies.Add (enemies [i]);
					direction2 += (newPosition - enemies [i].position).normalized;
					Console.Error.WriteLine ("Enemy 2 " + direction2);

				}
			}

			direction = direction.normalized + direction2.normalized;
			direction.Normalize ();
			newPosition = player.position + (direction.normalized * Wolff.movementSpeed);
			Console.Error.WriteLine ("Second " + (direction.normalized * Wolff.movementSpeed) + " " + newPosition + " " + dangerousEnemies.Count);
			if (dangerousEnemies.Count > 0) {
				//				if (dangerousEnemies.Count == 1) {
				//					if (dangerousEnemies [0].HowManyBulletsCanTake (player.position) == 1)
				//						return "";
				//				}
				Console.Error.WriteLine ("Move Away from enemy" + dangerousEnemies.Count + "; direction- " + direction + "; direction2- " + direction2);

				return "MOVE " + newPosition.OutputString () + " " + direction;
			} else {
				return "";
			}
		}

		public Enemy GetEnemy (int ID)
		{
			for (int i = 0; i < enemies.Count; i++) {
				if (enemies [i].ID == ID)
					return enemies [i];
			}
			Enemy newEnemy = new Enemy (ID);
			enemies.Add (newEnemy);
			return newEnemy;
		}

		public Data GetData (int ID)
		{
			for (int i = 0; i < data.Count; i++) {
				if (data [i].ID == ID)
					return data [i];
			}
			Data newData = new Data (ID);
			data.Add (newData);
			return newData;
		}

		public Data GetClosestData (Vector2 position)
		{
			float minDist = float.MaxValue;
			float tmpDist;
			Data returnData = null;
			for (int i = 0; i < data.Count; i++) {
				tmpDist = Vector2.Distance (position, data [i].position);
				if (tmpDist < minDist) {
					minDist = tmpDist;
					returnData = data [i];
				}
			}
			return returnData;
		}
	}


	public class Enemy
	{
		public int ID;
		public Vector2 position;
		public int health;
		public Data target;
		public bool alive;
		public static float movementSpeed = 500;

		public Vector2 Direction {
			get {
				return (target.position - position).normalized;
			}
		}

		public Enemy (int ID)
		{
			this.ID = ID;
		}

		public override string ToString ()
		{
			return "[Enemy]: ID - " + ID + "; position - " + position + "; health - " + health;
		}

		public int HowManyBulletsCanTake (Vector2 playerPosition)
		{
			float distance = Vector2.Distance (position, playerPosition);
			float bullets = health / (125000 / ((float)Math.Pow ((double)distance, 1.2))) + 0.5f;
			return (int)bullets;
		}

		//How close do player has to be to take down this enemy with "bullets" shots
		public float DistanceToTakeDown (int bullets)
		{
			float distance = (float)Math.Pow ((bullets * 125000) / health, 1 / 1.2);
			return distance;
		}

		public Vector2 GetNextTurnPosition ()
		{
			return position + (Direction * movementSpeed);
		}
	}

	public class Wolff
	{
		public Vector2 position;
		public float deathRadious = 2000f;
		public static float movementSpeed = 1000;
	}

	public class Data
	{
		public int ID;
		public Vector2 position;
		public bool status;

		public Data (int ID)
		{
			this.ID = ID;
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
			return new Vector2 (v1.x * (int)variable, v1.y * (int)variable);
		}

		public static Vector2 operator / (Vector2 v1, float variable)
		{
			return new Vector2 (v1.x / (int)variable, v1.y / (int)variable);
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
			return Convert.ToInt32 (x).ToString () + " " + Convert.ToInt32 (y).ToString ();

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

		public override bool Equals (object obj)
		{
			return ReferenceEquals (this, obj);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode ();
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
			return (vec1.x * vec2.x) + (vec1.y * vec2.y);
		}

		public Vector2 GetPerpedicular ()
		{
			return new Vector2 (y, -x);
		}

	}

	#endregion
}