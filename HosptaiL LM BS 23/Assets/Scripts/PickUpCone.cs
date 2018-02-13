using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PickUpCone : MonoBehaviour {

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
        if (other.tag == "Player" && variables.generatorOn)
        {
            StartCoroutine(showText());
            this.gameObject.GetComponentInChildren<MeshRenderer>().enabled = false;
            this.gameObject.GetComponent<BoxCollider>().enabled = false;
            variables.pickedUpCones = true;
        }
    }

    IEnumerator showText()
    {
        CameraText.text = "Picked up cones";
        TextCanvas.enabled = true;
        CameraText.enabled = true;
        yield return new WaitForSeconds(3);
        TextCanvas.enabled = false;
        CameraText.enabled = false;
    }
}
