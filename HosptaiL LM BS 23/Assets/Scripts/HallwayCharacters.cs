using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class HallwayCharacters : MonoBehaviour {

    public Transform Target1;
    public Transform Target2;
    private Transform currentTarget;

    private float dist;

    private NavMeshAgent agent;

    bool position = true;

    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
        currentTarget = Target1;
        agent.SetDestination(currentTarget.position);
    }

    void Update()
    {
        agent.SetDestination(currentTarget.position);
        agent.Resume();
        dist = Vector3.Distance(currentTarget.position, transform.position);
        if (dist < 0.5f)
        {
            agent.Stop();
            if (position)
            {
                currentTarget = Target2;
            }
            else
            {
                currentTarget = Target1;
            }
            position = !position;
        }
    }
}
