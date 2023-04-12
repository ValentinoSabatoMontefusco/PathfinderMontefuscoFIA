using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using System.Diagnostics;
using System.Runtime.Serialization.Json;

public class Pathfinding : MonoBehaviour
{

    PathRequestManager pathRequestManager;
    public static Action onProcessingEnd;
    public bool riuso = true;
    public bool graphRep = false;

    public void Awake()
    {
        pathRequestManager = GetComponent<PathRequestManager>();
    }
    public void StartPathFinding(Vector3 startPos, Vector3 targetPos, searchAlgorithm searchType)
    {
        if (!riuso)
        {
            switch (searchType)
            {
                case searchAlgorithm.Astar: StartCoroutine(AstarPathfind(startPos, targetPos)); break;
                case searchAlgorithm.BFS: StartCoroutine(BFSPathfind(startPos, targetPos)); break;
                case searchAlgorithm.BFGreedy: StartCoroutine(BFGreedyPathfind(startPos, targetPos)); break;
                case searchAlgorithm.DFS: StartCoroutine(DFSPathfind(startPos, targetPos)); break;
                default: UnityEngine.Debug.Log("There'z a been some prob with yer pathfounding, m8"); break; //kek
            }
        }
        else
            StartCoroutine(PathFind(startPos, targetPos, searchType));
        
        
    }

    // Generic pathfinding algorithm that chooses how to search based on the searchType enum variable
    public IEnumerator PathFind(Vector3 startPos, Vector3 targetPos, searchAlgorithm searchType)
    {
        // ALGORITHM INITIALIZATION
        Grid gridScript = GetComponent<Grid>();                                     
        GridNode startNode = gridScript.getNodeFromPoint(startPos);                 // Translation of 3D coordinates to grid nodes
        GridNode targetNode = gridScript.getNodeFromPoint(targetPos);

        if (!targetNode.walkable)                                                   // If target position unwalkable, searches through BFS
            targetNode = gridScript.closestWalkableNode(targetNode);                // closest walkable node

        bool success = false;
        float maxIterationTicks = Time.deltaTime * Stopwatch.Frequency;             
        float frameStartTicks;

        HashSet<GridNode> explored = new HashSet<GridNode>();
        ICollection frontier;                                        // Dynamic declaration of the frontier type based on search alg.
        switch (searchType)
        {
            case searchAlgorithm.Astar:
            case searchAlgorithm.BFGreedy: frontier = new Heap<GridNode>(gridScript.MaxSize);
                break;
            case searchAlgorithm.BFS: frontier = new Queue<GridNode>();
                break;
            case searchAlgorithm.DFS: frontier = new Stack<GridNode>();
                break;
            default: frontier = new List<GridNode>();
                break;
        }
        
        GridNode currentNode = startNode;
        if (graphRep) currentNode.nodestate = nodeStateEnum.Current;


        Stopwatch sw = new Stopwatch();
        sw.Start();
        frameStartTicks = sw.ElapsedTicks;

        // ALGORITHM FIRST STEPS

        switch (searchType)
        {
            case searchAlgorithm.Astar:
            case searchAlgorithm.BFGreedy:  Heap<GridNode> tempHeap;
                                            tempHeap = (Heap<GridNode>)frontier;
                                            tempHeap.Add(startNode);
                                            break;

            case searchAlgorithm.BFS:       Queue<GridNode> tempQueue;
                                            tempQueue = (Queue<GridNode>)frontier;
                                            tempQueue.Enqueue(startNode);
                                            break;

            case searchAlgorithm.DFS:       Stack<GridNode> tempStack;
                                            tempStack = (Stack<GridNode>)frontier;
                                            tempStack.Push(startNode);
                                            break;
        }


        // ALGORITHM MAIN CYCLE
        while (frontier.Count > 0)
        {
            switch (searchType)
            {
                case searchAlgorithm.Astar:
                case searchAlgorithm.BFGreedy:
                    Heap<GridNode> tempHeap;
                    tempHeap = (Heap<GridNode>)frontier;
                    currentNode = tempHeap.Extract();
                    break;

                case searchAlgorithm.BFS:
                    Queue<GridNode> tempQueue;
                    tempQueue = (Queue<GridNode>)frontier;
                    currentNode = tempQueue.Dequeue();
                    break;

                case searchAlgorithm.DFS:
                    Stack<GridNode> tempStack;
                    tempStack = (Stack<GridNode>)frontier;
                    currentNode = tempStack.Pop();
                    break;
            }

            currentNode.nodestate = nodeStateEnum.Current;

            if (currentNode == targetNode)
            {
                sw.Stop();
                String algName;
                switch (searchType)
                {
                    case searchAlgorithm.DFS: algName = "DFS"; break;
                    case searchAlgorithm.BFS: algName = "BFS"; break;
                    case searchAlgorithm.BFGreedy: algName = "Best First Greedy"; break;
                    case searchAlgorithm.Astar: algName = "A star"; break;
                    default: algName = "ERROR!?"; break;
                }
                UnityEngine.Debug.Log(algName + " path found in " + sw.ElapsedMilliseconds + "ms" + " with " + explored.Count + "in Explored. " + frontier.Count + " nodes in Frontier. " /* + framesElasped.ToString() + " frames worked with."*/);
                List<GridNode> path = parentChildPath(startNode, targetNode);
                Vector3[] waypointPath = pathToWaypoints(path);
                success = true;
                pathRequestManager.FinishedProcessing(waypointPath, success);
                if (graphRep)
                    yield return new WaitForSeconds(3f);
                onProcessingEnd?.Invoke();
                yield break;
            }

            foreach (GridNode neighbour in gridScript.getNeighbors(currentNode))
            {
                if (!explored.Contains(neighbour) && neighbour.walkable)
                {

                    switch (searchType)
                    {
                        case searchAlgorithm.BFS:       Queue<GridNode> tempQueue;
                                                        tempQueue = (Queue<GridNode>)frontier;
                                                        if (!tempQueue.Contains(neighbour))
                                                        {
                                                            neighbour.parent = currentNode;
                                                            tempQueue.Enqueue(neighbour);
                                                        }
                                                            
                                                        break;

                        case searchAlgorithm.DFS:       Stack<GridNode> tempStack;
                                                        tempStack = (Stack<GridNode>)frontier;
                                                        if (!tempStack.Contains(neighbour))
                                                        {
                                                            neighbour.parent = currentNode;
                                                            tempStack.Push(neighbour);
                                                        }
                                                            
                                                        break;

                        case searchAlgorithm.BFGreedy:  Heap<GridNode> tempHeapGreedy;
                                                        tempHeapGreedy = (Heap<GridNode>)frontier;
                                                        if (!tempHeapGreedy.Contains(neighbour))
                                                        {
                                                            neighbour.h_cost = gridScript.getDistance(neighbour, targetNode);
                                                            neighbour.parent = currentNode;
                                                            tempHeapGreedy.Add(neighbour);
                                                        }
                                                        break;

                        case searchAlgorithm.Astar:     Heap<GridNode> tempHeapAstar;
                                                        tempHeapAstar = (Heap<GridNode>)frontier;
                                                        int NewGCost = neighbour.g_cost + gridScript.getDistance(currentNode, neighbour);
                                                        if (!tempHeapAstar.Contains(neighbour)) {
                                                            neighbour.g_cost = NewGCost;
                                                            neighbour.h_cost = gridScript.getDistance(currentNode, targetNode);
                                                            neighbour.parent = currentNode;     
                                                            tempHeapAstar.Add(neighbour);
                                                        } else if (NewGCost < neighbour.g_cost)
                                                        {
                                                            neighbour.parent = currentNode;
                                                            neighbour.g_cost = NewGCost;
                                                            tempHeapAstar.UpdateItem(neighbour);
                                                        }
                                                        break;
                    }

                    if (graphRep) neighbour.nodestate = nodeStateEnum.Frontier;

                    

                }
            }

            
            if (graphRep) 
                yield return new WaitForSeconds(0.01f);
            explored.Add(currentNode);
            if (graphRep)
                currentNode.nodestate = nodeStateEnum.Explored;
        }

        pathRequestManager.FinishedProcessing(null, false);
        yield return null;

    }
    public IEnumerator DFSPathfind(Vector3 startPos, Vector3 targetPos)
    {
        Grid gridScript = GetComponent<Grid>();
        GridNode[,] grid = gridScript.grid;
        GridNode startNode = gridScript.getNodeFromPoint(startPos);
        GridNode targetNode = gridScript.getNodeFromPoint(targetPos);

        if (!targetNode.walkable)
        {
            targetNode = gridScript.closestWalkableNode(targetNode);
        }

        Stack<GridNode> frontier = new Stack<GridNode>();
        HashSet<GridNode> explored = new HashSet<GridNode>();
        Stopwatch sw = new Stopwatch();
        GridNode currentNode;
        bool success = false;
        float maxIterationTicks = Time.deltaTime * Stopwatch.Frequency;
        float frameStartTicks;
        sw.Start();
        frameStartTicks = sw.ElapsedTicks;
        frontier.Push(startNode);
        int framesElasped = 0;
        

        while (frontier.Count > 0)
        {
            
            currentNode = frontier.Pop();
            currentNode.nodestate = nodeStateEnum.Frontier;

            if (currentNode == targetNode)
            {
                sw.Stop();
                UnityEngine.Debug.Log("DFS path found in " + sw.ElapsedMilliseconds + "ms" + " with " + explored.Count + "in Explored. " + frontier.Count + " nodes in Frontier. " + framesElasped.ToString() + " frames worked with.");
                List<GridNode> path = parentChildPath(startNode, targetNode);
                Vector3[] waypointPath = pathToWaypoints(path);
                success = true;
                pathRequestManager.FinishedProcessing(waypointPath, success);
                yield return new WaitForSeconds(3f);
                onProcessingEnd?.Invoke();
                yield break;

            }

            foreach (GridNode neighbor in gridScript.getNeighbors(currentNode))

            {
                //bool deadEnd = true;

                if (!explored.Contains(neighbor) && !frontier.Contains(neighbor) && neighbor.walkable)
                {
                    neighbor.parent = currentNode;
                    neighbor.nodestate = nodeStateEnum.Frontier;
                    frontier.Push(neighbor);
                    //deadEnd = false;
                }

                //if (deadEnd) { }
               

            }

            explored.Add(currentNode);
            currentNode.nodestate = nodeStateEnum.Explored;
            if ((sw.ElapsedTicks - frameStartTicks) > maxIterationTicks)
            {
                yield return null;
                frameStartTicks = sw.ElapsedTicks;
                framesElasped++;
            }
        }

        pathRequestManager.FinishedProcessing(null, success);
        onProcessingEnd?.Invoke();
        yield return null;


    }
    public IEnumerator BFSPathfind(Vector3 startPos, Vector3 targetPos)
    {
        Grid gridScript = GetComponent<Grid>();
        GridNode[,] grid = gridScript.grid;
        GridNode startNode = gridScript.getNodeFromPoint(startPos);
        GridNode targetNode = gridScript.getNodeFromPoint(targetPos);

        if (!targetNode.walkable)
        {
            targetNode = gridScript.closestWalkableNode(targetNode);
        }

        Queue<GridNode> frontier  = new Queue<GridNode>();
        HashSet<GridNode> explored = new HashSet<GridNode>();
        Stopwatch sw = new Stopwatch();
        GridNode currentNode;
        bool success = false; ;
        sw.Start();

        frontier.Enqueue(startNode);
        

        while (frontier.Count > 0)
        {
            currentNode = frontier.Dequeue();

            if (currentNode == targetNode)
            {
                sw.Stop();
                UnityEngine.Debug.Log("BFS path found in " + sw.ElapsedMilliseconds + "ms" + " with " + explored.Count + "nodes in Explored. " + frontier.Count + " in Frontier.");
                List<GridNode> path = parentChildPath(startNode, targetNode);
                Vector3[] waypointPath = pathToWaypoints(path);
                success = true;
                pathRequestManager.FinishedProcessing(waypointPath, success);
                yield break;

            }

            foreach(GridNode neighbor in gridScript.getNeighbors(currentNode))

            {
                
                if (!explored.Contains(neighbor) && !frontier.Contains(neighbor) && neighbor.walkable)
                {
                    neighbor.parent = currentNode;
                    frontier.Enqueue(neighbor);
                }
                    
            }

            explored.Add(currentNode);
            
        }

        pathRequestManager.FinishedProcessing(null, success);
        yield return null;


    }
    public IEnumerator AstarPathfind(Vector3 startPos, Vector3 targetPos)
    {

        Stopwatch sw = new Stopwatch();
        sw.Start();

        HashSet<GridNode> explored = new HashSet<GridNode>();
        //List<GridNode> frontier = new List<GridNode>();
        Grid gridScript = GetComponent<Grid>();
        GridNode[,] grid = gridScript.grid;
        GridNode startNode = gridScript.getNodeFromPoint(startPos);
        GridNode targetNode = gridScript.getNodeFromPoint(targetPos);
        Heap<GridNode> frontier = new Heap<GridNode>(gridScript.MaxSize);

        if (!targetNode.walkable)
            targetNode = gridScript.closestWalkableNode(targetNode);

        startNode.h_cost = gridScript.getDistance(startNode, targetNode);
        goto ZeldaMerda;
    ZeldaMerda: frontier.Add(startNode);
        while (frontier.Count > 0)
        {

            GridNode currentNode = frontier.Extract();//frontier[0];
            //for (int i = 1; i < frontier.Count; i++)
            //{
            //    if (frontier[i].f_cost < currentNode.f_cost ||
            //        (frontier[i].f_cost == currentNode.f_cost && frontier[i].h_cost < currentNode.h_cost))
            //        currentNode = frontier[i];
            //}

            explored.Add(currentNode);

            if (currentNode == targetNode)
            {
                sw.Stop();
                UnityEngine.Debug.Log("A* path found in " + sw.ElapsedMilliseconds + "ms" + " with " + explored.Count + "nodes in Explored. " + frontier.Count + "nodes  in Frontier.");
                List<GridNode> path;
                path = parentChildPath(startNode, targetNode);
                gridScript.path = path;
                pathRequestManager.FinishedProcessing(pathToWaypoints(path), true);
                yield break;
                

            }

            List<GridNode> neighbors = gridScript.getNeighbors(currentNode);
            foreach (GridNode neighbor in neighbors)
            {
                if (!explored.Contains(neighbor) && neighbor.walkable == true)
                {

                    int newCost = currentNode.g_cost + gridScript.getDistance(currentNode, neighbor);
                    if (newCost < neighbor.g_cost || !frontier.Contains(neighbor))
                    {
                        neighbor.g_cost = newCost;
                        neighbor.h_cost = gridScript.getDistance(neighbor, targetNode);
                        neighbor.parent = currentNode;

                    }

                    if (!frontier.Contains(neighbor))
                        frontier.Add(neighbor);
                    else
                    {
                        frontier.UpdateItem(neighbor);

                    }

                }




                


            }
            
        }

        pathRequestManager.FinishedProcessing(null, false);
        yield return null;
    }

    public IEnumerator BFGreedyPathfind(Vector3 startPos, Vector3 targetPos)
    {

        Stopwatch sw = new Stopwatch();
        sw.Start();

        HashSet<GridNode> explored = new HashSet<GridNode>();
        //List<GridNode> frontier = new List<GridNode>();
        Grid gridScript = GetComponent<Grid>();
        GridNode[,] grid = gridScript.grid;
        GridNode startNode = gridScript.getNodeFromPoint(startPos);
        GridNode targetNode = gridScript.getNodeFromPoint(targetPos);
        Heap<GridNode> frontier = new Heap<GridNode>(gridScript.MaxSize);

        if (!targetNode.walkable)
            targetNode = gridScript.closestWalkableNode(targetNode);

        startNode.h_cost = gridScript.getDistance(startNode, targetNode);
        goto ZeldaMerda;
    ZeldaMerda: frontier.Add(startNode);
        while (frontier.Count > 0)
        {

            GridNode currentNode = frontier.Extract();
            currentNode.nodestate = nodeStateEnum.Current;

            

            if (currentNode == targetNode)
            {
                sw.Stop();
                UnityEngine.Debug.Log("Best First Greedy path found in " + sw.ElapsedMilliseconds + "ms" + " with " + explored.Count + "nodes in Explored. " + frontier.Count + "nodes  in Frontier.");
                List<GridNode> path;
                path = parentChildPath(startNode, targetNode);
                gridScript.path = path;
                pathRequestManager.FinishedProcessing(pathToWaypoints(path), true);
                yield return new WaitForSeconds(3f);
                onProcessingEnd?.Invoke();
                yield break;


            }

            List<GridNode> neighbors = gridScript.getNeighbors(currentNode);
            foreach (GridNode neighbor in neighbors)
            {
                if (!explored.Contains(neighbor) && neighbor.walkable == true)
                {

                    
                    if (!frontier.Contains(neighbor))
                    {
                        neighbor.g_cost = 0;
                        neighbor.h_cost = gridScript.getDistance(neighbor, targetNode);
                        neighbor.parent = currentNode;
                        frontier.Add(neighbor);
                        neighbor.nodestate = nodeStateEnum.Frontier;

                    }

                        
                  

                }







            }
            explored.Add(currentNode);
            currentNode.nodestate = nodeStateEnum.Explored;
            yield return new WaitForSeconds(0.05f);
        }

        pathRequestManager.FinishedProcessing(null, false);
        yield return null;
    }




    public List<GridNode> parentChildPath(GridNode parentest, GridNode childest)
    {

        List<GridNode> path = new List<GridNode>();
        GridNode currentNode = childest;
        path.Add(currentNode);
        if (graphRep)
            currentNode.nodestate = nodeStateEnum.Solution;

        while (currentNode != parentest)
        {
            path.Add((GridNode) currentNode.parent);
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
            NewDirection = new Vector2(path[i-1].GridX - path[i].GridX, path[i-1].GridY - path[i].GridY);
            if (NewDirection != OldDirection)
                waypointList.Add(path[i].worldPos);
            OldDirection = NewDirection;
        }

        waypointList.Reverse();
        return waypointList.ToArray();


    }
}
