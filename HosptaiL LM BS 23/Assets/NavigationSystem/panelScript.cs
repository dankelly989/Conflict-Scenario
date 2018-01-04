using UnityEngine;
using VRStandardAssets.Utils;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections;

namespace VRStandardAssets.Examples
{
    public class panelScript : MonoBehaviour
    {
        [SerializeField] private GameObject FPC;
        [SerializeField]
        private VRInteractiveItem m_InteractiveItem;

        GameObject action;
        public bool close;

        private void Awake()
        {
            action = GameObject.Find("textAction");
            close = false;
        }

        private void Update()
        {
            if (m_InteractiveItem.IsOver == true)
            {
                Debug.Log("Active Panel");
                FPC.GetComponent<RigidbodyFirstPersonController>().walkPermission = false;
            }

        }

        private void OnEnable()
        {
            m_InteractiveItem.OnDoubleClick += HandleDoubleClick;

        }


        private void OnDisable()
        {
            m_InteractiveItem.OnDoubleClick -= HandleDoubleClick;

        }

        //Handle the Click event
        private void HandleDoubleClick()
        {
            if (close)
            {
                Debug.Log("Show click state");
                this.GetComponent<MeshRenderer>().enabled = false;
                this.GetComponent<BoxCollider>().enabled = false;

                action.GetComponent<MeshRenderer>().enabled = false;
                action.GetComponent<BoxCollider>().enabled = false;
                FPC.GetComponent<RigidbodyFirstPersonController>().walkPermission = true;
                close = false;

                GameObject.Find("DamageController").GetComponent<MouseLook>().actionOUT = true;
            }

        }


    }
}
