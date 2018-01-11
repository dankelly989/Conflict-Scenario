using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flicker : MonoBehaviour {
    public float minSpeed;
    public float maxSpeed;
    public Light lighting;
    private MeshRenderer mesh;
    private bool onoff;
    public bool circleLight;
     
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
        try { lighting.enabled = true; } catch { }
        onoff = true;
        mesh.materials[0].SetColor("_Color", defaultAl);
        mesh.materials[0].SetColor("_EmissionColor", defaultEm);
        if (circleLight)
        {
            mesh.materials[1].SetColor("_Color", defaultAl);
            mesh.materials[1].SetColor("_EmissionColor", defaultEm);
        }
    }

    public void turnOff()
    {
        try { lighting.enabled = false; } catch { }
        onoff = false;
        mesh.materials[0].SetColor("_Color", Color.black);
        mesh.materials[0].SetColor("_EmissionColor", Color.black);
        if (circleLight)
        {
            mesh.materials[1].SetColor("_Color", Color.black);
            mesh.materials[1].SetColor("_EmissionColor", Color.black);
        }
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
            if (onoff)
            {
                turnOff();
            }
            else{
                turnOn();
            }
        }
    }
}
