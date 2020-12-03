using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Numerics;
using CsvHelper;

namespace FirstML
{
    class Program
    {
        int total_episodes = 10000;                             // Numer of episodes the policy will be trained on
        int max_steps = 200;                                    // Maximum number of steps each episode lasts
        double startEpsilon = 0.85;                             // Exploration rate, between 0 and 1
        double epsilonDecay = 0.9995;                           // Decay rate of epsilon
        double startAlpha = 0.9;                                // learning rate, between 0 and 1
        double alphaDecay = 0.9995;                             // Decay rate of alpha
        double startGamma = 0.95;                               // Discount factor, between 0 and 1
        double gammaDecay = 0.875;                              // Decay rate of gamma
        double decayThreshold = 75;                            // after this many episodes gamma will start decaying
        int type = 1;                                           // The target's behaviour, 0 = random, 1 = move away from pursuer, 2 = stay along walls as much as possible
        string filename = "trial";
        string path = "./../../../../output/";


        static void Main(string[] args)
        {
            Program p = new Program();

            int length = 10;                                    // Size of the field
            int width = 10;
            int numberAction = 4;

            Wall[] walls = new Wall[4];
            walls[0] = new Wall(new Vector2(0, 3), new Vector2(3, 3));
            walls[1] = new Wall(new Vector2(2, 5), new Vector2(2, 7));
            walls[2] = new Wall(new Vector2(5, 7), new Vector2(8, 7));
            walls[3] = new Wall(new Vector2(6, 1), new Vector2(6, 3));
            Field field = new Field(length, width, walls);             // Initialising the playing field and all the walls
      
            double[,,,,,] qTable = new double[length, width, length, width, 2, numberAction];   // Making q-table and initialising them, [xp, yp, xt, yt, l, a] with (xp, yp) = pursuer location, (xt, yt) = target location, l = bool for line of sight, a = action(eihter north, east, south or west)
            Random randQ = new Random();


            for(int x = 0; x < length; x++)                                 
                for(int y = 0; y < width; y++)
                    for(int x2 = 0; x2 < length; x2++)
                        for(int y2 = 0; y2 < width; y2++)
                            for(int bl = 0; bl < 2; bl++)
                                for (int n = 0; n < numberAction; n++)
                                {
                                    if ((n == 0 && (y - 1) == y2) || (n == 1 && (x + 1) == x2) || (n == 2 && (y + 1) == y2) || (n == 3 && (x - 1) == x2))
                                    {
                                        qTable[x, y, x2, y2, bl, n] = 0;                // Set the q-value of the terminal state to 0
                                    }
                                    else
                                    {
                                        qTable[x, y, x2, y2, bl, n] = 0 - randQ.NextDouble();           
                                    }
                                }



            double gamma = p.startGamma;
            double alpha = p.startAlpha;
            double epsilon = p.startEpsilon;
            double[] allSumRewards = new double[p.total_episodes];
            bool[] caughtResult = new bool[p.total_episodes];
            Vector2[] startLocPursuer = new Vector2[p.total_episodes];
            Vector2[] startLocTarget = new Vector2[p.total_episodes];
            Vector2[] endLocPursuer = new Vector2[p.total_episodes];
            Random ranStart = new Random();
            double pursuerX;
            double pursuerY;
            double targetX;
            double targetY;

            for (int episode = 0; episode < p.total_episodes; episode++)    // Doing all the episodes
            {
                pursuerX = -1;
                pursuerY = -1;
                targetX = -1;
                targetY = -1;

                while (pursuerX == -1 || field.field[(int)pursuerX, (int)pursuerY] == 0)
                {
                    pursuerX = ranStart.Next(0, field.field.GetLength(0));
                    pursuerY = ranStart.Next(0, field.field.GetLength(1));
                }

                Agent pursuer = new Agent(true, 1, 10, new Vector2((int)pursuerX, (int)pursuerY));  // Initialising the pursuer
                field.field[(int)pursuerX, (int)pursuerY] = 2;
                startLocPursuer[episode] = new Vector2((int)pursuerX, (int)pursuerY);

                while (targetX == -1 || field.field[(int)targetX, (int)targetY] == 0)
                {
                    targetX = ranStart.Next(0, field.field.GetLength(0));
                    targetY = ranStart.Next(0, field.field.GetLength(1));
                }

                Agent target = new Agent(true, 1, 10, new Vector2((int)targetX, (int)targetY));  // Initialising the target
                field.field[(int)targetX, (int)targetY] = 3;
                startLocTarget[episode] = new Vector2((int)targetX, (int)targetY);

                (allSumRewards[episode], caughtResult[episode]) = p.episode_simulator(pursuer, target, field, qTable, epsilon, alpha, gamma, episode);
                endLocPursuer[episode] = pursuer.location;

                field.field[(int)pursuer.location.X, (int)pursuer.location.Y] = 0;      // Remove the pursuers location on the field
                field.field[(int)target.location.X, (int)target.location.Y] = 0;        // Remove the targets location on the field
                gamma = p.startGamma;                                                   // Resets gamma

                alpha *= p.alphaDecay;
                epsilon *= p.epsilonDecay;
            }

            p.writeRewards(allSumRewards, caughtResult, startLocPursuer, startLocTarget, endLocPursuer);
                        
        }

        public (double, bool) episode_simulator(Agent pursuer, Agent target, Field field, double[,,,,,] qTable, double e, double a, double g, int episode)
        {
            bool caught = false;
            int step = 0;
            int lineOfSight;
            (int, int, int, int, int) state1;
            (int, int, int, int, int) state2;
            int action1;
            int action2;
            int at;
            double q1;
            double q2;
            double r;
            double sumReward = 0;

            lineOfSight = 0;
            pursuer.lastSeen = target.location;
            target.lastSeen = pursuer.location;

            if (inSight(pursuer.location, target.location, field.walls))     // Check if there is line of sight between the pursuer and the target
            {
                lineOfSight = 1;
            }

            state1 = ((int)pursuer.location.X, (int)pursuer.location.Y, (int)pursuer.lastSeen.X, (int)pursuer.lastSeen.Y, lineOfSight); // The starting state
            action1 = choose_pursuer_action(state1, qTable, e);                                                                         // The first action of the pursuer
            at = choose_target_action(target, lineOfSight, qTable.GetLength(5), type);                                                  // The first action of the target

            while (!caught && step < max_steps)                                          // Loops till the episode is complete or the time is up
            {
                caught = pursuer.Action(action1, field, target);                                  // Do the action of the pursuer
                caught = target.Action(at, field, pursuer);                                       // Do the action of the target

                lineOfSight = 0;
                if (inSight(pursuer.location, target.location, field.walls))             // Check if there is line of sight between the pursuer and the target
                {
                    pursuer.lastSeen = target.location;
                    target.lastSeen = pursuer.location;
                    lineOfSight = 1;
                }

                state2 = ((int)pursuer.location.X, (int)pursuer.location.Y, (int)pursuer.lastSeen.X, (int)pursuer.lastSeen.Y, lineOfSight);     // The new state
                action2 = choose_pursuer_action(state2, qTable, e);                                                                             // The next action the pursuer is going to do
                at = choose_target_action(target, lineOfSight, qTable.GetLength(5), type);                                                      // The next action the target is going to do

                q1 = qTable[state1.Item1, state1.Item2, state1.Item3, state1.Item4, state1.Item5, action1];
                q2 = qTable[state2.Item1, state2.Item2, state2.Item3, state2.Item4, state2.Item5, action2];

                if (caught)
                    r = 10;
                else
                    r = -1;

                qTable[state1.Item1, state1.Item2, state1.Item3, state1.Item4, state1.Item5, action1] = q1 + a * ((r + g * q2) - q1);


                state1 = state2;
                action1 = action2;

                sumReward += r;
                step++;

                if (step > decayThreshold)
                {
                    g *= gammaDecay;                                                   // Decays gamma
                }
            }
            
            return (sumReward, caught);
        }

        public int choose_pursuer_action((int, int, int, int, int) currentState, double[,,,,,] qTable, double eps)      // This function determines the next action the pursuer is going to take
        {
            Random rng = new Random();
            int action = -1;
            double highestQ = double.MinValue;

            if (rng.NextDouble() > eps)                                                                                                            // Determines if the action is exploratory(random) or exploitory(optimal)
            {
                for (int i = 0; i < qTable.GetLength(5); i++)                                                                                          // Looks up every q-value for each action and chooses the action with the highest q-value
                {
                    double q = qTable[currentState.Item1, currentState.Item2, currentState.Item3, currentState.Item4, currentState.Item5, i];
                    if (q > highestQ)
                    {
                        highestQ = q;
                        action = i;
                    }
                }
            }
            else
            {
                action = rng.Next(0, qTable.GetLength(5));    //Use of random ok with this? // Picks a random action
            }

            return action;
        }

        public int choose_target_action(Agent target, int lineOfSight, int numberOfActions, int type)     // This function determines the next action the target is going to take, the tactic it uses depents on type 0 = random, 1 = move away from pursuer, 2 = stay along walls as much as possible
        {
            Random r = new Random();

            if (type == 1)
            {                
                {
                    int deltaX = (int)target.location.X - (int)target.lastSeen.X;
                    int deltaY = (int)target.location.Y - (int)target.lastSeen.Y;

                    if (Math.Abs(deltaX) > Math.Abs(deltaY))
                    {
                        if (deltaX > 0)
                        {
                            return 1;
                        }
                        else
                            return 3;
                    }
                    else
                    {
                        if (deltaY > 0)
                        {
                            return 2;
                        }
                        else
                            return 0;
                    }
                }
            }

            else
            {
                return (r.Next(0, numberOfActions));                              // Target will do a random action
            }
            
        }
        public bool inSight(Vector2 pLoc, Vector2 tLoc, Wall[] walls)           // This function checks if the pursuer and target have line of sight on each other
        {
            bool sight = true;
            float xSmall;
            float xBig;
            float ySmall;
            float yBig;
            
            if (pLoc.X <= tLoc.X)
            {
                xSmall = pLoc.X;
                xBig = tLoc.X;
            }
            else
            {
                xSmall = tLoc.X;
                xBig = pLoc.X;
            }
            if (pLoc.Y <= tLoc.Y)
            {
                ySmall = pLoc.Y;
                yBig = tLoc.Y;
            }
            else
            {
                ySmall = tLoc.Y;
                yBig = pLoc.Y;
            }


            foreach (Wall w in walls)           // Calculates if one of the walls is in the line of sight
            {
                if (w.xBig < xSmall)
                    continue;
                else if (w.xSmall > xBig)
                    continue;
                else if (w.yBig < ySmall)
                    continue;
                else if (w.ySmall > yBig)
                    continue;
                else
                {
                    float a1 = tLoc.Y - pLoc.Y;
                    float b1 = tLoc.X - pLoc.Y;
                    float c1 = a1 * pLoc.X + b1 * pLoc.Y;

                    float a2 = w.endCor.Y - w.startCor.Y;
                    float b2 = w.endCor.X - w.startCor.X;
                    float c2 = a2 * w.startCor.X + b2 * w.startCor.Y;

                    float d = a1 * b2 - a2 * b1;

                    if (d == 0)
                        continue;
                    else
                    {
                        float xIntersect = (b2 * c1 - b1 * c2) / d;
                        float yIntersect = (a1 * c2 - a2 * c1) / d;
                        Vector2 intersection = new Vector2(xIntersect, yIntersect);

                        if (xSmall <= xIntersect && xIntersect <= xBig && ySmall <= yIntersect && yIntersect <= yBig)                   // Checks if the intersection is on both line segments  
                            if (w.xSmall <= xIntersect && xIntersect <= w.xBig && w.ySmall <= yIntersect && yIntersect <= w.yBig)
                                return false;      
                    }
                }
            }
            return sight;
        }


        public void writeRewards(double[] allSumRewards, bool[] caughtResult, Vector2[] startLocPursuer, Vector2[] startLocTarget, Vector2[] endLocPursuer)
        {
            double numberSteps;
            using (var writer = new StreamWriter(path + filename + ".csv"))
            using (var csvWriter = new CsvWriter(writer, System.Globalization.CultureInfo.CurrentCulture))
            {
                csvWriter.Configuration.Delimiter = ",";

                csvWriter.WriteField("alpha");
                csvWriter.WriteField(startAlpha);
                csvWriter.WriteField("alpha decay");
                csvWriter.WriteField(alphaDecay);
                csvWriter.WriteField("epsilon");
                csvWriter.WriteField(startEpsilon);
                csvWriter.WriteField("epsilon decay");
                csvWriter.WriteField(epsilonDecay);
                csvWriter.WriteField("gamma");
                csvWriter.WriteField(startGamma);
                csvWriter.WriteField("gamma decay");
                csvWriter.WriteField(gammaDecay);
                csvWriter.WriteField("gamma threshold decay");
                csvWriter.WriteField(decayThreshold);
                csvWriter.WriteField("max number of steps");
                csvWriter.WriteField(max_steps);
                csvWriter.WriteField("type target");
                csvWriter.WriteField(type);

                csvWriter.NextRecord();

                csvWriter.WriteField("episode");
                csvWriter.WriteField("sum of rewards");
                csvWriter.WriteField("number of steps");
                csvWriter.WriteField("caught");
                csvWriter.WriteField("start location pursuer X");
                csvWriter.WriteField("start location pursuer Y");
                csvWriter.WriteField("start location target X");
                csvWriter.WriteField("start location target Y");
                csvWriter.WriteField("end location pursuer X");
                csvWriter.WriteField("end location pursuer Y");

                csvWriter.NextRecord();

                for (int epi = 0; epi < allSumRewards.Length; epi++)
                {
                    csvWriter.WriteField(epi);
                    csvWriter.WriteField(allSumRewards[epi]);
                    numberSteps = Math.Min(200, -(allSumRewards[epi] - 10));
                    csvWriter.WriteField(numberSteps);
                    csvWriter.WriteField(caughtResult[epi]);
                    csvWriter.WriteField(startLocPursuer[epi].X);
                    csvWriter.WriteField(startLocPursuer[epi].Y);
                    csvWriter.WriteField(startLocTarget[epi].X);
                    csvWriter.WriteField(startLocTarget[epi].Y);
                    csvWriter.WriteField(endLocPursuer[epi].X);
                    csvWriter.WriteField(endLocPursuer[epi].Y);

                    csvWriter.NextRecord();
                }

                writer.Flush();
            }
        }
        
    }
}
