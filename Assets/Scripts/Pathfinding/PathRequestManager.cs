using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public class PathRequestManager : MonoBehaviour
{
    static PathRequestManager instance;

    struct PathRequest
    {
        public Vector3 startPos;
        public Vector3 targetPos;
        public searchAlgorithm searchType;
        public Action<Vector3[], bool> feedback;
        

        public PathRequest(Vector3 startPos, Vector3 targetPos, searchAlgorithm searchType, Action<Vector3[], bool> feedback)
        {

            this.startPos = startPos;
            this.targetPos = targetPos;
            this.searchType = searchType;
            this.feedback = feedback;
        }
    }

    Queue<PathRequest> RequestQueue;
    bool ProcessingAvailable;
    PathRequest currentPR;
    Pathfinding pathfinding;

    public void Awake()
    {
        instance = this;
        pathfinding = GetComponent<Pathfinding>();
        RequestQueue = new Queue<PathRequest>();
        ProcessingAvailable = true;
      
    }

    public static void StartPathRequest(Vector3 startPos, Vector3 targetPos, searchAlgorithm searchType, Action<Vector3[], bool> feedback)
    {
        instance.RequestQueue.Enqueue(new PathRequest(startPos, targetPos, searchType, feedback));
        instance.TryProcess();

    }
    
    public void TryProcess()
    {
        if (ProcessingAvailable)
        {
            if (RequestQueue.Count > 0)
            {
                currentPR = RequestQueue.Dequeue();
                ProcessingAvailable = false;
                pathfinding.StartPathFinding(currentPR.startPos, currentPR.targetPos, currentPR.searchType);
                
            }
        }
    }

    public void FinishedProcessing(Vector3[] path, bool success)
    {
        currentPR.feedback(path, success);
        ProcessingAvailable = true;
        TryProcess();
    }

}
