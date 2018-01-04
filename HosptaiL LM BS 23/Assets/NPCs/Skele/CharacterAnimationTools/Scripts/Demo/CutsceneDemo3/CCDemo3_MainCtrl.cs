#if UNITY_5 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3
#define U5
#endif


using UnityEngine;
using System.Collections.Generic;
using System;

using Job = System.Collections.IEnumerator;

namespace MH
{

    public class CCDemo3_MainCtrl : MonoBehaviour
    {
        #region "configurable data"
        // configurable data

        public GameObject m_PlayerGO;

        public float m_HP = 100;
        public float m_LightPunchPwr = 10;
        public float m_HeavyPunchPwr = 50;

        public float m_AnimatorSpeed = 1.0f;

        public float m_InPlaceRotateSpeed = 360f; //angle per sec
        public float m_RotateSpeed = 2f;

        public CutsceneController m_StartCC;
        public CutsceneController m_EndTitleCC;
        public CutsceneController m_ParryCC;

        public CutsceneController[] m_WaveClearCC;

        public Texture2D m_CtrlImg;

        public GUISkin m_Skin;

        #endregion "configurable data"

        #region "data"
        // data

        private bool m_AllowManualControl = false;
        private bool m_EnableParry = false;
        private Animator m_PlayerAnimator;

        private CCDemo3_EnemyCtrl m_EnemyCtrl;
        private float m_MaxHP;

        #endregion "data"

        #region "unity event handlers"
        // unity event handlers

        void Start()
        {
            Dbg.Assert(m_StartCC != null, "CCDemo3_MainCtrl.Start: StartCC not set");
            Dbg.Assert(m_EndTitleCC != null, "CCDemo3_MainCtrl.Start: EndTitleCC not set");
            Dbg.Assert(m_ParryCC != null, "CCDemo3_MainCtrl.Start: Parry CC not set");
            Dbg.Assert(m_PlayerGO != null, "CCDemo3_MainCtrl.Start: playerGO not set");
            Dbg.Assert(m_CtrlImg != null, "CCDemo3_MainCtrl.Start: CtrlImg not set");

            m_MaxHP = m_HP;

            m_PlayerAnimator = m_PlayerGO.GetComponent<Animator>();
            Dbg.Assert(m_PlayerAnimator != null, "CCDemo3_MainCtrl.Start: player doesn't have Animator component");
            m_PlayerAnimator.speed = m_AnimatorSpeed;

            m_EnemyCtrl = GetComponent<CCDemo3_EnemyCtrl>();
            Dbg.Assert(m_EnemyCtrl != null, "CCDemo3_MainCtrl.Start: cannot get EnemyCtrl component");

            Dbg.Assert(m_WaveClearCC.Length == m_EnemyCtrl.TotalWave, "CCDemo3_MainCtrl.Start: WaveClearCC length != TotalWave");

            // begin the starting cam/fadein cutscene
            CutsceneController.StartCC(m_StartCC);
        }

        void LateUpdate()
        {
            if (m_AllowManualControl)
            {
                if (IsCanWalkState(m_PlayerGO))
                {
                    // parry
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        m_PlayerAnimator.SetTrigger(PARRY_HASH);
                    }
                    // atk
                    else if (Input.GetMouseButton(1))
                    {
                        m_PlayerAnimator.SetTrigger(HEAVY_PUNCH_HASH);
                    }
                    else if (Input.GetMouseButton(0))
                    {
                        m_PlayerAnimator.SetTrigger(LIGHT_PUNCH_HASH);
                    }
                    // movement
                    else
                    {
                        if (m_EnemyCtrl.ActiveEnemyCount == 0)
                        {
                            _NoEnemyMovement();
                        }
                        else
                        {
                            _Movement();
                        }
                    }
                }
            }


        }

        void OnGUI()
        {
            if (m_AllowManualControl)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(m_CtrlImg);
                }
                GUILayout.EndHorizontal();

                GUIUtil.PushSkin(m_Skin);
                GUILayout.BeginArea(new Rect(Screen.width - 100, 0, 100, 50));
                {
                    GUILayout.Label(string.Format("{0}/{1}", m_HP, m_MaxHP));
                }
                GUILayout.EndArea();
                GUIUtil.PopSkin();
            }



        }

        #region "Msg Handlers"
        // "Msg Handlers" 

        /// <summary>
        /// StartCC's notification that we can take the control now
        /// </summary>
        private void Msg_StartControl()
        {
            _TakeControl(true);
            m_EnemyCtrl.HintSpawn(1f);
        }

        private void Msg_UsrCtrl(CC_MsgParam p)
        {
            _TakeControl(p.m_bool);
        }

        #endregion "Msg Handlers"


        #endregion "unity event handlers"

        #region "public method"
        // public method

        public bool AllowControl
        {
            get { return m_AllowManualControl; }
        }

        public bool EnableParry
        {
            get { return m_EnableParry; }
            set { m_EnableParry = value; }
        }

        /// <summary>
        /// start the EndTitleCC sequence
        /// </summary>
        public void CallEndTitleCC()
        {
            CutsceneController.StartCC(m_EndTitleCC);
        }

        public GameObject GetPlayerGO()
        {
            return m_PlayerGO;
        }

        /// <summary>
        /// he who dares to touch the wall will be dead
        /// </summary>
        public void OnTouchWall(GameObject chara)
        {
            GameObject owner = _GetCollisionOwner(chara);
            Animator anim = owner.GetComponent<Animator>();
            anim.SetTrigger(LIGHT_PUNCH_HASH);

            if (owner == m_PlayerGO)
            {
                m_HP = 0;
                _OnPlayerDie();
            }
            else
            {
                CCDemo3_EnemyProp prop = owner.GetComponent<CCDemo3_EnemyProp>();
                prop.HP = 0;
                _OnEnemyDie(owner);
            }
        }

        public void OnHit(GameObject hit, GameObject beHit)
        {
            GameObject atkGO = _GetCollisionOwner(hit);
            GameObject defGO = _GetCollisionOwner(beHit);

            if (defGO == m_PlayerGO) //player gets hit by enemy
            {
                if (atkGO == m_PlayerGO)
                { //this is possible if the action make his own hand touches his own body...
                    return;
                }
                if (!IsAtking(atkGO))
                { //the accident collision should not be counted
                    return;
                }
                if (IsUnderAtk(defGO))
                {
                    return; //if already be hit, don't add new
                }

                if (EnableParry)
                {
                    if (!m_ParryCC.gameObject.activeSelf)
                    {
                        Transform cctr = m_ParryCC.transform;
                        Transform playerTr = m_PlayerGO.transform;

                        m_ParryCC.gameObject.SetActive(true);
                        var swaplst = CutsceneController.GetSwapObjList(m_ParryCC);
                        swaplst.Add(new CutsceneController.SwapObjPair(defGO, cctr.Find("Player").gameObject));
                        swaplst.Add(new CutsceneController.SwapObjPair(atkGO, cctr.Find("Enemy").gameObject));

                        Vector3 ccPos = playerTr.position;
                        ccPos.y = cctr.position.y;
                        cctr.position = ccPos;

                        Vector3 fdir = atkGO.transform.position - playerTr.position;
                        fdir.y = 0;
                        cctr.forward = fdir;
                        CutsceneController.StartCC(m_ParryCC);
                        m_ParryCC.OnPlayStopped += _OnParryCCStop;
                    }
                }
                else
                {
                    CCDemo3_EnemyProp prop = atkGO.GetComponent<CCDemo3_EnemyProp>();
                    Transform playerTr = m_PlayerGO.transform;
                    Vector3 atkPos = atkGO.transform.position;
                    atkPos.y = playerTr.position.y;
                    playerTr.forward = atkPos - playerTr.position;

                    m_HP = Mathf.Clamp(m_HP - prop.AtkPwr, 0, int.MaxValue);
                    Dbg.Log("player under atk, hp = {0}", m_HP);

                    if (m_HP <= 0)
                    {
                        m_PlayerAnimator.SetTrigger(HEAVY_PUNCHED_HASH);
                        _OnPlayerDie();
                    }
                    else
                    {
                        m_PlayerAnimator.SetTrigger(HEAVY_PUNCHED_HASH);
                    }
                }
            }
            else //player hit the enemy
            {
                if (atkGO != m_PlayerGO)
                { //two enemies collide...
                    return;
                }
                if (!IsAtking(atkGO))
                {
                    return;
                }
                if (IsUnderAtk(defGO))
                {
                    return; //if already be hit, don't add new
                }

                int atkStateHash = CCDemo3_Helper.GetAnimatorStateHash(m_PlayerAnimator.GetCurrentAnimatorStateInfo(0));
                Transform defTr = defGO.transform;
                CCDemo3_EnemyProp prop = defGO.GetComponent<CCDemo3_EnemyProp>();
                Animator enemyAnimator = defTr.GetComponent<Animator>();

                if (atkStateHash == LIGHT_PUNCH_STATE)
                {
                    Vector3 dir = atkGO.transform.position - defTr.position;
                    dir.y = 0;
                    defTr.forward = dir;
                    prop.HP -= m_LightPunchPwr;
                    enemyAnimator.SetTrigger(LIGHT_PUNCHED_HASH);
                    Dbg.Log("Enemy {0} under atk, hp = {1}", defGO.name, prop.HP);
                }
                else if (atkStateHash == HEAVY_PUNCH_STATE)
                {
                    Vector3 dir = atkGO.transform.position - defTr.position;
                    dir.y = 0;
                    defTr.forward = dir;
                    prop.HP -= m_HeavyPunchPwr;
                    enemyAnimator.SetTrigger(HEAVY_PUNCHED_HASH);
                    Dbg.Log("Enemy {0} under atk, hp = {1}", defGO.name, prop.HP);
                }

                if (prop.HP <= 0)
                {
                    _OnEnemyDie(prop.gameObject);
                }
            }
        }

        /// <summary>
        /// called on an enemy spawn
        /// </summary>
        public void OnEnemySpawn(GameObject newEnemy)
        {
            var animator = newEnemy.GetComponent<Animator>();
            var prop = newEnemy.GetComponent<CCDemo3_EnemyProp>();
            animator.speed = prop.AnimatorSpeed;
            animator.SetTrigger(JUMP_DOWN1_HASH);
        }

        /// <summary>
        /// callback from EnemyCtrl, when a wave is cleared
        /// </summary>
        public void OnEnemyWaveClear()
        {
            if (m_HP <= 0)
            {
                return; //do nothing if player dies
            }

            int curWaveIdx = m_EnemyCtrl.CurrentWave - 1;
            CutsceneController cc = m_WaveClearCC[curWaveIdx];
            cc.OnPlayStopped += _OnWaveClearCCStopped;
            CutsceneController.StartCC(cc);
        }

        public bool IsUnderAtk(GameObject go)
        {
            Animator animator = go.GetComponent<Animator>();
            int st = 0;
            if (animator.IsInTransition(0))
            {
                st = CCDemo3_Helper.GetAnimatorStateHash(animator.GetNextAnimatorStateInfo(0));
            }
            else
            {
                st = CCDemo3_Helper.GetAnimatorStateHash(animator.GetCurrentAnimatorStateInfo(0));
            }

            return st == HEAVY_PUNCHED_STATE || st == LIGHT_PUNCHED_STATE;
        }

        public bool IsAtking(GameObject go)
        {
            Animator animator = go.GetComponent<Animator>();
            int st = 0;
            if (animator.IsInTransition(0))
            {
                st = CCDemo3_Helper.GetAnimatorStateHash(animator.GetNextAnimatorStateInfo(0));
            }
            else
            {
                st = CCDemo3_Helper.GetAnimatorStateHash(animator.GetCurrentAnimatorStateInfo(0));
            }
            return (st == HEAVY_PUNCH_STATE ||
                    st == LIGHT_PUNCH_STATE);
        }

        public bool IsWalkingState(GameObject go)
        {
            Animator animator = go.GetComponent<Animator>();
            if (animator.IsInTransition(0))
            {
                int nextStateTag = animator.GetNextAnimatorStateInfo(0).tagHash;
                return nextStateTag == WALKING_TAG;
            }
            else
            {
                int curStateTag = animator.GetCurrentAnimatorStateInfo(0).tagHash;
                return curStateTag == WALKING_TAG;
            }
        }

        public bool IsCanWalkState(GameObject go)
        {
            Animator animator = go.GetComponent<Animator>();
            int st = 0;
            if (animator.IsInTransition(0))
            {
                st = CCDemo3_Helper.GetAnimatorStateHash(animator.GetNextAnimatorStateInfo(0));
            }
            else
            {
                st = CCDemo3_Helper.GetAnimatorStateHash(animator.GetCurrentAnimatorStateInfo(0));
            }

            if (st == IDLE_STATE)
                return true;
            else
                return IsWalkingState(go);
        }

        #endregion "public method"

        #region "private method"
        // private method

        /// <summary>
        /// the callback for WaveClear cutscene stop
        /// </summary>
        /// <param name="cc"></param>
        private void _OnWaveClearCCStopped(CutsceneController cc)
        {
            cc.OnPlayStopped -= _OnWaveClearCCStopped;

            if (m_EnemyCtrl.CurrentWave != m_EnemyCtrl.TotalWave)
            {
                m_EnemyCtrl.HintSpawn(1.5f);
                _TakeControl(true);
            }
            else
            {
                CallEndTitleCC(); // finish, you win
            }
        }

        private void _OnParryCCStop(CutsceneController cc)
        {
            cc.OnPlayStopped -= _OnParryCCStop;
            var swaplst = CutsceneController.GetSwapObjList(cc);

            foreach (var pr in swaplst)
            {
                if (pr.m_ExternalGO.name == "Enemy")
                {
                    // apply root motion
                    GameObject atkGO = pr.m_ExternalGO;
                    Transform tr = atkGO.transform;
                    Transform hips = tr.Find("Hips");

                    Vector3 pos = hips.position;
                    pos.y = tr.position.y;
                    tr.position = pos;

                    pos = hips.localPosition;
                    pos.x = pos.z = 0f;
                    hips.localPosition = pos;

                    Vector3 angles = hips.eulerAngles;
                    angles.x = angles.z = 0;
                    tr.eulerAngles = angles;

                    // jump to LIEDOWN_STATE
                    Animator anim = tr.GetComponent<Animator>();
                    anim.Play(LIEDOWN_STATE);

                    // apply dmg
                    CCDemo3_EnemyProp prop = atkGO.GetComponent<CCDemo3_EnemyProp>();
                    prop.HP -= prop.AtkPwr;
                    if (prop.HP <= 0)
                    {
                        _OnEnemyDie(prop.gameObject);
                    }
                }
                else if (pr.m_ExternalGO.name == "Player")
                {
                    EnableParry = false;

                    // apply root motion
                    Transform tr = m_PlayerGO.transform;
                    Transform hips = tr.Find("Hips");

                    Vector3 pos = hips.position;
                    pos.y = tr.position.y;
                    tr.position = pos;
                    Dbg.Log("enemy pos: {0}", pos);

                    pos = hips.localPosition;
                    pos.x = pos.z = 0f;
                    hips.localPosition = pos;

                    Vector3 angles = hips.eulerAngles;
                    angles.x = angles.z = 0;
                    tr.eulerAngles = angles;

                    // jump to idle state
                    m_PlayerAnimator.Play(IDLE_STATE, 0, 0);
                }
            }


        }

        /// <summary>
        /// enable/disable the player control
        /// </summary>
        private void _TakeControl(bool bAllowControl)
        {
            m_AllowManualControl = bAllowControl;
        }

        /// <summary>
        /// movement when enemy present
        /// </summary>
        private void _Movement()
        {
            if (!IsCanWalkState(m_PlayerGO))
            {
                m_PlayerAnimator.SetFloat(FORWARD_SPD_HASH, 0);
                m_PlayerAnimator.SetFloat(STRAFE_SPD_HASH, 0);
                return;
            }

            Transform curEnemy = m_EnemyCtrl.CurrentEnemy;
            // forward and backward
            float fwd = Input.GetAxis("Vertical");
            m_PlayerAnimator.SetFloat(FORWARD_SPD_HASH, fwd);

            // rotation around enemy (pos)
            float strafe = Input.GetAxis("Horizontal");
            m_PlayerAnimator.SetFloat(STRAFE_SPD_HASH, strafe);
            Transform playerTr = m_PlayerGO.transform;
            float dist = (playerTr.position - curEnemy.position).magnitude; //the radius
            float deltaAngle = m_RotateSpeed * Time.deltaTime / dist * Mathf.Rad2Deg;

            if (strafe > THRES)
            {
                Quaternion q = Quaternion.Euler(0, -deltaAngle, 0);
                Vector3 diff = playerTr.position - curEnemy.position;
                Vector3 newDiff = q * diff;
                playerTr.position = newDiff + curEnemy.position;
            }
            else if (strafe < -THRES)
            {
                Quaternion q = Quaternion.Euler(0, deltaAngle, 0);
                Vector3 diff = playerTr.position - curEnemy.position;
                Vector3 newDiff = q * diff;
                playerTr.position = newDiff + curEnemy.position;
            }

            // change locked enemy
            if (Input.GetKeyDown(KeyCode.Q))
            {
                m_EnemyCtrl.SwitchToNextEnemy();
                curEnemy = m_EnemyCtrl.CurrentEnemy;
            }

            // rotate to face the enemy
            Vector3 fixedEnemyPos = curEnemy.position;
            fixedEnemyPos.y = playerTr.position.y;
            Vector3 targetForward = (fixedEnemyPos - playerTr.position).normalized;
            Vector3 newFwd = Vector3.RotateTowards(playerTr.forward, targetForward, 2 * Mathf.PI * Time.deltaTime, 1f);
            playerTr.forward = newFwd; //internally called Quaternion.LookRotation

        }

        /// <summary>
        /// movement when no enemy present
        /// </summary>
        private void _NoEnemyMovement()
        {
            if (!IsCanWalkState(m_PlayerGO))
            {
                m_PlayerAnimator.SetFloat(FORWARD_SPD_HASH, 0);
                m_PlayerAnimator.SetFloat(STRAFE_SPD_HASH, 0);
                return;
            }

            // forward and backward
            float fwd = Input.GetAxis("Vertical");
            m_PlayerAnimator.SetFloat(FORWARD_SPD_HASH, fwd);

            // In-place rotation
            float strafe = Input.GetAxis("Horizontal");
            m_PlayerAnimator.SetFloat(STRAFE_SPD_HASH, strafe);
            if (strafe > THRES)
            {
                Transform tr = m_PlayerGO.transform;
                tr.Rotate(0, m_InPlaceRotateSpeed * Time.deltaTime, 0, Space.Self);
            }
            else if (strafe < -THRES)
            {
                Transform tr = m_PlayerGO.transform;
                tr.Rotate(0, -m_InPlaceRotateSpeed * Time.deltaTime, 0, Space.Self);
            }
        }

        /// <summary>
        /// called on an enemy dies
        /// </summary>
        private void _OnEnemyDie(GameObject enemy)
        {
            // animator control
            Animator animator = enemy.GetComponent<Animator>();
            animator.SetBool(DEATH_HASH, true);

            // disable all colliders
            Collider[] colliders = enemy.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; ++i)
            {
                colliders[i].enabled = false;
            }

            // notify EnemyCtrl
            StartCoroutine(Job_NotifyEnemyDieToEnemyCtrl(enemy));
        }

        /// <summary>
        /// called on player dies
        /// </summary>
        private void _OnPlayerDie()
        {
            m_PlayerAnimator.SetBool(DEATH_HASH, true);
            m_PlayerAnimator.SetFloat(FORWARD_SPD_HASH, 0f);
            m_PlayerAnimator.SetFloat(STRAFE_SPD_HASH, 0f);

            Collider[] colliders = m_PlayerGO.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; ++i)
            {
                colliders[i].enabled = false;
            }

            _TakeControl(false);

            StartCoroutine(Job_StartCCEndTitle());
        }

        /// <summary>
        /// given the collider GO, find the topmost owner GO
        /// </summary>
        private GameObject _GetCollisionOwner(GameObject hit)
        {
            Animator anim = null;
            Transform tr = hit.transform;
            while (anim == null)
            {
                anim = tr.GetComponent<Animator>();
                if (anim == null)
                {
                    tr = tr.parent;
                }
            }

            return anim.gameObject;
        }

        #endregion "private method"

        #region "Coroutines"
        // "Coroutines" 

        private Job Job_NotifyEnemyDieToEnemyCtrl(GameObject enemy)
        {
            yield return new WaitForSeconds(3f);

            m_EnemyCtrl.OnEnemyDie(enemy);
        }

        private Job Job_StartCCEndTitle(float delay = 3.0f)
        {
            yield return new WaitForSeconds(delay);

            CallEndTitleCC();
        }


        #endregion "Coroutines"

        #region "constant data"
        // constant data

        private const float THRES = 0.1f;

        public enum AtkType
        {
            None, LightPunch, HeavyPunch
        }

        public readonly static int FORWARD_SPD_HASH = Animator.StringToHash("ForwardSpd");
        public readonly static int STRAFE_SPD_HASH = Animator.StringToHash("StrafeSpd");
        public readonly static int LIGHT_PUNCH_HASH = Animator.StringToHash("LightPunch");
        public readonly static int LIGHT_PUNCHED_HASH = Animator.StringToHash("LightPunched");
        public readonly static int HEAVY_PUNCH_HASH = Animator.StringToHash("HeavyPunch");
        public readonly static int HEAVY_PUNCHED_HASH = Animator.StringToHash("HeavyPunched");
        public readonly static int JUMP_DOWN1_HASH = Animator.StringToHash("JumpDown1");
        public readonly static int DEATH_HASH = Animator.StringToHash("Death");
        public readonly static int PARRY_HASH = Animator.StringToHash("Parry");

        public readonly static int HEAVY_PUNCH_STATE = Animator.StringToHash("Base Layer.HeavyPunch");
        public readonly static int HEAVY_PUNCHED_STATE = Animator.StringToHash("Base Layer.HeavyPunched");
        public readonly static int LIGHT_PUNCH_STATE = Animator.StringToHash("Base Layer.LightPunch");
        public readonly static int LIGHT_PUNCHED_STATE = Animator.StringToHash("Base Layer.LightPunched");
        public readonly static int IDLE_STATE = Animator.StringToHash("Base Layer.Idle");
        public readonly static int FORWARD_STATE = Animator.StringToHash("Base Layer.Forward");
        public readonly static int BACKWARD_STATE = Animator.StringToHash("Base Layer.Backward");
        public readonly static int STRAFE_LEFT_STATE = Animator.StringToHash("Base Layer.StrafeLeft");
        public readonly static int STRAFE_RIGHT_STATE = Animator.StringToHash("Base Layer.StrafeRight");
        public readonly static int LIEDOWN_STATE = Animator.StringToHash("Base Layer.LieDown");

        public readonly static int WALKING_TAG = Animator.StringToHash("walking");


        #endregion "constant data"



    }

    public class CCDemo3_Helper
    {
        public static int GetAnimatorStateHash(AnimatorStateInfo s)
        {
#if !U5
            return s.nameHash;
#else
        return s.fullPathHash;
#endif
        }
    }
}