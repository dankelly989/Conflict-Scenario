using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityStandardAssets.CrossPlatformInput;

using UnityStandardAssets.Characters.FirstPerson;

public class EsclatorFPC : MonoBehaviour {

    public float speed;
    public Transform escalatorExit;

    private bool activate = false;
    GameObject[] NPCs;
    private Animator anim;

    void Awake()
    {
        NPCs = GameObject.FindGameObjectsWithTag("NPC2");
        foreach (GameObject NPC in NPCs)
        {
           anim= NPC.GetComponent<Animator>();
           anim.SetBool("IsStanding", true);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.name == "EscalatorEntry")
        {
            print("enterFPC");
            activate = true;
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<RigidbodyFirstPersonController>().walkPermission = false;

            foreach (GameObject NPC in NPCs)
            {
                anim = NPC.GetComponent<Animator>();
                anim.SetBool("IsStanding", false);
                anim.ForceStateNormalizedTime(UnityEngine.Random.Range(0.0f, 1.0f));
            }
        }

        if (other.name == "EscalatorExitFPS")
        {
            print("exitFPC");
            activate = false;
            GetComponent<Rigidbody>().useGravity = true;
            GetComponent<RigidbodyFirstPersonController>().walkPermission = true;
        }
    }

    void Update()
    {
        if (activate)
        {
            transform.position = Vector3.MoveTowards(transform.position, escalatorExit.position, speed * Time.deltaTime);
        }
    }
}
