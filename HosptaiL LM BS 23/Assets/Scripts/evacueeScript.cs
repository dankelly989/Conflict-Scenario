using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using VRStandardAssets.Utils;

public class evacueeScript : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            GameObject.Find("Evacuee1").GetComponent<walkingBehaviour>().enabled = true;
            GameObject.Find("Evacuee2").GetComponent<walkingBehaviour>().enabled = true;
        }
    }


}
