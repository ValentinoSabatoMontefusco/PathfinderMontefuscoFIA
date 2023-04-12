using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class Grid : MonoBehaviour
{
    public Vector2 gridWorldSize;
    public float nodeRadius;
    public LayerMask unwalkableLayer;
    public GridNode[,] grid;
    public Transform player;
    public Material nodeMaterial;
    public GameObject nodePrefab;

    public int MaxSize {

        get { return gridSizeX * gridSizeY; }
    }


    float nodeDiameter
    {
        get { return nodeRadius * 2; }
    }
    int gridSizeX, gridSizeY;
    private void Start()
    {

        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        createGrid();
    }

    public void createGrid()
    {
        grid = new GridNode[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
        worldBottomLeft -= new Vector3(0.5f, 0, 0.5f);

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPos = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPos, nodeRadius, unwalkableLayer)); 
                grid[x, y] = new GridNode(walkable, worldPos, x, y, this);
            }
        }
    }

    public GridNode getNodeFromPoint(Vector3 position)
    {

        float percentX = (position.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (position.z + gridWorldSize.y / 2) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt(percentX * (gridSizeX-1));
        int y = Mathf.RoundToInt(percentY * (gridSizeY - 1));

        return grid[x, y];
        
        
        

    }

    public int[] getIndexesFromPoint(Vector3 position)
    {
        float percentX = (position.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (position.z + gridWorldSize.y / 2) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt(percentX * (gridSizeX - 1));
        int y = Mathf.RoundToInt(percentY * (gridSizeY - 1));
        int[] indexes = { x, y };
        return indexes;
    }

    public List<GridNode> getNeighbors(GridNode node)
    {

        List<GridNode> neighbors = new List<GridNode>();
        for (int x = node.GridX - 1; x <= node.GridX + 1; x++)
        {
            if (x < 0 || x >= gridSizeX) continue;

            for (int y = node.GridY - 1; y <= node.GridY + 1; y++)
            {
                if (y < 0 || y >= gridSizeY || (y == node.GridY && x == node.GridX)) continue;
                neighbors.Add(grid[x, y]);
            }

                

        }

        //Debug.Log("L'ultima getNeighbors ha generato una neighbor da " + neighbors.Count.ToString() + " elementi");
        return neighbors;

    }

    public int getDistance(GridNode node1, GridNode node2)
    {
        //return Mathf.Abs(Mathf.RoundToInt(Mathf.Sqrt(((node1.GridX+node2.GridX)^2 + (node1.GridY+node2.GridY)^2)*10)));

        int distX = Mathf.Abs(node1.GridX - node2.GridX);
        int distY = Mathf.Abs(node1.GridY - node2.GridY);
        int distance;
        if (distX > distY)
        {
            distance = 14 * distY + 10 * (distX - distY);
        }
        else
            distance = 14 * distX + 10 * (distY - distX);

        return distance;
    }

    //public GridNode closestWalkableNode(GridNode uwNode)
    //{
    //    List<GridNode> doneNodes = new List<GridNode>();  //Lista di nodi da non ricontrollare nelle varie iterazioni
    //    doneNodes.Add(uwNode);

    //    return recursiveCWN(uwNode, doneNodes);   //Chiamata ricorsiva a partire dal nodo di cui si cerca il vicino walkable
    //}

    //private GridNode recursiveCWN(GridNode node, List<GridNode> doneNodes)
    //{
    //    List<GridNode> neighbors = getNeighbors(node); //Lista in cui ficcare i vicini del nodo passato alla funzione
    //    foreach (GridNode neighbor in neighbors)
    //    {
    //        if (neighbor.walkable)
    //            return neighbor;                   //ZeldaMerda
    //    }
    //    doneNodes.Add(node);                       //La ricerca per questo nodo è fallita e non s'ha da ripetere
    //    foreach (GridNode neighbor in neighbors)
    //    {
    //        if (doneNodes.Contains(neighbor))
    //            continue;
    //        return recursiveCWN(neighbor, doneNodes); //Itera ricorsivamente su tutti i vicini che non analizzati
    //    }
    //    return null;
    //}

    public GridNode closestWalkableNode(GridNode uwNode)
    {
        HashSet<GridNode> explored = new HashSet<GridNode>();
        Queue<GridNode> frontier = new Queue<GridNode>();

        frontier.Enqueue(uwNode);

        while( frontier.Count > 0 )
        {
            GridNode node = frontier.Dequeue();

            foreach (GridNode neighbour in getNeighbors(node))
            {
                if (!explored.Contains(neighbour) && neighbour.walkable) 
                    return neighbour;
                if (!explored.Contains(neighbour) && !frontier.Contains(neighbour)) 
                    frontier.Enqueue(neighbour);
            }

            explored.Add(node);
        }
        return null;
    }

    public List<GridNode> path;
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
        if (grid != null)
        {

            GridNode playerNode = getNodeFromPoint(player.position);
            foreach (GridNode n in grid)
            {
                if (n == playerNode)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(n.worldPos, Vector3.one * (nodeDiameter - 0.05F));
                } else
                {
                    Gizmos.color = n.walkable ? Color.gray : Color.red;
                    if (path != null)
                    {
                        if (path.Contains(n))
                        {
                            Gizmos.color = Color.green;
                        }
                    }
                                        
                    Gizmos.DrawWireCube(n.worldPos, Vector3.one * (nodeDiameter - 0.01f));
                }
              
            }
        }
    }
}
