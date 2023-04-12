using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Movement : MonoBehaviour
{
    public Transform playerTransform;
    private readonly  Vector3 centerPoint = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
    private Vector3 worldPoint;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        worldPoint = Camera.main.ScreenToWorldPoint(centerPoint);
        transform.LookAt(Vector3.Lerp(worldPoint, playerTransform.position, 0.03f));
    }
}
