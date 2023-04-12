using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeCell_Behaviour : MonoBehaviour
{

    public GameObject[] walls;
    public void RemoveWall(int wallNEWS)
    {
        
        switch (wallNEWS)
        {
            case 0:
                walls[0].SetActive(false); break;
            case 1:
                walls[1].SetActive(false); break;
            case 2:
                walls[2].SetActive(false); break;
            case 3:
                walls[3].SetActive(false); break;
            default: break;
        }
    }
}
