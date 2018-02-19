using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HouthiConvo : MonoBehaviour {

    Canvas Houthi1;
    Canvas Houthi2;
    Canvas Houthi3;
    Canvas Houthi4;

    bool activated = false;

    // Use this for initialization
    void Start()
    {
        Houthi1 = GameObject.Find("Houthi1").GetComponentInChildren<Canvas>();
        Houthi1.enabled = false;
        Houthi2 = GameObject.Find("Houthi2").GetComponentInChildren<Canvas>();
        Houthi2.enabled = false;
        Houthi3 = GameObject.Find("Houthi3").GetComponentInChildren<Canvas>();
        Houthi3.enabled = false;
        Houthi4 = GameObject.Find("Houthi4").GetComponentInChildren<Canvas>();
        Houthi4.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !activated)
        {
            StartCoroutine(showText());
        }
    }

    IEnumerator showText()
    {
        activated = true;

        Houthi3.enabled = true;
        Houthi3.GetComponentInChildren<Text>().text = "What happened upstairs? It sounded like a bomb.";
        yield return new WaitForSeconds(7);
        Houthi3.enabled = false;

        Houthi4.enabled = true;
        Houthi4.GetComponentInChildren<Text>().text = "Of course it was a bomb you Hmar kelb tfou";
        yield return new WaitForSeconds(7);
        Houthi4.enabled = false;

        Houthi2.enabled = true;
        Houthi2.GetComponentInChildren<Text>().text = "The government forces must suspect we are down here";
        yield return new WaitForSeconds(7);
        Houthi2.enabled = false;

        Houthi1.enabled = true;
        Houthi1.GetComponentInChildren<Text>().text = "Ya Ibn el Sharmouta!";
        yield return new WaitForSeconds(7);
        Houthi1.enabled = false;

        Houthi4.enabled = true;
        Houthi4.GetComponentInChildren<Text>().text = "The power is out. People are dying. We need to get it running straight away";
        yield return new WaitForSeconds(7);
        Houthi4.enabled = false;

        Houthi3.enabled = true;
        Houthi3.GetComponentInChildren<Text>().text = "Take this fuel to Aamir in the generator room next door. We sent him in there earlier but he hasn’t come back";
        yield return new WaitForSeconds(7);
        Houthi3.enabled = false;
    }
}
