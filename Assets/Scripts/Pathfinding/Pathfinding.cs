using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Threading;

public class Pathfinding : MonoBehaviour
{
    public static Action onProcessingBegin;
    public static Action onProcessingEnd;
    public static Action<string, searchAlgorithm> onStatsReady;


    //public readonly float maxIterationTicks;
    public static float maxIterationTicks; //= Time.fixedDeltaTime * Stopwatch.Frequency
    public static float waitTime = 0.05f;
    public GameObject objective;

    [SerializeField]
    public static int beamSearchK = 2;



    // Metodo che incamera le richieste di pathfinding e sceglie l'algoritmo opportuno cui passarle
    public static void StartPathFinding(PathRequest pathRequest)
    {
        Thread tentativeThread;

        GridNode startNode = Grid.getNodeFromPoint(pathRequest.startPos, pathRequest.grid);                 // Conversione di coordinate di spazio continuo in nodi griglia
        GridNode targetNode = Grid.getNodeFromPoint(pathRequest.targetPos, pathRequest.grid);
        Stopwatch sw = new();
        List<GridNode> solutionPath = null;                                                                 // Lista di nodi del cammino trovato da passare all'attuatore
        if (!PresentationLayer.GraphRep)
        {
            tentativeThread = new Thread(() =>                                                              // Asincronia grezza per catturare Stack Overflow senza interruzione esecuzione
            {
                try
                {
                    if (pathRequest.searchType == searchAlgorithm.RecursiveDFS || pathRequest.searchType == searchAlgorithm.IDDFS || pathRequest.searchType == searchAlgorithm.RBFS)
                        solutionPath = RecursivePathFind(startNode, targetNode, pathRequest.grid, pathRequest.searchType);
                    else
                        solutionPath = PathFind(startNode, targetNode, pathRequest.grid, pathRequest.searchType);

                }
                catch (StackOverflowException e)
                {
                    Debug.Log("StackOverflow verificata");
                }
            });

            tentativeThread.Start();
            tentativeThread.Join();
        }
        else
        {
            if (pathRequest.searchType == searchAlgorithm.RecursiveDFS || pathRequest.searchType == searchAlgorithm.IDDFS || pathRequest.searchType == searchAlgorithm.RBFS)
                solutionPath = RecursivePathFind(startNode, targetNode, pathRequest.grid, pathRequest.searchType);
            else
                solutionPath = PathFind(startNode, targetNode, pathRequest.grid, pathRequest.searchType);
        }
        if (solutionPath != null)
            PathRequestManager.FinishedProcessing(new PathResult(pathToWaypoints(solutionPath), true, pathRequest));
        else
            PathRequestManager.FinishedProcessing(new PathResult(null, false, pathRequest));

        return;
    }


    // Sistema di delegati per favorire il riuso del codice

    private delegate void startNodeInitialization();
    private delegate void ExplorationPolicy(GridNode currentNode, GridNode neighbour, ExplorationInfo explorationInfo);
    private delegate void PerFrontierNodePolicy(GridNode currentNode, ExplorationInfo explorationInfo);
    private delegate bool RecursivePolicy(GridNode currentNode, ExplorationInfo explorationInfo, int param);


    private class ExplorationInfo                                       // Classe ausiliaria contenente pacchetti di informazioni da passare ai delegati
    {
        public GridNode startNode { get; set; }
        public GridNode targetNode { get; set; }
        public GridNode[,] grid { get; set; }
        public IFrontier<GridNode> frontier { get; set; }
        public ICollection<GridNode> explored { get; set; }
        public Dictionary<(int, int), NodeLabels> nodeTable { get; set; }

        public SortedList<int, GridNode> beamSearchList;



        public ExplorationInfo(GridNode targetNode, IFrontier<GridNode> frontier, ICollection<GridNode> explored, Dictionary<(int, int), NodeLabels> nodeTable)
        {
            this.targetNode = targetNode;
            this.frontier = frontier;
            this.explored = explored;
            this.nodeTable = nodeTable;
            // BeamSearchOnly
            beamSearchList = new SortedList<int, GridNode>(new DuplicateKeyComparer<int>());

        }

        public ExplorationInfo(GridNode targetNode, GridNode[,] grid, ICollection<GridNode> explored, Dictionary<(int, int), NodeLabels> nodeTable)
        {
            this.targetNode = targetNode;
            this.grid = grid;
            this.explored = explored;
            this.nodeTable = nodeTable;
        }
        public void Unpackage(out GridNode targetNode, out IFrontier<GridNode> frontier, out ICollection<GridNode> explored, out Dictionary<(int, int), NodeLabels> nodeTable)
        {

            targetNode = this.targetNode;
            frontier = this.frontier;
            explored = this.explored;
            nodeTable = this.nodeTable;
        }

        public void Unpackage(out GridNode targetNode, out GridNode[,] grid, out ICollection<GridNode> explored, out Dictionary<(int, int), NodeLabels> nodeTable)
        {

            targetNode = this.targetNode;
            grid = this.grid;
            explored = this.explored;
            nodeTable = this.nodeTable;
        }
    }

    private class NodeLabels : IComparable<NodeLabels>                                      // Etichette dei nodi da mantenere uniche e circostanziali per ogni richiesta di pathfinding
    {
        public Int16 depth = Int16.MaxValue;
        public int g_cost = 0;
        public int h_cost = 0;
        public int f_cost => g_cost + h_cost;
        public int F_cost = 0;
        public GridNode parent;

        public NodeLabels()
        {
            depth = short.MaxValue;
        }

        public int CompareTo(NodeLabels otherNL)
        {
            int f_value = Mathf.Max(f_cost, F_cost);
            int other_f = Mathf.Max(otherNL.f_cost, otherNL.F_cost);
            if (f_value != other_f)
                return (f_value.CompareTo(other_f));

            return h_cost.CompareTo(otherNL.h_cost);
        }
    }


    private static void AStarPolicy(GridNode currentNode, GridNode neighbour, ExplorationInfo expInfo)
    {
        expInfo.Unpackage(out GridNode targetNode, out IFrontier<GridNode> frontier, out ICollection<GridNode> explored, out Dictionary<(int, int), NodeLabels> nodeTable);


        if (!explored.Contains(neighbour) && neighbour.walkable == true)
        {
            int newCost = nodeTable[currentNode.GridXY].g_cost + Grid.getDistance(currentNode, neighbour) + neighbour.movementPenalty;
            if (!nodeTable.ContainsKey(neighbour.GridXY) || (nodeTable.ContainsKey(neighbour.GridXY) && newCost < nodeTable[neighbour.GridXY].g_cost))
            {
                if (!nodeTable.ContainsKey(neighbour.GridXY))
                {
                    nodeTable.Add(neighbour.GridXY, new NodeLabels());
                }
                nodeTable[neighbour.GridXY].g_cost = newCost;
                nodeTable[neighbour.GridXY].h_cost = Grid.getDistance(neighbour, targetNode);
                nodeTable[neighbour.GridXY].parent = currentNode;

                // TENTATIVE
                neighbour.g_cost = newCost;
                neighbour.h_cost = nodeTable[neighbour.GridXY].h_cost;

                if (!frontier.Contains(neighbour))
                {
                    frontier.Add(neighbour, nodeTable[neighbour.GridXY].f_cost, nodeTable[neighbour.GridXY].h_cost);
                    if (PresentationLayer.GraphRep) neighbour.nodestate = nodeStateEnum.Frontier;
                }
                else
                {
                    frontier.UpdateItem(neighbour, nodeTable[neighbour.GridXY].f_cost);
                    if (PresentationLayer.GraphRep) neighbour.nodestate = nodeStateEnum.Frontier;
                }
            }

        }

    }

    private static void BeamSearchCleanup(GridNode currentNode, ExplorationInfo expInfo)
    {
        expInfo.Unpackage(out GridNode targetNode, out IFrontier<GridNode> frontier, out ICollection<GridNode> explored, out Dictionary<(int, int), NodeLabels> nodeTable);
        SortedList<int, GridNode> beamList = expInfo.beamSearchList;
        for (int i = 0; i < beamSearchK && i < beamList.Count; i++)
        {
            GridNode candidateNode = beamList.Values[i];
            frontier.Add(candidateNode, nodeTable[candidateNode.GridXY].f_cost, nodeTable[candidateNode.GridXY].h_cost);
            if (PresentationLayer.GraphRep) candidateNode.nodestate = nodeStateEnum.Frontier;
        }
        expInfo.beamSearchList.Clear();
    }

    private static void BeamSearchPolicy(GridNode currentNode, GridNode neighbour, ExplorationInfo expInfo)
    {
        expInfo.Unpackage(out GridNode targetNode, out IFrontier<GridNode> frontier, out ICollection<GridNode> explored, out Dictionary<(int, int), NodeLabels> nodeTable);
        SortedList<int, GridNode> beamList = expInfo.beamSearchList;


        if (!explored.Contains(neighbour) && neighbour.walkable == true)
        {
            int newCost = nodeTable[currentNode.GridXY].g_cost + Grid.getDistance(currentNode, neighbour) + neighbour.movementPenalty;
            if (!nodeTable.ContainsKey(neighbour.GridXY) || (nodeTable.ContainsKey(neighbour.GridXY) && newCost < nodeTable[neighbour.GridXY].g_cost))
            {
                if (!nodeTable.ContainsKey(neighbour.GridXY))
                {
                    nodeTable.Add(neighbour.GridXY, new NodeLabels());
                }
                nodeTable[neighbour.GridXY].g_cost = newCost;
                nodeTable[neighbour.GridXY].h_cost = Grid.getDistance(neighbour, targetNode);
                nodeTable[neighbour.GridXY].parent = currentNode;



                // TENTATIVE
                neighbour.g_cost = newCost;
                neighbour.h_cost = nodeTable[neighbour.GridXY].h_cost;



                if (!frontier.Contains(neighbour))
                {
                    beamList.Add(nodeTable[neighbour.GridXY].f_cost, neighbour);
                }
                else
                {
                    frontier.UpdateItem(neighbour, nodeTable[neighbour.GridXY].f_cost);
                    if (PresentationLayer.GraphRep) neighbour.nodestate = nodeStateEnum.Frontier;
                }
            }

        }

    }

    private static void BFGreedyPolicy(GridNode currentNode, GridNode neighbour, ExplorationInfo expInfo)
    {
        expInfo.Unpackage(out GridNode targetNode, out IFrontier<GridNode> frontier, out ICollection<GridNode> explored, out Dictionary<(int, int), NodeLabels> nodeTable);

        if (!explored.Contains(neighbour) && neighbour.walkable == true)
        {
            if (!nodeTable.ContainsKey(neighbour.GridXY))
            {
                nodeTable.Add(neighbour.GridXY, new NodeLabels());

                nodeTable[neighbour.GridXY].h_cost = Grid.getDistance(neighbour, targetNode);
                nodeTable[neighbour.GridXY].parent = currentNode;

                neighbour.g_cost = 0;
                neighbour.h_cost = nodeTable[neighbour.GridXY].h_cost;

                if (!frontier.Contains(neighbour))
                {
                    frontier.Add(neighbour, nodeTable[neighbour.GridXY].h_cost);
                    if (PresentationLayer.GraphRep) neighbour.nodestate = nodeStateEnum.Frontier;
                }
            }

        }
    }

    private static void UniformCostPolicy(GridNode currentNode, GridNode neighbour, ExplorationInfo expInfo)
    {
        expInfo.Unpackage(out GridNode targetNode, out IFrontier<GridNode> frontier, out ICollection<GridNode> explored, 
            out Dictionary<(int, int), NodeLabels> nodeTable);
        
        /* L'algoritmo verifica il costo di cammino per il vicino. Se incontrato per la prima volta, lo etichetta 
         * opportunamente e lo inserisce in frontiera. Altrimenti aggiorna la frontiera solo se ha trovato un costo
         * di cammino inferiore rispetto a prima. */

        if (!explored.Contains(neighbour) && neighbour.walkable == true)
        {
            int newCost = nodeTable[currentNode.GridXY].g_cost + Grid.getDistance(currentNode, neighbour) + 
                neighbour.movementPenalty;
            if (!nodeTable.ContainsKey(neighbour.GridXY) || (nodeTable.ContainsKey(neighbour.GridXY) 
                && newCost < nodeTable[neighbour.GridXY].g_cost))
            {
                if (!nodeTable.ContainsKey(neighbour.GridXY))
                {
                    nodeTable.Add(neighbour.GridXY, new NodeLabels());
                }
                nodeTable[neighbour.GridXY].g_cost = newCost;
                nodeTable[neighbour.GridXY].parent = currentNode;

                neighbour.g_cost = newCost;

                if (!frontier.Contains(neighbour))
                {
                    frontier.Add(neighbour, nodeTable[neighbour.GridXY].g_cost);
                    if (PresentationLayer.GraphRep) neighbour.nodestate = nodeStateEnum.Frontier;
                }
                else
                {
                    frontier.UpdateItem(neighbour, nodeTable[neighbour.GridXY].g_cost);
                }
            }

        }
    }


    // CODE FOR BFS & DFS
    private static void BasicPolicy(GridNode currentNode, GridNode neighbour, ExplorationInfo expInfo)
    {
        expInfo.Unpackage(out GridNode targetNode, out IFrontier<GridNode> frontier, out ICollection<GridNode> explored,
            out Dictionary<(int, int), NodeLabels> nodeTable);

        /* Se il successore considerato è agibile e non presente nella lista Esplorati o Frontiera, viene
           aggiunto a quest'ultima e l'arco con il nodo corrente salvato in memoria */

        if (!explored.Contains(neighbour) && neighbour.walkable == true)        
        {
            if (!frontier.Contains(neighbour))
            {
                nodeTable.Add(neighbour.GridXY, new NodeLabels());              
                nodeTable[neighbour.GridXY].parent = currentNode;

                frontier.Add(neighbour);
                if (PresentationLayer.GraphRep) neighbour.nodestate = nodeStateEnum.Frontier;
            }

        }
    }




    public static List<GridNode> PathFind(GridNode startNode, GridNode targetNode, GridNode[,] grid, searchAlgorithm searchAlg)
    {
        // SETUP ALGORITMO

        Stopwatch sw = new Stopwatch();                                     // Cronometro di C# per monitorare l'efficienza temporale del codice
        HashSet<GridNode> explored = new();                                 // Insieme di nodi esauriti durante l'esplorazione dello spazio di ricerca
        IFrontier<GridNode> frontier;                                       // Interfaccia di struttura dati frontiera poi istanziata a runtime
        Dictionary<(int, int), NodeLabels> nodeTable = new();               // Tabella che associa a ogni coordinata nella griglia le etichette del nodo corrispondente
        ExplorationPolicy explorationPolicy;                                // Delegato che determina a runtime come esplorare lo spazio di ricerca
        PerFrontierNodePolicy perNodePolicy = (a, b) => { };                // Delegato di supporto utile per la BeamSearch

        if (!targetNode.walkable)                                           // Nel caso di un nodo di destinazione non valido, restituisce il più vicino
            targetNode = Grid.closestWalkableNode(targetNode);

        sw.Restart();                                                       // Avvio del cronometro

        frontier = ChooseFrontierDataStructure(searchAlg);                  // Inizializzazioni varie basate sull'algoritmo scelto
        explorationPolicy = ChooseExplorationPolicy(searchAlg);
        InitializeFrontier(startNode, targetNode, frontier, nodeTable, searchAlg);

        GridNode currentNode;
        ExplorationInfo explorationInfo = new ExplorationInfo(targetNode, frontier, explored, nodeTable);

        if (searchAlg == searchAlgorithm.BeamSearch)
            perNodePolicy = BeamSearchCleanup;
        if (searchAlg == searchAlgorithm.IDAstar)
        {
            explorationInfo.grid = grid;
            return IterativeDeepeningAStar(startNode, explorationInfo);

        }

        // CORE ALGORITMO

        while (frontier.Count() > 0)
        {
            currentNode = frontier.Extract();
            if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Current;

            if (currentNode == targetNode)                                          // Fintantoché la frontiera non è vuota l'algoritmo ne estrae un nodo e verifica se
            {                                                                       // sia quello obbiettivo. In caso affermativo ricostruisce e comunica il cammino trovato

                sw.Stop();
                List<GridNode> solutionPath = BuildSolutionPath(startNode, targetNode, nodeTable);
                string stats;
                stats = AlgToString(searchAlg) + " path found in " + sw.ElapsedMilliseconds + " ms\n" +
                     + explored.Count + " nodes in Explored. " + frontier.Count() + " nodes  in Frontier.\n" +
                    "Overall walking cost: " + (nodeTable[currentNode.GridXY].g_cost + 10) / 10;
                onStatsReady?.Invoke(stats, searchAlg);
                return solutionPath;

            }

            explored.Add(currentNode);                                              // Altrimenti aggiunge il nodo corrente alla lista Esplorati e aggiunge i vicini
                                                                                    // in frontiera

            foreach (GridNode neighbour in Grid.getNeighbors(currentNode, grid, false))
            {
                explorationPolicy(currentNode, neighbour, explorationInfo);             // Lo fa in maniera variabile a seconda della policy scelta a monte
            }

            perNodePolicy(currentNode, explorationInfo);

            if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Explored;
        }
        return null;
    }

    private static List<GridNode> IterativeDeepeningAStar(GridNode startNode, ExplorationInfo explorationInfo)
    {
        Stopwatch sw = new();
        sw.Restart();
        explorationInfo.Unpackage(out GridNode targetNode, out IFrontier<GridNode> frontier, out ICollection<GridNode> explored, out Dictionary<(int, int), NodeLabels> nodeTable);
        GridNode[,] grid = explorationInfo.grid;

        int minFCost = nodeTable[startNode.GridXY].f_cost;
        int nextBestCutValue = int.MaxValue;
        GridNode currentNode;

        do
        {

            while (frontier.Count() > 0)
            {
                currentNode = frontier.Extract();
                if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Current;

                if (currentNode == targetNode)
                {
                    sw.Stop();
                    UnityEngine.Debug.Log("IDA* path found in " + sw.ElapsedMilliseconds + " ms");
                    string stats;
                    stats = "IDA* path found in " + sw.ElapsedMilliseconds + " ms\n" +
                         +explored.Count + " nodes in Explored. " + frontier.Count() + " nodes  in Frontier.\n" +
                        "Overall walking cost: " + (nodeTable[currentNode.GridXY].g_cost + 10) / 10;
                    onStatsReady?.Invoke(stats, searchAlgorithm.IDAstar);
                    return BuildSolutionPath(startNode, targetNode, nodeTable);
                }

                explored.Add(currentNode);

                foreach (GridNode neighbour in Grid.getNeighbors(currentNode, grid, false))
                {
                    if (!explored.Contains(neighbour) && neighbour.walkable == true)
                    {
                        int newCost = nodeTable[currentNode.GridXY].g_cost + Grid.getDistance(currentNode, neighbour) + neighbour.movementPenalty;
                        if (!nodeTable.ContainsKey(neighbour.GridXY) || (nodeTable.ContainsKey(neighbour.GridXY) && newCost < nodeTable[neighbour.GridXY].g_cost))
                        {
                            if (!nodeTable.ContainsKey(neighbour.GridXY))
                            {
                                nodeTable.Add(neighbour.GridXY, new NodeLabels());
                            }
                            nodeTable[neighbour.GridXY].g_cost = newCost;
                            nodeTable[neighbour.GridXY].h_cost = Grid.getDistance(neighbour, targetNode);
                            nodeTable[neighbour.GridXY].parent = currentNode;

                            // TENTATIVE
                            neighbour.g_cost = newCost;
                            neighbour.h_cost = nodeTable[neighbour.GridXY].h_cost;

                            if (!frontier.Contains(neighbour))
                            {
                                if (nodeTable[neighbour.GridXY].f_cost <= minFCost)
                                {
                                    frontier.Add(neighbour, nodeTable[neighbour.GridXY].f_cost, nodeTable[neighbour.GridXY].h_cost);
                                    if (PresentationLayer.GraphRep) neighbour.nodestate = nodeStateEnum.Frontier;
                                }
                                else
                                {
                                    nextBestCutValue = Mathf.Min(nextBestCutValue, nodeTable[neighbour.GridXY].f_cost);
                                }
                            }


                            else
                            {
                                frontier.UpdateItem(neighbour, nodeTable[neighbour.GridXY].f_cost);
                                if (PresentationLayer.GraphRep) neighbour.nodestate = nodeStateEnum.Frontier;
                            }
                        }
                    }
                }
                if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Explored;
            }

            if (PresentationLayer.GraphRep)
                foreach (GridNode node in explored)
                {
                    node.nodestate = nodeStateEnum.Unexplored;
                }


            if (minFCost == nextBestCutValue)
                break;

            explored.Clear();

            nodeTable.Clear();
            nodeTable.Add(startNode.GridXY, new NodeLabels());
            nodeTable[startNode.GridXY].g_cost = 0;
            nodeTable[startNode.GridXY].h_cost = Grid.getDistance(startNode, targetNode);
            minFCost = nextBestCutValue;
            nextBestCutValue = int.MaxValue;
            frontier.Add(startNode, nodeTable[startNode.GridXY].f_cost, nodeTable[startNode.GridXY].h_cost);

        } while (minFCost < grid.Length / 2);

        return null;
    }

    private static List<GridNode> RecursivePathFind(GridNode startNode, GridNode targetNode, 
        GridNode[,] grid, searchAlgorithm searchAlg)
    {
        Stopwatch sw = new Stopwatch();
        sw.Restart();
        HashSet<GridNode> explored = new();
        Dictionary<(int, int), NodeLabels> nodeTable = new();

        /* Il pathfinding ricorsivo fa uso di un delegato distinto da quello iterativo*/

        RecursivePolicy recursivePolicy = ChooseRecursivePolicy(searchAlg);

        if (!targetNode.walkable)
            targetNode = Grid.closestWalkableNode(targetNode);

        ExplorationInfo explorationInfo = new(targetNode, grid, explored, nodeTable);

        InitializeFrontier(startNode, targetNode, null, nodeTable, searchAlg);

        /* Se la ricorsione trova un cammino valido restituisce true e consente di ricostruirlo
         * tramite la tabella delle etichette dei nodi che ha informazioni sugli archi parent-child */
        if (!recursivePolicy(startNode, explorationInfo, int.MaxValue))
            return null;

        sw.Stop();
        List<GridNode> solutionPath = BuildSolutionPath(startNode, targetNode, nodeTable);
        string stats;
        stats = AlgToString(searchAlg) + " path found in " + sw.ElapsedMilliseconds + "ms\n" +
             +explored.Count + " nodes in Explored.\n" +
            "Overall walking cost: " + (nodeTable[targetNode.GridXY].g_cost + 10) / 10;
        onStatsReady?.Invoke(stats, searchAlg);
        
        return BuildSolutionPath(startNode, targetNode, nodeTable);

    }

    
    private static bool RecursiveDFS(GridNode currentNode, ExplorationInfo expInfo, int additionalInfo)
    {
        expInfo.Unpackage(out GridNode targetNode, out GridNode[,] grid, out ICollection<GridNode> explored,
            out Dictionary<(int, int), NodeLabels> nodeTable);
        if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Current;


        /* Se il nodo corrente è quello obbiettivo, si interrompe la ricorsione con successo. Altrimenti 
           si considerano i vicini del nodo corrente e, se non sono stati già esplorati e sono agibili,
           vengono immediatamente chiamati */

        if (currentNode == targetNode)
            return true;

        explored.Add(currentNode);
        if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Explored;

        foreach (GridNode neighbour in Grid.getNeighbors(currentNode, grid, true))
        {
            if (!explored.Contains(neighbour) && neighbour.walkable == true)
            {
                if (!nodeTable.ContainsKey(neighbour.GridXY))
                    nodeTable.Add(neighbour.GridXY, new NodeLabels());

                nodeTable[neighbour.GridXY].parent = currentNode;
                if (RecursiveDFS(neighbour, expInfo, int.MaxValue))
                    return true;
            }
        }
        return false;
    }

    private static bool RecursiveIDDFS(GridNode currentNode, ExplorationInfo expInfo, int cutValue)
    {
        expInfo.Unpackage(out GridNode targetNode, out GridNode[,] grid, out ICollection<GridNode> explored, 
            out Dictionary<(int, int), NodeLabels> nodeTable);

        if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Current;

        if (currentNode == targetNode)
        {
            return true;
        }

        explored.Add(currentNode);
        if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Explored;

        /* Se si è raggiunto il punto di taglio si interrompe la ricorsione, altrimenti si continua
         * dando priorità ai vicini più distanti dalla radice */

        if (nodeTable[currentNode.GridXY].depth == cutValue)
        {
            return false;
        }

        ICollection<GridNode> neighbours = Grid.getNeighbors(currentNode, grid, true);
        neighbours = OrderNeighboursByDepth(neighbours, currentNode, nodeTable);

        foreach (GridNode neighbour in neighbours)
        {
            if (!explored.Contains(neighbour) && neighbour.walkable == true)
            {
                if (RecursiveIDDFS(neighbour, expInfo, cutValue))
                    return true;
            }
        }

        return false;
    }

    
    private static bool IterativeDeepeningRecDFS(GridNode currentNode, ExplorationInfo expInfo, int cutValue)
    {
        for (int i = 0; i < expInfo.grid.Length; i++)
        {
            cutValue = i;
            if (PresentationLayer.GraphRep)
            {
                foreach (GridNode node in expInfo.explored)
                {
                    node.nodestate = nodeStateEnum.Unexplored;  
                }
            }
            /* A ogni iterazione la lista degli esplorati è ripulita e le informazioni dimenticate */
            expInfo.explored.Clear();

            if (RecursiveIDDFS(currentNode, expInfo, cutValue))
                return true;
        }
        return false;
    }

    private static bool RecursiveBestFirst(GridNode currentNode, ExplorationInfo expInfo, int limit)
    {

        return RecursiveBestFirst2(currentNode, expInfo, int.MaxValue).Item1;
    }

    private static (bool, int) RecursiveBestFirst2(GridNode currentNode, ExplorationInfo expInfo, int f_limit)
    {

        expInfo.Unpackage(out GridNode targetNode, out GridNode[,] grid, out ICollection<GridNode> explored, out Dictionary<(int, int), NodeLabels> nodeTable);

        if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Current;

        if (currentNode == targetNode)
        {
            return (true, f_limit);
        }

        explored.Add(currentNode);
        if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Explored;

        SortedList<NodeLabels, GridNode> successors = new(new DuplicateKeyComparer<NodeLabels>());

        foreach (GridNode neighbour in Grid.getNeighbors(currentNode, grid, false))
        {
            if (!explored.Contains(neighbour) && neighbour.walkable == true)
            {
                int newCost = nodeTable[currentNode.GridXY].g_cost + Grid.getDistance(currentNode, neighbour) + neighbour.movementPenalty;
                if (!nodeTable.ContainsKey(neighbour.GridXY) || (nodeTable.ContainsKey(neighbour.GridXY) && newCost < nodeTable[neighbour.GridXY].g_cost))
                {
                    if (!nodeTable.ContainsKey(neighbour.GridXY))
                    {
                        nodeTable.Add(neighbour.GridXY, new NodeLabels());
                    }
                    nodeTable[neighbour.GridXY].g_cost = newCost;
                    nodeTable[neighbour.GridXY].h_cost = Grid.getDistance(neighbour, targetNode);
                    nodeTable[neighbour.GridXY].parent = currentNode;

                    // TENTATIVE
                    neighbour.g_cost = newCost;
                    neighbour.h_cost = nodeTable[neighbour.GridXY].h_cost;

                }

                int newFCost = Mathf.Max(nodeTable[neighbour.GridXY].f_cost, nodeTable[currentNode.GridXY].F_cost);
                nodeTable[neighbour.GridXY].F_cost = newFCost;
                // TENTATIVE
                neighbour.F_cost = newFCost;
                successors.Add(nodeTable[neighbour.GridXY], neighbour);
            }
        }

        while (successors.Count > 0)
        {
            if (successors.Keys[0].F_cost > f_limit)
            {
                explored.Remove(currentNode);
                if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Unexplored;
                return (false, successors.Keys[0].F_cost);
            }
            int secondBest = int.MaxValue;
            if (successors.Count >= 2)
                secondBest = successors.Keys[1].F_cost;
            GridNode fittest = successors.Values[0];

            if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Explored;
            //successors.RemoveAt(0);
            bool isSuccess;
            (isSuccess, nodeTable[fittest.GridXY].F_cost) = RecursiveBestFirst2(fittest, expInfo, Mathf.Min(f_limit, secondBest));

            if (isSuccess)
                return (true, f_limit);
            fittest.F_cost = nodeTable[fittest.GridXY].F_cost;
            if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Current;

            successors.RemoveAt(0);
            successors.Add(nodeTable[fittest.GridXY], fittest);


        }


        return (false, int.MaxValue);

    }

    public static string AlgToString(searchAlgorithm searchAlg)
    {
        switch (searchAlg)
        {
            case searchAlgorithm.BFGreedy: return "Best-First Greedy";
            case searchAlgorithm.BFS: return "Breadth First Search";
            case searchAlgorithm.Astar: return "A* ";
            case searchAlgorithm.UniformCost: return "Uniform Cost Search";
            case searchAlgorithm.DFS: return "Depth First Search";
            case searchAlgorithm.RecursiveDFS: return "Recursive Depth First Search";
            case searchAlgorithm.IDDFS: return "Iterative Depth First Search";
            case searchAlgorithm.IDAstar: return "Iterative Deepening A*";
            case searchAlgorithm.RBFS: return "Recursive Best First Search";
            case searchAlgorithm.BeamSearch: return "Beam Search";
            default: return "Unknown Algorithm";
        }
    }

    private static IFrontier<GridNode> ChooseFrontierDataStructure(searchAlgorithm searchAlg)
    {
        switch (searchAlg) {
            case searchAlgorithm.Astar:
            case searchAlgorithm.BFGreedy:
            case searchAlgorithm.UniformCost:
            case searchAlgorithm.BeamSearch:
            case searchAlgorithm.IDAstar: return new HeapFrontier();
            case searchAlgorithm.BFS: return new QueueFrontier();
            case searchAlgorithm.DFS: return new StackFrontier();
            default: return null;
        }

    }

    private static ExplorationPolicy ChooseExplorationPolicy(searchAlgorithm searchAlg)
    {
        switch (searchAlg)
        {
            case searchAlgorithm.Astar: return AStarPolicy;
            case searchAlgorithm.BFGreedy: return BFGreedyPolicy;
            case searchAlgorithm.UniformCost: return UniformCostPolicy;
            case searchAlgorithm.BFS:
            case searchAlgorithm.DFS: return BasicPolicy;
            case searchAlgorithm.BeamSearch: return BeamSearchPolicy;
            default: return null;
        }
    }

    private static RecursivePolicy ChooseRecursivePolicy(searchAlgorithm searchAlg)
    {
        switch (searchAlg)
        {
            case searchAlgorithm.RecursiveDFS: return RecursiveDFS;
            case searchAlgorithm.IDDFS: return IterativeDeepeningRecDFS;
            case searchAlgorithm.RBFS: return RecursiveBestFirst;
            default: return null;
        }
    }



    private static void InitializeFrontier(GridNode startNode, GridNode targetNode, IFrontier<GridNode> frontier, Dictionary<(int, int), NodeLabels> nodeTable, searchAlgorithm searchAlg)
    {
        NodeLabels startNodeLabels = new NodeLabels();
        startNodeLabels.g_cost = 0;
        startNodeLabels.depth = 0;
        switch (searchAlg)
        {
            case searchAlgorithm.Astar:
            case searchAlgorithm.BeamSearch:
            case searchAlgorithm.BFGreedy:
            case searchAlgorithm.RBFS:
            case searchAlgorithm.IDAstar: startNodeLabels.h_cost = Grid.getDistance(startNode, targetNode); break;
            default: break;
        }
        if (searchAlg == searchAlgorithm.RBFS)
            startNodeLabels.F_cost = startNodeLabels.f_cost;
        nodeTable.Add(startNode.GridXY, startNodeLabels);
        switch (searchAlg)
        {
            case searchAlgorithm.Astar:
            case searchAlgorithm.BeamSearch:
            case searchAlgorithm.IDAstar: frontier.Add(startNode, nodeTable[startNode.GridXY].f_cost); break;
            case searchAlgorithm.BFGreedy: frontier.Add(startNode, nodeTable[startNode.GridXY].h_cost); break;
            case searchAlgorithm.UniformCost: frontier.Add(startNode, nodeTable[startNode.GridXY].g_cost); break;
            case searchAlgorithm.RecursiveDFS:
            case searchAlgorithm.IDDFS:
            case searchAlgorithm.RBFS: break;
            default: frontier.Add(startNode); break;
        }
        startNode.nodestate = nodeStateEnum.Frontier;

    }


    private static List<GridNode> BuildSolutionPath(GridNode eldest, GridNode youngest, Dictionary<(int, int), NodeLabels> nodeTable)
    {

        List<GridNode> path = new List<GridNode>();
        GridNode currentNode = youngest;
        path.Add(currentNode);
        if (PresentationLayer.GraphRep)
            currentNode.nodestate = nodeStateEnum.Solution;

        while (currentNode != eldest)
        {
            path.Add(nodeTable[currentNode.GridXY].parent);
            currentNode = nodeTable[currentNode.GridXY].parent;
            if (PresentationLayer.GraphRep)
                currentNode.nodestate = nodeStateEnum.Solution;
        }

        if (nodeTable[youngest.GridXY].g_cost == 0)
        {
            nodeTable[eldest.GridXY].g_cost = 0;
            for (int i = path.Count - 1; i > 0; i--)
            {
                nodeTable[path[i - 1].GridXY].g_cost = nodeTable[path[i].GridXY].g_cost + Grid.getDistance(path[i], path[i - 1]) + path[i-1].movementPenalty;
            }
        }

        return path;
    }

    public static Vector3[] pathToWaypoints(List<GridNode> path)
    {
        Vector2 OldDirection = Vector2.zero;
        Vector2 NewDirection;
        List<Vector3> waypointList = new List<Vector3>();
        waypointList.Add(path[0].worldPos);

        for (int i = 1; i < path.Count; i++)
        {
            NewDirection = new Vector2(path[i - 1].GridX - path[i].GridX, path[i - 1].GridY - path[i].GridY);
            if (NewDirection != OldDirection)
                waypointList.Add(path[i].worldPos);
            OldDirection = NewDirection;
        }

        waypointList.Reverse();
        return waypointList.ToArray();


    }

    private static ICollection<GridNode> OrderNeighboursByDepth(ICollection<GridNode> neighbourList, GridNode currentNode, Dictionary<(int, int), NodeLabels> nodeTable)
    {
        SortedList<Int16, GridNode> depthList = new(new DuplicateKeyComparer<Int16>());
        foreach (GridNode neighbour in neighbourList)
        {
            if (!nodeTable.ContainsKey(neighbour.GridXY))
            {
                nodeTable.Add(neighbour.GridXY, new NodeLabels());
            }
            //int newCost = nodeTable[currentNode.GridXY].g_cost + Grid.getDistance(currentNode, neighbour);
            //nodeTable[neighbour.GridXY].g_cost = Mathf.Min(newCost, nodeTable[neighbour.GridXY].g_cost);
            //nodeTable[neighbour.GridXY].parent = currentNode;
            if (nodeTable[neighbour.GridXY].depth > nodeTable[currentNode.GridXY].depth + 1)
            {
                nodeTable[neighbour.GridXY].depth = (short)(nodeTable[currentNode.GridXY].depth + 1);
                neighbour.depth = (short)(nodeTable[currentNode.GridXY].depth + 1);
            }

            if (nodeTable[neighbour.GridXY].parent == null)
                nodeTable[neighbour.GridXY].parent = currentNode;

            depthList.Add(nodeTable[neighbour.GridXY].depth, neighbour);
        }

        List<GridNode> newList = new();
        for (int i = depthList.Values.Count - 1; i >= 0; i--)
        {

            newList.Add(depthList.Values[i]);
        }

        return newList;
    }

    // VALUTARE SE ç_ç
    public static List<GridNode> SMAStarPathFind(GridNode startNode, GridNode targetNode, GridNode[,] grid, searchAlgorithm searchAlg)
    {
        Stopwatch sw = new Stopwatch(); // Valutare di separare
        HashSet<GridNode> explored = new();
        IFrontier<GridNode> frontier;
        Dictionary<(int, int), NodeLabels> nodeTable = new();
        ExplorationPolicy explorationPolicy;
        PerFrontierNodePolicy perNodePolicy;

        if (!targetNode.walkable)
            targetNode = Grid.closestWalkableNode(targetNode);

        sw.Restart();

        frontier = ChooseFrontierDataStructure(searchAlg);
        explorationPolicy = ChooseExplorationPolicy(searchAlg);
        InitializeFrontier(startNode, targetNode, frontier, nodeTable, searchAlg);

        GridNode bestForgotten;



        GridNode currentNode;
        ExplorationInfo explorationInfo = new ExplorationInfo(targetNode, frontier, explored, nodeTable);


        while (frontier.Count() > 0)
        {
            currentNode = frontier.Extract();
            if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Current;

            if (currentNode == targetNode)
            {

                sw.Stop();
                UnityEngine.Debug.Log(AlgToString(searchAlg) + " path found in " + sw.ElapsedMilliseconds + "ms" +
                    " with " + explored.Count + " nodes in Explored. " + frontier.Count() + " nodes  in Frontier.\n" +
                    "Overall walking cost: " + (nodeTable[currentNode.GridXY].g_cost + 10) / 10);
                return BuildSolutionPath(startNode, targetNode, nodeTable);

            }

            explored.Add(currentNode);


            foreach (GridNode neighbour in Grid.getNeighbors(currentNode, grid, false))
            {
                if (!explored.Contains(neighbour) && neighbour.walkable == true)
                {
                    int newCost = nodeTable[currentNode.GridXY].g_cost + Grid.getDistance(currentNode, neighbour) + neighbour.movementPenalty;
                    if (!nodeTable.ContainsKey(neighbour.GridXY) || (nodeTable.ContainsKey(neighbour.GridXY) && newCost < nodeTable[neighbour.GridXY].g_cost))
                    {
                        if (!nodeTable.ContainsKey(neighbour.GridXY))
                        {
                            nodeTable.Add(neighbour.GridXY, new NodeLabels());
                        }
                        nodeTable[neighbour.GridXY].g_cost = newCost;
                        nodeTable[neighbour.GridXY].h_cost = Grid.getDistance(neighbour, targetNode);
                        nodeTable[neighbour.GridXY].parent = currentNode;

                        nodeTable[neighbour.GridXY].F_cost = Mathf.Max(nodeTable[neighbour.GridXY].f_cost, nodeTable[currentNode.GridXY].f_cost);

                        // TENTATIVE
                        neighbour.g_cost = newCost;
                        neighbour.h_cost = nodeTable[neighbour.GridXY].h_cost;

                        if (frontier.Contains(neighbour))
                        {
                            frontier.UpdateItem(neighbour, nodeTable[neighbour.GridXY].F_cost);
                            if (PresentationLayer.GraphRep) neighbour.nodestate = nodeStateEnum.Frontier;
                        }
                        else
                        {
                            if (frontier.Count() < 100)
                            {
                                frontier.Add(neighbour, nodeTable[neighbour.GridXY].F_cost, nodeTable[neighbour.GridXY].h_cost);
                                if (PresentationLayer.GraphRep) neighbour.nodestate = nodeStateEnum.Frontier;
                            }
                            else
                            {
                                frontier.Add(neighbour, nodeTable[neighbour.GridXY].F_cost, nodeTable[neighbour.GridXY].h_cost);
                                if (PresentationLayer.GraphRep) neighbour.nodestate = nodeStateEnum.Frontier;
                            }
                        }

                        if (!frontier.Contains(neighbour) && frontier.Count() < 100)
                        {
                            
                        }
                        else
                        {
                            
                        }
                    }

                }
            }

            

            if (PresentationLayer.GraphRep) currentNode.nodestate = nodeStateEnum.Explored;
        }
        return null;
    }

}