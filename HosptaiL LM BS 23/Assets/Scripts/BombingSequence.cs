﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

public class BombingSequence : MonoBehaviour
{
    Animator doctor;
    Canvas textbox;
    Text yemenDoctorText;
    SingleDoorOpen ward;
    BoxCollider exitStopper1;
    BoxCollider exitStopper2;
    BoxCollider exitStopper3;
    SingleDoorOpen office;
    DoubleDoorOpen hall;
    QuakeShake quake;
    RigidbodyFirstPersonController controller;
    public List<Flicker> lights;
    public List<GameObject> brokenObjects;
    public List<GameObject> newObjects;
    bool activated = false;

    void Start()
    {
        doctor = GameObject.Find("YemenDoctor").GetComponent<Animator>();
        textbox = GameObject.Find("TextBoxCanvas").GetComponent<Canvas>();
        textbox.enabled = false;
        yemenDoctorText = GameObject.Find("YemenDoctorText").GetComponent<Text>();
        ward = GameObject.Find("SmallWard").GetComponent<SingleDoorOpen>();
        office = GameObject.Find("SingleDoorRoom").GetComponent<SingleDoorOpen>();
        //Debug.Log(GameObject.Find("EndOfHallDoor"));
        hall = GameObject.Find("EndOfHallDoor").GetComponent<DoubleDoorOpen>();
        exitStopper1 = GameObject.Find("exitstopper1").GetComponent<BoxCollider>();
        exitStopper2 = GameObject.Find("exitstopper2").GetComponent<BoxCollider>();
        exitStopper3 = GameObject.Find("exitstopper3").GetComponent<BoxCollider>();
        quake = GameObject.Find("PlayerTrigger").GetComponent<QuakeShake>();
        controller = GameObject.Find("RigidBodyFPSController").GetComponent<RigidbodyFirstPersonController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !activated)
        {
            doctor.SetTrigger("PlayerEnter");
            exitStopper1.enabled = true;
            ward.active = false;
        }
    }

    private void Update()
    {
        if (doctor.GetCurrentAnimatorStateInfo(0).IsName("Stand") && !activated)
        {
            yemenDoctorText.text = "This is some example text. Blah blah blah. Something about starving orphans or whatever.\nBOOM EXPLOSION!!!!!\nsjfsfubsdufbsdufsdufbsdui";
            textbox.enabled = true;

            StartCoroutine(explosion());
            activated = true;
        }
    }

    IEnumerator explosion()
    {
        yield return new WaitForSeconds(7);
        textbox.enabled = false;

        //Play sound
        this.GetComponent<AudioSource>().Play();
        yield return new WaitForSeconds(1);

        //screen shake
        controller.walkPermission = false;
        quake.quake();
        RenderSettings.ambientLight = new Color(0.0627451f, 0.0627451f, 0.0627451f, 1);
        doctor.SetTrigger("Bomb");

        //lights go off
        foreach (Flicker f in lights)
        {
            f.turnOff();
        }

        //items move
        foreach (GameObject g in brokenObjects)
        {
            g.SetActive(false);
        }
        
        controller.crouch = true;

        yield return new WaitForSeconds(4);

        foreach (GameObject g in newObjects)
        {
            g.SetActive(true);
        }
        controller.walkPermission = true;

        office.active = true;
        exitStopper1.enabled = false;
        exitStopper2.enabled = false;
        ward.active = true;
        exitStopper3.enabled = false;
        hall.active = true;

        yield return new WaitForSeconds(1);

        RenderSettings.ambientLight = new Color(0.1254902f, 0.1254902f, 0.1254902f, 1);

        //Lights flicker
        lights[0].Startflicker();
        lights[11].Startflicker();
        lights[12].Startflicker();
    }
}
