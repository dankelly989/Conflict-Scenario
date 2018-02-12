using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Talking : MonoBehaviour {

    Canvas textbox;
    Text workerText;
    public List<string> talking;

    // Use this for initialization
    void Start()
    {
        textbox = this.GetComponentInChildren<Canvas>();
        textbox.enabled = false;
        workerText = textbox.GetComponentInChildren<Text>();
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
        foreach (string s in talking)
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
