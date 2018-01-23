using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RogoDigital.Lipsync;

public class PersonController : MonoBehaviour
{
    public List<Transform> Targets;
    private Transform currentTarget;
    private int currentIndex = 0;

    private float dist;

    private NavMeshAgent agent;

    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
        currentTarget = Targets[0];
        agent.SetDestination(currentTarget.position);
    }

    void Update()
    {
        agent.SetDestination(currentTarget.position);
        agent.Resume();
        dist = Vector3.Distance(currentTarget.position, transform.position);
        if (dist < 0.5f && currentIndex < 5)
        {
            agent.Stop();
            currentIndex++;
            currentTarget = Targets[currentIndex];
        }
        else if (currentIndex == 5)
        {
            removeCharacter();
        }
    }

    private void removeCharacter()
    {
        Destroy(gameObject, 5.0f);
    }
}
