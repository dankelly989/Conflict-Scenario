using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class endSequence : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}

    void OnTriggerEnter(Collider other)
    {
        Application.Quit();
    }
}
