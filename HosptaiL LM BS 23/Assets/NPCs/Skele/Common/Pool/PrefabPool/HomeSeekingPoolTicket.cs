using System;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH
{
    /// <summary>
    /// This is used for those prefab objects pre-set in the scene,
    /// so it can be returned to correct pool on despawn()
    /// </summary>
    [RequireComponent(typeof(PoolTicket))]
    public class HomeSeekingPoolTicket : SpawnableMonoBehaviour
    {
        #region "conf data"

        [SerializeField][Tooltip("which prefab this object belongs to")]
        public GameObject _pfSource;

        #endregion "conf data"

        #region "data"
        #endregion "data"

        #region "unity methods"

        protected override void _OnStart()
        {
            base._OnStart();

            PoolTicket ticket = this.AssertGetComponent<PoolTicket>();
            ticket.Pool = PrefabPool.ForceGetPoolByPrefab(_pfSource);
        }

        protected override void _OnDespawn()
        {
            base._OnDespawn();
            MonoBehaviour.Destroy(this); //destroy this MB after despawn
        }

        #endregion "unity methods"

        #region "public methods"
        #endregion "public methods"

        #region "private methods"
        #endregion "private methods"
    }
}
