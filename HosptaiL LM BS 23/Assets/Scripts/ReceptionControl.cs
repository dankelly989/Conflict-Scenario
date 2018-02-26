using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReceptionControl : MonoBehaviour {

    public List<Flicker> lights;
    GameObject cones;
    storeVariables variables;
    bool visited = false;
    public List<string> talking;
    Image TextCanvas;
    Text CameraText;

    subtitleController subtitles;

    // Use this for initialization
    void Start () {
        cones = GameObject.Find("cones");
        cones.SetActive(false);
        variables = GameObject.Find("Variables").GetComponent<storeVariables>();
        TextCanvas = GameObject.Find("CameraBackground").GetComponent<Image>();
        CameraText = GameObject.Find("CameraText").GetComponent<Text>();

        subtitles = GameObject.Find("Subtitles").GetComponent<subtitleController>();

        foreach (Flicker f in lights)
        {
            f.Startflicker();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            showText();
        }
    }

    private void showText()
    {
        if (!visited)
        {
            subtitles.stopDisplay();
            foreach (string s in talking)
            {
                subtitles.updateQueue(s,7);
            }
        }

        if (variables.pickedUpCones)
        {
            if (!cones.active)
            {
                subtitles.updateQueue("Pass me those cones. I can use them to mark off the area so no one wanders into this bomb. <b><color=#8D0000FF>Now we must leave immediately.</color></b>", 7);
                cones.SetActive(true);
                variables.placedCones = true;
                visited = false;
                StartCoroutine(showCameraText());   
            }
        }
        else
        {
            subtitles.updateQueue("Go <b><color=#8D0000FF>find some orange cones</color></b> to mark this one off so I don’t have to keep standing in front of it. <b><color=#8D0000FF>Try the closet by reception</color></b>", 7);
            visited = true;
        }
    }

    IEnumerator showCameraText()
    {
        CameraText.text = "Put down cones";
        TextCanvas.enabled = true;
        CameraText.enabled = true;
        yield return new WaitForSeconds(3);
        TextCanvas.enabled = false;
        CameraText.enabled = false;
    }
}
