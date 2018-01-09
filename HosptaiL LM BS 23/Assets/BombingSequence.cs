using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BombingSequence : MonoBehaviour
{
    Animator doctor;
    Canvas textbox;
    Text yemenDocotrText;
    SingleDoorOpen ward;
    BoxCollider exitStopper;
    SingleDoorOpen office;
    QuakeShake quake;
    public List<Flicker> lights;
    public List<GameObject> sparks;
    public List<GameObject> brokenObjects;
    public List<GameObject> newObjects;

    bool activated = false;

    void Start()
    {
        doctor = GameObject.Find("YemenDoctor").GetComponent<Animator>();
        textbox = GameObject.Find("TextBoxCanvas").GetComponent<Canvas>();
        textbox.enabled = false;
        yemenDocotrText = GameObject.Find("YemenDoctorText").GetComponent<Text>();
        ward = GameObject.Find("SmallWard").GetComponent<SingleDoorOpen>();
        office = GameObject.Find("SingleDoorRoom").GetComponent<SingleDoorOpen>();
        exitStopper = GameObject.Find("exitstopper (1)").GetComponent<BoxCollider>();
        quake = GameObject.Find("PlayerTrigger").GetComponent<QuakeShake>();
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
            yemenDocotrText.text = "This is some example text. Blah blah blah. Something about starving orphans or whatever.\nBOOM EXPLOSION!!!!!\nsjfsfubsdufbsdufsdufbsdui";
            textbox.enabled = true;

            StartCoroutine(wait10());
            activated = true;
        }
    }

    private void explosion()
    {
        //Play sound
        this.GetComponent<AudioSource>().Play();

        //screen shake
        quake.quake();

        //lights go off
        foreach(Flicker f in lights)
        {
            f.turnOff();
        }

        //items move
        foreach (GameObject g in brokenObjects)
        {
            g.SetActive(false);
        }
        foreach (GameObject g in newObjects)
        {
            g.SetActive(true);
        }
        //lights

        //sparks
        foreach (GameObject g in sparks)
        {
            g.SetActive(true);
        }

        //Lights flicker
        lights[0].Startflicker();
    }
}
