using UnityEngine;
using System.Collections;
using MH;

namespace MH
{
    public class CCDemo1_MainCtrl : MonoBehaviour
    {
        #region "configurable data"
        // configurable data

        public Animator m_Animator;
        public GameObject m_PrefabAction;
        public GameObject m_EndTitleCC;

        // player
        public float m_Speed = 1f;

        // enemy
        public Transform[] m_Enemies;

        //dist
        public float m_NearestDist = 0.3f; //if too near, forbid walking
        public float m_AttackThresDist = 1f; //the distance start to allow attacking

        //images
        public Texture2D m_ForwardHint;
        public Texture2D m_AtkHint;

        #endregion "configurable data"

        #region "data"
        // data

        private int m_EnemyIdx = 0;
        private Transform m_Enemy;

        private bool m_Attacking = false;
        private Transform m_Player;

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void Start()
        {
            Dbg.Assert(m_ForwardHint != null, "CCDemo1_MainCtrl.Start: ForwardHint not set");
            Dbg.Assert(m_AtkHint != null, "CCDemo1_MainCtrl.Start: AtkHint not set");
            Dbg.Assert(m_Animator != null, "CCDemo1_MainCtrl.Start: not set Animator");
            Dbg.Assert(m_PrefabAction != null, "CCDemo1_MainCtrl.Start: prefab action not set");
            Dbg.Assert(m_Enemies.Length > 0, "CCDemo1.MainCtrl.Start: not set enemies");

            m_Player = m_Animator.transform;
            m_Enemy = m_Enemies[0];
        }

        void OnGUI()
        {
            if (m_EnemyIdx >= m_Enemies.Length || m_Attacking)
                return;

            float distToEnemy = Vector3.Distance(m_Player.position, m_Enemy.position);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(10f);
                if (distToEnemy > m_NearestDist)
                {
                    GUILayout.Label(m_ForwardHint, GUILayout.Height(50), GUILayout.Width(50));
                    GUILayout.Space(10f);
                }
                if (distToEnemy < m_AttackThresDist)
                {
                    GUILayout.Label(m_AtkHint, GUILayout.Height(50));
                }
            }
            GUILayout.EndHorizontal();
        }

        void Update()
        {
            float distToEnemy = Vector3.Distance(m_Player.position, m_Enemy.position);

            // moving
            if (Input.GetKey(KeyCode.W) && distToEnemy > m_NearestDist && !m_Attacking)
            {
                m_Animator.SetBool(WALKING_HASH, true);
                Vector3 pos = m_Player.position;
                pos += Time.deltaTime * m_Speed * m_Player.forward;
                m_Player.position = pos;
            }
            else
            {
                m_Animator.SetBool(WALKING_HASH, false);
            }

            // execute killing blow!
            if (distToEnemy < m_AttackThresDist && !m_Attacking)
            {
                if (Input.GetMouseButton(0))
                {
                    m_Attacking = true;
                    _StartPrefabAction(m_Enemy.position);
                }
            }

        }

        #endregion "unity event handlers"

        #region "public method"
        // public method

        #endregion "public method"

        #region "private method"
        // private method

        private void _StartPrefabAction(Vector3 pos)
        {
            GameObject action = (GameObject)GameObject.Instantiate(m_PrefabAction, pos, Quaternion.identity);
            action.name = "KillingBlow";

            // prepare actors and start the cutscene
            CutsceneController cc = action.GetComponent<CutsceneController>();

            GameObject internal_Player = action.transform.Find("Player").gameObject;
            GameObject internal_Enemey = action.transform.Find("Enemy").gameObject;

            var swaplist = CutsceneController.GetSwapObjList(cc);
            swaplist.Clear();
            swaplist.Add(new CutsceneController.SwapObjPair(m_Player.gameObject, internal_Player, false, true));
            swaplist.Add(new CutsceneController.SwapObjPair(m_Enemy.gameObject, internal_Enemey, false, false));
            CutsceneController.StartCC(cc);

            cc.OnPlayStopped += _OnKillingBlowEnd;
        }

        private void _OnKillingBlowEnd(CutsceneController cc)
        {
            //Dbg.Log("_OnKillingBlowEnd");
            m_Attacking = false;
            cc.OnPlayStopped -= _OnKillingBlowEnd;

            ++m_EnemyIdx;
            if (m_EnemyIdx >= m_Enemies.Length)
            {
                _StartEndCC();
            }
            else
            {
                m_Enemy = m_Enemies[m_EnemyIdx];
            }
        }

        private void _StartEndCC()
        {
            var action = m_EndTitleCC;
            action.SetActive(true);

            // start the cutscene
            CutsceneController cc = action.GetComponent<CutsceneController>();
            CutsceneController.StartCC(cc);
        }

        #endregion "private method"

        #region "constant data"
        // constant data

        private readonly int WALKING_HASH = Animator.StringToHash("Walking");

        #endregion "constant data"

    }
}