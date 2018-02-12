using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class lookAtCamera : MonoBehaviour
{
    public Transform transform;
    Transform controller;

    private void Start()
    {
        controller = GameObject.Find("MainCameraVR").GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(controller.transform);
    }
}