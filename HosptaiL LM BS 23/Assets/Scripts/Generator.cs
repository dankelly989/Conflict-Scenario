using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour {

    storeVariables variables;
    GameObject handle;
    Image TextCanvas;
    Text CameraText;
    public List<Flicker> lights;
    GameObject siren;
    DoubleDoorOpen hall;

    void Start()
    {
        variables = GameObject.Find("Variables").GetComponent<storeVariables>();
        handle = GameObject.Find("handle");
        TextCanvas = GameObject.Find("CameraBackground").GetComponent<Image>();
        CameraText = GameObject.Find("CameraText").GetComponent<Text>();
        siren = GameObject.Find("Siren");
        siren.GetComponent<AudioSource>().enabled = false;
        hall = GameObject.Find("EndOfHallDoor").GetComponent<DoubleDoorOpen>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && variables.hasFuel && !variables.generatorOn)
        {
            variables.generatorOn = true;
            handle.transform.rotation = new Quaternion(0.1f,-0.1f,-0.8f,0.6f);
            this.GetComponentInChildren<AudioSource>().enabled = true;
            StartCoroutine(showText());
            RenderSettings.ambientLight = new Color(0.3647059f, 0.3647059f, 0.3647059f, 1);
            //lights go off
            foreach (Flicker f in lights)
            {
                f.Stopflicker();
                f.turnOn();
            }
            siren.GetComponent<AudioSource>().enabled = true;
            hall.active = true;
        }
    }

    IEnumerator showText()
    {
        CameraText.text = "Refueled generator";
        CameraText.enabled = true;
        TextCanvas.enabled = true;
        yield return new WaitForSeconds(3);
        TextCanvas.enabled = false;
        CameraText.enabled = false;
    }
}
