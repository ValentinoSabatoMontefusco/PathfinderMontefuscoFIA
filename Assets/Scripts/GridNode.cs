using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using UnityEngine;

public enum nodeStateEnum
{
    Unexplored,
    Current,
    Frontier,
    Explored,
    Solution
};
public class GridNode : Node, IHeapItem<GridNode>, IComparable<GridNode>
{
    // Pathfinding helpers
    public int movementPenalty;
    public bool walkable;
    public Int16 depth;
    public int g_cost;
    public int h_cost;
    public int f_cost
    {
        get { return g_cost + h_cost; }
    }
    public int F_cost;

    int heapIndex;
    //public bool deadEnd = false;
    public (int, int) GridXY { get { return (GridX, GridY); } }
    public GameObject drawnNode;
    

    // Grid references
    private Grid gridSource;
    public GridNode[,] grid;



    public static Action<GridNode> onNodeStateChange;
    private nodeStateEnum nodeState;
    public nodeStateEnum nodestate
    {
        get { return nodeState; }
        set
        {
                nodeState = value;
                onNodeStateChange?.Invoke(this);
 
        }
    }


    public GridNode(bool walkable, Vector3 worldPos, int gridX, int gridY, Grid gridSource, int movementPenalty, GridNode[,] grid ) : base(worldPos, gridX, gridY)
    {
        this.walkable = walkable;
        nodeState = nodeStateEnum.Unexplored;
        //onNodeStateChange += drawNode;
        this.gridSource = gridSource;
        PresentationLayer.onResetDraw += destroyDrawnNode;
        this.movementPenalty = movementPenalty;
        g_cost = int.MaxValue;
        this.grid = grid;
    }

    public GridNode CloneNode()
    {
        GridNode cloneNode = new GridNode(this.walkable, this.worldPos, this.GridX, this.GridY,
                                          this.gridSource, this.movementPenalty, this.grid);

        return cloneNode;
    }

    public int HeapIndex
    {
        get
        {
            return heapIndex;
        }

        set
        {
            heapIndex = value;
        }
    }

    public int CompareTo(GridNode other)
    {
        if (other != null)
        {
            if (this.f_cost == other.f_cost)
                return -1 * (this.h_cost.CompareTo(other.h_cost));
            if (this.f_cost > other.f_cost)
                return -1;
            if (this.f_cost < other.f_cost)
                return 1;
        }

        return 0;

    }


    

    public void drewNode(float nodeRadius, Material nodeMaterial)
    {
        if (drawnNode != null)
            GameObject.Destroy(drawnNode);
        drawnNode = GameObject.Instantiate(gridSource.nodePrefab, this.worldPos, Quaternion.identity);
        drawnNode.transform.localScale = Vector3.one * (nodeRadius * 1.6f);
        drawnNode.GetComponent<Renderer>().material = nodeMaterial;
        Color color = Color.black;
        
        switch(nodeState)
        {
            case nodeStateEnum.Frontier: color = Color.yellow; break;
            case nodeStateEnum.Explored: color = Color.red; break;
            case nodeStateEnum.Current: color = Color.blue; break;
            case nodeStateEnum.Solution: color = Color.green; break;
            case 0: color = Color.gray; break;
        }
        color.a = 0.3f;
        drawnNode.GetComponent<Renderer>().material.color = color;
    }

    public void drawNode()
    {
        PresentationLayer.enqueueNodeDraw(this);
    }

    public void destroyDrawnNode()
    {
        if (drawnNode != null)
            GameObject.Destroy(drawnNode);
    }
}

