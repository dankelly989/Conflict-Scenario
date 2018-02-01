using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BabyController : MonoBehaviour {

    private float timeLastgenerated = 0;
    private Animator anim;

    // Use this for initialization
    void Start () {
        anim = GetComponent<Animator>();
    }
	
	// Update is called once per frame
	void Update () {
        if (Time.time > timeLastgenerated + 5)
        {
            float r = Random.Range(0, 3);
            anim.SetFloat("Speed",r);

            timeLastgenerated = Time.time;
        }
    }
}
