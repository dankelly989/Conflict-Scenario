using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombingSequence : MonoBehaviour
{
    Animator doctor;
    Canvas textbox;
    SingleDoorOpen ward;
    BoxCollider exitStopper;
    SingleDoorOpen office;
    public List<Flicker> lights;

    bool activated = false;

    void Start()
    {
        doctor = GameObject.Find("YemenDoctor").GetComponent<Animator>();
        textbox = GameObject.Find("TextBoxCanvas").GetComponent<Canvas>();
        textbox.enabled = false;
        ward = GameObject.Find("SmallWard").GetComponent<SingleDoorOpen>();
        office = GameObject.Find("SingleDoorRoom").GetComponent<SingleDoorOpen>();
        exitStopper = GameObject.Find("exitstopper (1)").GetComponent<BoxCollider>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !activated)
        {
            doctor.SetTrigger("PlayerEnter");
            exitStopper.enabled = true;
            ward.active = false;
        }
    }

    IEnumerator wait10()
    {
        yield return new WaitForSeconds(10);
        textbox.enabled = false;
        explosion();
        StartCoroutine(wait5());
        
    }

    IEnumerator wait5()
    {
        yield return new WaitForSeconds(5);
        office.active = true;
        exitStopper.enabled = false;
        ward.active = true;
    }

    private void Update()
    {
        if (doctor.GetCurrentAnimatorStateInfo(0).IsName("Stand") && !activated)
        {
            textbox.enabled = true;
            StartCoroutine(wait10());
            activated = true;
        }
    }

    private void explosion()
    {
        this.GetComponent<AudioSource>().Play();
        //screen shake
        //lights go off
        foreach(Flicker f in lights)
        {
            f.turnOff();
        }
        //items move
        //sparks
        //Lights flicker
        lights[0].Startflicker();
    }
}
