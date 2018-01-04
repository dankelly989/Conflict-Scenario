using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audioPlaying : MonoBehaviour {
    AudioSource audio;
    public float startTime=0;

    // Use this for initialization
    void Start () {
        audio = GetComponent<AudioSource>();
        audio.time = startTime;
        audio.Play();
    }
	
	// Update is called once per frame
	void Update () {
        Debug.Log(audio.time);
    }
}
