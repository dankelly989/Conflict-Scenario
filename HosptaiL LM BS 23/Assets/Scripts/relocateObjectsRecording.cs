using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

public class relocateObjectsRecording : MonoBehaviour
{
    public bool relocate = false;

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (relocate)
        {
            relocate = false;
            StartCoroutine(Example());
        }
    }


    IEnumerator Example()
    {
        GameObject relcObject;
        string filename = GameObject.Find("DamageController").GetComponent<MouseLook>().filename;
        string[] lines = File.ReadAllLines("Assets/Resources/" + filename + "relocate.txt");
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

            yield return null;
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
    }
}