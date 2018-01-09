using UnityEngine;
using System.Collections;

public class QuakeShake : MonoBehaviour
{

    public Transform camTransform;
    public float shakeDuration = 5f;
    public float shakeAmount = 0.7f;
    public float decreaseFactor = 1.0f;
    public bool active = false;

    Vector3 originalPos;

    void Start()
    {
        camTransform = GameObject.Find("RigidBodyFPSController").transform;
    }

    public void quake()
    {
        originalPos = camTransform.localPosition;
        active = true;
    }

    void Update()
    {
        if (shakeDuration > 0 && active)
        {
            camTransform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;

            shakeDuration -= Time.deltaTime * decreaseFactor;
        }
        else
        {
            shakeDuration = 5f;
            active = false;
        }
    }
}
