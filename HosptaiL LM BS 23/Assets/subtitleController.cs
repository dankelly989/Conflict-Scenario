using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class subtitleController : MonoBehaviour {
    Text subtitleText;
    private Queue<string> textqueue = new Queue<string>();
    private Queue<int> timequeue = new Queue<int>();
    bool active = false;
	
    // Use this for initialization
	void Start ()
    {
        subtitleText = this.GetComponentInChildren<Text>();
	}


    private void Update()
    {
        if (!active)
        {
            StartCoroutine("DisplayText");
        }
    }

    public void stopDisplay()
    {
        textqueue.Clear();
        timequeue.Clear();
        this.GetComponent<Image>().enabled = false;
        subtitleText.enabled = false;
        active = false;
        StopCoroutine("DisplayText");
    }

    public void updateQueue(string s, int i = 7)
    {
        textqueue.Enqueue(s);
        timequeue.Enqueue(i);
    }

    IEnumerator DisplayText()
    {
        if (textqueue.Count != 0)
        {
            active = true;
            this.GetComponent<Image>().enabled = true;
            subtitleText.enabled = true;
            subtitleText.text = textqueue.Dequeue();
            yield return new WaitForSeconds(timequeue.Dequeue());
            this.GetComponent<Image>().enabled = false;
            subtitleText.enabled = false;
            active = false;
        }
    }
}
