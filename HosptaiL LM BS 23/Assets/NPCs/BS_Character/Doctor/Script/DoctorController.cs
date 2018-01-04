using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RogoDigital.Lipsync;

public class DoctorController : MonoBehaviour 
{
    public float earthquakeLast;
	public float checkOutsideLast;
    public float evacSound;

    public LipSyncData lipSyncData1;
	public LipSyncData lipSyncData2;
	public LipSyncData lipSyncData3;
	public LipSyncData lipSyncData4;
	public LipSyncData lipSyncData5;

	public Transform walk1Target;
	public Transform walk2Target;
	public Transform walk3Target;
	public Transform walk4Target;
	public Transform walk5Target;

	private bool walk1;
	private bool walk2;
	public bool walk3;
	private bool walk4;
	private bool walk5;

	private float dist1;
	private float dist2;
	private float dist3;
	private float dist4;
	private float dist5;
	public float distp;

	private Animator anim;
	private LipSync ls;
	private GameObject quake;
	private GameObject player;
	private Transform playerPostion;
	private NavMeshAgent agent;

	void Start () 
	{
        agent = this.GetComponent<NavMeshAgent>();
        anim = GetComponent <Animator> ();
		ls = GetComponent <LipSync> ();
		quake = GameObject.Find ("FloorRoom");
		player = GameObject.FindGameObjectWithTag ("Player");
		playerPostion = player.GetComponent<Transform> ();
		walk1 = false;
		walk2 = false;
		walk3 = false;
		walk4 = false;
		walk5 = false;
	}

	void OnTriggerEnter(Collider other) 
	{
		if (other.tag == "Player" && anim.GetCurrentAnimatorStateInfo (0).IsName ("Stand0")) 
		{
			ls.defaultClip = lipSyncData1;
			ls.Play (ls.defaultClip, ls.defaultDelay);
            anim.SetTrigger("PreQuake");
        }

		if (other.tag == "Player" && anim.GetCurrentAnimatorStateInfo (0).IsName ("Stand4")) 
		{
			anim.SetTrigger ("LastStand");
			ls.defaultClip = lipSyncData5;
			ls.Play (ls.defaultClip, ls.defaultDelay);
		}
	}

	void Update () 
	{
		if (quake.GetComponent<EarthQuake> ().quake == true) 
		{
			anim.SetTrigger ("Quake");
            gameObject.GetComponent<HeadLookController>().enabled = false;
		}

		if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Crouch"))
		{
			StartCoroutine (stopEarthquake());
		}

		if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Walk1")) 
		{
			walk1 = true;
		}

		if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Stand2"))
		{			
			StartCoroutine (checkOutside());
		}

		if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Walk3")) 
		{
			walk3 = true;
            StartCoroutine(evacSoundActivation());       
        }

        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Stand3"))
        {
            gameObject.GetComponent<HeadLookController>().enabled = true;
        }

        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Walk3"))
        {
            gameObject.GetComponent<HeadLookController>().enabled = false;
        }

        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Stand4"))
        {
            gameObject.GetComponent<HeadLookController>().enabled = true;
        }

        if (walk1) 
		{
			agent.SetDestination(walk1Target.position);
			dist1 = Vector3.Distance (walk1Target.position, transform.position);
			if (dist1 < 0.1f) 
			{
				anim.SetTrigger ("CheckOutside");
				walk1 = false;
			}
		}

		if (walk2) 
		{
            agent.SetDestination(walk2Target.position);
			dist2 = Vector3.Distance (walk2Target.position, transform.position);
			if (dist2 < 0.1f) 
			{                
                anim.SetTrigger ("Brief");
				walk2 = false;
                GameObject.Find("SingleDoorRoom").GetComponent<SingleDoorOpen>().keepopen = true;
            }
		}

		if (walk3) 
		{

            agent.SetDestination(walk3Target.position);
			dist3 = Vector3.Distance (walk3Target.position, transform.position);
			if (dist3 < 0.5f) 
			{
                walk3 = false;
				walk4 = true;
			}

			distp = Vector3.Distance (transform.position, playerPostion.position);
			if (distp < 1000f) 
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

		if (walk4) 
		{
			agent.SetDestination(walk4Target.position);
			dist4 = Vector3.Distance (walk4Target.position, transform.position);
			if (dist4 < 0.5f) 
			{
				walk4 = false;
				walk5 = true;
			}

			distp = Vector3.Distance (transform.position, playerPostion.position);
			if (distp < 1000f) 
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

		if (walk5) 
		{
			agent.SetDestination(walk5Target.position);
			dist5 = Vector3.Distance (walk5Target.position, transform.position);
			if (dist5 < 0.1f) 
			{
				walk5 = false;
				anim.SetTrigger ("Wait");
			}

			distp = Vector3.Distance (transform.position, playerPostion.position);
			if (distp < 1000f) 
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

	IEnumerator stopEarthquake() 
	{
		yield return new WaitForSeconds(earthquakeLast);
		anim.SetTrigger("StandUp");
		yield break;
	}

	IEnumerator checkOutside() 
	{
		yield return new WaitForSeconds(checkOutsideLast);
		anim.SetTrigger("Return");
		walk2 = true;
        yield break;
	}

    IEnumerator evacSoundActivation()
    {
        yield return new WaitForSeconds(evacSound);
        GameObject.Find("soundEvac").GetComponent<AudioSource>().enabled = true;
        walk2 = true;
        yield break;
    }

    void EarthquakeHappen ()
	{
		ls.defaultClip = lipSyncData2;
		ls.Play (ls.defaultClip, ls.defaultDelay);
	}

	void CheckTheSituation ()
	{
		ls.defaultClip = lipSyncData3;
		ls.Play (ls.defaultClip, ls.defaultDelay);
	}

	void Breif()
	{
		ls.defaultClip = lipSyncData4;
		ls.Play (ls.defaultClip, ls.defaultDelay);
	}

//	void Walk3()
//	{
//		walk3 = true;
//		anim.SetTrigger ("GoOutside");
//	}

}
