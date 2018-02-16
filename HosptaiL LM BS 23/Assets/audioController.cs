using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audioController : MonoBehaviour {

    void OnTriggerEnter(Collider other)
    {
        GetComponent<AudioSource>().Play();
    }

    void OnTriggerExit(Collider other)
    {
        GetComponent<AudioSource>().Stop();
    }
}
