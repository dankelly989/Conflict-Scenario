using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReceptionControl : MonoBehaviour {

    public List<Flicker> lights;
    Canvas textbox;
    Text workerText;
    GameObject cones;
    PickUpCone controller;
    bool visited = false;
    public List<string> talking;

    // Use this for initialization
    void Start () {
        textbox = GameObject.Find("WorkerTextBox").GetComponent<Canvas>();
        textbox.enabled = false;
        workerText = GameObject.Find("WorkerText").GetComponent<Text>();
        cones = GameObject.Find("cones");
        cones.SetActive(false);
        controller = GameObject.Find("RigidBodyFPSController").GetComponent<PickUpCone>();

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

        if (controller.cones)
        {
            if (!cones.active)
            {
                workerText.text = "Pass me those cones. I can use them to mark off the area so no one wanders into this bomb and I can get back to work.";
                yield return new WaitForSeconds(7);
                cones.SetActive(true);
                visited = false;
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
}
