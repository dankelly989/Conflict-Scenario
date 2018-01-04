using UnityEngine;
using System.Collections;

public class EarthQuake : MonoBehaviour
{
    public bool quake = false;
    bool once = true;
    [UnityEngine.SerializeField, Range(1f, 50f)]
	public float magnitude = 9f;
	[UnityEngine.SerializeField, Range(0f, 100f)]
	public float shakingSpeed = 20;
	[UnityEngine.SerializeField, Range(0f, 1f)]
	public float randomAmount = 0.5f;
	public float duration = 10;
	public Vector3 forceByAxis;
	public AnimationCurve forceOverTime;
	public bool forceRecenter = true;
	public bool loop = false;
	[SerializeField]
	string currentState;

	bool running = false;
	public bool Running
	{
		get { return running; }
		set { startStop(value); }
	}

	Vector3 startPosition;
	Vector3 delta;
	[SerializeField]
	float currentMagnitude;
	float timeSinceStarted;

    GameObject quakeObject;
    GameObject sound1;
    GameObject sound2;
    GameObject sound3;
    public Material Mat1;
    GameObject[] panels;
    GameObject[] panels2;
    GameObject[] dust;
    GameObject[] glasses;
    bool volume;
    GameObject[] shalving;
    GameObject[] chair;
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
    GameObject collidersStatic;
    GameObject collidersDynamic;

    // Use this for initialization
    void Start ()
	{
		startPosition = transform.position;
		try { gameObject.AddComponent<Rigidbody>(); } catch {};
		//rigidbody.isKinematic = true;
		GetComponent<Rigidbody>().mass = float.MaxValue;
		GetComponent<Rigidbody>().useGravity = false;

        quakeObject = GameObject.Find("DamageController");
        printerSpark = GameObject.Find("ElectricalSparksEffect");
        floor = GameObject.Find("FloorRoom");
        sound1 = GameObject.Find("sound1");
        sound2 = GameObject.Find("sound2");
        sound3 = GameObject.Find("sound3");
        volume = true;
        light1 = GameObject.Find("lightRoom1");
        light2 = GameObject.Find("lightRoom2");
        printerSpark.active = false;
        plaster1 = GameObject.Find("plaster1");
        plaster2 = GameObject.Find("plaster2");
        proj1 = GameObject.Find("SpotlightP");
        proj2 = GameObject.Find("Projection");
        glasses = GameObject.FindGameObjectsWithTag("BreakableGlass");
        panels = GameObject.FindGameObjectsWithTag("panel");
        panels2 = GameObject.FindGameObjectsWithTag("panel2");
        dust = GameObject.FindGameObjectsWithTag("dust");
        shalving = GameObject.FindGameObjectsWithTag("shalving");
        chair = GameObject.FindGameObjectsWithTag("chair");
        collidersStatic = GameObject.Find("collidersStatic");
        collidersDynamic = GameObject.Find("collidersDynamic");

        foreach (GameObject d in dust)
        {
            d.active = false;
        }
       
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (quake && once)
        {
            quakeObject.GetComponent<relocateObjectsCR>().damage = true;
            floor.GetComponent<AudioSource>().Play();
            StartCoroutine(Example());
            Running = true;
            once = false;
            GameObject.Find("SingleDoorRoom").GetComponent<SingleDoorOpen>().active = false;
            collidersStatic.active = false;
            collidersDynamic.active = false;
        }

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


        GetComponent<Rigidbody>().velocity = Vector3.zero;
        if (running)
            OnRunning();
        if (forceRecenter)
            GetComponent<Rigidbody>().velocity += (startPosition - transform.position) * Time.deltaTime * 40;
    }

    void startStop(bool value)
	{
		if (value)
			currentState = "quake started";
		else
			currentState = "quake stopped";
		currentMagnitude = 0;
		timeSinceStarted = 0;
		running = value;
		GetComponent<Rigidbody>().velocity = Vector3.zero;
		delta = Vector3.zero;
	}

	void OnRunning()
	{
		forceByAxis = new Vector3(Mathf.Clamp(forceByAxis.x, 0f, 1f),
		                          Mathf.Clamp(forceByAxis.y, 0f, 1f),
		                          Mathf.Clamp(forceByAxis.z, 0f, 1f));
		timeSinceStarted += Time.deltaTime;
		currentMagnitude = forceOverTime.Evaluate(timeSinceStarted / duration) * magnitude * 15;
		if (timeSinceStarted > duration && !loop)
			Running = false;
		if (timeSinceStarted > duration && loop)
			Running = true;
		delta += new Vector3(Time.deltaTime * shakingSpeed * my_rand(),
		                     Time.deltaTime * shakingSpeed * my_rand(),
		                     Time.deltaTime * shakingSpeed * my_rand());
		GetComponent<Rigidbody>().velocity = new Vector3(Mathf.Cos(delta.x) * Time.deltaTime * currentMagnitude * forceByAxis.x,
		                                 Mathf.Cos(delta.y) * Time.deltaTime * currentMagnitude * forceByAxis.y,
		                                 Mathf.Cos(delta.z) * Time.deltaTime * currentMagnitude * forceByAxis.z);
		currentMagnitude /= 15;
	}



	float my_rand()
	{
		return (1f - Random.Range(0, randomAmount));
	}

    IEnumerator Example()
    {
        yield return new WaitForSeconds(13);

        sound1.GetComponent<AudioSource>().Stop();
        sound2.GetComponent<AudioSource>().Stop();
        sound3.GetComponent<AudioSource>().Play();

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


        yield return new WaitForSeconds(39);

        foreach (GameObject p in panels)
        {
            p.isStatic = true;
            p.GetComponent<BoxCollider>().isTrigger = true;
            p.GetComponent<Rigidbody>().freezeRotation = true;
            p.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionY;
        }

        foreach (GameObject p in panels2)
        {
            p.isStatic = true;
            p.GetComponent<BoxCollider>().isTrigger = true;
            p.GetComponent<Rigidbody>().freezeRotation = true;
            p.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionY;
        }

        foreach (GameObject s in shalving)
        {
            s.isStatic = true;
            s.GetComponent<MeshCollider>().isTrigger = true;
            s.GetComponent<Rigidbody>().freezeRotation = true;
            s.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionY;
        }

        foreach (GameObject s in chair)
        {
            s.isStatic = true;
            s.GetComponent<MeshCollider>().isTrigger = true;
            s.GetComponent<Rigidbody>().freezeRotation = true;
            s.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionY;
        }

        if (GameObject.Find("DamageController").GetComponent<MouseLook>().replayMode == false)
        {
            GameObject.Find("DamageController").GetComponent<locationsaving>().save = true;
        }
        else
        {
            GameObject.Find("DamageController").GetComponent<relocateObjectsRecording>().relocate = true;
        }

        GameObject.Find("SingleDoorRoom").GetComponent<SingleDoorOpen>().active = true;
        GameObject.Find("exitstopper").GetComponent<BoxCollider>().enabled = false;

        collidersStatic.active = true;

    }
}
