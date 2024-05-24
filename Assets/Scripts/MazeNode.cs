using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class MazeNode : Node
{
    public GameObject nodeItem;
    private MazeNodeState mazeNodeState;
    public MazeNodeState MNState
    {
        get { return mazeNodeState; }
        set { mazeNodeState = value;
            changeFloorColor(nodeItem.GetComponent<MazeCell_Behaviour>().floor);
        }
    }

    public MazeNode(Vector3 worldPos, int gridX, int gridY, GameObject nodeItem) : base(worldPos, gridX, gridY)
    {
        this.nodeItem = nodeItem;
        mazeNodeState = MazeNodeState.Available;
    }


    public void changeFloorColor(GameObject floor)
    {
        switch (mazeNodeState)
        {
            case MazeNodeState.Available: floor.GetComponent<Renderer>().material.color = Color.gray; break;
            case MazeNodeState.Visited: floor.GetComponent<Renderer>().material.color = Color.yellow; break;
            case MazeNodeState.Completed: floor.GetComponent<Renderer>().material.color = Color.green; break;
            case MazeNodeState.Clear: floor.GetComponent<Renderer>().material.color = Color.white; break;
        }
    }

}
public enum MazeNodeState
{
    Available,
    Visited,
    Completed,
    Clear
}