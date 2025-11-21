# PN-DSQG: An Accelerated RL Scheduling Framework of RCMSs with PTPNs
This repository provides an accelerated scheduling framework for **robotic cellular manufacturing systems (RCMSs)**, built upon **place-timed Petri nets (PTPNs)** and an improved **reinforcement learning** framework.

Compared with traditional approaches, **PN-DSQG** significantly improves both learning speed and solution quality:<br>
--**A star search algorithm with PTPNs**can find optimal solutions but requires carefully designed heuristics, leading to high computation cost and limited scalability;<br>
--**Dispatching-rule-based algorithms (e.g., FIFO, SRPT)**are fast but often produce unfavorable schedules.<br>
--**Conventional PN-based RL methods**prove to be a desirable self-optimized technique but suffer from slow convergence and suboptimal performance for real-world RCMS scheduling.<br>

To address these issues, we propose PN-DSQG.
It begins by modeling the RCMSs using generalized PTPNs. Based on this model, we incorporate **double speedy Q-learning** with PTPN reinforcement learning environment, which introduces a momentum term into the Bellman operator for faster convergence. Furthermore, a **gradient-adaptive successive over-relaxation (GSOR)** mechanism operates under a two-timescale learning framework. GSOR parameters are optimized on a slow timescale using a one-simulation simultaneous perturbation stochastic approximation technique, driven by temporal-difference error. Meanwhile, Q-values are updated iteratively on a fast timescale, ensuring stability and creating a synergistic acceleration effect. Most importantly, PN-DSQG accelerates learning without sacrificing solution quality, making it suitable for practical RCMS scheduling scenarios.
