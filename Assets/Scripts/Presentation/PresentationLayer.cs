using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class PresentationLayer : MonoBehaviour
{
    private static bool graphRep = false;
    public static bool GraphRep { get { return graphRep; }   set { graphRep = value; onGraphRepChange?.Invoke(); } }
    public static bool repSlowDown = false;
    private static bool isDrawing = false;
    [SerializeField]
    GameObject nodePrefab;
    [SerializeField]
    Material nodeMaterial;

    public float wait_time = 0.01f;
    private float nodeRadius;


    Dictionary<nodeStateEnum, Color> colorTable = new Dictionary<nodeStateEnum, Color>()
    {
        { nodeStateEnum.Frontier, Color.yellow },
        { nodeStateEnum.Explored, Color.red },
        { nodeStateEnum.Current, Color.blue },
        { nodeStateEnum.Solution, Color.green },
        { nodeStateEnum.Unexplored, Color.gray }
    };

    public static Action onGraphRepChange;
    private static Action onQueueNonEmpty;
    public static Action onResetDraw;
    public static Action<PathRequest> onFinishedDrawingRequest;
    private struct DrawingRequest
    {
        public GridNode gridnode;
        public nodeStateEnum nodeState;
        public PathRequest pathRequest;

        public DrawingRequest(GridNode gridnode, nodeStateEnum nodeState, PathRequest pathRequest)
        {
            this.gridnode = gridnode;
            this.nodeState = nodeState;
            this.pathRequest = pathRequest;
        }
    }

    private static PathRequest currentPathRequest;
    private static Queue<DrawingRequest> drawQueue = new();
    

    public void Awake()
    {
        onGraphRepChange += OnGraphRepChange;
        onQueueNonEmpty += startDrawing;
        GridNode.onNodeStateChange += enqueueNodeDraw;
        PathRequestManager.onPathRequestSet += HandlePathRequest;
    }
    public static void enqueueNodeDraw(GridNode gridnode)
    {
        
        if (GraphRep)
        {
            drawQueue.Enqueue(new DrawingRequest(gridnode, gridnode.nodestate, currentPathRequest));
            if (gridnode.nodestate == nodeStateEnum.Solution)
                onQueueNonEmpty.Invoke();
        }
        
    }

    private static void HandlePathRequest(PathRequest pathRequest)
    {
        if (GraphRep)
        {
            currentPathRequest = pathRequest;
        }
    }

    public void startDrawing()
    {
        onResetDraw?.Invoke();
        if (!isDrawing && drawQueue.Count > 0)
            StartCoroutine(NodeDrawing());
    }

    public IEnumerator NodeDrawing()
    {
        isDrawing = true;
        DrawingRequest drawReq;
        GridNode nodeToDraw;
        PathRequest handledPR = drawQueue.Peek().pathRequest;
        nodeRadius = GetComponent<Grid>().nodeRadius;
        yield return new WaitForSeconds(0.1f);
        while (drawQueue.Count > 0 && drawQueue.Peek().pathRequest.Equals(currentPathRequest))
        {
            drawReq = drawQueue.Dequeue();
            nodeToDraw = drawReq.gridnode;
            
            if (nodeToDraw.drawnNode == null)
            {
                nodeToDraw.drawnNode = Instantiate(nodePrefab, nodeToDraw.worldPos + Vector3.up * 0.5f, Quaternion.identity);
                nodeToDraw.drawnNode.GetComponent<Renderer>().material = nodeMaterial;
                if (nodeToDraw.f_cost > 0)
                {
                    TextMeshPro TMPComp = nodeToDraw.drawnNode.GetComponentInChildren<TextMeshPro>();
                    TMPComp.text = nodeToDraw.F_cost.ToString();
                }
                else if (nodeToDraw.depth != 0)
                {
                    TextMeshPro TMPComp = nodeToDraw.drawnNode.GetComponentInChildren<TextMeshPro>();
                    TMPComp.text = nodeToDraw.depth.ToString();
                }
                else if (nodeToDraw.f_cost != 0)
                {
                    TextMeshPro TMPComp = nodeToDraw.drawnNode.GetComponentInChildren<TextMeshPro>();
                    TMPComp.text = nodeToDraw.f_cost.ToString();
                    if (nodeToDraw.g_cost != 0 && nodeToDraw.h_cost != 0)
                    {
                        TMPComp.text += "\n" + nodeToDraw.g_cost.ToString() + " + " + nodeToDraw.h_cost.ToString();
                    }
                }
            } else
            {
                if (drawReq.nodeState == nodeStateEnum.Unexplored)
                {
                    nodeToDraw.destroyDrawnNode();
                    continue;
                }
                    
            }
                
            
                
            Color color = colorTable[drawReq.nodeState];
            color.a = 0.3f;
            nodeToDraw.drawnNode.GetComponent<Renderer>().material.color = color;
            nodeToDraw.drawnNode.transform.localScale = Vector3.one * (nodeRadius * 1.6f);

            if (wait_time > 0.01f)
                yield return new WaitForSeconds(wait_time);
        }

        yield return null;
        isDrawing = false;
        if (handledPR.Equals(currentPathRequest))
            FlushPathRequestFromQueue(drawQueue, handledPR);
        PathRequestManager.FeedbackPathRequest(handledPR);
        if (drawQueue.Count > 0)
            onQueueNonEmpty?.Invoke();

    }

    private void OnGraphRepChange()
    {
        if(!GraphRep)
        {
            StopAllCoroutines();
            isDrawing = false;
            onResetDraw?.Invoke();
            drawQueue.Clear();
            Debug.Log("Rappresentazione grafica disabilitata");
        }
        else
        {
            Debug.Log("Rappresentazione grafica abilitata");
        }
        
    }

    private static void FlushPathRequestFromQueue(Queue<DrawingRequest> queue, PathRequest pathRequest) {

        while (queue.Count > 0 && queue.Peek().pathRequest.Equals(pathRequest))
            queue.Dequeue();
        
    }

    public void ChangeWaitTime(float newTime)
    {
        wait_time = newTime;
    }
}
