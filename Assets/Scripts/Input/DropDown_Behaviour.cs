using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DropDown_Behaviour : MonoBehaviour
{
    GameObject[] playerList;
    Dictionary<int, searchAlgorithm> optionMap;
    private void OnEnable()
    {
        optionMap = new();
        playerList = GameObject.FindGameObjectsWithTag("Player");
        int i = 0;
        foreach (searchAlgorithm sA in System.Enum.GetValues(typeof(searchAlgorithm)))
        {
            optionMap.Add(i, sA);
            i++;
        }

        string[] algNames = System.Enum.GetNames(typeof(searchAlgorithm));

        string debug = "GetNames produced the following: ";
        
        List<Dropdown.OptionData> options = new();
        foreach (string name in algNames)
        {
            options.Add(new Dropdown.OptionData(name));
            debug += name + ", ";
        }
        Debug.Log(debug);

        GetComponent<Dropdown>().options = options;
            

        //optionMap = new Dictionary<int, searchAlgorithm>
        //{
        //    { 0, searchAlgorithm.Astar },
        //    { 1, searchAlgorithm.BFS },
        //    { 2, searchAlgorithm.BFGreedy },
        //    { 3, searchAlgorithm.DFS },
        //    { 4, searchAlgorithm.UniformCost }
        //};
        
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
