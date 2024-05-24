using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Player_Movement : MonoBehaviour
{
    public GameObject pathfindingReference;

    private Vector3[] path;
    private int currentIndex;
    public float moveSpeed = 10f;
    public bool isSelected;
    public searchAlgorithm searchType;
    Color originalColor;
    public LayerMask slowerMask;
    private GridNode[,] grid;

    private Animator animator;
    int isMoving;
    int isImpaired;
    // Start is called before the first frame update
    void Start()
    {
        if (pathfindingReference == null)
            pathfindingReference = GameObject.Find("PathfindingController");
        //Grid.multiGrid.Invoke();
        grid = pathfindingReference.GetComponent<Grid>().createGrid();
        //originalColor = GetComponent<Renderer>().material.color;


        isSelected = false;
        //Mouse_Controller.onSelection += selectionToggle;
        Maze_Generator.onMazeUpdate += () =>
        {
            //Grid.multiGrid.Invoke();
            //grid = pathfindingReference.GetComponent<Grid>().gridService[0];
            //pathfindingReference.GetComponent<Grid>().gridService.RemoveAt(0);
            grid = pathfindingReference.GetComponent<Grid>().createGrid();

        };
        Mouse_Controller.onObstaclesUpdate += () =>
        {
            grid = pathfindingReference.GetComponent<Grid>().createGrid();
        };
        //if (pathfindingReference.GetComponent<Grid>().gridService.Count == 0)

        //grid = pathfindingReference.GetComponent<Grid>().gridService[0];
        //pathfindingReference.GetComponent<Grid>().gridService.RemoveAt(0);

        animator = GetComponent<Animator>();
        isMoving = Animator.StringToHash("isMoving");
        isImpaired = Animator.StringToHash("isImpaired");
    }



    // Update is called once per frame
    void Update()
    {
        if (isSelected)
        {
            if (Input.GetMouseButtonUp((int)MouseButton.RightMouse))
            {
                
                    Vector3 mousePos = Input.mousePosition;
                    Ray ray = Camera.main.ScreenPointToRay(mousePos);
                    RaycastHit hit;


                    if (Physics.Raycast(ray, out hit))
                    {
                        mousePos = hit.point;
                        //Debug.Log("Ye hit here: " + mousePos.ToString());
                        //path = PRM.pathfind(transform.position, mousePos);
                        Pathfinding.onProcessingEnd?.Invoke();
                        Instantiate(pathfindingReference.GetComponent<Pathfinding>().objective, mousePos + Vector3.up, pathfindingReference.GetComponent<Pathfinding>().objective.transform.rotation);
                        PathRequestManager.StartPathRequest(new PathRequest(transform.position, mousePos, searchType, grid, startMovement));



                    }
                  

                }
            }

            if (Input.GetKeyDown(KeyCode.R)) 
            {
                StopAllCoroutines();
                transform.position = new Vector3(-23.5f, 0, -23.5f);
            }
       }

    

    public void startMovement(Vector3[] path, bool success)
    {
        if (success)
        {
           StopAllCoroutines();
            StartCoroutine(movePath(path));
        }
        
        
    }



    private void selectionToggle()
    {
        if (isSelected)
            GetComponent<Renderer>().material.color = Color.red;
        else
            GetComponent<Renderer>().material.color = originalColor;
    }
    private IEnumerator movePath(Vector3[] path)
    {

        for (currentIndex = 0; currentIndex < path.Length; currentIndex++)
        {
            animator.SetBool(isMoving, true);

            while (transform.position != path[currentIndex])
            {
                
                //if (Physics.CheckSphere(transform.position, 1.25f/*GetComponent<Collider>().bounds.max.x GetComponentInChildren<SkinnedMeshRenderer>().bounds.max.x*/, slowerMask))
                //{
                //    moveSpeed = 3.33f;
                //    animator.SetBool(isImpaired, true);
                //}
                //else
                //{
                //    moveSpeed = 10.0f;
                //    animator.SetBool(isImpaired, false);
                //}


               

                RaycastHit[] sphereHits;
                sphereHits = Physics.SphereCastAll(transform.position, 1.25f, Vector3.forward, 1.25f, slowerMask);
                
                if (sphereHits.Length == 0)
                {
                    moveSpeed = 10.0f;
                    animator.SetBool(isImpaired, false);
                } 
                else
                {
                    int movePenalty = 0;
                    foreach (RaycastHit sphereHit in sphereHits)
                    {
                        int nodePenalty = Grid.getNodeFromPoint(sphereHit.collider.transform.position, grid).movementPenalty;
                        movePenalty = Mathf.Max(movePenalty, nodePenalty);
                    }
                    float slowValue = (float) movePenalty / Grid.MAX_PENALTY;
                    moveSpeed = Mathf.Lerp(3.33f, 10,  1 - slowValue);
                    animator.SetBool(isImpaired, true);
                }
                transform.position = Vector3.MoveTowards(transform.position, path[currentIndex] /*- Vector3.up * path[currentIndex].y*/, moveSpeed * Time.deltaTime);
                transform.LookAt(path[currentIndex]);

                yield return null;
            }

        }

        animator.SetBool(isMoving, false);
        yield return null;
    }

    public void OnDrawGizmos()
    {

        if (path != null)
        {
            for (int i = currentIndex; i < path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(path[i], Vector3.one);

                if (i == currentIndex)
                {
                    Gizmos.DrawLine(transform.position, path[i]);
                }
                else
                {
                    Gizmos.DrawLine(path[i - 1], path[i]);
                }
            }
        }
    }

    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.layer == slowerMask.value)
    //        moveSpeed = 2.0f;
    //}
    //private void OnCollisionExit(Collision collision)
    //{
    //    if (collision.gameObject.layer == slowerMask.value)
    //        moveSpeed = 10.0f;

    //}
}





    

