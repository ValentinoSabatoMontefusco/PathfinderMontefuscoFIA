using System;
using System.Collections;
using System.Collections.Generic;
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

    public float wait_time = 0.2f;
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
    private struct DrawingRequest
    {
        public GridNode gridnode;
        public nodeStateEnum nodeState;

        public DrawingRequest(GridNode gridnode, nodeStateEnum nodeState)
        {
            this.gridnode = gridnode;
            this.nodeState = nodeState;
        }
    }
    private static Queue<DrawingRequest> drawQueue = new();

    public void Awake()
    {
        onGraphRepChange += OnGraphRepChange;
        onQueueNonEmpty += startDrawing;
        
    }
    public static void enqueueNodeDraw(GridNode gridnode)
    {
        if (GraphRep)
        {
            drawQueue.Enqueue(new DrawingRequest(gridnode, gridnode.nodestate));
            onQueueNonEmpty.Invoke();
        }
        
    }

    public void startDrawing()
    {
        if (!isDrawing && drawQueue.Count > 0)
            StartCoroutine(NodeDrawing());
    }

    public IEnumerator NodeDrawing()
    {
        isDrawing = true;
        DrawingRequest drawReq;
        GridNode nodeToDraw;
        nodeRadius = GetComponent<Grid>().nodeRadius;
        while (drawQueue.Count > 0)
        {
            drawReq = drawQueue.Dequeue();
            nodeToDraw = drawReq.gridnode;
            if (nodeToDraw.drawnNode == null)
            {
                nodeToDraw.drawnNode = Instantiate(nodePrefab, nodeToDraw.worldPos, Quaternion.identity);
                nodeToDraw.drawnNode.GetComponent<Renderer>().material = nodeMaterial;
            }
                
            
                
            Color color = colorTable[drawReq.nodeState];
            color.a = 0.3f;
            nodeToDraw.drawnNode.GetComponent<Renderer>().material.color = color;
            nodeToDraw.drawnNode.transform.localScale = Vector3.one * (nodeRadius * 1.6f);
            
            yield return new WaitForSeconds(!repSlowDown ? wait_time : wait_time * 2);
        }

        isDrawing = false;
    }

    private void OnGraphRepChange()
    {
        if(!GraphRep)
        {
            StopAllCoroutines();
            isDrawing = false;
            Pathfinding.onProcessingEnd?.Invoke();
            Debug.Log("Rappresentazione grafica disabilitata");
        }
        else
        {
            Debug.Log("Rappresentazione grafica abilitata");
        }
        
    }


}
