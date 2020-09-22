﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System;
using System.IO;

public class PSOConfig : MonoBehaviour
{
    public enum Algorithm { PSO = 1, SSPSO = 2 };
    public enum Problem { Sphere = 1, Quadric = 2, Hyper = 3, Rastrigin = 4,
                          Griewank = 5, Schaffer = 6, Weierstrass = 7,
                          Ackley = 8, ShiftedQuadricWithNoise = 9, RotatedGriewank = 10,
                          PerlinLandscape = 56 };
    public enum InertiaWeight { Fixed = 0, TVIW = 1 }
    public enum CStrategy { Fixed = 0, TVACPSO = 1 }
    public enum WatershedStrategy { None, WorstSoFar, WorstLastIteration, BestWorst };
    public enum Topology { StaticRing1D, StaticGrid2D, StaticGraph };
    public enum NeightborhoodType { Moore, VN, Ring };

    public int                  functionSamplingSize = 257;
    public int                  maxIterations = 10000;
    public int                  maxEvaluations = 980000;
    public Algorithm            algorithm = Algorithm.PSO;
    public bool                 gBest = false;
    public Problem              problem = Problem.PerlinLandscape;
    public float                xMax = 100;
    public float                vMax = 1;
    public float                chi = 1;
    public float                omega = 0.729844f;
    public float                c = 1.494f;
    public InertiaWeight        weightStrategy = 0;
    public CStrategy            cStrategy = 0;
    public bool                 assymetricInitialization = true;
    [ShowIf("assymetricInitialization")]
    public Vector2              initialX = new Vector2(-100.0f, 100.0f);
    public int                  numExtraRandomNeighbours = 0;
    public float                stopCriterion = 0.01f;
    public bool                 continueAfterStop = false;
    public WatershedStrategy    watershedStrategy = WatershedStrategy.None;
    public Topology             topology = Topology.StaticGrid2D;
    [ShowIf("IsStaticGrid2D")]
    public Vector2Int           dimension = new Vector2Int(7,7);
    [ShowIf("IsStaticGrid2D")]
    public NeightborhoodType    neighborhoodType = NeightborhoodType.VN;
    [ShowIf("IsStaticRing1D")]
    public int                  nParticles = 10;
    [ShowIf("IsStaticRing1D")]
    public float                radius = 1;
    [ShowIf("IsStaticGraph")]
    public string               tgf_filename = "graphs/ring_n10r1.tgf";

    System.Diagnostics.Process psoRun;

    bool IsStaticRing1D()
    {
        return topology == Topology.StaticRing1D;
    }

    bool IsStaticGrid2D()
    {
        return topology == Topology.StaticGrid2D;
    }

    bool IsStaticGraph()
    {
        return topology == Topology.StaticGraph;
    }

    void Awake()
    {
        using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"Assets/PSORun/current.ini"))
        {
            file.WriteLine("[pso]");
            file.WriteLine("");
            file.WriteLine("instrument = 1");
            file.WriteLine("function_points = " + functionSamplingSize);
            file.WriteLine("n_runs = 1");
            file.WriteLine("max_t = " + maxIterations);
            file.WriteLine("max_evaluations = " + maxEvaluations);
            file.WriteLine("algorithm = " + (int)algorithm);
            file.WriteLine("gbest = " + ((gBest)?("1"):("0")));
            file.WriteLine("problem = " + (int)problem);
            file.WriteLine("Xmax = " + xMax);
            file.WriteLine("Vmax = " + vMax);
            file.WriteLine("chi = " + chi);
            file.WriteLine("omega = " + omega);
            file.WriteLine("c = " + c);
            file.WriteLine("numberVariables = 2");
            file.WriteLine("iWeightStrategy = " + (int)weightStrategy);
            file.WriteLine("cStrategy = " + (int)cStrategy);
            file.WriteLine("assyInitialization = " + ((assymetricInitialization) ? ("1") : ("0")));
            file.WriteLine("initialXmin = " + initialX.x);
            file.WriteLine("initialXmax = " + initialX.y);
            file.WriteLine("numExtraRndNeighs = " + numExtraRandomNeighbours);
            file.WriteLine("crit = " + stopCriterion);
            file.WriteLine("crit_keep_going = " + ((continueAfterStop) ? ("1") : ("0")));
            file.WriteLine("bsf_save_period = 1000");
            switch (watershedStrategy)
            {
                case WatershedStrategy.None: file.WriteLine("watershed_strategy = none"); break;
                case WatershedStrategy.WorstSoFar: file.WriteLine("watershed_strategy = worst_so_far"); break;
                case WatershedStrategy.WorstLastIteration: file.WriteLine("watershed_strategy = worst_last_iter"); break;
                case WatershedStrategy.BestWorst: file.WriteLine("watershed_strategy = best_worst"); break;
            }
            file.WriteLine("");
            file.WriteLine("[topology]");
            file.WriteLine("");
            switch (topology)
            {
                case Topology.StaticRing1D:
                    file.WriteLine("type = staticring1d");
                    file.WriteLine("nparticles = " + nParticles);
                    file.WriteLine("radius = " + radius);
                    break;
                case Topology.StaticGrid2D:
                    file.WriteLine("type = staticgrid2d");
                    file.WriteLine("xdim = " + dimension.x);
                    file.WriteLine("ydim = " + dimension.y);
                    switch (neighborhoodType)
                    {
                        case NeightborhoodType.Moore: file.WriteLine("neighbordhood = MOORE"); break;
                        case NeightborhoodType.VN: file.WriteLine("neighbordhood = VN"); break;
                        case NeightborhoodType.Ring: file.WriteLine("neighbordhood = RING"); break;
                        default:
                            break;
                    }
                    break;
                case Topology.StaticGraph:
                    file.WriteLine("type = staticgraph");
                    file.WriteLine("tgf_file = " + tgf_filename);
                    break;
                default:
                    break;
            }
        }

        // Run open PSO
        psoRun = new System.Diagnostics.Process();
        psoRun.StartInfo.FileName = Application.dataPath + "/PSORun/runpso.exe";
        psoRun.StartInfo.Arguments = Application.dataPath + "/PSORun/current.ini";
        psoRun.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        if (psoRun.Start())
        {
            Debug.Log("Running simulation...");

            var psoRender = GetComponent<PSORender>();
            psoRender.enabled = false;
            psoRender.runData = null;
            psoRender.functionData = null;
            psoRender.topologyData = null;
        }
        else
        {
            psoRun = null;
        }
    }

    void Update()
    {
        if (psoRun != null)
        {
            if (psoRun.HasExited)
            {
                if (psoRun.ExitCode == 0)
                {
                    // Get the data, dump into the PSORender and start it
                    Debug.Log("Finished simulation!");

                    var psoRender = GetComponent<PSORender>();
                    psoRender.functionText = File.ReadAllText(Application.dataPath + "/PSORun/current.ini_function.txt");
                    psoRender.runText = File.ReadAllText(Application.dataPath + "/PSORun/current.ini_run0.txt");
                    psoRender.topologyText = File.ReadAllText(Application.dataPath + "/PSORun/current.ini_run0_topology.txt");
                    psoRender.enabled = true;
                }
                else
                {
                    Debug.LogError("Failed to run simulation!");
                }

                Destroy(this);
            }
        }
    }
}