using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RogoDigital.Lipsync;

public class VisitorController : MonoBehaviour {

	public float earthquakeLast;

	public LipSyncData lipSyncData1;
	public LipSyncData lipSyncData2;
	public LipSyncData lipSyncData3;
	public LipSyncData lipSyncData4;

	public Transform walkTarget;

	private Animator anim;
	private LipSync ls;
	private GameObject quake;
	private GameObject doctor;
	private NavMeshAgent agent;
	private float dist;
	private bool walk;

	// Use this for initialization
	void Start () 
	{
		anim = GetComponent <Animator> ();
		ls = GetComponent <LipSync> ();
		quake = GameObject.Find ("FloorRoom");
		doctor = GameObject.FindGameObjectWithTag ("Doctor");
		agent = this.GetComponent<NavMeshAgent>();
		walk = false;
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (quake.GetComponent<EarthQuake> ().quake == true) 
		{
			anim.SetTrigger ("Quake");
		}

		if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Crouch"))
		{
			StartCoroutine (stopEarthquake());
		}

		if (doctor.GetComponent<DoctorController> ().walk3)
		{
			walk = true;
		}

		if (walk == true) 
		{
			anim.SetTrigger ("Jog");
			agent.SetDestination(walkTarget.position);
			dist = Vector3.Distance (walkTarget.position, transform.position);
			print (dist);
			if (dist > 2f && doctor.GetComponent<DoctorController> ().distp < 1000f) 
			{
				anim.SetBool ("Idle", false);
				agent.Resume ();
			} 
			else
			{
				anim.SetBool ("Idle", true);
				agent.Stop();
			}
		}
	}

//	void FixedUpdate ()
//	{
//		if (walk==true)
//		{
//			if (transform.position != walkTarget [current].position ) 			
//			{
//				transform.LookAt (walkTarget [current]);
//
//				anim.SetTrigger ("Jog");
//
//				Vector3 pos = Vector3.MoveTowards (transform.position, walkTarget [current].position, walkSpeed * Time.deltaTime);
//
//				GetComponent<Rigidbody> ().MovePosition (pos);
//			} 					
//
//			else if (transform.position == walkTarget [walkTarget.Length - 1].position)
//			{
//				walk = false;
//
//				anim.SetTrigger ("Idle");
//			}
//
//			else				
//			{
//				if (current < walkTarget.Length - 1) 
//				{
//					current = current + 1;
//				} 
//			}
//		}
//	}

	IEnumerator stopEarthquake() 
	{
		yield return new WaitForSeconds(earthquakeLast);
		anim.SetTrigger("StandUp");
		yield break;
	}

	void Dodge()
	{
		ls.defaultClip = lipSyncData1;
		ls.Play (ls.defaultClip, ls.defaultDelay);
	}

	void Pray()
	{
		ls.defaultClip = lipSyncData2;
		ls.Play (ls.defaultClip, ls.defaultDelay);
	}

	void Terrified()
	{
		ls.defaultClip = lipSyncData3;
		ls.Play (ls.defaultClip, ls.defaultDelay);
	}

	void GetOut()
	{
		ls.defaultClip = lipSyncData4;
		ls.Play (ls.defaultClip, ls.defaultDelay);
	}

}
