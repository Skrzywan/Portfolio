using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WondevWoman
{
    using System;
    using System.Linq;
    using System.IO;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;

    /**
     * Auto-generated code below aims at helping you parse
     * the standard input according to the problem statement.
     **/
    class Player
    {
        static void Main(string[] args)
        {

            int turns = 0;
            string[] inputs;
            int size = int.Parse(Console.ReadLine());
            int unitsPerPlayer = int.Parse(Console.ReadLine());
            int[][] board = new int[size][];

            Vector2[] myUnits = new Vector2[unitsPerPlayer];
            Vector2[] enemyUnits = new Vector2[unitsPerPlayer];
            string[] myLegalActions;
            Random tmpRand = new Random();

            // game loop
            while (true)
            {
                for (int i = 0; i < size; i++)
                {
                    string row = Console.ReadLine();
                    board[i] = new int[size];
                    //Console.Error.WriteLine(row);
                    string myRow = "";
                    for (int j = 0; j < row.Length; j++)
                    {
                        char tmpChar = row[j];
                        if (tmpChar == '.')
                        {
                            board[i][j] = -1;
                        }
                        else
                        {
                            board[i][j] = (int)char.GetNumericValue(tmpChar);
                            if (board[i][j] > 3)
                            {
                                board[i][j] = -1;
                            }
                        }
                        myRow += board[i][j].ToString();
                    }
                    //Console.Error.WriteLine(myRow);


                }
                for (int i = 0; i < unitsPerPlayer; i++)
                {
                    inputs = Console.ReadLine().Split(' ');
                    int unitX = int.Parse(inputs[0]);
                    int unitY = int.Parse(inputs[1]);
                    myUnits[i] = new Vector2(unitX,unitY);

                }
                for (int i = 0; i < unitsPerPlayer; i++)
                {
                    inputs = Console.ReadLine().Split(' ');
                    int otherX = int.Parse(inputs[0]);
                    int otherY = int.Parse(inputs[1]);

                    enemyUnits[i] = new Vector2(otherX, otherY);

                }
                int legalActions = int.Parse(Console.ReadLine());
                myLegalActions = new string[legalActions];
                List<KeyValuePair<string, int>> actionWages = new List<KeyValuePair<string, int>>();

               


                for (int i = 0; i < legalActions; i++)
                {
                    inputs = Console.ReadLine().Split(' ');
                    string atype = inputs[0];
                    int index = int.Parse(inputs[1]);
                    string dir1 = inputs[2];
                    string dir2 = inputs[3];
                    myLegalActions[i] = atype + " " + index + " " + dir1 + " " + dir2;
                    Vector2 movePosition = myUnits[index] + GetDirection(dir1);
                    Vector2 buildPosition = movePosition + GetDirection(dir2);
                    //Console.Error.WriteLine(dir1 + " movePosition " + movePosition.ToString()+ "    "+ dir2 + "  buildPosition " + buildPosition.ToString());

                    //Console.Error.WriteLine(newPair.Key + "   - " + newPair.Value);// +"   " + board[movePosition.x][movePosition.y] + "   " + board[buildPosition.x][buildPosition.y] + " "+ +BuildOn(board[buildPosition.x][buildPosition.y]));
                    //Console.Error.WriteLine(" -- "+movePosition.ToString() + "    " + board[buildPosition.x][buildPosition.y] + "    " + buildPosition.ToString() + "     " + +BuildOn(board[buildPosition.x][buildPosition.y]));

                    int wage = (board[movePosition.y][movePosition.x] * 2) + BuildOn(board[buildPosition.y][buildPosition.x]);
                    KeyValuePair<string, int> newPair = new KeyValuePair<string, int>(myLegalActions[i], wage);
                    actionWages.Add(newPair);
                    //Console.Error.WriteLine(newPair.Key + "   - " + newPair.Value);// +"   " + board[movePosition.x][movePosition.y] + "   " + board[buildPosition.x][buildPosition.y] + " "+ +BuildOn(board[buildPosition.x][buildPosition.y]));
                    //Console.Error.WriteLine(movePosition.ToString() + "    " + board[movePosition.y][movePosition.x] + "    " + buildPosition.ToString() + "   "+ board[movePosition.y][movePosition.x]);

                }

                Console.Error.WriteLine("actionWages " + actionWages.Count);
                if (actionWages.Count > 0)
                {

                    KeyValuePair<string, int> maxVal = actionWages[0];
                    for (int i = 0; i < actionWages.Count; i++)
                    {

                        //Console.Error.WriteLine(maxVal.Key + " : " + maxVal.Value+"  -  "+ actionWages[i].Key+" : "+ actionWages[i].Value);
                        if (actionWages[i].Value > maxVal.Value)
                        {
                            maxVal = actionWages[i];
                        }
                    }
                    Console.WriteLine(maxVal.Key);

                }
                else
                {
                    Console.WriteLine("MOVE&BUILD "+(myUnits.Length - 1)+" N N");
                }
            }
        }

        static int BuildOn(int platformVal)
        {
            int newVal = platformVal + 1;
            if (newVal >= 4)
            {
                newVal = -1;
            }
            return platformVal >= 0 ? newVal : platformVal;
        }

        public static Vector2 GetDirection(string sDir)
        {
            Vector2 toReturn = new Vector2(0,0);
            if (sDir.Contains("N"))
            {
                toReturn.y = -1;
            }
            if (sDir.Contains("S"))
            {
                toReturn.y = 1;

            }
            if (sDir.Contains("E"))
            {
                toReturn.x = 1;

            }
            if (sDir.Contains("W"))
            {
                toReturn.x = -1;

            }
            return toReturn;
        }
    }



    public class Vector2
    {
        public int x;
        public int y;
        public const double radToDegrees = 180 / Math.PI;
        public const double degreesToRad = Math.PI / 180;

        public Vector2 Normalize()
        {
            int sum = GetLength();
            if ((sum / 100) != 0)
            {
                x /= (sum / 100);
                y /= (sum / 100);
            }
            return this;
        }

        public Vector2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x + v2.x, v1.y + v2.y);
        }

        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x - v2.x, v1.y - v2.y);
        }

        public static Vector2 operator *(Vector2 v1, float variable)
        {
            return new Vector2(v1.x * (int)variable, v1.y * (int)variable);
        }

        public static Vector2 operator /(Vector2 v1, float variable)
        {
            return new Vector2(v1.x / (int)variable, v1.y / (int)variable);
        }

        public static bool operator ==(Vector2 v1, Vector2 v2)
        {
            return (v1.x == v2.x && v1.y == v2.y);
        }

        public static bool operator !=(Vector2 v1, Vector2 v2)
        {
            return (v1.x != v2.x || v1.y != v2.y);
        }

        override public string ToString()
        {
            return "x = " + x + " , y = " + y;
        }

        public int GetLength()
        {
            return (int)Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
        }

        public static int Distance(Vector2 first, Vector2 target)
        {
            Vector2 connectVectors = first - target;
            return connectVectors.GetLength();
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        //Angle between point1,point2 and point3
        public static int Angle(Vector2 point1, Vector2 point2, Vector2 point3)
        {
            //double radToDegrees = 180 / Math.PI;

            int a = Vector2.Distance(point1, point2);
            int b = Vector2.Distance(point2, point3);
            int c = Vector2.Distance(point1, point3);
            double cosine = ((Math.Pow(a, 2) + Math.Pow(b, 2) - Math.Pow(c, 2)) / (2 * a * b));

            return (int)((Math.Acos(cosine) * radToDegrees) + 0.5f);
        }
        //Angle between point1,point2
        public static int WorldAngle(Vector2 point1, Vector2 point2)
        {
            //double radToDegrees = 180 / Math.PI;

            Vector2 point3 = new Vector2(point1.x, point2.y);

            int a = Vector2.Distance(point1, point2);
            int b = Vector2.Distance(point2, point3);
            int c = Vector2.Distance(point1, point3);
            double cosine = ((Math.Pow(a, 2) + Math.Pow(b, 2) - Math.Pow(c, 2)) / (2 * a * b));

            return (int)((Math.Acos(cosine) * radToDegrees) + 0.5f);
        }

        public static float Dot(Vector2 vec1, Vector2 vec2)
        {
            return (vec1.x * vec2.x) + (vec1.y * vec2.y);
        }

        public Vector2 GetPerpedicular()
        {
            return new Vector2(y, -x);
        }

        public static Vector2 Reflect(Vector2 toReflect, Vector2 reflectBy)
        {
            Vector2 retVal = null;
            Vector2 tmpCopy = new Vector2(reflectBy.x, reflectBy.y).Normalize();
            float dotProd = Dot(toReflect, reflectBy);
            retVal = toReflect - (tmpCopy * 2 * dotProd);
            return retVal;
        }

    }

}
