using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Talking : MonoBehaviour {

    storeVariables variables;
    subtitleController subtitles;

    public TextAsset text;
    public TextAsset AfterGeneratorText;

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
            if (other.tag == "Player" && text.text.Length > 0)
            {
                string[] speech = text.text.Split('\n');
                for (int i = 0; i < speech.Length; i++)
                {
                    subtitles.updateQueue(speech[i], int.Parse(speech[i + 1]));
                    i++;
                }
            }
        }
        else
        {
            if (other.tag == "Player" && AfterGeneratorText.text.Length > 0)
            {
                string[] speech = AfterGeneratorText.text.Split('\n');
                for (int i = 0; i < speech.Length; i++)
                {
                    subtitles.updateQueue(speech[i], int.Parse(speech[i+1]));
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
