using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nobody.RLPetri.Test
{
    class MainClass
    {
        [STAThread]
        static void Main(string[] args)
        {         

            Kyle.RLPetri.PNQL qL;
            Kyle.RLPetri.PNQLNode goalNode;
            Kyle.RLPetri.PNQLNode startNode;

            string fileName = "Example1_11";

            string[] initialMFile = new string[] { "PNfiles/" + fileName + "_init.txt" };
            string[] incidenceMatrixFile = new string[] { "PNfiles/" + fileName + "_matrix.txt" };

            List<int> Makespan = new List<int>();
            List<double> tTime = new List<double>();
            List<double> sTime = new List<double>();

            decimal discountrate = 0.3M;
            decimal PeReward = -10000;
            int Epi = 30000;
            bool use_gsor = true;
            decimal omega_static = 1.15M;
            decimal omega_learning_rate = 0.05M;
            decimal omega_perturb_magnitude = 0.03M;

            qL = new Nobody.RLPetri.PNQL(initialMFile[0], incidenceMatrixFile[0]);
            goalNode = new Nobody.RLPetri.PNQLNode(null, null, PNQL.GoalM, PNQL.GoalR, 0, -1, 0, 0);
            startNode = new Nobody.RLPetri.PNQLNode(null, goalNode, PNQL.StartM, PNQL.StartR, 0, 0, 0, 0);
            string algorithmName = use_gsor ? "PNDSQG" : "PNDSQ";
            string makespanLogFile = $"Total_Makespan_{algorithmName}_{fileName}.csv";
            Console.WriteLine();
            using (StreamWriter sw2 = new StreamWriter(makespanLogFile))
            {
                for (int i = 1; i <= 1; i++)
                {
                    Console.WriteLine($"\n---  {ith} training and infering ---");
                    DateTime startTime1 = DateTime.Now;
                    qL.Train_DSGSOR(startNode, goalNode, fileName, Epi, discountrate, PeReward, i,omega_static, use_gsor, omega_learning_rate, omega_perturb_magnitude);
                    DateTime endTime1 = DateTime.Now;
                    TimeSpan trainTime = new TimeSpan(endTime1.Ticks - startTime1.Ticks);
                    double toMs1 = trainTime.TotalMilliseconds;

                    DateTime startTime2 = DateTime.Now;
                    qL.FindPath(startNode, goalNode, fileName);
                    sw2.WriteLine($"{i},{PNQL.makespan}");
                    Makespan.Add(PNQL.makespan);
                    DateTime endTime2 = DateTime.Now;
                    TimeSpan elapsedTime = new TimeSpan(endTime2.Ticks - startTime2.Ticks);
                    double toMs2 = elapsedTime.TotalMilliseconds;
                    tTime.Add(toMs1);
                    sTime.Add(toMs2);
                    Console.WriteLine($"{fileName} Summary | Episodes: {Epi}, Train Time: {trainTime.TotalSeconds:F2}s, Search Time: {elapsedTime.TotalSeconds:F2}s");
                }
                double avgm = Makespan.Average();
                double avgttime = tTime.Average();
                double avgstime = sTime.Average();
                sw2.Flush();

                double targetMakespan = 21;
                double bias = (avgm - targetMakespan) / targetMakespan;
                Console.WriteLine($"{fileName} Search result：average makespan={avgm}, average traintime={avgttime / 1000.0:F2}s, average searchtime={avgstime / 1000.0:F2}s, makespan bias={bias:P2}");
            }
        }
    }
}
