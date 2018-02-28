using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class openingScreenController : MonoBehaviour {

    public TextAsset text;
    private Text displayText;
    string[] paragraphs;

    private bool buttonPressed = false;
    private float buttonTime = 0;

    // Use this for initialization
    void Start () {
        paragraphs = text.text.Split('\n');
        displayText = this.GetComponentInChildren<Text>();
        displayText.text = paragraphs[0] + "\n\n <b><color=#BBBBBBFF>Press any button to continue</color></b>";
	}
	
	// Update is called once per frame
	void Update () {
        if (!buttonPressed)
        {
            if (Input.GetButton("Fire1"))
            {
                displayText.text = paragraphs[1];
                buttonPressed = true;
                buttonTime = Time.time;
            }
        }
        else if (Time.time > buttonTime + 3)
        {
            displayText.text = paragraphs[1] + "\n\n <b><color=#BBBBBBFF>Press any button to start</color></b>";
            if (Input.GetButton("Fire1"))
            { 
                SceneManager.LoadScene("HospitaL AbsolutE 551f1");
            }
        }
	}
}