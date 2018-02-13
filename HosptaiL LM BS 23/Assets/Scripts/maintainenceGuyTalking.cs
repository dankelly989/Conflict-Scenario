﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class maintainenceGuyTalking : MonoBehaviour {

    Canvas textbox;
    Text workerText;
    storeVariables variables;
    public List<string> talking1;
    public List<string> talking2;

    // Use this for initialization
    void Start()
    {
        textbox = this.GetComponentInChildren<Canvas>();
        textbox.enabled = false;
        workerText = textbox.GetComponentInChildren<Text>();
        variables = GameObject.Find("Variables").GetComponent<storeVariables>();
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
        if (!variables.generatorOn)
        {
            variables.findFuel = true;
            foreach (string s in talking1)
            {
                workerText.text = s;
                yield return new WaitForSeconds(7);
            }
        }
        else
        {
            variables.talkedToGuy = true;
            foreach (string s in talking2)
            {
                workerText.text = s;
                yield return new WaitForSeconds(7);
            }
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