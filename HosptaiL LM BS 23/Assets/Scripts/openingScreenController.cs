using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class openingScreenController : MonoBehaviour {

    public TextAsset text;
    public TextAsset instructions;
    private Text displayText;
    string[] paragraphs;

    public int state = 0;
    private float buttonTime = 0;

    // Use this for initialization
    void Start () {
        paragraphs = text.text.Split('\n');
        displayText = this.GetComponentInChildren<Text>();
        displayText.text = paragraphs[0];
        buttonTime = Time.time;
	}
	
	// Update is called once per frame
	void Update () {
        switch (state)
        {
            case 0:
                if (Time.time > buttonTime + 3)
                {
                    displayText.text = paragraphs[0] + "\n\n <b><color=#BBBBBBFF>Press any button to continue</color></b>";
                    if (Input.GetButton("Fire1"))
                    {
                        displayText.text = paragraphs[1];
                        state = 1;
                        buttonTime = Time.time;
                    }
                }
                break;

            case 1:
                if (Time.time > buttonTime + 3)
                {
                    displayText.text = paragraphs[1] + "\n\n <b><color=#BBBBBBFF>Press any button to continue</color></b>";
                    if (Input.GetButton("Fire1"))
                    {
                        displayText.text = instructions.text;
                        state = 2;
                        buttonTime = Time.time;
                    }
                }
                break;

            case 2:
                if (Time.time > buttonTime + 3)
                {
                    displayText.text = instructions.text + "\n\n <b><color=#BBBBBBFF>Press any button to start</color></b>";
                    if (Input.GetButton("Fire1"))
                    {
                        SceneManager.LoadScene("HospitaL AbsolutE 551f1");
                    }
                }
                break;    
        }
	}
}