using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Talking : MonoBehaviour {

    Canvas textbox;
    Text workerText;
    storeVariables variables;
    public List<string> talking;
    public List<string> AfterGeneratortalking;

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
        if (!variables.generatorOn)
        {
            if (other.tag == "Player" && talking.Count > 0)
            {
                textbox.enabled = true;
                StartCoroutine(showPreBombText());
            }
        }
        else
        {
            if (other.tag == "Player" && AfterGeneratortalking.Count > 0)
            {
                textbox.enabled = true;
                StartCoroutine(showPostBombText());
            }
        }
        
    }

    IEnumerator showPreBombText()
    {
        foreach (string s in talking)
        {
            workerText.text = s;
            yield return new WaitForSeconds(7);
        }
    }

    IEnumerator showPostBombText()
    {
        foreach (string s in AfterGeneratortalking)
        {
            workerText.text = s;
            yield return new WaitForSeconds(7);
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
