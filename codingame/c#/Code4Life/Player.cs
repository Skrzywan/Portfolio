using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

class GlobalVariables
{
    public static string samples = "SAMPLES";
    public static string diagnosis = "DIAGNOSIS";
    public static string molecules = "MOLECULES";
    public static string lab = "LABORATORY";

    public static int sampleLimit = 3;
    public static int moleculesLimit = 10;
    public static int maxScore = 100000;
    public static int maxMoleculeCount = 5;
    public static int maxWaitCounter = 7;

    public static float acceptableValue = 1.3f;

    public static char[] moleculeTypes = { 'A', 'B', 'C', 'D', 'E' };

}
//TODO: Wyliczanie jakie probli potrzebuje zeby zakoczyc mecz (np na koniec potrzebuje 10 pkt to nie ma sensu pracowac nad smplem z 30 hp i wieksza iloscia potrzebnych modulow)
//TODO: wyjmowanie z clouda sampli ktore moge juz reaserchowac
//TODO: ile przydatnych sampli znajduje sie w cloudzie

class SciProj
{
    public int[] cost;
    public SciProj()
    {
        cost = new int[5];
    }
    public bool IsActive()
    {
        bool isDone = true;
        bool isDoneByEnemy = true;

        for (int i = 0; i < Player.myRobot.expertise.Length; i++)
        {
            if (Player.myRobot.expertise[i] < cost[i])
            {
                isDone = false;
                break;
            }
        }
        for (int i = 0; i < Player.enemyRobot.expertise.Length; i++)
        {
            if (Player.enemyRobot.expertise[i] < cost[i])
            {
                isDoneByEnemy = false;
                break;
            }
        }
        return !(isDone || isDoneByEnemy);
    }

}
class Robot
{
    public string target;
    public int eta;
    public int score;
    public int[] storage;
    public int storageUnits;
    public int myExpertise;

    public int[] expertise;
    public int storageA;
    public int storageB;
    public int storageC;
    public int storageD;
    public int storageE;
    public int expertiseA;
    public int expertiseB;
    public int expertiseC;
    public int expertiseD;
    public int expertiseE;

    public Robot()
    {
        storage = new int[5];
        expertise = new int[5];
    }

    public void Init(string[] inputs)
    {
        target = inputs[0];
        eta = int.Parse(inputs[1]);
        score = int.Parse(inputs[2]);
        storageA = int.Parse(inputs[3]);
        storageB = int.Parse(inputs[4]);
        storageC = int.Parse(inputs[5]);
        storageD = int.Parse(inputs[6]);
        storageE = int.Parse(inputs[7]);
        expertiseA = int.Parse(inputs[8]);
        expertiseB = int.Parse(inputs[9]);
        expertiseC = int.Parse(inputs[10]);
        expertiseD = int.Parse(inputs[11]);
        expertiseE = int.Parse(inputs[12]);

        CopyStorageAndExpertise();
    }

    public void CopyStorageAndExpertise()
    {
        storage[0] = storageA;
        storage[1] = storageB;
        storage[2] = storageC;
        storage[3] = storageD;
        storage[4] = storageE;

        expertise[0] = expertiseA;
        expertise[1] = expertiseB;
        expertise[2] = expertiseC;
        expertise[3] = expertiseD;
        expertise[4] = expertiseE;

        storageUnits = 0;
        myExpertise = 0;
        for (int i = 0; i < storage.Length; i++)
        {
            storageUnits += storage[i];
            myExpertise += expertise[i];

        }
    }
}

class Sample
{
    public int sampleId;
    public int carriedBy;
    public int rank;
    public char expertiseGain;
    public int expertiseGainInt;

    public int health;
    public int[] cost;
    public int myCost;
    public int missingMoleculesCount;
    public int overallCost;
    public float value;
    public bool sac;
    public bool stuck;

    public int costA;
    public int costB;
    public int costC;
    public int costD;
    public int costE;

    public bool updated;
    public bool inited;
    public bool diagnosed;


    public Sample()
    {
        cost = new int[5];
        diagnosed = false;
        sac = false;
        stuck = false;
    }

    public int GetRequired(int id)
    {
        return Math.Max(cost[id] - GetRobot().expertise[id], 0);
    }



    public void Init(string[] inputs)
    {
        inited = true;

        sampleId = int.Parse(inputs[0]);
        carriedBy = int.Parse(inputs[1]);
        rank = int.Parse(inputs[2]);
        expertiseGain = (char)inputs[3][0];
        health = int.Parse(inputs[4]);
        costA = int.Parse(inputs[5]);
        costB = int.Parse(inputs[6]);
        costC = int.Parse(inputs[7]);
        costD = int.Parse(inputs[8]);
        costE = int.Parse(inputs[9]);

        cost[0] = costA;
        cost[1] = costB;
        cost[2] = costC;
        cost[3] = costD;
        cost[4] = costE;

        myCost = 0;
        overallCost = 0;
        missingMoleculesCount = 0;

        for (int i = 0; i < cost.Length; i++)
        {
            myCost += GetRequired(i);
            overallCost += cost[i];
            missingMoleculesCount += Math.Max(0, GetRequired(i) - GetRobot().storage[i]);
        }
        value = (float)myCost / (float)health;

        int aValue = 'A';

        expertiseGainInt = (int)expertiseGain - aValue;
    }

    public bool CanBeExecutedNow()
    {
        for (int i = 0; i < cost.Length; i++)
        {
            if (cost[i] - GetRobot().expertise[i] > GlobalVariables.maxMoleculeCount)
            {
                return false;
            }
        }
        return true;
    }

    public Robot GetRobot()
    {
        return (carriedBy == 1 ? Player.enemyRobot : Player.myRobot);

    }

    public bool CanBeExecutedInNearFuture()
    {
        int[] expertise = new int[cost.Length];
        Player.myRobot.expertise.CopyTo(expertise, 0);
        int aValue = 'A';
        for (int i = 0; i < Player.myCurrentSamples.Count; i++)
        {
            if (Player.myCurrentSamples[i] != this)
            {
                int index = (int)Player.myCurrentSamples[i].expertiseGain - aValue;
                if (index >= 0 && index <= 4)
                {
                    expertise[index]++;
                }
            }
        }
        int reducedCost = 0;
        for (int i = 0; i < cost.Length; i++)
        {
            reducedCost += Math.Max(0, cost[i] - expertise[i]);
            if (cost[i] - expertise[i] > GlobalVariables.maxMoleculeCount)
            {
                return false;
            }
        }
        return reducedCost < GlobalVariables.moleculesLimit;
    }
}


/**
 * Bring data on patient samples from the diagnosis machine to the laboratory with enough molecules to produce medicine!
 **/
class Player
{
    internal static Robot myRobot;
    internal static Robot enemyRobot;
    internal static List<Sample> samples;
    internal static List<Sample> myCurrentSamples;
    internal static List<Sample> enemySamples;

    internal static int[] available;
    static int waitCounter = 0;
    static SciProj[] sciProjs;
    static void Main(string[] args)
    {

        available = new int[5];
        string[] inputs;
        myRobot = new Robot();
        enemyRobot = new Robot();
        samples = new List<Sample>();
        myCurrentSamples = new List<Sample>();
        enemySamples = new List<Sample>();

        int projectCount = int.Parse(Console.ReadLine());
        string[] projs = new string[projectCount];
        sciProjs = new SciProj[projectCount];


        for (int i = 0; i < projectCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            int a = int.Parse(inputs[0]);
            int b = int.Parse(inputs[1]);
            int c = int.Parse(inputs[2]);
            int d = int.Parse(inputs[3]);
            int e = int.Parse(inputs[4]);

            projs[i] = "Project " + i + ": " + a + " " + b + " " + c + " " + d + " " + e;
            sciProjs[i] = new SciProj();
            sciProjs[i].cost[0] = a;
            sciProjs[i].cost[1] = b;
            sciProjs[i].cost[2] = c;
            sciProjs[i].cost[3] = d;
            sciProjs[i].cost[4] = e;


        }
        // game loop
        while (true)
        {
            for (int i = 0; i < 2; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                Robot current = i == 0 ? myRobot : enemyRobot;
                current.Init(inputs);
            }
            inputs = Console.ReadLine().Split(' ');
            available[0] = int.Parse(inputs[0]);
            available[1] = int.Parse(inputs[1]);
            available[2] = int.Parse(inputs[2]);
            available[3] = int.Parse(inputs[3]);
            available[4] = int.Parse(inputs[4]);
            int sampleCount = int.Parse(Console.ReadLine());

            for (int i = 0; i < samples.Count; i++)
            {
                samples[i].updated = false;
            }
            enemySamples.Clear();
            myCurrentSamples.Clear();
            for (int i = 0; i < sampleCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                Sample newSample = GetSampleById(int.Parse(inputs[0]));
                if (!newSample.inited)
                {
                    samples.Add(newSample);
                }
                newSample.Init(inputs);
                newSample.updated = true;
                if (newSample.carriedBy == 0)
                {
                    myCurrentSamples.Add(newSample);
                }
                else
                {
                    if (newSample.carriedBy == -1)
                    {
                        newSample.sac = false;
                        newSample.stuck = false;
                    }
                    else  // carriedBy = 1 - enemy
                    {
                        enemySamples.Add(newSample);
                    }
                }
            }



            for (int i = 0; i < projectCount; i++)
            {
                WrErr(projs[i] + "\n");
            }

            for (int i = samples.Count - 1; i >= 0; i--)
            {
                if (!samples[i].updated)
                {
                    samples.RemoveAt(i);
                }
            }
            myCurrentSamples.Sort((n1, n2) => (SortCompare(n1, n2)));
            for (int i = 0; i < myCurrentSamples.Count; i++)
            {
                WrErr(myCurrentSamples[i].sampleId + " " + myCurrentSamples[i].rank + myCurrentSamples[i].sac + " helps " + HelpsInSciProj(myCurrentSamples[i]));
            }

            string action = GetAction();
            if (action.Contains("WAIT"))
            {
                waitCounter++;
                WrErr(waitCounter + " waitCounter");
            }
            else
            {
                waitCounter = 0;
            }
            Console.WriteLine(action);
        }
    }
    public static bool AreAllProjActive()
    {
        for (int i = 0; i < sciProjs.Length; i++)
        {
            if (!sciProjs[i].IsActive())
            {
                return false;
            }
        }
        return true;
    }
    public static bool AnyProjActive()
    {
        for (int i = 0; i < sciProjs.Length; i++)
        {
            if (sciProjs[i].IsActive())
            {
                return true;
            }
        }
        return false;
    }
    public static bool HelpsInSciProj(Sample sample)
    {
        if (sample.expertiseGainInt < 0 || sample.expertiseGainInt > 4)
        {
            return false;
        }
        for (int i = 0; i < sciProjs.Length; i++)
        {
            if (sciProjs[i].IsActive() && myRobot.expertise[sample.expertiseGainInt] + 1 <= sciProjs[i].cost[sample.expertiseGainInt])
            {
                return true;
            }
        }
        return false;
    }
    public static int GetPointsAfterBatch(bool countStuck = false)
    {
        int points = myRobot.score;
        for (int i = 0; i < myCurrentSamples.Count; i++)
        {
            if (!myCurrentSamples[i].stuck || countStuck)
            {
                points += myCurrentSamples[i].health;
            }
        }
        return points;
    }

    public static int GetRequiredPoints(bool countStuck = false)
    {
        int points = GlobalVariables.maxScore;
        if (waitCounter < GlobalVariables.maxWaitCounter)
        {
            points -= GetPointsAfterBatch(countStuck);
        }
        return points;
    }

    static bool IsSampleValid(Sample sample)
    {

        return (sample.CanBeExecutedNow() && GetRequiredMoleculesCount(sample) <= (GlobalVariables.moleculesLimit - myRobot.storageUnits)
        && (float)sample.health / (float)sample.myCost >= GlobalVariables.acceptableValue * (sample.rank - 1));
    }


    static int SortCompare(Sample first, Sample second)
    {
        float firstSum = 0;
        float secondSum = 0;

        firstSum += first.rank * 5;
        secondSum += second.rank * 5;

        if (!first.CanBeExecutedNow())
        {
            firstSum -= 100;
        }
        if (!second.CanBeExecutedNow())
        {
            secondSum -= 100;

        }
        firstSum += first.value;
        secondSum += second.value;

        firstSum -= first.missingMoleculesCount;
        secondSum -= second.missingMoleculesCount;


        if (AllMoleculesAvailable(first))
        {
            firstSum += 20;
        }
        if (AllMoleculesAvailable(second))
        {
            secondSum += 20;
        }

        if (HelpsInSciProj(first))
        {
            firstSum += 1000;
        }
        if (HelpsInSciProj(second))
        {
            secondSum += 1000;
        }
        return (firstSum > secondSum) ? -1 : 1;
    }

    static bool AllMoleculesAvailable(Sample sample)
    {
        for (int i = 0; i < sample.cost.Length; i++)
        {
            if (available[i] < sample.GetRequired(i))
            {
                return false;
            }
        }
        return true;
    }

    static Sample GetSampleById(int id)
    {
        for (int i = 0; i < samples.Count; i++)
        {
            if (samples[i].sampleId == id)
            {
                return samples[i];
            }
        }

        return new Sample();
    }

    static bool HaveRequiredMolecules()
    {
        int[] currentMolecules = new int[myRobot.storage.Length];
        myRobot.storage.CopyTo(currentMolecules, 0);
        for (int i = 0; i < myCurrentSamples.Count; i++)
        {
            for (int j = 0; j < myCurrentSamples[i].cost.Length; j++)
            {
                currentMolecules[j] -= myCurrentSamples[i].GetRequired(j);
                if (currentMolecules[j] < 0)
                {
                    return false;
                }
            }
        }
        return true;
    }

    static bool HaveRequiredMolecules(Sample sample)
    {
        int[] currentMolecules = new int[myRobot.storage.Length];
        myRobot.storage.CopyTo(currentMolecules, 0);
        for (int j = 0; j < sample.cost.Length; j++)
        {
            currentMolecules[j] -= sample.GetRequired(j);
            if (currentMolecules[j] < 0)
            {
                return false;
            }
        }

        return true;
    }

    static int HaveUndiagnostedSamples()
    {
        int[] currentMolecules = new int[myRobot.storage.Length];
        myRobot.storage.CopyTo(currentMolecules, 0);
        for (int i = 0; i < myCurrentSamples.Count; i++)
        {
            if (myCurrentSamples[i].overallCost <= 0 || myCurrentSamples[i].sac)
            {
                return myCurrentSamples[i].sampleId;
            }
        }
        return -1;
    }

    static char GetRequiredMolecules()
    {
        int[] currentMolecules = new int[myRobot.storage.Length];
        myRobot.storage.CopyTo(currentMolecules, 0);
        for (int i = 0; i < myCurrentSamples.Count; i++)
        {

            for (int j = 0; j < myCurrentSamples[i].cost.Length; j++)
            {
                currentMolecules[j] -= myCurrentSamples[i].GetRequired(j);

                if (currentMolecules[j] < 0)
                {
                    return GlobalVariables.moleculeTypes[j];
                }
            }
        }
        return ' ';
    }

    static int[] GetRequiredMoleculesArr(bool useMyRobot = true)
    {
        Robot tmpR = (useMyRobot ? myRobot:enemyRobot);
        int[] currentMolecules = new int[tmpR.storage.Length];
        tmpR.storage.CopyTo(currentMolecules, 0);
        for (int i = 0; i < myCurrentSamples.Count; i++)
        {

            for (int j = 0; j < myCurrentSamples[i].cost.Length; j++)
            {
                currentMolecules[j] -= (myCurrentSamples[i].GetRequired(j));
            }
        }
        return currentMolecules;
    }

    static int[] GetRequiredMoleculesArr(Sample sample)
    {
        int[] currentMolecules = new int[myRobot.storage.Length];
        sample.GetRobot().storage.CopyTo(currentMolecules, 0);

        for (int j = 0; j < sample.cost.Length; j++)
        {
            currentMolecules[j] -= (sample.GetRequired(j));
        }
        return currentMolecules;
    }

    static int GetReadySamples()
    {
        int[] currentMolecules = new int[myRobot.storage.Length];
        myRobot.storage.CopyTo(currentMolecules, 0);
        bool isDone = true;
        for (int i = 0; i < myCurrentSamples.Count; i++)
        {
            isDone = true;
            for (int j = 0; j < myCurrentSamples[i].cost.Length; j++)
            {
                //WrErr (myCurrentSamples [i].sampleId + "  porownanie " + myCurrentSamples [i].cost [j] + "  " + myCurrentSamples [i].GetRequired (j) + " " + currentMolecules [j]);
                if (myCurrentSamples[i].GetRequired(j) > currentMolecules[j])
                {
                    isDone = false;
                    break;
                }
            }
            if (isDone)
            {
                return myCurrentSamples[i].sampleId;
            }
        }
        return -1;
    }

    static int GetRequiredMoleculesCount(Sample sample = null)
    {
        int[] currentMolecules = sample == null ? GetRequiredMoleculesArr() : GetRequiredMoleculesArr(sample);

        int count = 0;
        for (int i = 0; i < currentMolecules.Length; i++)
        {
            count += Math.Max(-currentMolecules[i], 0);
        }
        return count;
    }

    static int GetReadySamplesCount()
    {
        int[] currentMolecules = new int[myRobot.storage.Length];
        myRobot.storage.CopyTo(currentMolecules, 0);
        bool isDone = true;
        int count = 0;
        for (int i = 0; i < myCurrentSamples.Count; i++)
        {
            isDone = true;
            for (int j = 0; j < myCurrentSamples[i].cost.Length; j++)
            {
                //WrErr(myCurrentSamples[i].sampleId + "  porownanie " + myCurrentSamples[i].cost[j] + "  " + myCurrentSamples[i].GetRequired(j) + " " + currentMolecules[j]);
                if (myCurrentSamples[i].GetRequired(j) > currentMolecules[j])
                {

                    isDone = false;
                    break;
                }
            }
            if (isDone)
            {
                for (int j = 0; j < currentMolecules.Length; j++)
                {
                    currentMolecules[j] -= myCurrentSamples[i].GetRequired(j);
                }
                count++;
            }
        }
        return count;
    }

    static bool CanPickUp(Sample sample)
    {

        if (GetRequiredMoleculesCount(sample) > GlobalVariables.moleculesLimit - myRobot.storageUnits || !sample.CanBeExecutedNow())
        {
            return false;
        }
        return true;
    }

    static int GetFutureExpertise()
    {
        return myRobot.myExpertise + myCurrentSamples.Count;
    }

    static Sample GetValidSampleFromDiagnostics()
    {

        for (int i = 0; i < samples.Count; i++)
        {
            if (samples[i].carriedBy == -1)
            {
                if (IsSampleValid(samples[i]) && (myRobot.storageUnits < 9 || AllMoleculesAvailable(samples[i])) )
                {
                    return samples[i];
                }
            }
        }
        return null;
    }

    static int GetValidSampleFromDiagnosticsCount()
    {
        int count = 0;

        for (int i = 0; i < samples.Count; i++)
        {
            if (samples[i].carriedBy == -1)
            {
                if (IsSampleValid(samples[i]) && (myRobot.storageUnits < 9 || AllMoleculesAvailable(samples[i])))
                {
                    count++;
                }
            }
        }
        return count;
    }

    static int GetSamplesInCloud()
    {
        int count = 0;

        for (int i = 0; i < samples.Count; i++)
        {
            if (samples[i].carriedBy == -1)
            {
                Console.Error.WriteLine(samples[i].sampleId + " " + samples[i].rank + "   missingMoleculesCount = " + samples[i].missingMoleculesCount + "   myCost = " + samples[i].myCost);
            }
        }
        return count;
    }


    public static string GetAction()
    {
        WrErr("GetValidSampleFromDiagnosticsCount() " + GetValidSampleFromDiagnosticsCount());

        GetSamplesInCloud();

        int reqMolCount = GetRequiredMoleculesCount();
        //jesli nie mam sampli ktore pozwala mi wygrac,  nie mam zadnych sampli lub i tak jestem przy Samplach a mam mniej niz max (lub w cloudzie sa jakies ciekawe sample)
        if (GetRequiredPoints() > 0 && (myCurrentSamples.Count == 0 || (myCurrentSamples.Count < (GlobalVariables.sampleLimit - GetValidSampleFromDiagnosticsCount()) && myRobot.target == GlobalVariables.samples)
            || (reqMolCount <= 2 && reqMolCount > 0 && myCurrentSamples.Count < 2 && myRobot.target == GlobalVariables.lab))// jesli moje sample wymagaja tylko jednego molekulu to nie ma sensu biec z powrotem
            || myRobot.target == GlobalVariables.diagnosis && (myCurrentSamples.Count < 2 && GetValidSampleFromDiagnosticsCount() <= 0))
        {   //jesli jestem w diagnoostyce i mam mniej niz 2 sample
            if (myRobot.target != GlobalVariables.samples)
            {
                return "GOTO " + GlobalVariables.samples;

            }
            else
            {
                int maxRank = (AreAllProjActive() && GetFutureExpertise() < 16)? 1 : 3;
                //int whatToGet = Math.Max(1, Math.Min(maxRank, (int)((float)(GetFutureExpertise() / 4)))); //GetFutureExpertise() < 6 ? 1 : 3;//
                int whatToGet = GetFutureExpertise() < 6 ? 1 : (GetFutureExpertise() > 11? 3 :2);

                //((GetFutureExpertise () >= 4) ? (int)(((float)myCurrentSamples.Count / 2) + 2f) : 2);//((myCurrentSamples.Count / 2) + 1);
                bool hasLow = false;
                bool allStuck = myCurrentSamples.Count > 0;

                //zawsze miej jedna zapasowego sampla 2
                for (int i = 0; i < myCurrentSamples.Count; i++)
                {
                    if (!myCurrentSamples[i].stuck)
                    {
                        allStuck = false;
                    }
                    if (myCurrentSamples[i].rank < 3)
                    {
                        hasLow = true;
                        break;
                    }
                }

                if (myRobot.storageUnits >= GlobalVariables.moleculesLimit - 1)
                {
                    whatToGet = 1;
                }
                if (allStuck)
                {
                    whatToGet = (myCurrentSamples.Count == 2) ? 2 : 1;
                }
                return "CONNECT " + whatToGet;
            }
        }
        else
        {
            int undiagnosted = HaveUndiagnostedSamples();
            WrErr(undiagnosted+ " undiagnosted");
            if (undiagnosted != -1)
            {
                if (myRobot.target != GlobalVariables.diagnosis)
                {
                    return "GOTO " + GlobalVariables.diagnosis;

                }
                else
                {
                    return "CONNECT " + undiagnosted;
                }
            }
            else
            {   //jesli mam sample
                Sample cloudSample = GetValidSampleFromDiagnostics();
                if (cloudSample != null && myCurrentSamples.Count < GlobalVariables.sampleLimit && myRobot.target != GlobalVariables.diagnosis && reqMolCount <=2)
                {
                    return "GOTO " + GlobalVariables.diagnosis;
                }

                if (myRobot.target == GlobalVariables.diagnosis)
                {

                    //jesli sample sa niskiej jakosci to je zwroc
                    for (int i = 0; i < myCurrentSamples.Count; i++)
                    {

                        //WrErr(myCurrentSamples[i].sampleId + "sac " + myCurrentSamples[i].sac + "   " + GetRequiredMoleculesCount(myCurrentSamples[i]) + " - " + (GlobalVariables.moleculesLimit - myRobot.storageUnits));
                        //WrErr("sample ratio for " + myCurrentSamples[i].sampleId + " = " + ((float)myCurrentSamples[i].health / (float)myCurrentSamples[i].myCost) + " my cost" + (float)myCurrentSamples[i].myCost);

                        if (!IsSampleValid(myCurrentSamples[i]))
                        {

                            return "CONNECT " + myCurrentSamples[i].sampleId;
                        }
                    }

                    if (myCurrentSamples.Count < GlobalVariables.sampleLimit)
                    {

                        //					//jesli w cloudzie sa jakies ciekawe sample to je pobierz

                        if (cloudSample != null)
                        {

                            return "CONNECT " + cloudSample.sampleId;
                        }
                    }
                }

                char reqMol = GetRequiredMolecules();
                WrErr(GetReadySamplesCount() + " " + (GetReadySamplesCount() < myCurrentSamples.Count) + " " + (myRobot.storageUnits < GlobalVariables.moleculesLimit) + "  " + (!(myRobot.target == GlobalVariables.lab && GetReadySamplesCount() > 0)));
                if (GetReadySamplesCount() < myCurrentSamples.Count && myRobot.storageUnits < GlobalVariables.moleculesLimit && !(myRobot.target == GlobalVariables.lab && GetReadySamplesCount() > 0))
                {// && myRobot.storageUnits < GlobalVariables.moleculesLimit && reqMol != ' ') {	//jesli jeszcze nie mam wszystkich wymaganych molekulow
                    if (myRobot.target != GlobalVariables.molecules)
                    {

                        return "GOTO " + GlobalVariables.molecules;
                    }
                    else
                    {
                        int reservedSpace = 0;
                        int tmpI = -1;
                        Sample tmpJ = null;
                        int[] enemyMols = GetRequiredMoleculesArr(false);
                        
                        for (int j = 0; j < myCurrentSamples.Count; j++)
                        {
                            int[] currentMolecules = GetRequiredMoleculesArr(myCurrentSamples[j]);
                            string toPrint = myCurrentSamples[j].sampleId + " ";
                            for (int i = 0; i < currentMolecules.Length; i++)
                            {
                                for (int k = 0; k < j; k++)
                                {
                                    currentMolecules[i] -= Math.Max(0, myCurrentSamples[k].cost[i] - myRobot.expertise[i]);
                                }
                                toPrint += GlobalVariables.moleculeTypes[i] + "" + currentMolecules[i] + ", ";
                                if (j == 0)
                                {
                                    reservedSpace += Math.Max(0, -currentMolecules[i]);
                                }
                            }
                            WrErr(toPrint);
                            int bestMol = -1;
                            int bestDiff = -1000;
                            int currDiff = 0;
                            int count = GetRequiredMoleculesCount(myCurrentSamples[j]);
                            for (int i = 0; i < currentMolecules.Length; i++)
                            {
                                currDiff = currentMolecules[i] - enemyMols[i];
                                if (currDiff > bestDiff && currentMolecules[i] < 0 && available[i] > 0 && (j == 0 || (j != 0 && ((GlobalVariables.moleculesLimit - (myRobot.storageUnits + reservedSpace)) > 0 || count == 1))))
                                {
                                    bestMol = i;//"CONNECT " + GlobalVariables.moleculeTypes[i];
                                    bestDiff = currDiff;
                                }
                                else
                                {
                                    if (tmpJ != null)
                                    {
                                        tmpJ = myCurrentSamples[j];
                                        if (tmpI != -1)
                                        {
                                            tmpI = i;
                                        }
                                    }
                                }
                            }
                            if (bestMol != -1)
                            {
                                return "CONNECT " + GlobalVariables.moleculeTypes[bestMol];
                            }
                        }

                        WrErr("reservedSpace " + reservedSpace + "   molecule " + (tmpI >= 0 ? GlobalVariables.moleculeTypes[tmpI] : 'z') + "  for sample " + (tmpJ != null ? tmpJ.sampleId : -1));
                        if (GetReadySamplesCount() > 0)
                        {
                            return LaboratoryState();
                        }
                        return ResolveWait();
                    }

                }
                else
                {   //jedz wygenerowac lek
                    return LaboratoryState();
                }
            }
        }
        WrErr("---DONT KNOW WHAT TO DO");
        return "GOTO " + GlobalVariables.molecules;

    }

    static string ResolveWait()
    {

        if (GetRequiredPoints() > 0 && (waitCounter > GlobalVariables.maxWaitCounter || (enemyRobot.target != GlobalVariables.lab && enemyRobot.target != GlobalVariables.molecules && waitCounter > 2)))
        {
            if (myCurrentSamples.Count < GlobalVariables.sampleLimit && GetValidSampleFromDiagnosticsCount() == 0)
            {
                for (int i = 0; i < myCurrentSamples.Count; i++)
                {
                    WrErr("====" + myCurrentSamples[i].sampleId + "  set stuck " + myCurrentSamples[i].stuck);
                    myCurrentSamples[i].stuck = true;
                }

                return "GOTO " + GlobalVariables.samples;
            }
            else
            {
                for (int i = 1; i < myCurrentSamples.Count; i++)
                {
                    myCurrentSamples[i].sac = true;
                }

                return "GOTO " + GlobalVariables.diagnosis;
            }
        }
        return "WAIT";
    }

    static string LaboratoryState()
    {
        if (GetReadySamplesCount() > 0)
        {

            if (myRobot.target != GlobalVariables.lab)
            {
                return "GOTO " + GlobalVariables.lab;
            }
            else
            {

                bool canGetMore = true;
                for (int i = 0; i < enemySamples.Count; i++)
                {
                    int[] mols = GetRequiredMoleculesArr(enemySamples[i]);
                    for (int j = 0; j < mols.Length; j++)
                    {
                        if (mols[j] < 0 && available[j] < Math.Abs(mols[j]))
                        {
                            WrErr(enemySamples[i].sampleId+"    "+ mols[j]+" "+ (available[j] > Math.Abs(mols[j])));
                            canGetMore = false;
                            break;
                        }
                    }
                    if (!canGetMore)
                    {
                        break;
                    }
                }
                if (myRobot.score <= enemyRobot.score || canGetMore)
                {
                    for (int i = 0; i < myCurrentSamples.Count; i++)
                    {
                        if (HaveRequiredMolecules(myCurrentSamples[i]))
                        {
                            return "CONNECT " + myCurrentSamples[i].sampleId;
                        }
                    }
                }
                else
                {
                    waitCounter--;
                    return "WAIT";

                }
            }
        }
        return ResolveWait();
    }

    static string DiagnosticState()
    {
        if (GetReadySamplesCount() > 0)
        {

            if (myRobot.target != GlobalVariables.lab)
            {
                return "GOTO " + GlobalVariables.lab;
            }
            else
            {
                for (int i = 0; i < myCurrentSamples.Count; i++)
                {
                    if (HaveRequiredMolecules(myCurrentSamples[i]))
                    {
                        return "CONNECT " + myCurrentSamples[i].sampleId;
                    }
                }
            }
        }
        return "WAIT";
    }

    static string SampleState()
    {
        if (GetReadySamplesCount() > 0)
        {

            if (myRobot.target != GlobalVariables.lab)
            {
                return "GOTO " + GlobalVariables.lab;
            }
            else
            {
                for (int i = 0; i < myCurrentSamples.Count; i++)
                {
                    if (HaveRequiredMolecules(myCurrentSamples[i]))
                    {
                        return "CONNECT " + myCurrentSamples[i].sampleId;
                    }
                }
            }
        }
        return "WAIT";
    }

    static string MoleculeState()
    {
        if (GetReadySamplesCount() > 0)
        {

            if (myRobot.target != GlobalVariables.lab)
            {
                return "GOTO " + GlobalVariables.lab;
            }
            else
            {
                for (int i = 0; i < myCurrentSamples.Count; i++)
                {
                    if (HaveRequiredMolecules(myCurrentSamples[i]))
                    {
                        return "CONNECT " + myCurrentSamples[i].sampleId;
                    }
                }
            }
        }
        return "WAIT";
    }

    public static void WrErr(string msg)
    {
        Console.Error.WriteLine(msg);
    }
}