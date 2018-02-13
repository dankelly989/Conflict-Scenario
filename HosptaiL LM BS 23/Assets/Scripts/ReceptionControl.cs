using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReceptionControl : MonoBehaviour {

    public List<Flicker> lights;
    Canvas textbox;
    Text workerText;
    GameObject cones;
    storeVariables variables;
    bool visited = false;
    public List<string> talking;
    Image TextCanvas;
    Text CameraText;

    // Use this for initialization
    void Start () {
        textbox = GameObject.Find("WorkerTextBox").GetComponent<Canvas>();
        textbox.enabled = false;
        workerText = GameObject.Find("WorkerText").GetComponent<Text>();
        cones = GameObject.Find("cones");
        cones.SetActive(false);
        variables = GameObject.Find("Variables").GetComponent<storeVariables>();
        TextCanvas = GameObject.Find("CameraBackground").GetComponent<Image>();
        CameraText = GameObject.Find("CameraText").GetComponent<Text>();

        foreach (Flicker f in lights)
        {
            f.Startflicker();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            textbox.enabled = true;
            StartCoroutine(showText());
        }
    }

    IEnumerator showText()
    {
        if (!visited)
        {
            foreach (string s in talking)
            {
                workerText.text = s;
                yield return new WaitForSeconds(7);
            }
        }

        if (variables.pickedUpCones)
        {
            if (!cones.active)
            {
                workerText.text = "Pass me those cones. I can use them to mark off the area so no one wanders into this bomb and I can get back to work.";
                yield return new WaitForSeconds(7);
                cones.SetActive(true);
                visited = false;
                StartCoroutine(showCameraText());
            }
        }
        else
        {
            workerText.text = "Go find some orange cones to mark this one off so I don’t have to keep standing in front of it.";
            visited = true;
        }
    }

        private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            textbox.enabled = false;
        }
    }

    IEnumerator showCameraText()
    {
        CameraText.text = "Put down cones";
        TextCanvas.enabled = true;
        CameraText.enabled = true;
        yield return new WaitForSeconds(3);
        TextCanvas.enabled = false;
        CameraText.enabled = false;
    }
}
