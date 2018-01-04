using UnityEngine;
using VRStandardAssets.Utils;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections;
using cakeslice;


namespace VRStandardAssets.Examples
{
    // This script is a simple example of how an interactive item can
    // be used to change things on gameobjects by handling events.
    public class ExampleInteractiveItem : MonoBehaviour
    {
        GameObject FPC;
        GameObject action1;
        GameObject action2;
        Transform cameraLocation;
        Renderer m_Renderer;
        VRInteractiveItem m_InteractiveItem;

        private void Awake ()
        {
            FPC= GameObject.Find("RigidBodyFPSController");
            action1 = GameObject.Find("panelAction");
            action2 = GameObject.Find("textAction");
            cameraLocation = GameObject.Find("MainCameraVR").transform;
            m_Renderer = GetComponent<Renderer>();
            m_InteractiveItem= GetComponent<VRInteractiveItem>();
        }


        private void OnEnable()
        {
            m_InteractiveItem.OnOver += HandleOver;
            m_InteractiveItem.OnOut += HandleOut;
            //m_InteractiveItem.OnDoubleClick += HandleDoubleClick;

        }


        private void OnDisable()
        {
            m_InteractiveItem.OnOver -= HandleOver;
            m_InteractiveItem.OnOut -= HandleOut;
            //m_InteractiveItem.OnDoubleClick -= HandleDoubleClick;

        }


        private void Update()
        {
            float dist = Vector3.Distance(cameraLocation.position, transform.position);
            if (dist < 1.3 && m_InteractiveItem.IsOver==true)
            {
                Debug.Log("Show over state");
                GetComponent<Outline>().enabled = true;
                FPC.GetComponent<RigidbodyFirstPersonController>().walkPermission = false;
            }

        }

        //Handle the Over event
        private void HandleOver()
        {
            float dist = Vector3.Distance(cameraLocation.position, transform.position);
            if (dist < 1.3)
            {
                Debug.Log("Show over state");
                GetComponent<Outline>().enabled = true;
                FPC.GetComponent<RigidbodyFirstPersonController>().walkPermission=false;
                GameObject.Find("DamageController").GetComponent<MouseLook>().objectName = this.name;
                GameObject.Find("DamageController").GetComponent<MouseLook>().objectIN = true;
            }

        }


        //Handle the Out event
        private void HandleOut()
        {
            float dist = Vector3.Distance(cameraLocation.position, transform.position);
            if (dist < 1.3)
            {
                Debug.Log("Show out state");
                GetComponent<Outline>().enabled = false;
                FPC.GetComponent<RigidbodyFirstPersonController>().walkPermission = true;
                GameObject.Find("DamageController").GetComponent<MouseLook>().objectOUT = true;
            }
        }


        //Handle the Click event
        //private void HandleDoubleClick()
        //{
        //    float dist = Vector3.Distance(cameraLocation.position, transform.position);
        //    if (dist < 1.3 && FPC.GetComponent<RigidbodyFirstPersonController>().walkPermission==false)
        //    {
        //        Debug.Log("Show click state");
        //        action1.GetComponent<BoxCollider>().enabled = true;
        //        action1.GetComponent<MeshRenderer>().enabled = true;
        //        action2.GetComponent<BoxCollider>().enabled = true;
        //        action2.GetComponent<MeshRenderer>().enabled = true;
        //        StartCoroutine(Example());
        //        FPC.GetComponent<RigidbodyFirstPersonController>().walkPermission = false;
        //        GameObject.Find("DamageController").GetComponent<MouseLook>().actionIN = true;
        //    }
        //}

        IEnumerator Example()
        {
            yield return new WaitForSeconds(1);
            action1.GetComponent<panelScript>().close = true;
        }


    }

}