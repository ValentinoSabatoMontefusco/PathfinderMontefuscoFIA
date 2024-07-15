using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    [SerializeField]
    GameObject infoPanel;
    [SerializeField]
    GameObject mainCanvas;
    [SerializeField]
    GameObject obstacles;
    [SerializeField]
    List<GameObject> optionButtons;

    GameObject speedSlider;


    public float wait_time = 0.01f;
    private float nodeRadius;

    private static GameObject content;
    private static Queue<(string, searchAlgorithm)> statsQueue = new();


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

    public static Action onObstaclesUpdate;
    public static Action<string, searchAlgorithm> onConsoleWritePath;
    public static Action<string> onConsoleWrite;

    public static Action onGraphClick;
    public static Action onSpeedClick;

    public static Action onObstaclesClick;
    public static Action onMazeClick;
    public static Action onResetClick;


    public void Awake()
    {
        content = GameObject.Find("Content");
        onGraphRepChange += OnGraphRepChange;
        onQueueNonEmpty += startDrawing;
        GridNode.onNodeStateChange += enqueueNodeDraw;
        PathRequestManager.onPathRequestSet += HandlePathRequest;
        Pathfinding.onStatsReady += EnqueueStats;

        onObstaclesClick += OnObstaclesClick;
        onGraphClick += OnGraphClick;
        onSpeedClick += OnSpeedClick;
        onMazeClick += OnMazeClick;
        onResetClick += OnResetClick;

        speedSlider = mainCanvas.transform.Find("SpeedSlider").gameObject;
        


    }

    private void Update()
    {
        if (statsQueue.Count > 0)                                                                   // SCRITTURA IN "CONSOLE" DELL'ESITO DI UNA CHIAMATA DI PATHFINDING
        {
            (string, searchAlgorithm) statBundle = statsQueue.Dequeue();
            onConsoleWritePath?.Invoke(statBundle.Item1, statBundle.Item2);
           
            
        }
    }
    
    private void EnqueueStats(string stats, searchAlgorithm searchAlg)
    {
        statsQueue.Enqueue((stats, searchAlg));
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

                TextMeshPro TMPComp = nodeToDraw.drawnNode.GetComponentInChildren<TextMeshPro>();
                switch (drawReq.pathRequest.searchType)
                {
                    
                    case searchAlgorithm.IDDFS: if (nodeToDraw.depth != 0)
                        {
                            TMPComp.text = nodeToDraw.depth.ToString();
                        }
                        break;

                    case searchAlgorithm.BFGreedy:
                    case searchAlgorithm.Astar:
                    case searchAlgorithm.UniformCost:
                    case searchAlgorithm.IDAstar:
                    case searchAlgorithm.BeamSearch: if (nodeToDraw.f_cost != 0)
                        {
                            TMPComp.text = nodeToDraw.f_cost.ToString();
                            if (nodeToDraw.g_cost != 0 && nodeToDraw.h_cost != 0)
                            {
                                TMPComp.text += "\n" + nodeToDraw.g_cost.ToString() + " + " + nodeToDraw.h_cost.ToString();
                            }
                        }
                        break;
                    case searchAlgorithm.RBFS: if (nodeToDraw.f_cost > 0)
                        {
                            TMPComp.text = nodeToDraw.f_cost.ToString() + "\n (" + nodeToDraw.F_cost.ToString() + ")";
                        } break;
                    default: break;
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
                yield return new WaitForSeconds(wait_time < 1 ? wait_time * wait_time : wait_time);
          
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
        wait_time = 2.001f - newTime;
    }



    public void ToggleActiveComponent(GameObject go)
    {
        go.SetActive(!go.activeSelf);
    }


    // OPTIONS MENU SECTION

    public void OnMazeClick()
    {
        if (obstacles.activeSelf)
            return;

        GetComponent<Maze_Generator>().DestroyMazeGrid();
        GetComponent<Maze_Generator>().CreateMazeGrid();
        GetComponent<Maze_Generator>().StartCoroutine(GetComponent<Maze_Generator>().DFSMazeGeneration((GetComponent<Maze_Generator>().MazeGrid[0, 0])));
    }

    public void OnObstaclesClick()
    {
        if (GetComponent<Maze_Generator>().MazeGrid != null)
        {
            Debug.Log("Generazione ostacoli incompatibile con labirinto. Eliminarlo col tasto 'N'.");
        }
        else
        {
            bool obsties = obstacles.activeSelf;
            obstacles.SetActive(!obsties);
            onObstaclesUpdate?.Invoke();

            onConsoleWrite("Obstacles mode " + (obstacles.activeSelf ? "enabled" : "disabled"));

        }
    }

    public void OnSpeedClick()
    {

        if (Time.timeScale != 1.0f)
        {
            Time.timeScale = 1.0f;
            onConsoleWrite("Speed returned to normal");
        }
        else
        {
            Time.timeScale = 3.0f;
            onConsoleWrite("Speed increased");
        }

    }

    public void OnGraphClick()
    {
        bool toggleValue;
        toggleValue = PresentationLayer.GraphRep ? false : true;
        PresentationLayer.GraphRep = toggleValue;
        
        speedSlider.SetActive(toggleValue);
        optionButtons[2].GetComponentInChildren<TextMeshProUGUI>().text = (toggleValue ? "Disable" : "Enable") + " Showcase";
        onConsoleWrite("Showcase mode " + (toggleValue ? "enabled" : "disabled"));
    }

    public void OnResetClick()
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            player.GetComponent<Player_Movement>().ResetPlayer();
        }

       
    }
}
