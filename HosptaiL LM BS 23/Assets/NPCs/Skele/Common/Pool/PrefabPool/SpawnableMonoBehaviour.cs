using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// subclasses must not write Start/OnSpawn/OnDespawn,
    /// and should only write _OnStart [one-time init], _OnSpawn/_OnDespawn [everytime spawn/despawn]
    /// </summary>
    public class SpawnableMonoBehaviour : MonoBehaviour
    {
        #region "conf data"
        #endregion "conf data"

        #region "data"

        protected bool _startCalled = false;
        protected bool _onSpawnCalled = false;

        #endregion "data"

        #region "unity methods"

        void Start()
        {
            if (!_startCalled)
            {
                _startCalled = true;
                _OnStart();
            }

            if (!_onSpawnCalled)
            {
                _onSpawnCalled = true;
                _OnSpawn();
            }
        }

        void OnSpawn()
        {
            if (!_startCalled)
            {
                _startCalled = true;
                _OnStart();
            }

            if (!_onSpawnCalled)
            {
                _onSpawnCalled = true;
                _OnSpawn();
            }
        }

        void OnDespawn()
        {
            _onSpawnCalled = false;
            _OnDespawn();
        }

        protected virtual void _OnStart()
        {
        }

        protected virtual void _OnSpawn()
        {
        }

        protected virtual void _OnDespawn()
        {
        }

        #endregion "unity methods"

        #region "public methods"
        #endregion "public methods"

        #region "private methods"
        #endregion "private methods"

        #region "constants"
        #endregion "constants"
    }
}
