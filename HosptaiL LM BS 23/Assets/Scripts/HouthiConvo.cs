using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HouthiConvo : MonoBehaviour {

    subtitleController subtitles;

    public TextAsset text;

    // Use this for initialization
    void Start()
    {
        subtitles = GameObject.Find("Subtitles").GetComponent<subtitleController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            string[] speech = text.text.Split('\n');
            for (int i = 0; i < speech.Length; i++)
            {
                subtitles.updateQueue(speech[i], int.Parse(speech[i + 1]));
                i++;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        subtitles.stopDisplay();
    }
}
