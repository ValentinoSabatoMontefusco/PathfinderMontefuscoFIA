using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Player_Movement : MonoBehaviour
{
    public GameObject pathfindingReference;
    private PathRequestManager PRM;
    private Vector3[] path;
    private int currentIndex;
    public float moveSpeed = 5f;
    public bool isSelected;
    public searchAlgorithm searchType;
    Color originalColor;
    // Start is called before the first frame update
    void Start()
    {
        originalColor = GetComponent<Renderer>().material.color;
        PRM = pathfindingReference.GetComponent<PathRequestManager>();
        if (PRM == null)
            Debug.Log("Player missing path request manager");

        isSelected = false;
        Mouse_Controller.onSelection += selectionToggle;
    }



    // Update is called once per frame
    void Update()
    {
        if (isSelected)
        {
            if (Input.GetMouseButtonUp((int)MouseButton.RightMouse))
            {
                if (PRM != null)
                {
                    Vector3 mousePos = Input.mousePosition;
                    Ray ray = Camera.main.ScreenPointToRay(mousePos);
                    RaycastHit hit;


                    if (Physics.Raycast(ray, out hit))
                    {
                        mousePos = hit.point;
                        //Debug.Log("Ye hit here: " + mousePos.ToString());
                        //path = PRM.pathfind(transform.position, mousePos);
                        PathRequestManager.StartPathRequest(transform.position, mousePos, searchType, startMovement);
                     


                    }
                    else
                    {
                        //Debug.Log("Ye didn hit nuffin', mate");
                    }


                }
            }
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

            while (transform.position != path[currentIndex])
            {
                transform.position = Vector3.MoveTowards(transform.position, path[currentIndex], moveSpeed * Time.deltaTime);

                yield return null;
            }

        }

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
}





    

