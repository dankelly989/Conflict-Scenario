using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

public class locationChecking : MonoBehaviour
{

    public float timeSaving = 3f;
    bool save = true;

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

        if (save && Time.realtimeSinceStartup > timeSaving)
        {

            save = false;


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
                    System.IO.File.AppendAllText("initialPositions.txt", (relcObject.transform.parent != null ? relcObject.transform.parent.name : "null") + ";" + relcObject.name + ";" + relcObject.transform.position.x + ";" + relcObject.transform.position.y + ";" + relcObject.transform.position.z + ";" + relcObject.transform.eulerAngles.x + ";" + relcObject.transform.eulerAngles.y + ";" + relcObject.transform.eulerAngles.z + "\n");
                }
                catch
                {
                    print(col[0] + "/" + col[1]);
                }
            }

            print("Saved");

        }


    }
}
