using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

public class relocateObjectsCR : MonoBehaviour
{

    public Material Mat1;
    public Material Mat2;
    public bool damage;
    bool Bdamage;
    GameObject damage1;
    GameObject damage2;
    GameObject[] NPCs;
    GameObject slidingDoor;
    GameObject escalator;
    GameObject Triggers;



    // Use this for initialization
    void Start()
    {
        damage1 = GameObject.Find("PartitionDamage");
        damage2 = GameObject.Find("CeilingDamage");
        NPCs = GameObject.FindGameObjectsWithTag("NPC");
        Bdamage = damage;
        slidingDoor = GameObject.Find("Door Parent");
        escalator = GameObject.Find("Escalator_Upwards");
        Triggers = GameObject.Find("Triggers");
    }

    private void Update()
    {
        if (damage != Bdamage)
        {
            Bdamage = damage;
            StartCoroutine("coroutine");
        }
    }

    IEnumerator coroutine()
    {
        {
            if (damage)
            {
                slidingDoor.active = false;
                escalator.GetComponent<Animation>().Stop();
                Triggers.active = false;

                GameObject relcObject;

                string[] lines = File.ReadAllLines("postions.txt");
                foreach (string line in lines)
                {
                    string[] col = line.Split(new char[] { ';' });

                    if (col[0] == "null")
                    {
                        relcObject = GameObject.Find(col[1]);
                    }
                    else
                    {
                        relcObject = GameObject.Find(col[0] + "/" + col[1]);
                    }


                    try
                    {
                        relcObject.transform.position = new Vector3(float.Parse(col[2]), float.Parse(col[3]), float.Parse(col[4]));
                        relcObject.transform.eulerAngles = new Vector3(float.Parse(col[5]), float.Parse(col[6]), float.Parse(col[7]));
                    }
                    catch
                    {
                        print(col[0] + "/" + col[1]);
                    }
                    yield return null;
                }

                damage1.transform.position = new Vector3(damage1.transform.position.x, 0, damage1.transform.position.z);

                damage2.transform.position = new Vector3(damage2.transform.position.x, 0, damage2.transform.position.z);



                foreach (GameObject npc in NPCs)
                {
                    npc.SetActive(false);
                    yield return null;
                }

                GameObject[] lights;
                lights = GameObject.FindGameObjectsWithTag("lighting");

                foreach (GameObject lig in lights)
                {
                    try
                    {
                        Material[] materials;
                        materials = lig.GetComponent<Renderer>().materials;
                        materials[0] = Mat1;
                        lig.GetComponent<Renderer>().materials = materials;
                    }
                    catch { }
                    yield return null;
                }

                lights = GameObject.FindGameObjectsWithTag("lighting2");

                foreach (GameObject lig in lights)
                {
                    try
                    {
                        Material[] materials;
                        materials = lig.GetComponent<Renderer>().materials;
                        materials[1] = Mat1;
                        lig.GetComponent<Renderer>().materials = materials;
                    }
                    catch { }
                    yield return null;
                }
            }
            else
            {

                GameObject relcObject;

                string[] lines = File.ReadAllLines("initialPositions.txt");
                foreach (string line in lines)
                {
                    string[] col = line.Split(new char[] { ';' });

                    if (col[0] == "null")
                    {
                        relcObject = GameObject.Find(col[1]);
                    }
                    else
                    {
                        relcObject = GameObject.Find(col[0] + "/" + col[1]);
                    }


                    try
                    {
                        relcObject.transform.position = new Vector3(float.Parse(col[2]), float.Parse(col[3]), float.Parse(col[4]));
                        relcObject.transform.eulerAngles = new Vector3(float.Parse(col[5]), float.Parse(col[6]), float.Parse(col[7]));
                    }
                    catch
                    {
                        print(col[0] + "/" + col[1]);
                    }
                    yield return null;
                }


                damage1.transform.position = new Vector3(damage1.transform.position.x, -10, damage1.transform.position.z);

                damage2.transform.position = new Vector3(damage2.transform.position.x, -10, damage2.transform.position.z);



                foreach (GameObject npc in NPCs)
                {
                    npc.SetActive(true);
                    yield return null;
                }

                GameObject[] lights;
                lights = GameObject.FindGameObjectsWithTag("lighting");

                foreach (GameObject lig in lights)
                {
                    try
                    {
                        Material[] materials;
                        materials = lig.GetComponent<Renderer>().materials;
                        materials[0] = Mat2;
                        lig.GetComponent<Renderer>().materials = materials;
                    }
                    catch { }
                    yield return null;
                }

                lights = GameObject.FindGameObjectsWithTag("lighting2");

                foreach (GameObject lig in lights)
                {
                    try
                    {
                        Material[] materials;
                        materials = lig.GetComponent<Renderer>().materials;
                        materials[1] = Mat2;
                        lig.GetComponent<Renderer>().materials = materials;
                    }
                    catch { }
                    yield return null;
                }
            }
        }


    }
}

