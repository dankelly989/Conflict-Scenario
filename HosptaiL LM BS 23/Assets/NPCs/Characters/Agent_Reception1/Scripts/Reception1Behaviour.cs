using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reception1Behaviour : MonoBehaviour 
{	
	private Animator anim;
	private AudioSource audi;

	void Start ()
	{
		anim = GetComponent <Animator> ();
		audi = GetComponent <AudioSource> ();
	}

	void OnTriggerEnter (Collider other)
	{
		if (other.tag == "Player" && anim.GetCurrentAnimatorStateInfo (0).IsName ("Typing0")) 
		{
			anim.SetTrigger ("StopTyping");
			//anim.CrossFade ("NoTyping",2,0,0);
			//anim.Play ("NoTyping");
		}
	}

	void Update ()
	{
		if (anim.GetCurrentAnimatorStateInfo (0).IsTag ("NoTalking"))
		{			
			audi.Play ();
		}
	}
}
