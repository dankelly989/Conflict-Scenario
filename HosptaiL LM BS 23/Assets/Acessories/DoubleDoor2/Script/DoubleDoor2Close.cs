using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleDoor2Close : MonoBehaviour
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
			anim.SetTrigger ("IsClose");
		}
	}
}
