using System;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;
using Job = System.Collections.IEnumerator;

namespace MH
{
    /// <summary>
    /// enemy spawn ctrl, AI ctrl
    /// </summary>
    public class CCDemo3_EnemyCtrl : MonoBehaviour
    {
        #region "configurable data"
        // configurable data

        public List<EnemyDesc> m_EnemyDescLst;
        public EnemyDesc m_BossDesc;
        public int m_TotalWaves = 4;

        public float m_SpawnDist = 6f;
        public float m_AtkInterval = 6f;

        #endregion "configurable data"

        #region "data"
        // data

        private CCDemo3_MainCtrl m_MainCtrl;
        private List<Transform> m_ActiveEnemies; //the spawn-ed enemies list
        private int m_CurrentWave = 0;  // 1-based

        private int m_CurLockedEnemyIdx = 0;

        private bool m_bHinted = false;

        private float m_TimeSinceLastAtk = 0f;

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void Start()
        {
            Dbg.Assert(m_EnemyDescLst.Count > 0, "CCDemo3_EnemyCtrl.Start: enemy prefabs not set");

            m_ActiveEnemies = new List<Transform>();

            m_MainCtrl = GetComponent<CCDemo3_MainCtrl>();
            Dbg.Assert(m_MainCtrl != null, "CCDemo3_EnemyCtrl.Start: failed to get mainctrl");
        }

        void LateUpdate()
        {
            m_TimeSinceLastAtk += Time.deltaTime;

            // take different actions according to current states
            // if state matches, face the enemy to the player
            for (int idx = 0; idx < m_ActiveEnemies.Count; ++idx)
            {
                Transform enemy = m_ActiveEnemies[idx];
                if (!enemy.GetComponent<Animator>().enabled)
                    continue; //do not update when in parry prefab-action

                if (_InCanMoveState(enemy))
                {
                    if (_IsWalking(enemy))
                    {
                        _DoWalking(enemy);
                    }
                    else
                    {
                        _ThinkActions(enemy); //decide what to do, forward/backward/left/right/
                    }
                }
                else
                {
                    // do nothing
                }
            }

        }

        #endregion "unity event handlers"

        #region "public method"
        // public method

        public int ActiveEnemyCount
        {
            get { return m_ActiveEnemies.Count; }
        }

        public int CurrentWave
        {
            get { return m_CurrentWave; }
        }

        public int TotalWave
        {
            get { return m_TotalWaves; }
        }

        public Transform CurrentEnemy
        {
            get
            {
                if (ActiveEnemyCount == 0)
                    return null;
                return m_ActiveEnemies[m_CurLockedEnemyIdx];
            }
        }

        public void SwitchToNextEnemy()
        {
            if (m_ActiveEnemies.Count == 0)
            {
                m_CurLockedEnemyIdx = 0;
                return;
            }

            m_CurLockedEnemyIdx = (m_CurLockedEnemyIdx + 1) % m_ActiveEnemies.Count;
        }

        public void OnEnemyDie(GameObject enemy)
        {
            Transform curLockTarget = m_ActiveEnemies[m_CurLockedEnemyIdx];
            Transform enemyTr = enemy.transform;
            if (curLockTarget == enemyTr)
            {
                int idx = m_ActiveEnemies.FindIndex(x => { return x == enemyTr; });
                idx = (idx + 1) % m_ActiveEnemies.Count;
                curLockTarget = m_ActiveEnemies[idx];
            }
            m_ActiveEnemies.Remove(enemyTr);

            if (m_ActiveEnemies.Count == 0)
            {
                m_CurLockedEnemyIdx = 0;
                m_MainCtrl.OnEnemyWaveClear();
            }
            else
            {
                m_CurLockedEnemyIdx = m_ActiveEnemies.FindIndex(x => { return x == curLockTarget; });
            }
        }

        public void HintSpawn(float delay)
        {
            if (m_bHinted)
                return;

            m_bHinted = true;
            StartCoroutine(Job_SpawnEnemyWave(delay));
        }

        #endregion "public method"

        #region "private method"
        // private method

        /// <summary>
        /// decide to walk or atk
        /// when called, it is guaranteed to be in valid state
        /// </summary>
        private void _ThinkActions(Transform enemy)
        {
            Transform playerTr = m_MainCtrl.GetPlayerGO().transform;
            float dist = (playerTr.position - enemy.position).magnitude;
            Animator anim = enemy.GetComponent<Animator>();
            CCDemo3_EnemyProp prop = enemy.GetComponent<CCDemo3_EnemyProp>();
            prop.TimeUntilNextThink = Random.Range(0.2f, 0.5f);

            float r = Random.value;

            //reset the values
            anim.SetFloat(CCDemo3_MainCtrl.FORWARD_SPD_HASH, 0f);
            anim.SetFloat(CCDemo3_MainCtrl.STRAFE_SPD_HASH, 0f);

            if (m_TimeSinceLastAtk > m_AtkInterval && dist < 2 * HEAVY_PUNCH_DIST)
            { //atk
                if (r > 0.5f)
                {
                    //Dbg.Log("Atk");
                    anim.SetTrigger(CCDemo3_MainCtrl.HEAVY_PUNCH_HASH);
                    m_TimeSinceLastAtk = 0f;
                }
            }
            else
            { //move
                if (dist < NEAR_THRES)
                {
                    //Dbg.Log("Force backward");
                    anim.SetFloat(CCDemo3_MainCtrl.FORWARD_SPD_HASH, -1.0f);
                }
                else if (dist > FAR_THRES)
                {
                    //Dbg.Log("Force forward");
                    anim.SetFloat(CCDemo3_MainCtrl.FORWARD_SPD_HASH, 1.0f);
                }
                else
                {
                    if (r <= 0.25f)
                    { //left
                      //Dbg.Log("left");
                        anim.SetFloat(CCDemo3_MainCtrl.STRAFE_SPD_HASH, -1f);
                    }
                    else if (r <= 0.5f)
                    { //right
                      //Dbg.Log("right");
                        anim.SetFloat(CCDemo3_MainCtrl.STRAFE_SPD_HASH, 1f);
                    }
                    else if (r <= 0.75f)
                    { //forward
                      //Dbg.Log("foward");
                        anim.SetFloat(CCDemo3_MainCtrl.FORWARD_SPD_HASH, 1f);
                    }
                    else if (r <= 1f)
                    { //backward
                      //Dbg.Log("backward");
                        anim.SetFloat(CCDemo3_MainCtrl.FORWARD_SPD_HASH, -1f);
                    }
                }
            }

        }

        private bool _IsWalking(Transform enemy)
        {
            return m_MainCtrl.IsWalkingState(enemy.gameObject);
        }

        private bool _InCanMoveState(Transform enemy)
        {
            return m_MainCtrl.IsCanWalkState(enemy.gameObject);
        }

        private void _DoWalking(Transform enemy)
        {
            Transform playerTr = m_MainCtrl.GetPlayerGO().transform;
            CCDemo3_EnemyProp prop = enemy.GetComponent<CCDemo3_EnemyProp>();
            prop.TimeUntilNextThink -= Time.deltaTime;

            if (prop.TimeUntilNextThink < 0 ||
                (playerTr.position - enemy.position).magnitude < NEAR_THRES)
            {
                _ThinkActions(enemy);
                return;
            }

            Animator anim = enemy.GetComponent<Animator>();

            int st = 0;

            // get the state
            if (anim.IsInTransition(0))
            {
                AnimatorStateInfo s = anim.GetNextAnimatorStateInfo(0);
                st = CCDemo3_Helper.GetAnimatorStateHash(s);
            }
            else
            {
                AnimatorStateInfo s = anim.GetCurrentAnimatorStateInfo(0);
                st = CCDemo3_Helper.GetAnimatorStateHash(s);
            }

            //execute move
            if (st == CCDemo3_MainCtrl.FORWARD_STATE)
            {
                anim.SetFloat(CCDemo3_MainCtrl.FORWARD_SPD_HASH, 1.0f);
            }
            else if (st == CCDemo3_MainCtrl.BACKWARD_STATE)
            {
                anim.SetFloat(CCDemo3_MainCtrl.FORWARD_SPD_HASH, -1.0f);
            }
            else if (st == CCDemo3_MainCtrl.STRAFE_LEFT_STATE || st == CCDemo3_MainCtrl.STRAFE_RIGHT_STATE)
            {
                float strafe = (st == CCDemo3_MainCtrl.STRAFE_LEFT_STATE) ? -1f : 1f;
                anim.SetFloat(CCDemo3_MainCtrl.STRAFE_SPD_HASH, strafe);

                float dist = (playerTr.position - enemy.position).magnitude; //the radius
                float deltaAngle = prop.RotateSpeed * Time.deltaTime / dist * Mathf.Rad2Deg * -strafe;

                Quaternion q = Quaternion.Euler(0, deltaAngle, 0);
                Vector3 diff = enemy.position - playerTr.position;
                Vector3 newDiff = q * diff;
                enemy.position = newDiff + playerTr.position;

            }

            _FaceEnemyToPlayer(enemy, playerTr);
        }

        private static void _FaceEnemyToPlayer(Transform enemy, Transform playerTr)
        {
            // face the enemy to player
            Vector3 dir = playerTr.position - enemy.position;
            dir.y = 0f;
            enemy.forward = Vector3.RotateTowards(enemy.forward, dir, Time.deltaTime * Mathf.PI, 1f);
        }

        #endregion "private method"

        #region "Coroutine"
        // "Coroutine" 

        private Job Job_SpawnEnemyWave(float delay)
        {
            yield return new WaitForSeconds(delay);

            m_CurrentWave++;

            Vector3 pos = m_MainCtrl.GetPlayerGO().transform.position;

            if (m_CurrentWave != m_TotalWaves) //for mobs
            {
                int enemyCategoryCnt = m_EnemyDescLst.Count;
                for (int i = 0; i < m_CurrentWave; ++i)
                {
                    int enemyTypeIdx = Random.Range(0, enemyCategoryCnt);
                    EnemyDesc desc = m_EnemyDescLst[enemyTypeIdx];

                    float radian = Random.Range(0f, 2 * Mathf.PI);
                    float x = pos.x + Mathf.Cos(radian) * m_SpawnDist;
                    float z = pos.z + Mathf.Sin(radian) * m_SpawnDist;
                    float y = pos.y;
                    Vector3 enemyPos = new Vector3(x, y, z);
                    //Dbg.Log("radian : {0}, pos {1}", radian, enemyPos);

                    Quaternion enemyFacing = Quaternion.LookRotation(pos - enemyPos);

                    GameObject enemy = GameObject.Instantiate(desc.m_Prefab, enemyPos, enemyFacing) as GameObject;
                    CCDemo3_EnemyProp prop = enemy.GetComponent<CCDemo3_EnemyProp>();
                    prop.HP = desc.m_HP;
                    prop.AtkPwr = desc.m_Atk;
                    prop.m_AnimatorSpeed = desc.m_AnimatorSpeed;

                    m_ActiveEnemies.Add(enemy.transform);
                    m_MainCtrl.OnEnemySpawn(enemy);
                }
            }
            else //for boss
            {
                float radian = Random.Range(0f, 2 * Mathf.PI);
                float x = pos.x + Mathf.Cos(radian) * m_SpawnDist;
                float z = pos.z + Mathf.Sin(radian) * m_SpawnDist;
                float y = pos.y;
                Vector3 enemyPos = new Vector3(x, y, z);

                Quaternion enemyFacing = Quaternion.LookRotation(pos - enemyPos);

                EnemyDesc desc = m_BossDesc;
                GameObject enemy = GameObject.Instantiate(desc.m_Prefab, enemyPos, enemyFacing) as GameObject;
                CCDemo3_EnemyProp prop = enemy.GetComponent<CCDemo3_EnemyProp>();
                prop.HP = desc.m_HP;
                prop.AtkPwr = desc.m_Atk;
                prop.m_AnimatorSpeed = desc.m_AnimatorSpeed;

                var marker = enemy.GetComponent<CCDemo3_EnemyMarker>();
                marker.enabled = true;

                m_ActiveEnemies.Add(enemy.transform);
                m_MainCtrl.OnEnemySpawn(enemy);
            }

            m_bHinted = false;
        }
        #endregion "Coroutine"

        #region "constant data"
        // constant data

        public const float NEAR_THRES = 1.0f;
        public const float FAR_THRES = 5f;
        public const float HEAVY_PUNCH_DIST = 2.0f;

        #endregion "constant data"

        #region "inner struct"
        // "inner struct" 

        [Serializable]
        public class EnemyDesc
        {
            public GameObject m_Prefab;
            public float m_HP;
            public float m_Atk;
            public float m_AnimatorSpeed;
            public float m_RotateSpeed;
        }

        #endregion "inner struct"
    }
}