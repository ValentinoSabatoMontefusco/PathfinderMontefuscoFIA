using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
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

    Vector3 lastMousePos;
    [SerializeField]
    LayerMask nodeRepMask;
    Camera topDownCamera;
    RawImage topPerspective;
    bool isHovering;

   
    

    // Start is called before the first frame update
    void Start()
    {
        topDownCamera = GameObject.Find("Top-Down Camera").GetComponent<Camera>();
        topPerspective = GameObject.Find("NodePerspective").GetComponent<RawImage>();
        players = GameObject.FindGameObjectsWithTag("Player");
        isPressed = false;

        
    }

    // Update is called once per frame
    void Update()

    {
        NodeHoverCheck();
        if (Input.GetMouseButtonDown(0))
            mousePos0 = Input.mousePosition;

        if (Input.GetKeyDown(KeyCode.M))
        {
            PresentationLayer.onMazeClick?.Invoke();

        }

        if (Input.GetKeyDown(KeyCode.N)) 
        {
            GetComponent<Maze_Generator>().DestroyMazeGrid();

        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            PresentationLayer.onObstaclesClick?.Invoke();

        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            PresentationLayer.onSpeedClick?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            PresentationLayer.onGraphClick?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.R)) {

            PresentationLayer.onResetClick?.Invoke();

        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            if (PresentationLayer.repSlowDown == true)
            {
                PresentationLayer.repSlowDown = false;
                Debug.Log("Rallentamento grafico disabilitata");
            }
            else
            {
                PresentationLayer.repSlowDown = true;
                Debug.Log("Rallentamento grafico abilitata");
            }
        }

        clickOnSinglePlayer();
        clickDragRectangle();

        lastMousePos = Input.mousePosition;
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

    


    private void NodeHoverCheck()
    {
        if (Input.mousePosition == lastMousePos)
            return;

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, nodeRepMask))
        {
            isHovering = true;
            topDownCamera.transform.position = hit.collider.transform.position + Vector3.up * 10;
            topDownCamera.orthographicSize = hit.collider.gameObject.transform.localScale.x/2;
        }
        else
        {
            isHovering = false;
        }

        topPerspective.gameObject.SetActive(isHovering);


    }

   
}

