using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class roomDamage : MonoBehaviour {

    GameObject quakeObject;
    public Material Mat1;
    GameObject[] panels;
    GameObject[] panels2;
    GameObject[] dust;
    GameObject[] glasses;
    bool volume;
    GameObject[] shalving;
    GameObject light1;
    GameObject light2;
    GameObject printerSpark;
    GameObject floor;
    GameObject plaster1;
    GameObject plaster2;
    GameObject proj1;
    GameObject proj2;
    bool partitionActive = false;
    static float t = 0.0f;

    // Use this for initialization
    void Start () {
        quakeObject = GameObject.Find("FloorRoom");
        printerSpark = GameObject.Find("ElectricalSparksEffect");
        floor = GameObject.Find("FloorRoom");
        floor.GetComponent<AudioSource>().Play();
        volume = true;
        light1 = GameObject.Find("lightRoom1");
        light2 = GameObject.Find("lightRoom2");
        printerSpark.active = false;

        plaster1 = GameObject.Find("plaster1");
        plaster2= GameObject.Find("plaster2");
        proj1 = GameObject.Find("SpotlightP");
        proj2 = GameObject.Find("Projection");
        glasses = GameObject.FindGameObjectsWithTag("BreakableGlass");
        panels = GameObject.FindGameObjectsWithTag("panel");
        panels2 = GameObject.FindGameObjectsWithTag("panel2");
        dust = GameObject.FindGameObjectsWithTag("dust");
        shalving = GameObject.FindGameObjectsWithTag("shalving");

        foreach (GameObject d in dust)
        {
            d.active = false;
        }
        StartCoroutine(Example());
    }

    private void Update()
    {



        if (volume)
        {
            floor.GetComponent<AudioSource>().volume += 0.0007f;
        }
        else
        {
            floor.GetComponent<AudioSource>().volume -= 0.0004f;
        }


        if (partitionActive)
        {
            plaster1.GetComponent<Transform>().position += new Vector3(0, Mathf.Lerp(0f, -0.008f, t), 0);
            plaster1.GetComponent<Transform>().rotation = Quaternion.Euler(Mathf.Lerp(0f, -2f, t), 176.937f, Mathf.Lerp(0f, 40f, t));

            plaster2.GetComponent<Transform>().position += new Vector3(0, Mathf.Lerp(0f, -0.01f, t), 0);
            plaster2.GetComponent<Transform>().rotation = Quaternion.Euler(Mathf.Lerp(0.76f, 2f, t), 176.937f, Mathf.Lerp(0f, -20f, t));
            t += 1.8f * Time.deltaTime;

            if (t > 1f)
            {
                t = 0;
                partitionActive = false;
            }
        }

    }

    IEnumerator Example()
    {
        yield return new WaitForSeconds(13);
        foreach (GameObject g in glasses)
        {
            g.GetComponent<Breakable>().broken = true;
        }

        proj1.active = false;
        proj2.active = false;


        Material[] materials;
        materials = light1.GetComponent<Renderer>().materials;
        materials[1] = Mat1;
        light1.GetComponent<Renderer>().materials = materials;
        light2.GetComponent<Renderer>().materials = materials;

        yield return new WaitForSeconds(3);
        foreach (GameObject d in dust)
        {
            d.active = true;
        }
        yield return new WaitForSeconds(0.1f);
        partitionActive = true;
        foreach (GameObject p in panels)
        {
            p.GetComponent<FixedJoint>().breakForce = 0;
        }
        volume = false;
        printerSpark.active = true;

        yield return new WaitForSeconds(0.8f);
        foreach (GameObject p in panels2)
        {
            p.GetComponent<FixedJoint>().breakForce = 0;
        }
        yield return new WaitForSeconds(0.5f);
        foreach (GameObject s in shalving)
        {
            s.GetComponent<Rigidbody>().AddForce(0, 0, -1.5f, ForceMode.Impulse);
        }



    }

}
