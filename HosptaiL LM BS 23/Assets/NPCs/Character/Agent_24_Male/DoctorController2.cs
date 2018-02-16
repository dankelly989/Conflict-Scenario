using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RogoDigital.Lipsync;

public class DoctorController2 : MonoBehaviour 
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
    public Transform walk6Target;

    private bool walk1;
	private bool walk2;
	private bool walk3;
	private bool walk4;
	private bool walk5;

    private float dist;
    private bool part1 = true;

	private Animator anim;
    private LipSync ls;
	private GameObject player;
	private Transform playerPostion;
	private NavMeshAgent agent;

    void Start () 
	{
        agent = this.GetComponent<NavMeshAgent>();
        anim = GetComponent <Animator> ();
        ls = GetComponent<LipSync>();
		player = GameObject.FindGameObjectWithTag ("Player");
		playerPostion = player.GetComponent<Transform> ();
		walk1 = false;
		walk2 = false;
		walk3 = false;
		walk4 = false;
		walk5 = false;
	}

	void Update () 
	{
        if (anim.GetCurrentAnimatorStateInfo (0).IsName("walk1")) 
		{
			walk2 = true;
		}
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("walk2"))
        {
            walk3 = true;
        }
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("walk3"))
        {
            walk4 = true;
        }
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("walk4"))
        {
            walk1 = true;
        }
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("walk5"))
        {
            walk5 = true;
        }
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("stand"))
        {
            ls.defaultClip = lipSyncData1;
            ls.Play(ls.defaultClip, ls.defaultDelay);
        }

        if (walk1) 
		{
			agent.SetDestination(walk1Target.position);
            agent.Resume();
            dist = Vector3.Distance (walk1Target.position, transform.position);
			if (dist < 0.5f) 
			{
                agent.Stop();
                anim.SetBool("Position1",true);
                anim.SetBool("Position4", false);
                walk1 = false;
			}
		}

		if (walk2) 
		{
            agent.SetDestination(walk2Target.position);
            agent.Resume();
            dist = Vector3.Distance (walk2Target.position, transform.position);
			if (dist < 0.5f) 
			{
                agent.Stop();
                anim.SetBool("Position2", true);
                anim.SetBool("Position1", false);
                walk2 = false;
            }
		}

		if (walk3)
        { 
            agent.SetDestination(walk3Target.position);
            agent.Resume();
            dist = Vector3.Distance (walk3Target.position, transform.position);
			if (dist < 0.5f) 
			{
                agent.Stop();
                anim.SetBool("Position3", true);
                anim.SetBool("Position2", false);
                walk3 = false;
			}
		}

		if (walk4) 
		{
			agent.SetDestination(walk4Target.position);
            agent.Resume();
            dist = Vector3.Distance (walk4Target.position, transform.position);
			if (dist < 0.5f) 
			{
                agent.Stop();
                anim.SetBool("Position4", true);
                anim.SetBool("Position3", false);
                walk4 = false;
            }
		}

        if (walk5)
        {
            if (part1)
            {
                agent.SetDestination(walk5Target.position);
                agent.Resume();
                dist = Vector3.Distance(walk5Target.position, transform.position);
                if (dist < 0.5f)
                {
                    part1 = false;
                }
            }
            else
            {
                dist = Vector3.Distance(walk6Target.position, transform.position);
                agent.SetDestination(walk6Target.position);
                if (dist < 0.5f)
                {
                    agent.Stop();
                    anim.SetBool("Position5", true);
                    anim.SetBool("Position1", false);
                    anim.SetBool("Position2", false);
                    anim.SetBool("Position3", false);
                    anim.SetBool("Position4", false);
                }
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
