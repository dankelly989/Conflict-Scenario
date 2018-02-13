using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour {

    storeVariables variables;
    GameObject handle;
    Image TextCanvas;
    Text CameraText;

    void Start()
    {
        variables = GameObject.Find("Variables").GetComponent<storeVariables>();
        handle = GameObject.Find("handle");
        TextCanvas = GameObject.Find("CameraBackground").GetComponent<Image>();
        CameraText = GameObject.Find("CameraText").GetComponent<Text>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && variables.hasFuel && !variables.generatorOn)
        {
            variables.generatorOn = true;
            handle.transform.rotation = new Quaternion(0.1f,-0.1f,-0.8f,0.6f);
            GetComponent<AudioSource>().Play();
            StartCoroutine(showText());
        }
    }

    IEnumerator showText()
    {
        CameraText.text = "Turned on generator";
        CameraText.enabled = true;
        TextCanvas.enabled = true;
        yield return new WaitForSeconds(3);
        TextCanvas.enabled = false;
        CameraText.enabled = false;
    }
}
