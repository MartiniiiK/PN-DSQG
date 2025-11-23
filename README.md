# PN-DSQG: An Accelerated RL Scheduling Framework of RCMSs with PTPNs
## This repository provides an accelerated scheduling framework for **robotic cellular manufacturing systems (RCMSs)**, built upon **place-timed Petri nets (PTPNs)** and an improved **reinforcement learning** framework.

Compared with traditional approaches, **PN-DSQG** significantly improves both learning speed and solution quality:<br>
--**A star search algorithm with PTPNs**can find optimal solutions but requires carefully designed heuristics, leading to high computation cost and limited scalability;<br>
--**Dispatching-rule-based algorithms (e.g., FIFO, SRPT)**are fast but often produce unfavorable schedules.<br>
--**Conventional PN-based RL methods**prove to be a desirable self-optimized technique but suffer from slow convergence and suboptimal performance for real-world RCMS scheduling.<br>

To address these issues, we propose PN-DSQG.
It begins by modeling the RCMSs using generalized PTPNs. Based on this model, we incorporate **double speedy Q-learning** with PTPN reinforcement learning environment, which introduces a momentum term into the Bellman operator for faster convergence. Furthermore, a **gradient-adaptive successive over-relaxation (GSOR)** mechanism operates under a two-timescale learning framework. GSOR parameters are optimized on a slow timescale using a one-simulation simultaneous perturbation stochastic approximation technique, driven by temporal-difference error. Meanwhile, Q-values are updated iteratively on a fast timescale, ensuring stability and creating a synergistic acceleration effect. Most importantly, PN-DSQG accelerates learning without sacrificing solution quality, making it suitable for practical RCMS scheduling scenarios.

## Program Demonstration
The project is developed in C# using a platform equipped with a 10-core Intel(R) Xeon(R) E5-2673 v4 CPU and 30 GB of RAM.<br>
https://private-user-images.githubusercontent.com/147319599/517396412-cfa2386b-d0f9-4efa-8222-0c785c1f293e.mp4

This demonstration is performed in Visual Studio Code. For a detailed explanation of the proposed algorithm, please refer to our paper.<br>
First, make sure the .NET SDK is properly installed and verified on your test platform, since this project is written in C#. Once the environment is ready, you can compile and run the program.<br>

The project consists of the following components:<br>
--**Main.cs** — The main entry file, where the user parameters are configured.<br>
--**PNQL.cs** — The reinforcement learning module, including the environment setup and agent logic.<br>
--**PNfiles folder** — This folder stores the input files. <br>
  For each PTPN model, two files are required: <br>
  - `xxx_matrix.txt`: contains the transpose of the incidence matrix, where rows represent transitions and columns represent places. <br>
  - `xxx_init.txt`: contains the initial marking, operation times of places, and the target marking. <br>

There are two main types of output files:<br>
--**Total_Makespan_algorithmname_PTPNname_Lotsize.csv** — These record the schedule quality found during inference after each training session, measured by the minimum makespan.<br>
--**training_process_xx.csv** — The number at the end indicates a snapshot taken every 100 training episodes. These files record metrics such as cumulative average rewards and deadlock occurrences.<br>

For any inquiries or issues, please reach out to us at [kylemartiniii@njust.edu.cn](mailto:kylemartiniii@njust.edu.cn).
