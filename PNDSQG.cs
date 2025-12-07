using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nobody.RLPetri
{
    public class PNQLNode
    {
        #region Properties
        public PNQLNode Parent { get; set; }
        public PNQLNode GoalNode { get; set; }
        public decimal gValue { get; set; }
        public int[] M { get; private set; }
        public decimal[,] R { get; private set; }
        public int tFireFrom { get; set; }
        public int[] EnabledTransitions { get; private set; }
        public int MarkingDepth { get; set; }
        public int StateIndex { get; set; }
        public decimal Delt { get; set; }

        private int[] _MF = new int[PNQL.N_P];
        private int[] _MZ = new int[PNQL.N_P];
        private decimal[,] _RF = new decimal[PNQL.N_P, PNQL.MAX_TOK_PA];
        public decimal[,] _RZ = new decimal[PNQL.N_P, PNQL.MAX_TOK_PA];
        #endregion

        #region Constructors
        public PNQLNode(PNQLNode parent, PNQLNode goalNode, int[] M, decimal[,] R, int tFireFrom, int stateindex, decimal markingDepth, decimal Delt)
        {
            Parent = parent;
            GoalNode = goalNode;
            this.M = new int[PNQL.N_P];
            Array.Copy(M, this.M, M.Length);
            this.R = new decimal[PNQL.N_P, PNQL.MAX_TOK_PA];
            Array.Copy(R, this.R, R.Length);
            this.tFireFrom = tFireFrom;
            EnabledTransitions = new int[PNQL.N_T];
            StateIndex = stateindex;
            MarkingDepth = (int)markingDepth;
            this.Delt = Delt;
        }
        #endregion

        #region Public Methods
        public virtual bool IsSameStateM(PNQLNode aNode)
        {
            if (aNode == null) return false;
            if (M.Length != aNode.M.Length) return false;
            for (int i = 0; i < M.Length; ++i)
                if (M[i] != aNode.M[i]) return false;
            return true;
        }

        public virtual bool IsSameStateM_R(PNQLNode aNode)
        {
            if (aNode == null) return false;
            if (M.Length != aNode.M.Length) return false;
            for (int i = 0; i < M.Length; ++i)
                if (M[i] != aNode.M[i]) return false;
            for (int i = 0; i < PNQL.N_P; ++i)
                for (int j = 0; j < PNQL.MAX_TOK_PA; ++j)
                {
                    if (R[i, j] == 0 && aNode.R[i, j] == 0) break;
                    if (R[i, j] != aNode.R[i, j]) return false;
                }
            return true;
        }

        public virtual bool IsDeadLock()
        {
            return this.EnabledTransitions.Sum() == 0;
        }

        public bool IsGoal()
        {
            return IsSameStateM_R(GoalNode);
        }

        public virtual void FindEnabledTransitions()
        {
            int e;
            for (int j = 0; j < PNQL.N_T; ++j)
            {
                e = 1;
                for (int i = 0; i < PNQL.N_P; ++i)
                {
                    if (PNQL.LMINUS[i, j] != 0 && M[i] < PNQL.LMINUS[i, j])
                    {
                        e = 0;
                        break;
                    }
                }
                EnabledTransitions[j] = e;
            }
        }

        public virtual void GetSuccessors(string fileName, ArrayList Successors, ArrayList _States)
        {
            FindEnabledTransitions();

            for (int i = 0; i < EnabledTransitions.Length; ++i)
            {
                if (EnabledTransitions[i] == 1)
                {
                    decimal currentDelt = 0;
                    for (int x = 0; x < PNQL.N_P; ++x)
                    {
                        if (PNQL.LMINUS[x, i] > 0 && PNQL.operationTimes[x] > 0)
                        {
                            if (currentDelt < R[x, M[x] - 1])
                                currentDelt = R[x, M[x] - 1];
                        }
                    }

                    for (int n = 0; n < PNQL.N_P; ++n)
                    {
                        _MZ[n] = (PNQL.LMINUS[n, i] > 0) ? M[n] - PNQL.LMINUS[n, i] : M[n];
                    }

                    for (int n = 0; n < PNQL.N_P; ++n)
                    {
                        _MF[n] = (PNQL.LPLUS[n, i] > 0) ? _MZ[n] + PNQL.LPLUS[n, i] : _MZ[n];
                    }

                    for (int n = 0; n < PNQL.N_P; ++n)
                    {
                        for (int m = 0; m < PNQL.MAX_TOK_PA; ++m)
                        {
                            _RZ[n, m] = Math.Max(0, R[n, m] - currentDelt);
                        }
                    }

                    for (int n = 0; n < PNQL.N_P; ++n)
                    {
                        if (PNQL.LPLUS[n, i] > 0 && PNQL.operationTimes[n] > 0)
                        {
                            for (int j = PNQL.MAX_TOK_PA - 1; j > 0; --j)
                                _RF[n, j] = _RZ[n, j - 1];
                            _RF[n, 0] = PNQL.operationTimes[n];
                        }
                        else
                        {
                            for (int j = 0; j < PNQL.MAX_TOK_PA; ++j)
                                _RF[n, j] = _RZ[n, j];
                        }
                    }

                    PNQLNode newNode = new PNQLNode(this, GoalNode, _MF, _RF, i, 0, MarkingDepth + 1, currentDelt);
                    bool foundSameState = false;

                    foreach (PNQLNode aNode in _States)
                    {
                        if (aNode.IsSameStateM_R(newNode))
                        {
                            newNode.StateIndex = aNode.StateIndex;
                            foundSameState = true;
                            break;
                        }
                    }

                    if (!foundSameState)
                    {
                        newNode.StateIndex = ++PNQL._Index;
                        _States.Add(newNode);
                    }
                    Successors.Add(newNode);

                    if (!PNQL.QtableA.ContainsKey(this.StateIndex) || !PNQL.QtableA[this.StateIndex].ContainsKey(newNode.StateIndex))
                    {
                        PNQL.QtableA.SetQValue(this, newNode.StateIndex, 0);
                    }
                    if (!PNQL.QtableB.ContainsKey(this.StateIndex) || !PNQL.QtableB[this.StateIndex].ContainsKey(newNode.StateIndex))
                    {
                        PNQL.QtableB.SetQValue(this, newNode.StateIndex, 0);
                    }
                }
            }
        }

        public virtual void PrintNodeInfo()
        {
            Console.Write("tFireFrom:{0} Depth:{1}", tFireFrom + 1, MarkingDepth);
            Console.Write(" tEnabled:");
            for (int n = 0; n < EnabledTransitions.Length; ++n)
            {
                if (EnabledTransitions[n] == 1) Console.Write("{0} ", n + 1);
            }
            Console.Write(" M:");
            for (int i = 0; i < M.Length; ++i)
            {
                if (M[i] == 1) Console.Write("{0} ", i + 1);
                if (M[i] > 1) Console.Write("{0}({1}) ", i + 1, M[i]);
            }
            Console.Write(" R:");
            for (int n = 0; n < PNQL.N_P; ++n)
                for (int m = 0; m < PNQL.MAX_TOK_PA; ++m)
                {
                    if (R[n, m] != 0) Console.Write("[{0}，{1}]:{2}  ", n + 1, m + 1, R[n, m]);
                }
            Console.Write(" Index:" + StateIndex);
            Console.WriteLine();
        }
        #endregion
    }

    public sealed class PNQL
    {
        #region Private Fields
        private PNQLNode _StartNode;
        private PNQLNode _GoalNode;
        private ArrayList _Successors;
        private ArrayList _States;
        #endregion

        #region Properties
        public static decimal[] operationTimes;
        public static int[,] LPLUS;
        public static int[,] LMINUS;
        public static int N_P;
        public static int N_T;
        public static int N_P_R;
        public static int MAX_TOK_PA;
        public static int[] StartM;
        public static int[] GoalM;
        public static decimal[,] StartR;
        public static decimal[,] GoalR;
        public static int _Index = 0;
        public static int makespan = 0;
        private Random rand;

        public static QTable QtableA = new QTable();
        public static QTable QtableB = new QTable();
        private static long _globalStepCounter = 0;
        private static Dictionary<int, decimal> _maxQPrevA = new Dictionary<int, decimal>();
        private static Dictionary<int, decimal> _maxQPrevB = new Dictionary<int, decimal>();

        private static decimal _omega_center = 1.0M;
        private static int _perturbationDirection = 1;
        #endregion

        #region Constructors
        public PNQL(string initialMFile, string incidenceMatrixFile)
        {
            _Successors = new ArrayList();
            _States = new ArrayList();
            rand = new Random();
            Read_initfile(initialMFile);
            Read_matrixfile(incidenceMatrixFile);

            Console.WriteLine("The number of places (including resource places)：" + N_P);
            Console.WriteLine("The number of transitions：" + N_T);
            Console.WriteLine("The number of resource places：" + N_P_R);
            Console.WriteLine("The initial marking：");
            for (int i = 0; i < N_P; i++) Console.Write(StartM[i] + " ");
            Console.WriteLine();
            Console.WriteLine("Operation times：");
            for (int i = 0; i < N_P; i++) Console.Write(operationTimes[i] + " ");
            Console.WriteLine();
            Console.WriteLine("The goal marking：");
            for (int i = 0; i < N_P; i++) Console.Write(GoalM[i] + " ");
            Console.WriteLine();
            Console.WriteLine("The post-incidence matrix L+：");
            for (int i = 0; i < N_P; ++i) { for (int j = 0; j < N_T; ++j) Console.Write(LPLUS[i, j] + " "); Console.WriteLine(); }
            Console.WriteLine();
            Console.WriteLine("The pre-incidence matrix L-：");
            for (int i = 0; i < N_P; ++i) { for (int j = 0; j < N_T; ++j) Console.Write(LMINUS[i, j] + " "); Console.WriteLine(); }
            Console.WriteLine();

            StartR = new decimal[N_P, MAX_TOK_PA];
            GoalR = new decimal[N_P, MAX_TOK_PA];
            for (int i = 0; i < N_P; ++i)
                for (int j = 0; j < MAX_TOK_PA; ++j)
                {
                    StartR[i, j] = 0;
                    GoalR[i, j] = 0;
                }
        }
        #endregion

        #region Private Methods (File IO, Unchanged)
        private static void Read_initfile(string fileName)
        {
            StreamReader SR;
            try { SR = File.OpenText(fileName); }
            catch (Exception ex) { Console.Write(fileName + " open failed! " + ex.Message); return; }
            string stringALine;
            string[] stringWords;
            {
                stringALine = SR.ReadLine();
                stringALine = stringALine.Trim();
                while (stringALine == "") { stringALine = SR.ReadLine(); stringALine = stringALine.Trim(); }
                stringWords = System.Text.RegularExpressions.Regex.Split(stringALine, @"\s{1,}");
                N_P = stringWords.Length;
                StartM = new int[N_P];
                for (int i = 0; i < stringWords.Length; ++i) StartM[i] = Convert.ToInt32(stringWords[i]);
            }
            {
                stringALine = SR.ReadLine();
                stringALine = stringALine.Trim();
                while (stringALine == "") { stringALine = SR.ReadLine(); stringALine = stringALine.Trim(); }
                stringWords = System.Text.RegularExpressions.Regex.Split(stringALine, @"\s{1,}");
                operationTimes = new decimal[N_P];
                for (int i = 0; i < stringWords.Length; ++i) operationTimes[i] = Convert.ToInt32(stringWords[i]);
            }
            {
                stringALine = SR.ReadLine();
                stringALine = stringALine.Trim();
                while (stringALine == "") { stringALine = SR.ReadLine(); stringALine = stringALine.Trim(); }
                stringWords = System.Text.RegularExpressions.Regex.Split(stringALine, @"\s{1,}");
                GoalM = new int[N_P];
                for (int i = 0; i < stringWords.Length; ++i) GoalM[i] = Convert.ToInt32(stringWords[i]);
            }
            N_P_R = 0;
            MAX_TOK_PA = 0;
            int maxTokP_R = 0;
            int maxTokP_Start = 0;
            for (int i = 0; i < stringWords.Length; ++i)
            {
                if (StartM[i] != 0 && GoalM[i] != 0 && StartM[i] == GoalM[i])
                {
                    N_P_R++;
                    if (maxTokP_R < StartM[i]) maxTokP_R = StartM[i];
                }
                else if (StartM[i] != 0 && maxTokP_Start < StartM[i]) maxTokP_Start = StartM[i];
            }
            MAX_TOK_PA = (maxTokP_Start <= maxTokP_R) ? maxTokP_Start : maxTokP_R;
            SR.Close();
        }

        private static void Read_matrixfile(string fileName)
        {
            StreamReader SR;
            try { SR = File.OpenText(fileName); } catch { Console.Write(fileName + " open failed!"); return; }
            string stringALine;
            N_T = 0;
            stringALine = SR.ReadLine();
            while (stringALine != null)
            {
                stringALine = stringALine.Trim();
                if (stringALine != "") ++N_T;
                stringALine = SR.ReadLine();
            }
            SR.Close();
            try { SR = File.OpenText(fileName); } catch { Console.Write(fileName + " open failed!"); return; }
            int[,] temp = new int[N_T, N_P];
            string[] stringWords;
            int n = 0;
            stringALine = SR.ReadLine();
            while (stringALine != null)
            {
                stringALine = stringALine.Trim();
                if (stringALine != "")
                {
                    stringWords = System.Text.RegularExpressions.Regex.Split(stringALine, @"\s{1,}");
                    for (int j = 0; j < N_P; ++j) temp[n, j] = Convert.ToInt32(stringWords[j]);
                    n++;
                }
                stringALine = SR.ReadLine();
            }
            SR.Close();
            LPLUS = new int[N_P, N_T];
            LMINUS = new int[N_P, N_T];
            for (int i = 0; i < N_T; ++i)
            {
                for (int j = 0; j < N_P; ++j)
                {
                    LPLUS[j, i] = (temp[i, j] >= 1) ? temp[i, j] : 0;
                    LMINUS[j, i] = (temp[i, j] <= -1) ? -temp[i, j] : 0;
                }
            }
        }

        private void PrintNodeList(object aNodeList)
        {
            Console.WriteLine("\nNode list:");
            int i = 0;
            foreach (PNQLNode n in (aNodeList as IEnumerable)) { Console.Write("{0})\t", i + 1); i++; n.PrintNodeInfo(); }
            Console.WriteLine("=============================================================");
        }
        #endregion

        #region Public Methods (QTable, Strategy, and DSQL Algorithm Logic)

        public class QTable
        {
            private Dictionary<int, Dictionary<int, decimal>> _table;
            public QTable() { _table = new Dictionary<int, Dictionary<int, decimal>>(); }
            public void SetQValue(PNQLNode currentState, int nextStateIndex, decimal qValue)
            {
                int currentStateIndex = currentState.StateIndex;
                if (!_table.ContainsKey(currentStateIndex)) _table[currentStateIndex] = new Dictionary<int, decimal>();
                _table[currentStateIndex][nextStateIndex] = qValue;
            }
            public decimal GetQValue(PNQLNode currentState, int nextStateIndex)
            {
                int currentStateIndex = currentState.StateIndex;
                if (!_table.ContainsKey(currentStateIndex) || !_table[currentStateIndex].ContainsKey(nextStateIndex)) return 0;
                return _table[currentStateIndex][nextStateIndex];
            }
            public decimal GetMaxQValue(PNQLNode currentState)
            {
                int currentStateIndex = currentState.StateIndex;
                if (!_table.ContainsKey(currentStateIndex) || _table[currentStateIndex].Count == 0) return 0;
                return _table[currentStateIndex].Values.Max();
            }
            public bool ContainsKey(int stateIndex) { return _table.ContainsKey(stateIndex); }
            public void Clear() { _table.Clear(); }
            public Dictionary<int, decimal> this[int stateIndex]
            {
                get { return _table.ContainsKey(stateIndex) ? _table[stateIndex] : new Dictionary<int, decimal>(); }
                set { _table[stateIndex] = value; }
            }
        }

        private void UpdateMaxQPrevTables()
        {
            _maxQPrevA.Clear();
            _maxQPrevB.Clear();
            foreach (PNQLNode state in _States)
            {
                if (state.StateIndex >= 0)
                {
                    _maxQPrevA[state.StateIndex] = QtableA.GetMaxQValue(state);
                    _maxQPrevB[state.StateIndex] = QtableB.GetMaxQValue(state);
                }
            }
        }

        public int GetNextStateWithMaxQValue_DoubleQ(PNQLNode currentState)
        {
            decimal maxCombinedQValue = decimal.MinValue;
            List<int> bestNextStates = new List<int>();
            ArrayList potentialSuccessors = new ArrayList();
            currentState.GetSuccessors("", potentialSuccessors, _States);
            if (potentialSuccessors.Count == 0) return -2;

            foreach (PNQLNode successor in potentialSuccessors)
            {
                int nextStateIndex = successor.StateIndex;
                decimal combinedQValue = QtableA.GetQValue(currentState, nextStateIndex) + QtableB.GetQValue(currentState, nextStateIndex);

                if (combinedQValue > maxCombinedQValue)
                {
                    maxCombinedQValue = combinedQValue;
                    bestNextStates.Clear();
                    bestNextStates.Add(nextStateIndex);
                }
                else if (combinedQValue == maxCombinedQValue)
                {
                    bestNextStates.Add(nextStateIndex);
                }
            }
            if (bestNextStates.Count > 0) return bestNextStates[rand.Next(bestNextStates.Count)];
            return ((PNQLNode)potentialSuccessors[rand.Next(potentialSuccessors.Count)]).StateIndex;
        }

        public int GetRandomNextState(ArrayList successors)
        {
            if (successors.Count == 0) return -2;
            return ((PNQLNode)successors[rand.Next(successors.Count)]).StateIndex;
        }

        public double EpsilonGreedyStrategy(int episode, int episodeCount)
        {
            return Math.Pow(0.01, (double)episode / episodeCount);
        }

        public PNQLNode TakeAction(string fileName, PNQLNode CurrentState, ArrayList Successors, ArrayList States, double epsilon)
        {
            CurrentState.GetSuccessors(fileName, Successors, States);
            if (Successors.Count == 0) return CurrentState;

            int nextIndex = (rand.NextDouble() < epsilon) ? GetRandomNextState(Successors) : GetNextStateWithMaxQValue_DoubleQ(CurrentState);
            foreach (PNQLNode aNode in Successors)
            {
                if (aNode.StateIndex == nextIndex) return aNode;
            }
            return (PNQLNode)Successors[rand.Next(Successors.Count)];
        }

        private int GetNextStateWithMaxQValue_SingleTable(PNQLNode state, QTable qTable)
        {
            decimal maxQ = decimal.MinValue;
            int bestActionIndex = -2;
            if (qTable.ContainsKey(state.StateIndex))
            {
                var actions = qTable[state.StateIndex];
                if (actions.Count > 0)
                {
                    foreach (var action in actions)
                    {
                        if (action.Value > maxQ)
                        {
                            maxQ = action.Value;
                            bestActionIndex = action.Key;
                        }
                    }
                }
            }
            return bestActionIndex;
        }

        public void UpdateQValue_DSGSOR(PNQLNode currentState, PNQLNode nextState, decimal reward, decimal discountFactor,
                                  decimal omega_static, bool use_gsor, decimal omega_learning_rate, decimal omega_perturb_magnitude)
        {
            _globalStepCounter++;
            decimal alpha = 0.9M;
            decimal omega;

            if (use_gsor)
            {
                _perturbationDirection = (_globalStepCounter % 2 == 0) ? 1 : -1;
                omega = _omega_center + omega_perturb_magnitude * _perturbationDirection;
            }
            else
            {
                omega = omega_static;
            }
            decimal contractionFactor = 1 - omega + omega * discountFactor;
            QTable qTableToUpdate, qTableView;
            Dictionary<int, decimal> maxQPrevView;
            if (rand.NextDouble() < 0.5)
            {
                qTableToUpdate = QtableA;
                qTableView = QtableB;
                maxQPrevView = _maxQPrevB;
            }
            else
            {
                qTableToUpdate = QtableB;
                qTableView = QtableA;
                maxQPrevView = _maxQPrevA;
            }

            int currentStateIndex = currentState.StateIndex;
            int nextStateIndex = nextState.StateIndex;
            decimal currentQValue = qTableToUpdate.GetQValue(currentState, nextStateIndex);
            int bestNextActionIndex = GetNextStateWithMaxQValue_SingleTable(nextState, qTableToUpdate);
            decimal q_View_for_best_action = (bestNextActionIndex != -2) ? qTableView.GetQValue(nextState, bestNextActionIndex) : 0;

            decimal delta_1 = reward + discountFactor * q_View_for_best_action - currentQValue;
            maxQPrevView.TryGetValue(nextState.StateIndex, out decimal max_q_View_previous);
            decimal max_q_View_current = qTableView.GetMaxQValue(nextState);
            decimal delta_2 = contractionFactor * (max_q_View_current - max_q_View_previous);

            decimal updatedQValue = currentQValue + alpha * delta_1 + (1 - alpha) * delta_2;
            qTableToUpdate.SetQValue(currentState, nextStateIndex, updatedQValue);

            if (use_gsor)
            {
                if (omega_perturb_magnitude * _perturbationDirection == 0) return;
                decimal gradient_estimate = delta_1 / (omega_perturb_magnitude * _perturbationDirection);

                decimal omega_step_size = omega_learning_rate / (decimal)Math.Pow(_globalStepCounter + 1, 1.01);
                _omega_center = _omega_center + omega_step_size * gradient_estimate;

                decimal max_omega = 1.0M / (1.0M - discountFactor) - 0.01M;
                _omega_center = Math.Max(1.0M, Math.Min(_omega_center, max_omega));
            }
        }

        public void Train_DSGSOR(PNQLNode startNode, PNQLNode goalNode, string fileName, int episodeCount, decimal discountrate, decimal Pereward, int trainth,
            decimal omega_static, bool use_gsor, decimal omega_learning_rate, decimal omega_perturb_magnitude)
        {
            _StartNode = startNode;
            _GoalNode = goalNode;
            _Successors.Clear();
            _States.Clear();
            QtableA.Clear();
            QtableB.Clear();
            _maxQPrevA.Clear();
            _maxQPrevB.Clear();
            _globalStepCounter = 0;
            _omega_center = 1.0M;

            _States.Add(goalNode);
            _States.Add(startNode);
            _Index = 0;
            startNode.StateIndex = 0;
            goalNode.StateIndex = -1;

            int Deadlockc = 0;
            List<decimal> aReward = new List<decimal>();
            string algorithmName = use_gsor ? "PNDSQG" : "PNDSQ";

            string resultsDir = Path.Combine("training_results", $"{algorithmName}_{fileName}");
            Directory.CreateDirectory(resultsDir);
            string logFilePath = Path.Combine(resultsDir, $"training_process{trainth}.csv");

            Console.WriteLine($"\nStarting Training with {algorithmName}...");
            using (StreamWriter sw = new StreamWriter(logFilePath))
            {
                for (int episode = 1; episode < episodeCount + 1; episode++)
                {
                    PNQLNode CurrentState = _StartNode;
                    decimal AcReward = 0;
                    UpdateMaxQPrevTables();

                    double epsilon = EpsilonGreedyStrategy(episode, episodeCount);

                    while (!CurrentState.IsSameStateM_R(_GoalNode))
                    {
                        _Successors.Clear();
                        PNQLNode nextState = TakeAction(fileName, CurrentState, _Successors, _States, epsilon);

                        if (nextState == CurrentState && !CurrentState.IsGoal())
                        {
                            UpdateQValue_DSGSOR(CurrentState, nextState, Pereward, discountrate, omega_static, use_gsor, omega_learning_rate, omega_perturb_magnitude);
                            AcReward += Pereward;
                            Deadlockc++;
                            Console.WriteLine("Deadlock encountered");
                            break;
                        }

                        decimal reward = -nextState.Delt;
                        AcReward += reward;
                        UpdateQValue_DSGSOR(CurrentState, nextState, reward, discountrate, omega_static, use_gsor, omega_learning_rate, omega_perturb_magnitude);
                        CurrentState = nextState;
                    }
                    aReward.Add(AcReward);
                    Console.WriteLine($"Episode: {episode}, States: {_States.Count}, Epsilon: {epsilon:F3}, AvgRew: {aReward.Average():F2}, Omega: {_omega_center:F3}");
                    if (episode % 100 == 0)
                    {
                        sw.WriteLine($"{episode / 100},{aReward.Average()},{Deadlockc}");
                        Deadlockc = 0;
                        aReward.Clear();
                    }
                }
            }
        }

        public void FindPath(PNQLNode startNode, PNQLNode goalNode, string fileName)
        {
            _StartNode = startNode;
            _GoalNode = goalNode;
            makespan = 0;
            List<PNQLNode> path = new List<PNQLNode>();
            PNQLNode currentNode = _StartNode;

            Console.WriteLine("\n--- Execution Phase ---");
            while (!currentNode.IsSameStateM_R(_GoalNode))
            {
                path.Add(currentNode);
                _Successors.Clear();
                currentNode.GetSuccessors(fileName, _Successors, _States);
                if (_Successors.Count == 0)
                {
                    Console.WriteLine("!!! DEADLOCK IN EXECUTION !!!");
                    makespan = 9999;
                    break;
                }

                int nextnodeindex = GetNextStateWithMaxQValue_DoubleQ(currentNode);
                PNQLNode nextNode = null;
                foreach (PNQLNode node in _Successors)
                {
                    if (node.StateIndex == nextnodeindex)
                    {
                        nextNode = node;
                        break;
                    }
                }

                if (nextNode == null)
                {
                    nextNode = (PNQLNode)_Successors[rand.Next(_Successors.Count)];
                }

                makespan = (int)(makespan + nextNode.Delt);
                currentNode = nextNode;
            }
            if (currentNode.IsSameStateM_R(_GoalNode)) path.Add(currentNode);

            Console.WriteLine("\nSolution Path Found:");
            foreach (var node in path) node.PrintNodeInfo();
            Console.WriteLine("Total Makespan: " + makespan);
        }
        #endregion
    }
}
