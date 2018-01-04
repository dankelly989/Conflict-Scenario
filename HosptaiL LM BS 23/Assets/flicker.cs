using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class flicker : MonoBehaviour {
    public float minSpeed;
    public float maxSpeed;
    private Light light;
    public GameObject lightFitting;
    private MeshRenderer mesh;
    // Use this for initialization
    void Start () {
        light = GetComponent<Light>();
        mesh = lightFitting.GetComponent<MeshRenderer>();
        StartCoroutine(Timer());
	}
	
	IEnumerator Timer()
    {
        var al1 = Color.black;
        var al2 = mesh.materials[0].GetColor("_Color");
        var em1 = Color.black;
        var em2 = mesh.materials[0].GetColor("_EmissionColor");
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpeed, maxSpeed));
            light.enabled = !light.enabled;
            mesh.materials[0].SetColor("_Color",al1);
            mesh.materials[0].SetColor("_EmissionColor", em1);
            al1 = al2;
            em1 = em2;
            al2 = mesh.materials[0].GetColor("_Color");
            em2 = mesh.materials[0].GetColor("_EmissionColor");
        }
    }
}
