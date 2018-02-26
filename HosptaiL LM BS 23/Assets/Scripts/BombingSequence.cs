using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

public class BombingSequence : MonoBehaviour
{
    Animator doctor;
    CapsuleCollider doctorCollider;
    Talking doctorTalk;

    SingleDoorOpen ward;
    SingleDoorOpen office;
    SingleDoorOpen stairs;
    BoxCollider exitStopper1;
    BoxCollider exitStopper2;
    BoxCollider exitStopper4;
    
    QuakeShake quake;
    RigidbodyFirstPersonController controller;

    public GameObject women;
    public Animator[] sittingWomen;
    public List<Flicker> lights;
    public List<GameObject> brokenObjects;
    public List<GameObject> newObjects;
    bool activated = false;

    subtitleController subtitles;

    void Start()
    {
        doctor = GameObject.Find("YemenDoctor").GetComponent<Animator>();
        doctorCollider = GameObject.Find("YemenDoctor").GetComponent<CapsuleCollider>();
        doctorCollider.enabled = false;
        doctorTalk = GameObject.Find("YemenDoctor").GetComponent<Talking>();
        doctorTalk.enabled = false;
        ward = GameObject.Find("SmallWard").GetComponent<SingleDoorOpen>();
        office = GameObject.Find("SingleDoorRoom").GetComponent<SingleDoorOpen>();
        stairs = GameObject.Find("door L1").GetComponent<SingleDoorOpen>();
        exitStopper1 = GameObject.Find("exitstopper1").GetComponent<BoxCollider>();
        exitStopper2 = GameObject.Find("exitstopper2").GetComponent<BoxCollider>();
        exitStopper4 = GameObject.Find("exitstopper4").GetComponent<BoxCollider>();
        quake = GameObject.Find("PlayerTrigger").GetComponent<QuakeShake>();
        controller = GameObject.Find("RigidBodyFPSController").GetComponent<RigidbodyFirstPersonController>();
        women = GameObject.Find("HallwayWomen");
        sittingWomen = women.GetComponentsInChildren<Animator>();

        subtitles = GameObject.Find("Subtitles").GetComponent<subtitleController>();

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
            StartCoroutine(explosion());
            activated = true;
        }
    }

    IEnumerator explosion()
    {
        yield return new WaitForSeconds(7);

        //Play sound
        this.GetComponent<AudioSource>().Play();
        yield return new WaitForSeconds(0.5f);

        //screen shake
        controller.walkPermission = false;
        quake.quake();
        RenderSettings.ambientLight = new Color(0.0627451f, 0.0627451f, 0.0627451f, 1);
        doctor.SetTrigger("Bomb");

        subtitles.stopDisplay();

        foreach (Animator a in sittingWomen)
        {
            a.SetTrigger("earthquake");
        }

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

        yield return new WaitForSeconds(6);

        foreach (GameObject g in newObjects)
        {
            g.SetActive(true);
        }

        doctor.SetTrigger("Stand");
        doctorCollider.enabled = true;
        doctorTalk.enabled = true;

        office.active = true;
        exitStopper1.enabled = false;
        exitStopper2.enabled = false;
        ward.active = true;
        exitStopper4.enabled = false;
        stairs.active = true;

        RenderSettings.ambientLight = new Color(0.1254902f, 0.1254902f, 0.1254902f, 1);

        //Lights flicker
        lights[0].Startflicker();
        lights[11].Startflicker();
        lights[12].Startflicker();
        lights[16].Startflicker();
        lights[17].Startflicker();
        lights[18].Startflicker();
        lights[21].Startflicker();
        lights[23].Startflicker();

        controller.walkPermission = true;
    }
}
