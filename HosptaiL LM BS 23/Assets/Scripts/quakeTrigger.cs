using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class quakeTrigger : MonoBehaviour {
    GameObject FloorRoom;
    GameObject phone;
    GameObject key2;
    GameObject key3;
    GameObject key4;
    GameObject key5;


    // Use this for initialization
    void Start () {
        FloorRoom = GameObject.Find("FloorRoom");
        phone = GameObject.Find("smartphone_n4h");
        key2 = GameObject.Find("key2");
        key3 = GameObject.Find("key3");
        key4 = GameObject.Find("key4");
        key5 = GameObject.Find("key5");
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            print("QUAKE!");
            StartCoroutine(Example());
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<BoxCollider>().enabled = false;
            GameObject.Find("SingleDoorRoom").GetComponent<SingleDoorOpen>().active = false;
            GameObject.Find("exitstopper").GetComponent<BoxCollider>().enabled = true;
        }

    }

    IEnumerator Example()
    {

        yield return new WaitForSeconds(0.3f);
        phone.GetComponent<AudioSource>().Play();
        yield return new WaitForSeconds(1);
        phone.GetComponent<MeshRenderer>().enabled = true;
        key2.GetComponent<MeshRenderer>().enabled = true;
        key3.GetComponent<MeshRenderer>().enabled = true;
        key4.GetComponent<MeshRenderer>().enabled = true;
        key5.GetComponent<MeshRenderer>().enabled = true;

        yield return new WaitForSeconds(3);
        FloorRoom.GetComponent<EarthQuake>().quake = true;

        
    }
}
