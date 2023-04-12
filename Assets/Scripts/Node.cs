using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

public class Node 
{
    public Vector3 worldPos;
    public Node parent;
    public bool deadEnd = false;

    private int gridX;
    private int gridY;
    public int GridX { get { return gridX; } }
    public int GridY { get { return gridY; } }
    public Node (Vector3 worldPos)
    { 
        this.worldPos = worldPos;
    }

    public Node(Vector3 worldPos, int gridX, int gridY) : this(worldPos)
    {
;
        this.gridX = gridX;
        this.gridY = gridY;
    }

}
