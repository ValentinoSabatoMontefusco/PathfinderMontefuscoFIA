using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


// SCRIPT CHE GESTISE IL COMPORTAMENTO DEL MENU' A TENDINA DI SELEZIONE DELL'ALGORITMO DI PATHFINDING
public class DropDown_Behaviour : MonoBehaviour
{
    GameObject[] playerList;                                        // Lista di giocatori per lo scenario di una situazione multi-agente
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
        
        List<Dropdown.OptionData> options = new();                          // La lista di opzioni del menù a tendina è aggiornata dinamicamente in base agli algoritmi effettivamente implementati
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
        
        GetComponent<Dropdown>().onValueChanged.AddListener((value) =>                  // Listener che, alla selezione di un algoritmo, lo applica a tutti gli agenti selezionati
        {                                                                               // oppure all'unico agente nella scena, se è solo
            Player_Movement playerScript;
            searchAlgorithm chosenAlgorithm;
            optionMap.TryGetValue(value, out chosenAlgorithm);

            if (playerList.Length > 1)
            {
                foreach (GameObject player in playerList)
                {
                    playerScript = player.GetComponent<Player_Movement>();
                    if (playerScript.isSelected)
                        playerScript.searchType = chosenAlgorithm;
                        //optionMap.TryGetValue(value, out playerScript.searchType);
                }
                return;
            }

            playerScript = playerList[0].GetComponent<Player_Movement>();
            playerScript.searchType = chosenAlgorithm;
        //optionMap.TryGetValue(value, out playerScript.searchType);

        PresentationLayer.onConsoleWritePath?.Invoke("Pathfinding Algorithm swapped to " + Pathfinding.AlgToString(chosenAlgorithm), chosenAlgorithm);

        });
    }

 
}
