using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

class Player
{
	static int deltaDist;


	static int nodeCount = 0;

	static int frameCounter = 0;
	static int lastBoostFrame = -1;

	static List<int> angles = new List<int> ();

	static Waypoint[] checkpoints;
	static List<Pod> myPods;
	static List<Pod> enemyPods;
	static List<Pod> allPods;
	static Waypoint longestDistStartIndex;
	static float velocityMul = 4;

	static Waypoint currentGuardingPoint;

	static void Main (string[] args)
	{

		InitializeInput ();
		InitPods ();
		longestDistStartIndex = GetLongestPathStartIndex ();

		currentGuardingPoint = checkpoints [0].GetTwoAhead ();
		// game loop
		while (true) {

			LoopInput ();
			Vector2 trajectory = null;
			for (int i = 0; i < myPods.Count; i++) {

				int thrust = 100;
				string stringThrust = "";

				myPods [i].target = GetTarget (myPods [i], myPods [i].target);
				trajectory = myPods [i].target.position - (myPods [i].velocity * velocityMul);

				int currentWaypoint = nodeCount;
				if (i == 0) {				//Pod jezdzacy

					thrust = CalculateThrust (myPods [i], trajectory, 30);
					if (i == 0 && frameCounter == 0) {
						thrust = 1000;
					}

				} else {					//Pod walczacy

					int tmp = -1;
					Pod targetPod = null;
					float minDist = float.MaxValue;
					trajectory = currentGuardingPoint.position;

					for (int j = 0; j < enemyPods.Count; j++) {
						Console.Error.WriteLine ((enemyPods [j].pointsChecked > tmp) + " " + enemyPods [j].pointsChecked + " " + tmp);
						if (enemyPods [j].target != null && enemyPods [j].pointsChecked >= tmp) {
							float tmpDist = Vector2.Distance (enemyPods [i].position, enemyPods [i].target.position);

							if ((enemyPods [j].pointsChecked == tmp && minDist > tmpDist) || enemyPods [j].pointsChecked > tmp) {
								minDist = tmpDist;
								tmp = enemyPods [j].pointsChecked;
								targetPod = enemyPods [j];
							}
						}
					}
					if (targetPod != null) {
						Vector2 toPod = null;

						if (currentGuardingPoint == targetPod.target.previous) {
							currentGuardingPoint = currentGuardingPoint.GetTwoAhead ();
						}								

						if (currentGuardingPoint == targetPod.target.next) {
							//toPod = targetPod.position - targetPod.target.position;

							toPod = currentGuardingPoint.position - currentGuardingPoint.previous.position;
							trajectory = currentGuardingPoint.position - (toPod / 2) + (targetPod.velocity * velocityMul);

							int maxDist = 2500;
							toPod = (myPods [i].position - trajectory);
							int distanceToTarget = toPod.GetLength ();

							if (distanceToTarget < maxDist) {
								//distanceToTarget = Clamp (distanceToTarget, minDist, maxDist) * minDist;
								thrust = -1;//(int)(((float)distanceToTarget * 100) / (maxDist));
								trajectory = targetPod.position;
							}
						} else {



							/*
							Jesli nie znajdujesz sie pomiedzy pojazdem a nextPOint to znajdz secondNext
						*/


							/*

							trajectory = targetPod.position;
							if (Vector2.Distance (myPods [i].position, targetPod.position) > 2000) {		//Jesli znajdujesz sie pomiedzy pojazdem a nextPoint
								trajectory += (myPods [i].velocity * velocityMul / 2) - targetPod.ForwardVector.Normalize () * velocityMul * 2;
							} else {
								trajectory += (targetPod.ForwardVector.Normalize () * velocityMul * 2) + (targetPod.velocity * velocityMul);//toPod + currentGuardingPoint.position;;
							}
							*/

							toPod = myPods [i].position - targetPod.position;
							toPod = Vector2.Reflect (toPod, targetPod.target.position - targetPod.position);
							trajectory = targetPod.position + toPod;

							if (trajectory.GetLength () > 2000) {
								thrust = CalculateThrust (myPods [i], trajectory, 30);
							} else {

								if (!myPods [i].boostUsed) {
									float dotProd = Vector2.Dot (myPods [i].velocity, targetPod.velocity);
									if (dotProd < -0.7f) {
										thrust = 1000;
									}
								}

								thrust = CheckForCollision (myPods [i], thrust, true);
							}
						}

					} else {
						//Console.Error.WriteLine ("targetPod = null");
						trajectory = currentGuardingPoint.position + (currentGuardingPoint.previous.position - currentGuardingPoint.position).Normalize () * 5;
					}
				}

				if (thrust > 100 && ((i == 1 && longestDistStartIndex == myPods [i].target && (frameCounter - lastBoostFrame > 100 || lastBoostFrame == -1) || i == 0))) {
					stringThrust = "BOOST";
					myPods [i].boostUsed = true;
					lastBoostFrame = frameCounter;
				} else {
					if (thrust < 0) {
						stringThrust = "SHIELD";
					} else
						stringThrust = Clamp (thrust, 0, 100).ToString ();
				}
				Console.Error.WriteLine (myPods [i].velocity + " " + myPods [i].position);
				Console.WriteLine (trajectory.x + " " + trajectory.y + " " + stringThrust);

				myPods [i].prevDistance = Vector2.Distance (myPods [i].position, myPods [i].inputarget.position);
			}
			frameCounter++;
		}
	}

	static void InitPods ()
	{
		myPods = new List<Pod> ();
		enemyPods = new List<Pod> ();
		allPods = new List<Pod> ();
		for (int i = 0; i < 2; i++) {
			
			myPods.Add (new Pod ());
			allPods.Add (new Pod ());
			if (i != 0) {
				myPods [i].racer = false;
			} else {
				myPods [i].racer = true;
			}
		}
		for (int i = 0; i < 2; i++) {
			enemyPods.Add (new Pod ());
			allPods.Add (new Pod ());
		}

	}

	static void InitializeInput ()
	{
		string[] inputs;

		int laps = int.Parse (Console.ReadLine ());
		int numOfCheckpoints = int.Parse (Console.ReadLine ());

		checkpoints = new Waypoint[numOfCheckpoints];

		for (int i = 0; i < numOfCheckpoints; i++) {
			checkpoints [i] = new Waypoint ();
			inputs = Console.ReadLine ().Split (' ');
			int x = int.Parse (inputs [0]);
			int y = int.Parse (inputs [1]);
			Vector2 position = new Vector2 (x, y);
			checkpoints [i].position = position;
			checkpoints [i].index = i;
			if (i > 0) {
				checkpoints [i].previous = checkpoints [i - 1];
				checkpoints [i - 1].next = checkpoints [i];
			}
		}
		checkpoints [0].previous = checkpoints [numOfCheckpoints - 1];
		checkpoints [numOfCheckpoints - 1].next = checkpoints [0];

		for (int i = 0; i < checkpoints.Length; i++) {
			int angle = Vector2.Angle (checkpoints [i].previous.position, checkpoints [i].position, checkpoints [i].next.position);
			int angleSign = GetAngleSign (checkpoints [i].previous.position, checkpoints [i].position, checkpoints [i].next.position);
			checkpoints [i].angleToNext = (180 - angle * angleSign);
			//Console.Error.WriteLine (checkpoints [i].angleToNext + " " + cosine + " " + (Math.Acos (cosine) * radToDegrees) + " " + GetAngleSign (checkpoints [i].previous.position, checkpoints [i].position, checkpoints [i].next.position));
		}
	}

	static void LoopInput ()
	{
		string[] inputs;
		for (int i = 0; i < 2; i++) {
			inputs = Console.ReadLine ().Split (' ');

			int x = int.Parse (inputs [0]);
			int y = int.Parse (inputs [1]);
			myPods [i].position = new Vector2 (x, y);

			x = int.Parse (inputs [2]);
			y = int.Parse (inputs [3]);
			myPods [i].velocity = new Vector2 (x, y);

			x = int.Parse (inputs [4]);
			y = int.Parse (inputs [5]);
			myPods [i].angle = x;
			//nextTarget = checkpoints [y];

			myPods [i].target = checkpoints [y];
			myPods [i].inputarget = checkpoints [y];
			allPods [i] = myPods [i];
		}

		for (int i = 0; i < 2; i++) {
			inputs = Console.ReadLine ().Split (' ');

			int x = int.Parse (inputs [0]);
			int y = int.Parse (inputs [1]);
			enemyPods [i].position = new Vector2 (x, y);

			x = int.Parse (inputs [2]);
			y = int.Parse (inputs [3]);
			enemyPods [i].velocity = new Vector2 (x, y);

			x = int.Parse (inputs [4]);
			y = int.Parse (inputs [5]);
			enemyPods [i].angle = x;

			//Console.Error.WriteLine ((enemyPods [i].target == checkpoints [y].previous) + " " + (enemyPods [i].target != null ? enemyPods [i].target.index : -1) + " " + checkpoints [y].index + "  enemyPods [i].pointsChecked " + enemyPods [i].pointsChecked);

			if (enemyPods [i].target != null && enemyPods [i].target == checkpoints [y].previous) {
				enemyPods [i].pointsChecked++;
			}
			if (y < checkpoints.Length && y >= 0) {
				enemyPods [i].target = checkpoints [y];
				enemyPods [i].inputarget = checkpoints [y];
			}
			allPods [i + 2] = enemyPods [i];

			//			if (y > 0 && y < checkpoints.Length)
			//				enemyNextTarget = checkpoints [y];
		}
	}

	static Waypoint GetLongestPathStartIndex ()
	{
		float maxDistance = float.MinValue;
		int index = 0;

		List<Waypoint> tmpPoints = new List<Waypoint> (checkpoints);

		for (int i = 0; i < checkpoints.Length; i++) {
			float tmpD = Vector2.Distance (checkpoints [i].position, checkpoints [i].previous.position);
			if (tmpD > maxDistance) {
				maxDistance = tmpD;
				index = i;
			}
		}
		return checkpoints [index];
	}

	//Oblicz sile
	static int CalculateThrust (Pod pod, Vector2 targetPosition, int minSpeed)
	{
		int retVal = 100;

		//Ustal zaleznosc kata
		//if (pod.velocity.GetLength () > 400) {

		int minAngle = 20;
		int maxAngle = 90;
		//Angle between forward vector of a pod and its target
		int angle = 180 - Vector2.Angle (targetPosition, pod.position + pod.ForwardVector, pod.position);
		int clampedAngle = Clamp (angle, minAngle, maxAngle) - minAngle;// (float)Math.Min (Math.Max (minAngle, Math.Abs (angle)), 120) - minAngle;
		retVal = (int)((float)(100 - (((float)clampedAngle / (float)(maxAngle - minAngle)) * (100 - minSpeed))));

		if (!pod.boostUsed) {
			Vector2 podToTarget = pod.target.position - pod.position;
			if (Vector2.Dot (podToTarget, pod.velocity) > 0.8f && podToTarget.GetLength () > 6000)
				retVal = 1000;
		}

		retVal = CheckForCollision (pod, retVal);

		return retVal;
	}

	static int CheckForCollision (Pod pod, int initVal = 100, bool alwaysCollide = false)
	{
		int retVal = initVal;

		for (int j = 0; j < allPods.Count; j++) {
			if (pod != allPods [j] && pod.WillCollide (allPods [j])) {
				Vector2 forward = (pod.target.position - (pod.velocity * velocityMul)) - pod.position;
				Vector2 enemyForward = allPods [j].velocity; 

				float dist = forward.GetLength ();
				float enemyDist = ((pod.target.position - (allPods [j].velocity * velocityMul)) - allPods [j].position).GetLength ();

				float velocityDiff = Math.Abs (pod.velocity.GetLength () - allPods [j].velocity.GetLength ());
				if (!(alwaysCollide && myPods.Contains (allPods [j]))) {

					if (((Vector2.Dot (forward, enemyForward) < (0.3f) || dist > enemyDist) && (pod.velocity.GetLength () > 110)) || (alwaysCollide)) {
						retVal = -1;
					}
				}
			}
		}
		return retVal;
	}

	static Waypoint GetTarget (Pod pod, Waypoint point)
	{
		int distToCurrPoint = Vector2.Distance (pod.position, point.position);
		deltaDist = pod.velocity.GetLength ();

		if (distToCurrPoint < pod.prevDistance && deltaDist > 100 && Vector2.Distance (point.position - (pod.velocity * velocityMul), pod.position) < 400 || Vector2.Distance (point.position, pod.position) < (pod.velocity.GetLength () * velocityMul)) {
			Console.Error.WriteLine (pod.WillPassCheckpoint ());
			if (pod.WillPassCheckpoint ())
				return point.next;
		}
		return point;
	}

	static int Clamp (int val, int min, int max)
	{

		return Math.Max (Math.Min (max, val), min);
	}

	/// -1 == left, 1 == right
	public static int GetAngleSign (Vector2 lineStart, Vector2 lineEnd, Vector2 point)
	{
		if ((lineEnd.x - lineStart.x) * (point.y - lineStart.y) - (lineEnd.y - lineStart.y) * (point.y - lineStart.y) * (-1) > 0) {
			return 1;
		} else
			return -1;
	}

	/// -1 == left, 1 == right
	public static int GetWorldAngle (Vector2 lineStart, Vector2 lineEnd, Vector2 point)
	{
		if ((lineEnd.x - lineStart.x) * (point.y - lineStart.y) - (lineEnd.y - lineStart.y) * (point.y - lineStart.y) * (-1) > 0) {
			return 1;
		} else
			return -1;
	}

	#region klasy  #######################################################################################################################################################################################################################

	public class Waypoint
	{
		public int index;
		public Vector2 position;
		public int angleToNext;

		public Waypoint next;
		public Waypoint previous;

		public Waypoint GetTwoAhead ()
		{
			return next.next;
		}
	}

	public class Pod
	{
		public Vector2 position;
		public Vector2 velocity;
		public Waypoint target;
		public Waypoint inputarget;
		public bool boostUsed = false;
		public int angle;
		public int pointsChecked = -1;
		public int prevDistance = 0;
		public bool racer = true;

		public bool WillCollide (Pod enemy)
		{
			Vector2 thisFuturePosition = position + velocity;
			Vector2 enemyFuturePosition = enemy.position + enemy.velocity;

			int futureDistance = Vector2.Distance (thisFuturePosition, enemyFuturePosition);

			return futureDistance < (racer ? 780 : 801);
		}

		/// <summary>
		/// Wills the pod pass successfully the checkpoint.
		/// </summary>
		/// <returns><c>true</c>, if pass checkpoint was willed, <c>false</c> otherwise.</returns>
		/// <param name="pod">Pod.</param>
		public bool WillPassCheckpoint ()
		{
			float tan = (float)400 / Vector2.Distance (position, target.position);
			float angle = (float)(Math.Atan (tan) * Vector2.radToDegrees);

			float velAngle = Vector2.Angle (target.position, position, position + velocity);
			Console.Error.WriteLine (" angle- " + angle + "  velAngle- " + velAngle + "   tan - " + tan);
			return angle > velAngle;
		}

		public Vector2 ForwardVector {
			get {
				Vector2 a = position;
				Vector2 b = new Vector2 (16000, position.y);
				Vector2 c = new Vector2 (16000, position.y);

				float dist = Vector2.Distance (a, b);

				double radAngle = angle * Vector2.degreesToRad;

				double height = Math.Abs (Math.Tan (radAngle) * dist);
				c.y += (int)height;

				c = c - position;

				c.Normalize ();
				if (AngleBetween (0, 89)) {

				}
				if (AngleBetween (90, 179)) {
					c.x *= -1;
				}
				if (AngleBetween (180, 269)) {
					c.x *= -1;
					c.y *= -1;
				}
				if (AngleBetween (270, 360)) {
					c.y *= -1;
				}
				return c;
			}
		}

		bool AngleBetween (int angle1, int angle2)
		{
			return (angle >= angle1 && angle <= angle2);
		}
	}

	public class Vector2
	{
		public int x;
		public int y;
		public const double radToDegrees = 180 / Math.PI;
		public const double degreesToRad = Math.PI / 180;

		public Vector2 Normalize ()
		{
			int sum = GetLength ();
			if ((sum / 100) != 0) {
				x /= (sum / 100);
				y /= (sum / 100);
			}
			return this;
		}

		public Vector2 (int x, int y)
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

		public int GetLength ()
		{
			return (int)Math.Sqrt (Math.Pow (x, 2) + Math.Pow (y, 2));
		}

		public static int Distance (Vector2 first, Vector2 target)
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

			int a = Vector2.Distance (point1, point2);
			int b = Vector2.Distance (point2, point3);
			int c = Vector2.Distance (point1, point3);
			double cosine = ((Math.Pow (a, 2) + Math.Pow (b, 2) - Math.Pow (c, 2)) / (2 * a * b));

			return (int)((Math.Acos (cosine) * radToDegrees) + 0.5f);
		}
		//Angle between point1,point2
		public static int WorldAngle (Vector2 point1, Vector2 point2)
		{
			//double radToDegrees = 180 / Math.PI;

			Vector2 point3 = new Vector2 (point1.x, point2.y);

			int a = Vector2.Distance (point1, point2);
			int b = Vector2.Distance (point2, point3);
			int c = Vector2.Distance (point1, point3);
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

		public static Vector2 Reflect (Vector2 toReflect, Vector2 reflectBy)
		{
			Vector2 retVal = null;
			Vector2 tmpCopy = new Vector2 (reflectBy.x, reflectBy.y).Normalize ();
			float dotProd = Dot (toReflect, reflectBy);
			retVal = toReflect - (tmpCopy * 2 * dotProd);
			return retVal;
		}

	}

	#endregion
}