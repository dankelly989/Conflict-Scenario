using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flicker : MonoBehaviour {
    public float minSpeed;
    public float maxSpeed;
    public Light lighting;
    private MeshRenderer mesh;
     
    private Color defaultAl;
    private Color defaultEm;

    // Use this for initialization
    void Start () {
        mesh = GetComponent<MeshRenderer>();
        defaultAl = mesh.materials[0].GetColor("_Color");
        defaultEm = mesh.materials[0].GetColor("_EmissionColor");
	}

    public void turnOn()
    {
        lighting.enabled = true;
        mesh.materials[0].SetColor("_Color", defaultAl);
        mesh.materials[0].SetColor("_EmissionColor", defaultEm);
    }

    public void turnOff()
    {
        lighting.enabled = false;
        mesh.materials[0].SetColor("_Color", Color.black);
        mesh.materials[0].SetColor("_EmissionColor", Color.black);
    }

    public void Startflicker()
    {
        StartCoroutine(Timer());
    }

    public void Stopflicker()
    {
        StopCoroutine(Timer());
    }

    private IEnumerator Timer()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpeed, maxSpeed));
            if (lighting.enabled)
            {
                turnOff();
            }
            else{
                turnOn();
            }
        }
    }
}
