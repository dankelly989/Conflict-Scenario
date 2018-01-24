using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{

    public List<GameObject> CharacterTypes;
    private float timeLastSpawned = 0;
    private float timeLastChecked = 0;

    public List<Transform> Targets1;
    public List<Transform> Targets2;


    void Start()
    {
        int r = Random.Range(0, 3);
        Vector3 position = new Vector3(-168.162f, -0.423f, -137.912f);
        GameObject character = Instantiate(CharacterTypes[r], this.GetComponent<Transform>().position, this.GetComponent<Transform>().rotation);
        if (r == 0 || r == 2)
        {
            character.GetComponent<PersonController>().Targets = Targets1;
        }
        else
        {
            character.GetComponent<PersonController>().Targets = Targets2;
        }
        
        timeLastSpawned = Time.time;
    }

    protected void Update()
    {
        if (Time.time > timeLastSpawned + 5)
        {
            int r = Random.Range(0, 3);
            Vector3 position = new Vector3(-168.162f, -0.423f, -137.912f);
            GameObject character = Instantiate(CharacterTypes[r], this.GetComponent<Transform>().position, this.GetComponent<Transform>().rotation);
            if (r == 0 || r == 2)
            {
                character.GetComponent<PersonController>().Targets = Targets1;
            }
            else
            {
                character.GetComponent<PersonController>().Targets = Targets2;
            }

            timeLastSpawned = Time.time;
        }
    }
}