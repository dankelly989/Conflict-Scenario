using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscalatorDownwards : MonoBehaviour 
{
	public AnimationClip animationClip;

	void Start()
	{
		GetComponent<Animation>()[animationClip.name].speed = -1.0f;
		GetComponent<Animation>()[animationClip.name].time = GetComponent<Animation>()[animationClip.name].length;
		GetComponent<Animation>().Play(animationClip.name);
	}
}
