using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class endSequence : MonoBehaviour {
    GameObject screen;
    storeVariables variables;

    // Use this for initialization
    void Start () {
        screen = GameObject.Find("EndScreen");
        screen.SetActive(false);
        variables = GameObject.Find("Variables").GetComponent<storeVariables>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (variables.placedCones)
        {
            StartCoroutine(endGame());
        }
    }

    IEnumerator endGame()
    {
        //Screen turns to black and game quits
        yield return new WaitForSeconds(7);
        screen.SetActive(true);
        yield return new WaitForSeconds(0.5f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
