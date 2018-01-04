using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleDoorOpen : MonoBehaviour
{
	private Animator anim;

	void Start ()
	{
		anim = GetComponent <Animator> ();
	}

	void OnTriggerEnter (Collider other)
	{
		if (other.tag == "Player")
		{
			anim.SetBool ("IsOpen", true);
		}
	}

	void OnTriggerExit (Collider other)
	{
		if (other.tag == "Player")
		{
			anim.SetBool ("IsOpen", false);
		}
	}

}
