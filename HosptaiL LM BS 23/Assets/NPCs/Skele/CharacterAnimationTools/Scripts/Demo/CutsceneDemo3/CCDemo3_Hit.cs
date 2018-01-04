using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    public class CCDemo3_Hit : MonoBehaviour
    {
        #region "configurable data"
        // configurable data

        #endregion "configurable data"

        #region "data"
        // data

        private CCDemo3_MainCtrl m_MainCtrl;

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void Start()
        {
            m_MainCtrl = GameObject.Find("GlobalScript").GetComponent<CCDemo3_MainCtrl>();
        }

        //    void OnCollisionEnter(Collision col)
        //    {
        ////      Dbg.Log("Hit: {0}", Time.frameCount);
        //        if( col.gameObject.layer == LayerMask.NameToLayer("Wall"))
        //        {
        //            m_MainCtrl.OnTouchWall(gameObject);
        //        }
        //        else
        //        {
        //            m_MainCtrl.OnHit(col.gameObject, gameObject);
        //        }
        //    }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                m_MainCtrl.OnTouchWall(gameObject);
            }
            else
            {
                m_MainCtrl.OnHit(other.gameObject, gameObject);
            }
        }

        //    void OnCollisionStay(Collision col)
        //    {
        //        Dbg.Log("Stay: {0}, def {1}, atk {2}", Time.frameCount, gameObject.name, col.gameObject.name);
        //    }

        #endregion "unity event handlers"

        #region "public method"
        // public method

        #endregion "public method"

        #region "private method"
        // private method

        #endregion "private method"

        #region "constant data"
        // constant data

        #endregion "constant data"
    }
}