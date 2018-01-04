using UnityEngine;
using System.Collections;

namespace MH
{
    public class CCDemo0_PlaneCollision : MonoBehaviour
    {
        #region "configurable data"
        // configurable data

        public Transform m_Ctrl;

        #endregion "configurable data"

        #region "data"
        // data

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void OnCollisionEnter()
        {
            m_Ctrl.SendMessage("Msg_CubeDropped");
        }

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