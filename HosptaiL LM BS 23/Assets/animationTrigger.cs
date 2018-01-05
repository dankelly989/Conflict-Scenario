using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animationTrigger : MonoBehaviour {

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            GameObject.Find("YemenDoctor").GetComponent<Animator>().SetTrigger("PlayerEnter");
            GameObject.Find("TextBoxCanvas").SetActive(true);
        }
    }
}
