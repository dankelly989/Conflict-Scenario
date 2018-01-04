using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// the base class for all Cutscene Animation EvtActions
    /// </summary>
    public abstract class CC_EvtActions : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        protected CutsceneController m_CC;

        public CutsceneController CC
        {
            get { return m_CC; }
            set { m_CC = value; }
        }

        public abstract void OnAnimEvent();
    }
}
