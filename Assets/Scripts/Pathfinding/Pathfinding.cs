using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using System.Diagnostics;


public class Pathfinding : MonoBehaviour
{

    public static Action onProcessingEnd;
    public static bool graphRep = false;
    //public readonly float maxIterationTicks;
    public static float maxIterationTicks =  Time.fixedDeltaTime* Stopwatch.Frequency;
    public static bool repSlowDown;
    public static float waitTime = 0.05f;
    public GameObject objective;

    

    // Metodo che incamera le richieste di pathfinding e sceglie l'algoritmo opportuno cui passarle
    public static void StartPathFinding(PathRequest pathRequest)
    {
        GridNode startNode = Grid.getNodeFromPoint(pathRequest.startPos, pathRequest.grid);
        GridNode targetNode = Grid.getNodeFromPoint(pathRequest.targetPos, pathRequest.grid);

     
        switch (pathRequest.searchType)
        {
            case searchAlgorithm.Astar: StartCoroutine(AstarPathfind(startNode, targetNode, pathRequest.feedback, pathRequest.grid)); break;
            case searchAlgorithm.BFS: StartCoroutine(BFSPathfind(startNode, targetNode, pathRequest.feedback, pathRequest.grid)); break;
            case searchAlgorithm.BFGreedy: StartCoroutine(BFGreedyPathfind(startNode, targetNode, pathRequest.feedback, pathRequest.grid)); break;
            case searchAlgorithm.DFS: StartCoroutine(DFSPathfind(startNode, targetNode, pathRequest.feedback, pathRequest.grid)); break;
            case searchAlgorithm.UniformCost: StartCoroutine(UniformCostPathfind(startNode, targetNode, pathRequest.feedback, pathRequest.grid)); break;
            //case searchAlgorithm.IDDFS: StartCoroutine(IDDFSPathfind(startNode, targetNode, pathRequest.feedback, pathRequest.grid)); break;
            default: UnityEngine.Debug.Log("There'z a been some prob with yer pathfounding, m8"); break; //kek
        }
 
     }

    public static void PathFind(GridNode startNode, GridNode targetNode, GridNode[,] grid, searchAlgorithm searchAlg)  
    {
        Stopwatch sw = new Stopwatch(); // Valutare di separare
        HashSet<GridNode> explored = new();

        IFrontier<GridNode> frontier;
        switch (searchAlg)
        {
            case searchAlgorithm.Astar:
            case searchAlgorithm.BFGreedy:
            case searchAlgorithm.UniformCost: frontier = new HeapFrontier(); break;
            case searchAlgorithm.BFS: frontier = new QueueFrontier(); break;
            case searchAlgorithm.DFS: frontier = new StackFrontier(); break;
            default: return; //Handle error
        }               // Use a separate method to initialize

        if (!targetNode.walkable)
            targetNode = Grid.closestWalkableNode(targetNode);

        sw.Restart();

        frontier.Add(startNode);

        GridNode currentNode;

        while (frontier.Count() > 0)
        {
            currentNode = frontier.Extract();

            explored.Add(currentNode);

            if (currentNode == targetNode)
            {
                sw.Stop();
                // Send results
            }

        }
    }
     

    

    
    public static IEnumerator AstarPathfind(GridNode startNode, GridNode targetNode, Action<Vector3[], bool> feedback, GridNode[,] grid)
    {

        Stopwatch sw = new Stopwatch();                                                 // Variabile per il tracciamento del tempo di computazione
        HashSet<GridNode> explored = new HashSet<GridNode>();                           
        Heap<GridNode> frontier = new Heap<GridNode>(Grid.MaxSize);               // Frontiera implementata tramite min-coda a priorità

        if (!targetNode.walkable)                                                       // Ricerca BFS del più vicino nodo percorribile se quello di
            targetNode = Grid.closestWalkableNode(targetNode);                    // destinazione scelto non lo è

        sw.Restart();
        float frameStartTicks = sw.ElapsedTicks;

        startNode.g_cost = 0;
        frontier.Add(startNode);                                                        // Inizializzazione della frontiera col nodo di partenza

        while (frontier.Count > 0)
        {

            GridNode currentNode = frontier.Extract();                                  // Pop del nodo a f_costo minimo
            if (graphRep) currentNode.nodestate = nodeStateEnum.Current;

            explored.Add(currentNode);                                                  // Aggiunta del nodo corrente agli esplorati
           

            if (currentNode == targetNode)                                              // Risoluzione 
            {
                sw.Stop();
                UnityEngine.Debug.Log("A* path found in " + sw.ElapsedMilliseconds + "ms" + 
                    " with " + explored.Count + "nodes in Explored. " + frontier.Count + "nodes  in Frontier.");
                yield return PathFoundRoutine(startNode, targetNode, feedback);
                yield break;
            }

            
            foreach (GridNode neighbour in Grid.getNeighbors(currentNode, grid))
            {
                if (!explored.Contains(neighbour) && neighbour.walkable == true)
                {

                    int newCost = currentNode.g_cost + Grid.getDistance(currentNode, neighbour) + neighbour.movementPenalty;
                    if (newCost < neighbour.g_cost || !frontier.Contains(neighbour))
                    {
                        neighbour.g_cost = newCost;
                        neighbour.h_cost = Grid.getDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;

                    if (!frontier.Contains(neighbour))
                        {
                            frontier.Add(neighbour);
                            if (graphRep) neighbour.nodestate = nodeStateEnum.Frontier;
                        }

                        else
                        {
                            frontier.UpdateItem(neighbour);

                        }

                    }
                }

            }
            if (graphRep)
            {
                currentNode.nodestate = nodeStateEnum.Explored;
                //yield return null;
            }

            if ((sw.ElapsedTicks - frameStartTicks) > maxIterationTicks)
            {
                yield return null;
                frameStartTicks = sw.ElapsedTicks;
                //framesElasped++;
            }

            if (graphRep)
                if (repSlowDown)
                    yield return new WaitForSeconds(waitTime);
                //else
                //    yield return null;
        }

        PathRequestManager.FinishedProcessing(new PathResult(null, false, null));
            yield return null;
    
}

    public IEnumerator DFSPathfind(GridNode startNode, GridNode targetNode, Action<Vector3[], bool> feedback, GridNode[,] grid)
    {

        
        if (!targetNode.walkable)
        {
            targetNode = Grid.closestWalkableNode(targetNode);
        }

        Stack<GridNode> frontier = new Stack<GridNode>();
        HashSet<GridNode> explored = new HashSet<GridNode>();
        Stopwatch sw = new Stopwatch();
        GridNode currentNode;
        

        
       
        sw.Restart();
        float frameStartTicks = sw.ElapsedTicks;

        frontier.Push(startNode);
        int framesElasped = 0;


        while (frontier.Count > 0)
        {

            currentNode = frontier.Pop();
            if (graphRep) currentNode.nodestate = nodeStateEnum.Frontier;

            if (currentNode == targetNode)
            {
                sw.Stop();
                UnityEngine.Debug.Log("DFS path found in " + sw.ElapsedMilliseconds + "ms" + " with " + explored.Count + "in Explored. "
                    + frontier.Count + " nodes in Frontier. " + framesElasped.ToString() + " frames worked with.");
                yield return PathFoundRoutine(startNode, targetNode, feedback);
                yield break;

            }
            explored.Add(currentNode);
            

            foreach (GridNode neighbor in Grid.GetOrtogonalNeighbors(currentNode, grid))

            {
                //bool deadEnd = true;

                if (!explored.Contains(neighbor) && !frontier.Contains(neighbor) && neighbor.walkable)
                {
                    neighbor.parent = currentNode;
                    if (graphRep) neighbor.nodestate = nodeStateEnum.Frontier;
                    frontier.Push(neighbor);
                    //deadEnd = false;
                }

                //if (deadEnd) { }


            }
            if (graphRep) currentNode.nodestate = nodeStateEnum.Explored;

            if ((sw.ElapsedTicks - frameStartTicks) > maxIterationTicks)
            {
                yield return null;
                frameStartTicks = sw.ElapsedTicks;
                framesElasped++;
            }
            if (graphRep)
                if (repSlowDown)
                    yield return new WaitForSeconds(waitTime);
                //else
                //    yield return null;

        }

        PathRequestManager.FinishedProcessing(new PathResult(new Vector3[0], false, feedback));
        yield return null;

    }
    public IEnumerator BFSPathfind(GridNode startNode, GridNode targetNode, Action<Vector3[], bool> feedback, GridNode[,] grid)
    {


        if (!targetNode.walkable)
        {
            targetNode = Grid.closestWalkableNode(targetNode);
        }

        Queue<GridNode> frontier = new Queue<GridNode>();
        HashSet<GridNode> explored = new HashSet<GridNode>();
        Stopwatch sw = new Stopwatch();
        GridNode currentNode;


        sw.Restart();
        float frameStartTicks = sw.ElapsedTicks;

        frontier.Enqueue(startNode);


        while (frontier.Count > 0)
        {
            currentNode = frontier.Dequeue();
            if (graphRep) currentNode.nodestate = nodeStateEnum.Current;

            if (currentNode == targetNode)
            {
                sw.Stop();
                UnityEngine.Debug.Log("BFS path found in " + sw.ElapsedMilliseconds + "ms" + " with " + explored.Count + "nodes in Explored. " + frontier.Count + " in Frontier.");
                yield return PathFoundRoutine(startNode, targetNode, feedback);
                yield break;

            }
            explored.Add(currentNode);
            

            foreach (GridNode neighbor in Grid.GetOrtogonalNeighbors(currentNode, grid))

            {

                if (!explored.Contains(neighbor) && !frontier.Contains(neighbor) && neighbor.walkable)
                {
                    neighbor.parent = currentNode;
                    frontier.Enqueue(neighbor);
                    if (graphRep) neighbor.nodestate = nodeStateEnum.Frontier;

                }

            }
            if (graphRep)
            {
                currentNode.nodestate = nodeStateEnum.Explored;

            }

            if ((sw.ElapsedTicks - frameStartTicks) > maxIterationTicks)
            {
                yield return null;
                frameStartTicks = sw.ElapsedTicks;
                //framesElasped++;
            }
            if (graphRep)
                if (repSlowDown)
                    yield return new WaitForSeconds(waitTime);
                //else
                //    yield return null;
        }

        PathRequestManager.FinishedProcessing(new PathResult(null, false, null));
        yield return null;


    }
    public IEnumerator BFGreedyPathfind(GridNode startNode, GridNode targetNode, Action<Vector3[], bool> feedback, GridNode[,] grid)
    {
        HashSet<GridNode> explored = new HashSet<GridNode>();
        Heap<GridNode> frontier = new Heap<GridNode>(Grid.MaxSize);
        Stopwatch sw = new Stopwatch();
        sw.Restart();
        float frameStartTicks = sw.ElapsedTicks;



        if (!targetNode.walkable)
            targetNode = Grid.closestWalkableNode(targetNode);

        startNode.h_cost = Grid.getDistance(startNode, targetNode);
        frontier.Add(startNode);

        while (frontier.Count > 0)
        {

            GridNode currentNode = frontier.Extract();
            if (graphRep) currentNode.nodestate = nodeStateEnum.Current;



            if (currentNode == targetNode)
            {
                sw.Stop();
                UnityEngine.Debug.Log("Best First Greedy path found in " + sw.ElapsedMilliseconds + "ms" + " with " + explored.Count + "nodes in Explored. " + frontier.Count + "nodes  in Frontier.");
                yield return PathFoundRoutine(startNode, targetNode, feedback);
                yield break;


            }
            explored.Add(currentNode);
         

            if ((sw.ElapsedTicks - frameStartTicks) > maxIterationTicks)
            {
                yield return null;
                frameStartTicks = sw.ElapsedTicks;
                //framesElasped++;
            }

            List<GridNode> neighbors = Grid.getNeighbors(currentNode, grid);
            foreach (GridNode neighbor in neighbors)
            {
                if (!explored.Contains(neighbor) && neighbor.walkable == true)
                {


                    if (!frontier.Contains(neighbor))
                    {
                        neighbor.g_cost = 0;
                        neighbor.h_cost = Grid.getDistance(neighbor, targetNode);
                        neighbor.parent = currentNode;
                        frontier.Add(neighbor);
                        if (graphRep) neighbor.nodestate = nodeStateEnum.Frontier;

                    }


                }

               
            }
            if (graphRep)
            {
                currentNode.nodestate = nodeStateEnum.Explored;
            }
            if (graphRep)
                if (repSlowDown)
                    yield return new WaitForSeconds(waitTime);
            //else
            //    yield return null;

        }

        PathRequestManager.FinishedProcessing(new PathResult(null, false, null));
        yield return null;
    }
    public IEnumerator UniformCostPathfind(GridNode startNode, GridNode targetNode, Action<Vector3[], bool> feedback, GridNode[,] grid)
    {

        Stopwatch sw = new Stopwatch();
        HashSet<GridNode> explored = new HashSet<GridNode>();
        Heap<GridNode> frontier = new Heap<GridNode>(Grid.MaxSize);


        if (!targetNode.walkable)
            targetNode = Grid.closestWalkableNode(targetNode);

        sw.Restart();
        float frameStartTicks = sw.ElapsedTicks;

        startNode.g_cost = 0;
        frontier.Add(startNode);

        while (frontier.Count > 0)
        {

            GridNode currentNode = frontier.Extract();
            if (graphRep) currentNode.nodestate = nodeStateEnum.Current;

            explored.Add(currentNode);


            if (currentNode == targetNode)
            {
                sw.Stop();
                UnityEngine.Debug.Log("Uniform Cost path found in " + sw.ElapsedMilliseconds + "ms" +
                    " with " + explored.Count + "nodes in Explored. " + frontier.Count + "nodes  in Frontier.");
                yield return PathFoundRoutine(startNode, targetNode, feedback);
                yield break;
            }


            foreach (GridNode neighbour in Grid.getNeighbors(currentNode, grid))
            {
                if (!explored.Contains(neighbour) && neighbour.walkable == true)
                {

                    int newCost = currentNode.g_cost + Grid.getDistance(currentNode, neighbour) + neighbour.movementPenalty;
                    if (newCost < neighbour.g_cost || !frontier.Contains(neighbour))
                    {
                        neighbour.g_cost = newCost;
                        neighbour.parent = currentNode;

                        if (!frontier.Contains(neighbour))
                        {
                            frontier.Add(neighbour);
                            if (graphRep) neighbour.nodestate = nodeStateEnum.Frontier;
                        }

                        else
                        {
                            frontier.UpdateItem(neighbour);

                        }

                    }
                }






            }
            if (graphRep)
            {
                currentNode.nodestate = nodeStateEnum.Explored;
                //yield return null;
            }

            if ((sw.ElapsedTicks - frameStartTicks) > maxIterationTicks)
            {
                yield return null;
                frameStartTicks = sw.ElapsedTicks;
                //framesElasped++;
            }
            if (graphRep)
                if (repSlowDown)
                    yield return new WaitForSeconds(waitTime);
                //else
                //    yield return null;
        }

        PathRequestManager.FinishedProcessing(new PathResult(null, false, null));
        yield return null;

    }
   
    public IEnumerator PathFoundRoutine(GridNode startNode, GridNode targetNode, Action<Vector3[], bool> feedback)
    {
        List<GridNode> path = parentChildPath(startNode, targetNode);
        //Grid.path = path;
        PathRequestManager.FinishedProcessing(new PathResult(pathToWaypoints(path), true, feedback));
        if (graphRep)
        {
            yield return new WaitForSeconds(3f);
            onProcessingEnd?.Invoke();
        }

    }

    public List<GridNode> parentChildPath(GridNode eldest, GridNode youngest)
    {

        List<GridNode> path = new List<GridNode>();
        GridNode currentNode = youngest;
        path.Add(currentNode);
        if (graphRep)
            currentNode.nodestate = nodeStateEnum.Solution;

        while (currentNode != eldest)
        {
            path.Add((GridNode)currentNode.parent);
            if (graphRep)
                currentNode.nodestate = nodeStateEnum.Solution;
            currentNode = (GridNode)currentNode.parent;

        }

        return path;
    }

    public Vector3[] pathToWaypoints(List<GridNode> path)
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
    

}








//// Generic pathfinding algorithm that chooses how to search based on the searchType enum variable
//public IEnumerator PathFind(Vector3 startPos, Vector3 targetPos, searchAlgorithm searchType)
//{
//    // ALGORITHM INITIALIZATION
//    Grid gridScript = GetComponent<Grid>();
//    GridNode startNode = gridScript.getNodeFromPoint(startPos);                 // Translation of 3D coordinates to grid nodes
//    GridNode targetNode = gridScript.getNodeFromPoint(targetPos);

//    if (!targetNode.walkable)                                                   // If target position unwalkable, searches through BFS
//        targetNode = gridScript.closestWalkableNode(targetNode);                // closest walkable node

//    bool success = false;
//    float maxIterationTicks = Time.deltaTime * Stopwatch.Frequency;
//    float frameStartTicks;

//    HashSet<GridNode> explored = new HashSet<GridNode>();
//    ICollection frontier;                                        // Dynamic declaration of the frontier type based on search alg.
//    Stack<GridNode> stackFrontier;
//    Heap<GridNode> heapFrontier = new Heap<GridNode>(gridScript.MaxSize);
//    Queue<GridNode> queueFrontier;

//    switch (searchType)
//    {
//        case searchAlgorithm.Astar:
//        case searchAlgorithm.BFGreedy:
//            frontier = new Heap<GridNode>(gridScript.MaxSize);
//            break;
//        case searchAlgorithm.BFS:
//            frontier = new Queue<GridNode>();
//            break;
//        case searchAlgorithm.DFS:
//            frontier = new Stack<GridNode>();
//            break;
//        default:
//            frontier = new List<GridNode>();
//            break;
//    }

//    GridNode currentNode = startNode;
//    if (graphRep) currentNode.nodestate = nodeStateEnum.Current;


//    Stopwatch sw = new Stopwatch();
//    sw.Start();
//    frameStartTicks = sw.ElapsedTicks;

//    // ALGORITHM FIRST STEPS

//    switch (searchType)
//    {
//        case searchAlgorithm.Astar: heapFrontier.Add(startNode); break;
//        case searchAlgorithm.BFGreedy:
//            Heap<GridNode> tempHeap;
//            tempHeap = (Heap<GridNode>)frontier;
//            tempHeap.Add(startNode);
//            break;

//        case searchAlgorithm.BFS:
//            Queue<GridNode> tempQueue;
//            tempQueue = (Queue<GridNode>)frontier;
//            tempQueue.Enqueue(startNode);
//            break;

//        case searchAlgorithm.DFS:
//            Stack<GridNode> tempStack;
//            tempStack = (Stack<GridNode>)frontier;
//            tempStack.Push(startNode);
//            break;
//    }


//    // ALGORITHM MAIN CYCLE
//    while (frontier.Count > 0 || heapFrontier.Count > 0)
//    {
//        switch (searchType)
//        {
//            case searchAlgorithm.Astar: currentNode = heapFrontier.Extract(); break;
//            case searchAlgorithm.BFGreedy:
//                Heap<GridNode> tempHeap;
//                tempHeap = (Heap<GridNode>)frontier;
//                currentNode = tempHeap.Extract();
//                break;

//            case searchAlgorithm.BFS:
//                Queue<GridNode> tempQueue;
//                tempQueue = (Queue<GridNode>)frontier;
//                currentNode = tempQueue.Dequeue();
//                break;

//            case searchAlgorithm.DFS:
//                Stack<GridNode> tempStack;
//                tempStack = (Stack<GridNode>)frontier;
//                currentNode = tempStack.Pop();
//                break;
//        }

//        currentNode.nodestate = nodeStateEnum.Current;

//        if (currentNode == targetNode)
//        {
//            sw.Stop();
//            String algName;
//            switch (searchType)
//            {
//                case searchAlgorithm.DFS: algName = "DFS"; break;
//                case searchAlgorithm.BFS: algName = "BFS"; break;
//                case searchAlgorithm.BFGreedy: algName = "Best First Greedy"; break;
//                case searchAlgorithm.Astar: algName = "A star"; break;
//                default: algName = "ERROR!?"; break;
//            }
//            UnityEngine.Debug.Log(algName + " path found in " + sw.ElapsedMilliseconds + "ms" + " with " + explored.Count + "in Explored. " + heapFrontier.Count + " nodes in Frontier. " /* + framesElasped.ToString() + " frames worked with."*/);
//            List<GridNode> path = parentChildPath(startNode, targetNode);
//            Vector3[] waypointPath = pathToWaypoints(path);
//            success = true;
//            pathRequestManager.FinishedProcessing(waypointPath, success);
//            if (graphRep)
//                yield return new WaitForSeconds(3f);
//            onProcessingEnd?.Invoke();
//            yield break;
//        }
//        explored.Add(currentNode);
//        if (graphRep)
//            currentNode.nodestate = nodeStateEnum.Explored;
//        List<GridNode> neighbors = gridScript.getNeighbors(currentNode);
//        foreach (GridNode neighbour in neighbors)
//        {
//            if (!explored.Contains(neighbour) && neighbour.walkable)
//            {

//                switch (searchType)
//                {
//                    case searchAlgorithm.BFS:
//                        Queue<GridNode> tempQueue;
//                        tempQueue = (Queue<GridNode>)frontier;
//                        if (!tempQueue.Contains(neighbour))
//                        {
//                            neighbour.parent = currentNode;
//                            tempQueue.Enqueue(neighbour);
//                        }

//                        break;

//                    case searchAlgorithm.DFS:
//                        Stack<GridNode> tempStack;
//                        tempStack = (Stack<GridNode>)frontier;
//                        if (!tempStack.Contains(neighbour))
//                        {
//                            neighbour.parent = currentNode;
//                            tempStack.Push(neighbour);
//                        }

//                        break;

//                    case searchAlgorithm.BFGreedy:
//                        Heap<GridNode> tempHeapGreedy;
//                        tempHeapGreedy = (Heap<GridNode>)frontier;
//                        if (!tempHeapGreedy.Contains(neighbour))
//                        {
//                            neighbour.h_cost = gridScript.getDistance(neighbour, targetNode);
//                            neighbour.parent = currentNode;
//                            tempHeapGreedy.Add(neighbour);
//                        }
//                        break;

//                    case searchAlgorithm.Astar:
//                        int NewGCost = currentNode.g_cost + gridScript.getDistance(currentNode, neighbour);
//                        if (NewGCost < neighbour.g_cost || !heapFrontier.Contains(neighbour))
//                        {
//                            neighbour.g_cost = NewGCost;
//                            neighbour.h_cost = gridScript.getDistance(neighbour, targetNode);
//                            neighbour.parent = currentNode;
//                        }
//                        if (!heapFrontier.Contains(neighbour))
//                        {
//                            heapFrontier.Add(neighbour);
//                        }
//                        else
//                            heapFrontier.UpdateItem(neighbour);
//                        break;

//                }

//                if (graphRep) neighbour.nodestate = nodeStateEnum.Frontier;



//            }
//        }


//        if (graphRep)
//            yield return new WaitForSeconds(0.01f);

//    }

//    pathRequestManager.FinishedProcessing(null, false);
//    yield return null;

//}