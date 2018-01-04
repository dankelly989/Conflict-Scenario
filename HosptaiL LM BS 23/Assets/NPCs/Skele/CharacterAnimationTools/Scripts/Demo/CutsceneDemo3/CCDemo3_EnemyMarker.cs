using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
    /// <summary>
    /// draw GUI marker for enemy, 
    /// position and attack indicator
    /// </summary>
    public class CCDemo3_EnemyMarker : MonoBehaviour
    {
        #region "configurable data"
        // configurable data

        public float m_yOffset = 0.7f;
        public Texture2D m_NormalMark;
        public Texture2D m_AtkMark;

        #endregion "configurable data"

        #region "data"
        // data

        private CCDemo3_MainCtrl m_MainCtrl;
        private Transform m_RefTr;
        private CCDemo3_EnemyProp m_Prop;

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void Start()
        {
            m_MainCtrl = GameObject.Find("GlobalScript").GetComponent<CCDemo3_MainCtrl>();
            m_RefTr = transform.Find("Hips/Spine/Spine1/Spine2/Neck/Neck1");
            m_Prop = GetComponent<CCDemo3_EnemyProp>();

            Dbg.Assert(m_MainCtrl != null, "CCDemo3_EnemyMarker.Start: failed to get mainctrl");
            Dbg.Assert(m_RefTr != null, "CCDemo3_EnemyMarker.Start: failed to get spine transform");
            Dbg.Assert(m_Prop != null, "CCDemo3_EnemyMarker.Start: failed to get prop");
        }

        void OnGUI()
        {
            if (_IsDead())
            {
                enabled = false;
                return;
            }
            if (!m_MainCtrl.AllowControl)
            {
                return;
            }

            Vector3 pos = m_RefTr.position;
            pos.y += m_yOffset;

            Rect rc = _MapPos(pos);

            //Dbg.Log("pos : {0}, rc: {1}", pos, rc);

            if (m_MainCtrl.IsAtking(gameObject))
            {
                GUI.DrawTexture(rc, m_AtkMark);
            }
            else
            {
                GUI.DrawTexture(rc, m_NormalMark);
            }
        }

        #endregion "unity event handlers"

        #region "public method"
        // public method

        #endregion "public method"

        #region "private method"
        // private method

        private bool _IsDead()
        {
            return m_Prop.m_HP <= 0;
        }

        private Rect _MapPos(Vector3 pos)
        {
            Camera cam = Camera.main;

            Vector3 scrPos = cam.WorldToScreenPoint(pos);
            scrPos.x = Mathf.Clamp(scrPos.x, SIZE, Screen.width - SIZE);
            scrPos.y = Mathf.Clamp(scrPos.y, SIZE, Screen.height - SIZE);

            Transform camTr = cam.transform;

            if (Vector3.Dot(camTr.forward, pos - camTr.position) < 0)
            {
                scrPos.y = SIZE;
            }

            return new Rect(scrPos.x - HALF_SIZE, Screen.height - scrPos.y, SIZE, SIZE);
        }

        #endregion "private method"

        #region "constant data"
        // constant data

        public const float SIZE = 20f;
        public const float HALF_SIZE = 10f;

        #endregion "constant data"
    }
}