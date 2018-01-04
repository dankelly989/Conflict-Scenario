using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH
{
    public class PoolTicketOption : MonoBehaviour
    {
        #region "conf data"

        [SerializeField][Tooltip("")]
        protected bool _broadcastDespawnMsg = false;
        [SerializeField][Tooltip("")]
        protected bool _broadcastSpawnMsg = false;
        #endregion "conf data"

        #region "data"
        public bool broadcastSpawnMsg { get { return _broadcastSpawnMsg; } set { _broadcastSpawnMsg = value; } }
        public bool broadcastDespawnMsg { get { return _broadcastDespawnMsg; } set { _broadcastDespawnMsg = value; } }
        #endregion "data"

        #region "unity methods"

        #endregion "unity methods"

        #region "public methods"
        #endregion "public methods"

        #region "private methods"
        #endregion "private methods"
    }
}
