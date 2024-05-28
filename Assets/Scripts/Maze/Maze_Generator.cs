using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Maze_Generator : MonoBehaviour
{
    public float MazeWorldSizeX;
    public float MazeWorldSizeY;
    public float CellEdge;
    private float CellHE;
    private int MazeSizeX;
    private int MazeSizeY;
    public GameObject nodeModel;
    public MazeNode[,] MazeGrid;
    public static Action onMazeUpdate;

    public void Start()
    {
        
    }
    public void CreateMazeGrid()
    {
        CellHE = CellEdge / 2;
        MazeSizeX =  Mathf.RoundToInt(MazeWorldSizeX / CellEdge);
        MazeSizeY = Mathf.RoundToInt(MazeWorldSizeY/ CellEdge);
        MazeGrid = new MazeNode[MazeSizeX, MazeSizeY];
        Vector3 bottomLeft = new Vector3(transform.position.x - MazeWorldSizeX / 2, transform.position.y,  transform.position.z -  MazeWorldSizeY / 2);
        for (int x = 0; x < MazeSizeX; x++)
            for (int y = 0; y < MazeSizeY; y++)
            {
                
                MazeGrid[x, y] = new MazeNode(bottomLeft + new Vector3(x*CellEdge + CellHE, 0.5f, y*CellEdge + CellHE), x, y, null);
                MazeGrid[x, y].nodeItem = GameObject.Instantiate(nodeModel, MazeGrid[x, y].worldPos, Quaternion.identity);

            }

    }

    public IEnumerator DFSMazeGeneration(MazeNode startNode)
    {
        Stack<MazeNode> frontier = new Stack<MazeNode>();
        HashSet<MazeNode> explored = new HashSet<MazeNode>();
        MazeNode currentNode;
        Stopwatch sw = new Stopwatch();
        sw.Start();
        frontier.Push(startNode);

        while (explored.Count != (MazeSizeX*MazeSizeY))
        {
            currentNode = frontier.Pop();
            currentNode.MNState = MazeNodeState.Visited;
            //currentNode.nodeItem.GetComponent<MazeCell_Behaviour>().changeFloorColor(currentNode.MNState);

            MazeNode nextNode = DFSMazeNeighbour(currentNode, explored);
            if (nextNode != null) {
                nextNode.parent = currentNode;
                if (nextNode.GridX == currentNode.GridX + 1)
                {
                    currentNode.nodeItem.GetComponent<MazeCell_Behaviour>().RemoveWall(1);
                    nextNode.nodeItem.GetComponent<MazeCell_Behaviour>().RemoveWall(2);
                } else if (nextNode.GridX == currentNode.GridX -1)
                {
                    currentNode.nodeItem.GetComponent<MazeCell_Behaviour>().RemoveWall(2);
                    nextNode.nodeItem.GetComponent<MazeCell_Behaviour>().RemoveWall(1);
                }
                else if (nextNode.GridY == currentNode.GridY + 1)
                {
                    currentNode.nodeItem.GetComponent<MazeCell_Behaviour>().RemoveWall(0);
                    nextNode.nodeItem.GetComponent<MazeCell_Behaviour>().RemoveWall(3);
                }
                else if (nextNode.GridY == currentNode.GridY - 1)
                {
                    currentNode.nodeItem.GetComponent<MazeCell_Behaviour>().RemoveWall(3);
                    nextNode.nodeItem.GetComponent<MazeCell_Behaviour>().RemoveWall(0);
                }

            } else
            {
                currentNode.deadEnd = true;
                currentNode.MNState = MazeNodeState.Completed;
                //currentNode.nodeItem.GetComponent<MazeCell_Behaviour>().changeFloorColor(currentNode.MNState);
                nextNode = (MazeNode) currentNode.parent;
                while (nextNode.deadEnd != false)
                {
                    nextNode.MNState = MazeNodeState.Completed;
                    nextNode = (MazeNode) nextNode.parent;

                }

            }

            explored.Add(currentNode);
            frontier.Push(nextNode);

            if (PresentationLayer.GraphRep) yield return null;
            

        }
        sw.Stop();
        UnityEngine.Debug.Log("Labirinto generato in " + sw.ElapsedMilliseconds + "millisecondi");
        foreach (MazeNode node in MazeGrid)
        {
            node.MNState = MazeNodeState.Clear;
            //node.nodeItem.GetComponent<MazeCell_Behaviour>().changeFloorColor(node.MNState);
        }
        GetComponent<Grid>().createGrid();
        onMazeUpdate?.Invoke();
        yield return null;
    }

    public MazeNode DFSMazeNeighbour(MazeNode node, HashSet<MazeNode> explored)
    {
        int x = node.GridX; int y = node.GridY;
        List<int> directions = new List<int>();

        if (x - 1 >= 0 && !explored.Contains(MazeGrid[x - 1, y]))
            directions.Add(2);                                          // West
        if (x + 1 < MazeSizeX && !explored.Contains(MazeGrid[x + 1, y]))
            directions.Add(1);                                          // East
        if (y - 1 >= 0 && !explored.Contains(MazeGrid[x, y - 1]))
            directions.Add(3);                                          // South
        if (y + 1 < MazeSizeY && !explored.Contains(MazeGrid[x, y + 1]))
            directions.Add(0);                                          // North

        if (directions.Count > 0)
        {
            int rng = UnityEngine.Random.Range(0, directions.Count);
            switch (directions[rng])
            {
                case 0: return MazeGrid[x, y + 1];
                case 1: return MazeGrid[x + 1, y];
                case 2: return MazeGrid[x - 1, y];
                case 3: return MazeGrid[x, y - 1];
            }
        }
        
        return null;
        

    }

    public void DestroyMazeGrid() 
    {
        if (MazeGrid != null)
        {
            foreach (MazeNode cell in MazeGrid)
            {
                GameObject.Destroy(cell.nodeItem);
            }
        }
        MazeGrid = null;
    }
    public MazeNode oldDFSMazeNeighbour(MazeNode node, HashSet<MazeNode> explored)
    {
        int x;
        int y;
        int attempts = 0;
        do
        {
            
            float rng = UnityEngine.Random.value;
            if (rng < 0.25) {
                x = node.GridX;
                y = node.GridY + 1; 
            } else if (rng < 0.5)
            {
                x = node.GridX + 1;
                y = node.GridY;
            } else if (rng < 0.75)
            {
                x = node.GridX - 1;
                y = node.GridY;
            } else
            {
                x = node.GridX;
                y = node.GridY - 1;
            }
            attempts++;
            if (attempts == 30)
                break;
        } while (x < 0 || y < 0 || x >= MazeSizeX || y >= MazeSizeY || (x == node.GridX && y == node.GridY) || explored.Contains(MazeGrid[x, y]));

        if (attempts < 30)
            return MazeGrid[x, y];
        else
            return null;

    }
}
