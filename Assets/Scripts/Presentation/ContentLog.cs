using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContentLog : MonoBehaviour
{

    [SerializeField]
    GameObject infoPanel;
    [SerializeField]
    GameObject controller;
    // Start is called before the first frame update

    public static Dictionary<searchAlgorithm, Color> algorithmColors = new Dictionary<searchAlgorithm, Color>
    {
        { searchAlgorithm.BFGreedy, new Color(0.70f,0.70f,0.00f) },
        { searchAlgorithm.BFS, new Color(0.67f,0.15f,0.31f) },
        { searchAlgorithm.Astar, Color.blue },
        { searchAlgorithm.UniformCost, new Color(1.00f,0.40f,0.10f) },
        { searchAlgorithm.DFS, new Color(0.30f,0.30f,0.00f) },
        { searchAlgorithm.IDDFS, new Color(0.80f,0.40f,0.00f) },
        { searchAlgorithm.RecursiveDFS, new Color(0.15f,0.30f,0.00f) },
        { searchAlgorithm.BeamSearch, new Color(0.84f,0.80f,1.00f) },
        { searchAlgorithm.IDAstar, new Color(0.30f,1.00f,0.88f) },
        { searchAlgorithm.RBFS, new Color(0.60f,0.00f,0.90f) }
    };
    void Start()
    {
        
        PresentationLayer.onConsoleWritePath += WriteContent;
        PresentationLayer.onConsoleWrite += WriteContent;
    }

    public void WriteContent(string text)
    {
        GameObject newPanel = Instantiate(infoPanel, transform);

        newPanel.GetComponentInChildren<TextMeshProUGUI>().text = text;
        Color newColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        newPanel.GetComponent<Image>().color = newColor;
    }
    public void WriteContent(string text, searchAlgorithm searchType)
    {
        GameObject newPanel = Instantiate(infoPanel, transform);
       
        newPanel.GetComponentInChildren<TextMeshProUGUI>().text = text;
        Color newColor = algorithmColors[searchType];
        newColor.a = 0.55f;
        newPanel.GetComponent<Image>().color = newColor;
    }


}
