using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Talking : MonoBehaviour {

    storeVariables variables;
    subtitleController subtitles;
    public List<string> talking;
    public List<string> AfterGeneratortalking;

    public List<int> times;
    public List<int> AfterGeneratortimes;

    // Use this for initialization
    void Start()
    {
        variables = GameObject.Find("Variables").GetComponent<storeVariables>();
        subtitles = GameObject.Find("Subtitles").GetComponent<subtitleController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!variables.generatorOn)
        {
            if (other.tag == "Player" && talking.Count > 0)
            {
                int i = 0;
                foreach (string s in talking)
                {
                    subtitles.updateQueue(s,times[i]);
                    i++;
                }
            }
        }
        else
        {
            if (other.tag == "Player" && AfterGeneratortalking.Count > 0)
            {
                int i = 0;
                foreach (string s in AfterGeneratortalking)
                {
                    subtitles.updateQueue(s,AfterGeneratortimes[i]);
                    i++;
                }
            }
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            subtitles.stopDisplay();
        }
    }
}
