using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class clearQueue : MonoBehaviour {
    subtitleController subtitles;

    // Use this for initialization
    void Start () {
        subtitles = GameObject.Find("Subtitles").GetComponent<subtitleController>();
    }

    private void OnTriggerExit(Collider other)
    {
        subtitles.stopDisplay();
    }
}
