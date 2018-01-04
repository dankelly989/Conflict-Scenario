using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowEscalator : MonoBehaviour
{
	public float speed;
	public Transform escalatorExit;

	private Animator anim;
	private bool activate = false;

	void Awake ()
	{
		anim = GetComponent <Animator> ();
        
	}

	void OnTriggerEnter (Collider other)
	{
		if (other.name == "EscalatorEntry") 
		{
			anim.SetBool ("IsStanding", true);
			activate = true;
		}

		if (other.name == "EscalatorExit") 
		{
			anim.SetBool ("IsStanding", false);
			activate = false;
		}

        if (other.tag == "Reception")
        {
            anim.SetBool("IsStanding", true);
            activate = false;
        }
    }

	void Update ()
	{
		if (activate) 
		{
			transform.position = Vector3.MoveTowards(transform.position, escalatorExit.position, speed * Time.deltaTime);
		}
	}
}