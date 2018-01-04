using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cakeslice;

public class locationsaving : MonoBehaviour {
    string recordFilename;
    public bool save = false;

	// Use this for initialization
	void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {

        if (save )
        {
            recordFilename = GameObject.Find("DamageController").GetComponent<MouseLook>().recordFilename;
            StartCoroutine(Example());
            save = false;
        }


    }

    IEnumerator Example()
    {

        //change tag of an object if it is visible
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (GameObject go in allObjects)
        {

            if (go.GetComponent<Outline>())
            {
                System.IO.File.AppendAllText("Assets/"+ recordFilename +"relocate.txt", (go.transform.parent != null ? go.transform.parent.name : "null") + ";" + go.name + ";" + go.transform.position.x + ";" + go.transform.position.y + ";" + go.transform.position.z + ";" + go.transform.eulerAngles.x + ";" + go.transform.eulerAngles.y + ";" + go.transform.eulerAngles.z + "\n");
            }

            yield return null;

        }

        print("Saved");
        yield return null;
    }
}
