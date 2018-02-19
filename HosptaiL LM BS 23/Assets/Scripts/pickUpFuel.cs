using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class pickUpFuel : MonoBehaviour {

    storeVariables variables;
    Image TextCanvas;
    Text CameraText;

    void Start()
    {
        variables = GameObject.Find("Variables").GetComponent<storeVariables>();
        TextCanvas = GameObject.Find("CameraBackground").GetComponent<Image>();
        CameraText = GameObject.Find("CameraText").GetComponent<Text>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            StartCoroutine(showText());
            this.gameObject.GetComponent<MeshRenderer>().enabled = false;
            this.gameObject.GetComponent<BoxCollider>().enabled = false;
            variables.hasFuel = true;
        }
    }

    IEnumerator showText()
    {
        CameraText.text = "Picked up fuel";
        TextCanvas.enabled = true;
        CameraText.enabled = true;
        yield return new WaitForSeconds(3);
        TextCanvas.enabled = false;
        CameraText.enabled = false;
    }
}
