using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HouthiConvo : MonoBehaviour {

    subtitleController subtitles;

    // Use this for initialization
    void Start()
    {
        subtitles = GameObject.Find("Subtitles").GetComponent<subtitleController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            subtitles.updateQueue("Houthi 1:\nWhat happened upstairs? It sounded like a bomb.",6);
            subtitles.updateQueue("Houthi 2:\nOf course it was a bomb you Hmar kelb tfou",5);
            subtitles.updateQueue("Houthi 3:\nThe government forces must suspect we are down here",6);
            subtitles.updateQueue("Houthi 4:\nYa Ibn el Sharmouta!",3);
            subtitles.updateQueue("Houthi 2:\nThe power is out. People are dying. We need to get it running straight away",7);
            subtitles.updateQueue("Houthi 1:\n<b><color=#8D0000FF>Take this fuel to Aamir in the generator room next door</color></b>. We sent him in there earlier but he hasn’t come back", 7);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        subtitles.stopDisplay();
    }
}
