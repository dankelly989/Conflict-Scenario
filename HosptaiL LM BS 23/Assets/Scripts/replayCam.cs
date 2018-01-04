using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class replayCam : MonoBehaviour {
    GameObject[] NPCs;
    private Animator anim;

    void Awake()
    {
        NPCs = GameObject.FindGameObjectsWithTag("NPC2");
        foreach (GameObject NPC in NPCs)
        {
            anim = NPC.GetComponent<Animator>();
            anim.SetBool("IsStanding", true);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.name == "EscalatorEntry")
        {
            print("enter");

            foreach (GameObject NPC in NPCs)
            {
                anim = NPC.GetComponent<Animator>();
                anim.SetBool("IsStanding", false);
                anim.ForceStateNormalizedTime(UnityEngine.Random.Range(0.0f, 1.0f));
            }
        }

        if (other.name == "EscalatorExitFPS")
        {
            print("exit");
        }
    }
}
