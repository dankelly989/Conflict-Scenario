using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReceptionControl : MonoBehaviour {

    public List<Flicker> lights;
    Canvas textbox;
    Text workerText;

    // Use this for initialization
    void Start () {
        textbox = GameObject.Find("WorkerTextBox").GetComponent<Canvas>();
        textbox.enabled = false;
        workerText = GameObject.Find("WorkerText").GetComponent<Text>();
        workerText.text = "Quasimodo:\nEEEEEEKKKKKKKKSSSSSS This is a cluster bomb. Jinkies you should make it so people know to stay away.";

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
