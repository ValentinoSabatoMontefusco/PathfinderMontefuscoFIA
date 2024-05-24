using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DropDown_Behaviour : MonoBehaviour
{
    GameObject[] playerList;
    Dictionary<int, searchAlgorithm> optionMap;
    private void OnEnable()
    {
        playerList = GameObject.FindGameObjectsWithTag("Player");
        optionMap = new Dictionary<int, searchAlgorithm>
        {
            { 0, searchAlgorithm.Astar },
            { 1, searchAlgorithm.BFS },
            { 2, searchAlgorithm.BFGreedy },
            { 3, searchAlgorithm.DFS },
            { 4, searchAlgorithm.UniformCost }
        };
        GetComponent<Dropdown>().onValueChanged.AddListener((value) =>
        {
            Player_Movement playerScript;

            foreach (GameObject player in playerList)
            {
                playerScript = player.GetComponent<Player_Movement>();
                if (playerScript.isSelected)
                    optionMap.TryGetValue(value, out playerScript.searchType);
            }

        });
    }

 
}
