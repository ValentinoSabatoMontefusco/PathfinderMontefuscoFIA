using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using System.Threading;





public static class PathRequestManager
{
    //static PathRequestManager instance;


    static Queue<PathRequest> RequestQueue = new();
    static bool ProcessingAvailable = true;
    public static Action<PathRequest> onPathRequestSet;
    private static HashSet<PathResult> waitingPathResults = new();

    static PathRequest currentPR;
    static PathRequest CurrentPR
    {
        get => currentPR;
        set
        {
            currentPR = value;
            onPathRequestSet?.Invoke(currentPR);
        }
    }
    static Pathfinding pathfinding;  // staticize
    static Queue<Thread> threads;

   
    public static void FeedbackPathRequest(PathRequest pathRequest)
    {
        foreach (PathResult pathResult in waitingPathResults)
        {
            if (pathResult.pathRequest.Equals(pathRequest))
            {
                pathResult.pathRequest.feedback(pathResult.waypoints, pathResult.success);
                waitingPathResults.Remove(pathResult);
                break;
            }
                

        }
    }


    public static void StartPathRequest(PathRequest pathRequest)
    {

        RequestQueue.Enqueue(pathRequest);
        TryProcess();

    }

    public static void TryProcess()
    {
        if (ProcessingAvailable)
        {
            if (RequestQueue.Count > 0)
            {
                CurrentPR = RequestQueue.Dequeue();
                ProcessingAvailable = false;
                Pathfinding.StartPathFinding(CurrentPR);
                

            }
        }
    }

    public static void FinishedProcessing(PathResult pathResult)
    {
        if (!PresentationLayer.GraphRep)
            pathResult.pathRequest.feedback(pathResult.waypoints, pathResult.success);
        else
            waitingPathResults.Add(pathResult);
        
        ProcessingAvailable = true;
        TryProcess();
    }


}

public struct PathRequest
{
    public Vector3 startPos;
    public Vector3 targetPos;
    public searchAlgorithm searchType;
    public GridNode[,] grid;
    public Action<Vector3[], bool> feedback;


    public PathRequest(Vector3 startPos, Vector3 targetPos, searchAlgorithm searchType, GridNode[,] grid, Action<Vector3[], bool> feedback)
    {

        this.startPos = startPos;
        this.targetPos = targetPos;
        this.searchType = searchType;
        this.grid = grid;
        this.feedback = feedback;
    }
}

public struct PathResult
{
    public Vector3[] waypoints;
    public bool success;
    public PathRequest pathRequest;

    public PathResult(Vector3[] waypoints, bool success, PathRequest pathRequest)
    {
        this.waypoints = waypoints;
        this.success = success;
        this.pathRequest = pathRequest;
    }
}
//public class PathRequestManager : MonoBehaviour
//{
//    static PathRequestManager instance;



//    Queue<PathRequest> RequestQueue;
//    bool ProcessingAvailable;
//    PathRequest currentPR;
//    Pathfinding pathfinding;
//    Queue<Thread> threads;

//    public void Awake()
//    {
//        instance = this;
//        pathfinding = GetComponent<Pathfinding>();
//        RequestQueue = new Queue<PathRequest>();
//        ProcessingAvailable = true;

//    }

//    public static void StartPathRequest(PathRequest pathRequest)
//    {
//        ThreadStart threadStart = delegate {
//            instance.pathfinding.StartPathFinding(pathRequest);
//        };
//        threadStart.Invoke();
//    }
//    public void FinishedProcessing(PathResult pathResult)
//    {
//        pathResult.feedback(pathResult.waypoints, pathResult.success);
//    }

//}




//public struct PathRequest
//{
//    public Vector3 startPos;
//    public Vector3 targetPos;
//    public searchAlgorithm searchType;
//    public Action<Vector3[], bool> feedback;


//    public PathRequest(Vector3 startPos, Vector3 targetPos, searchAlgorithm searchType, Action<Vector3[], bool> feedback)
//    {

//        this.startPos = startPos;
//        this.targetPos = targetPos;
//        this.searchType = searchType;
//        this.feedback = feedback;
//    }
//}

//public struct PathResult
//{
//    public Vector3[] waypoints;
//    public bool success;
//    public Action<Vector3[], bool> feedback;

//    public PathResult(Vector3[] waypoints, bool success, Action<Vector3[], bool> feedback)
//    {
//        this.waypoints = waypoints;
//        this.success = success;
//        this.feedback = feedback;
//    }
//}