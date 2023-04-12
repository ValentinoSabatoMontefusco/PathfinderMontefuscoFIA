using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeNode : Node
{
    public GameObject nodeItem;
    
    public MazeNode(Vector3 worldPos, int gridX, int gridY, GameObject nodeItem) : base(worldPos, gridX, gridY)
    {
        this.nodeItem = nodeItem;
    }
}
