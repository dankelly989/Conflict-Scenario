using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audioController : MonoBehaviour {

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            GetComponent<AudioSource>().Play();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            GetComponent<AudioSource>().Stop();
        }
    }
}
