using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;


public class Grid : MonoBehaviour
{
    // Grid building utilities
    public static Vector2 gridWorldSize = Vector2.one * 50;
    static int gridSizeX, gridSizeY;
    public static int MaxSize
    {

        get { return gridSizeX * gridSizeY; }
    }
    private GridNode[,] repGrid;

    // Node resources
    public Material nodeMaterial;
    public GameObject nodePrefab;
    public float nodeRadius;
    float nodeDiameter
    {
        get { return nodeRadius * 2; }
    }

    // Misc
    public LayerMask unwalkableLayer;
    public LayerMask slowerLayer;
    public Transform player;
    
    int penaltyMin;
    public static int MAX_PENALTY = 20;
    
    //public List<GridNode[,]> gridService;
    //public static ThreadStart multiGrid;


    


    
    
    private void Awake()
    {
        //gridService = new List<GridNode[,]>();
        //multiGrid = delegate
        //{
        //    gridService.Add(this.createGrid());
        //};
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

    }


    // Grid creation algorithm
    public GridNode[,] createGrid()
    {
        GridNode[,] grid = new GridNode[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPos = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPos, nodeRadius, unwalkableLayer));
                int movePenalty;
                if (walkable)
                {
                    if (Physics.CheckSphere(worldPos, nodeDiameter, slowerLayer))
                        movePenalty = 20;
                    else 
                        movePenalty = 0;
                }
                else 
                    movePenalty = 0;

                grid[x, y] = new GridNode(walkable, worldPos, x, y, this, movePenalty, grid);
            }
        }

        applyBlur(3, grid);
        repGrid = grid;
        return grid;
    }

    public static GridNode[,] copyGrid(GridNode[,] grid)
    {
        if (grid == null)
            return null;

        GridNode[,] copiedGrid = new GridNode[grid.GetLength(0), grid.GetLength(1)];
        for (int i = 0;  i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {

                copiedGrid[i, j] = grid[i, j].CloneNode();
            }
        }
        return copiedGrid;
    }

    public static GridNode getNodeFromPoint(Vector3 position, GridNode[,] grid)
    {

        float percentX = (position.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (position.z + gridWorldSize.y / 2) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = (int)(percentX * gridSizeX);//Mathf.RoundToInt(percentX * (gridSizeX));
        int y = (int)(percentY * gridSizeY);//Mathf.RoundToInt(percentY * (gridSizeY));


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

    public static List<GridNode> getNeighbors(GridNode node, GridNode[,] grid)
    {
        if (node == null)
            return null;

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

    public static List<GridNode> GetOrtogonalNeighbors(GridNode node, GridNode[,] grid)
    {
        if (node != null)
        {
            List<GridNode> neighbours = new List<GridNode>();
            if (node.GridX - 1 >= 0)
                neighbours.Add(grid[node.GridX - 1, node.GridY]);
            if (node.GridY - 1 >= 0)
                neighbours.Add(grid[node.GridX, node.GridY - 1]);
            if (node.GridX + 1 < gridSizeX)
                neighbours.Add(grid[node.GridX + 1, node.GridY]);
            if (node.GridY + 1 < gridSizeY)
                neighbours.Add(grid[node.GridX, node.GridY + 1]);
            return neighbours;
        }
        return null;
    }

    public static int getDistance(GridNode node1, GridNode node2)
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

    public static GridNode closestWalkableNode(GridNode uwNode)
    {
        HashSet<GridNode> explored = new HashSet<GridNode>();
        Queue<GridNode> frontier = new Queue<GridNode>();

        frontier.Enqueue(uwNode);

        while( frontier.Count > 0 )
        {
            GridNode node = frontier.Dequeue();

            foreach (GridNode neighbour in getNeighbors(node, node.grid))
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


    // Disegno della griglia in forma visibile nello Scene Viewer dell'editor di Unity
    public List<GridNode> path;
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
        if (repGrid!= null)
        {

            GridNode playerNode = getNodeFromPoint(player.position, repGrid);
            foreach (GridNode n in repGrid)
            {
                if (n == playerNode)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(n.worldPos, Vector3.one * (nodeDiameter - 0.05F));
                }
                else
                {

                    Gizmos.color = n.walkable ? Color.gray : Color.red;
                    if (n.movementPenalty != 0)
                    {
                        Gizmos.color = Color.Lerp(Color.gray, Color.yellow, ((float)n.movementPenalty / MAX_PENALTY));
                    }
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

    private void applyBlur(int blurSize, GridNode[,] grid)
    {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        int[,] penaltiesHorizontalPass = new int[gridSizeX, gridSizeY];
        int[,] penaltiesVerticalPass = new int[gridSizeX, gridSizeY];

        for (int y = 0; y < gridSizeY; y++)
        {
            for (int x = -kernelExtents; x <= kernelExtents; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltiesHorizontalPass[0, y] += grid[sampleX, y].movementPenalty;
            }

            for (int x = 1; x < gridSizeX; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);

                penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty + grid[addIndex, y].movementPenalty;
            }
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = -kernelExtents; y <= kernelExtents; y++)
            {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
            }

            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
            grid[x, 0].movementPenalty = blurredPenalty;

            for (int y = 1; y < gridSizeY; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, y] / (kernelSize * kernelSize));
                grid[x, y].movementPenalty = blurredPenalty;

                if (blurredPenalty > MAX_PENALTY)
                {
                    MAX_PENALTY = blurredPenalty;
                }
                if (blurredPenalty < penaltyMin)
                {
                    penaltyMin = blurredPenalty;
                }
            }
        }

    }

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