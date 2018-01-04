using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class signScript : MonoBehaviour {
     Transform target;
    // Use this for initialization
    void Start () {
        target = GameObject.Find("MainCameraVR").GetComponent<Transform>();

    }
	
	// Update is called once per frame
	void Update () {
        Vector3 relativePos = target.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(relativePos);
        transform.rotation = rotation;
    }
}
