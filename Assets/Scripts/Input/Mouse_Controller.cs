using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Mouse_Controller : MonoBehaviour
{

    private Ray ray;
    private RaycastHit hit;
    private GameObject[] players;
    //public delegate void OnSelectionToggle();
    //public static event OnSelectionToggle onSelection;
    public static Action onSelection;
    public Canvas canvas;
    private Rect selectionRect;
    private Vector2 mousePos0;
    Vector2 center;
    float xMin;
    float yMin;
    float width;
    float height;
    public GameObject obstacles;
    bool isPressed;

    public static Action onObstaclesUpdate;

    // Start is called before the first frame update
    void Start()
    {
        players = GameObject.FindGameObjectsWithTag("Player");
        isPressed = false;
    }

    // Update is called once per frame
    void Update()

    {
        if (Input.GetMouseButtonDown(0))
            mousePos0 = Input.mousePosition;

        if (Input.GetKeyDown(KeyCode.M))
        {
            GetComponent<Maze_Generator>().DestroyMazeGrid();
            GetComponent<Maze_Generator>().CreateMazeGrid();
            GetComponent<Maze_Generator>().StartCoroutine(GetComponent<Maze_Generator>().DFSMazeGeneration((GetComponent<Maze_Generator>().MazeGrid[0, 0])));

        }

        if (Input.GetKeyDown(KeyCode.N)) 
        {
            GetComponent<Maze_Generator>().DestroyMazeGrid();

        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            if (GetComponent<Maze_Generator>().MazeGrid != null)
            {
                Debug.Log("Generazione ostacoli incompatibile con labirinto. Eliminarlo col tasto 'N'.");
            } 
            else
            {
                if (obstacles.activeSelf == false)
                    obstacles.SetActive(true);
                else
                    obstacles.SetActive(false);

                onObstaclesUpdate?.Invoke();

            }

        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Time.timeScale = Time.timeScale == 1.0f ? 3.0f : 1.0f;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (GetComponent<Pathfinding>().graphRep)
            {
                GetComponent<Pathfinding>().graphRep = false;
                Debug.Log("Rapp. grafica disabilitata");
            }
            else
            {
                GetComponent<Pathfinding>().graphRep = true;
                Debug.Log("Rapp. grafica abilitata");
            }
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            if (GetComponent<Pathfinding>().repSlowDown == true)
            {
                GetComponent<Pathfinding>().repSlowDown = false;
                Debug.Log("Rallentamento grafico disabilitata");
            }
            else
            {
                GetComponent<Pathfinding>().repSlowDown = true;
                Debug.Log("Rallentamento grafico abilitata");
            }
        }

        clickOnSinglePlayer();
        clickDragRectangle();
        
    }


    private void clickDragRectangle()
    {
        
        //if (Input.GetMouseButton(0))
        //{
        //    isPressed = true;
        //    mousePos0 = Input.mousePosition;
        //}

        if (Input.GetMouseButtonUp(0) /*&& isPressed */&& (Vector2) Input.mousePosition != mousePos0)
        {
            isPressed = false;
            canvas.GetComponentInChildren<Image>().rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
            canvas.GetComponentInChildren<Image>().rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);
            canvas.GetComponentInChildren<Image>().rectTransform.localPosition = Vector2.zero;
            Rect checkRect = new Rect(xMin + Screen.width / 2, yMin + Screen.height / 2, width, height);

            foreach (GameObject player in players)
            {
                if (checkRect.Contains(((Vector2)Camera.main.WorldToScreenPoint(player.transform.position))))
                    player.GetComponent<Player_Movement>().isSelected = true;
                else
                    player.GetComponent<Player_Movement>().isSelected = false;

            }
            
            onSelection?.Invoke();

        }

        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;

            xMin = Mathf.Min(mousePos0.x, mousePos.x) - Screen.width / 2;
            yMin = Mathf.Min(mousePos0.y, mousePos.y) - Screen.height / 2;
            width = Mathf.Abs(mousePos.x - mousePos0.x);
            height = Mathf.Abs(mousePos.y - mousePos0.y);

            center = new Vector2(xMin + width / 2, yMin + height / 2);
            
            canvas.GetComponentInChildren<Image>().rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            canvas.GetComponentInChildren<Image>().rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            canvas.GetComponentInChildren<Image>().rectTransform.localPosition = center;
        }
    }

    //void OnGUI()
    //{
    //    if (isPressed)
    //    {
    //        GUIStyle rectStyle = new GUIStyle();
    //        rectStyle.normal.background = Texture2D.whiteTexture;
    //        rectStyle.normal.textColor = UnityEngine.Color.white;
    //        rectStyle.alignment = TextAnchor.MiddleCenter;
    //        rectStyle.border = new RectOffset(2, 2, 2, 2);

    //        GUI.Box(selectionRect, GUIContent.none, rectStyle);
    //    }
    //}
    private void clickOnSinglePlayer()
    {
        if (Input.GetMouseButtonUp((int)MouseButton.Left) && (Vector2) Input.mousePosition == mousePos0)
        {
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                GameObject selectedPlayer = null;
                if (hit.collider.gameObject.tag == "Player")
                {
                    selectedPlayer = hit.collider.gameObject;
                }

                foreach (GameObject player in players)
                {
                    if (player != selectedPlayer)
                        player.GetComponent<Player_Movement>().isSelected = false;
                }

                if (selectedPlayer != null)
                {
                    selectedPlayer.GetComponent<Player_Movement>().isSelected = true;
                }
                onSelection?.Invoke();
                //Debug.Log("Mouse position (x,y) = " + Input.mousePosition.x.ToString() + ", " + Input.mousePosition.y.ToString());
                //Debug.Log("xMin = " + xMin.ToString() +", yMin = " + yMin.ToString());
            }
        }
    }
}

