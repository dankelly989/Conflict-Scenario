using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleDoorOpen : MonoBehaviour
{
    public bool active = true;
    private Animator anim;
    public bool keepopen = false;

    void Start ()
	{
		anim = GetComponent <Animator> ();
	}

	void OnTriggerEnter (Collider other)
	{
        if (other.tag == "Player" && active || other.tag == "Doctor" || other.tag == "Visitor")
        {
            anim.SetBool("IsOpen", true);
            if (!keepopen)
            {
                GetComponent<AudioSource>().Play();
            }
        }
	}

	void OnTriggerExit (Collider other)
	{
        if (other.tag == "Player" && active || other.tag == "Doctor" || other.tag == "Visitor")
        {
            anim.SetBool("IsOpen", keepopen);
        }
    }

}
