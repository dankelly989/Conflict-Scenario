using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ExtMethods;
using MH.Constraints;
using MH.IKConstraint;
using System.Text;

namespace MH
{
    public class BipedConstraintSetupWindow : EditorWindow
    {
        #region "conf data"
        #endregion "conf data"

        #region "data"

        private bool _refOpen = false;
        private bool _optionOpen = false;
        private List<Transform> _bones = new List<Transform>();

        private bool _addFloor = false;
        private Transform _floorObj = null;
        private bool _addPelvisFollow = false;
        private bool _addIKTarget = false;

        //private Material _matMarker;
        private GameObject _pfMarker;
        private StringBuilder _execLog = new StringBuilder();
        private Vector2 _scrollPos = new Vector2();

        #endregion "data"

        #region "unity methods"

        [MenuItem("Window/Skele/SetupBipedConstraint")]
        public static void OpenWindow()
        {
            var wnd = EditorWindow.GetWindow<BipedConstraintSetupWindow>();
            EUtil.SetEditorWindowTitle(wnd, "SetupConstraint");
            wnd._bones.Resize((int)Bones.END);

            var tr = Selection.activeTransform;
            if( tr != null )
            {
                var ator = tr.GetComponentInParent<Animator>();
                if( ator != null )
                {
                    wnd._bones[(int)Bones.Root] = ator.transform;
                    wnd._ClearBonesRefExceptRoot();
                    wnd._TryFindBones();
                }                
            }
        }

        void OnEnable()
        {
            //_matMarker = AssetDatabase.LoadAssetAtPath<Material>(MarkerMaterialPath);
            _pfMarker = AssetDatabase.LoadAssetAtPath<GameObject>(MarkerRectFrame);
            Dbg.Assert(_pfMarker != null, "BipedConstraintSetupWindow.OnEnable: failed to load rectFrame prefab at: {0}", MarkerRectFrame);
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _bones[(int)Bones.Root] = (Transform)EditorGUILayout.ObjectField(new GUIContent("Root", "the one at topmost with Animator componnet"), _bones[(int)Bones.Root], typeof(Transform), true);
            if( EditorGUI.EndChangeCheck() )
            {
                _ClearBonesRefExceptRoot();
                _TryFindBones();
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {

                _refOpen = EditorGUILayout.Foldout(_refOpen, "bones");
                if (_refOpen)
                {
                    for (Bones eBone = (Bones)0; eBone < Bones.END; ++eBone)
                    {
                        Transform t = _bones[(int)eBone];
                        _bones[(int)eBone] = (Transform)EditorGUILayout.ObjectField(eBone.ToString(), t, typeof(Transform), true);
                    }
                }

                _optionOpen = EditorGUILayout.Foldout(_optionOpen, "options");
                if (_optionOpen)
                {
                    _addIKTarget = EditorGUILayout.Toggle(new GUIContent("Add IKTarget", "Add IK Targets for limbs"), _addIKTarget);

                    if (_addIKTarget)
                    {
                        _addFloor = EditorGUILayout.Toggle(new GUIContent("Floor Constraint", "Add Floor constraint on feet"), _addFloor);
                        if (_addFloor)
                        {
                            EditorGUI.indentLevel++;
                            _floorObj = (Transform)EditorGUILayout.ObjectField(new GUIContent("Floor Obj", "the object be taken as floor"), _floorObj, typeof(Transform), true);
                            EditorGUI.indentLevel--;
                        }

                        _addPelvisFollow = EditorGUILayout.Toggle(new GUIContent("Pelvis Follow", "Pelvis position is calculated with two feet"), _addPelvisFollow);
                    }
                }

                bool isAllFound = _IsAllFound();
                EditorGUILayout.HelpBox(isAllFound ? "All bones are found, you might check if the mappings are correct" : "Not all bones are setup, you might want to manually set those left out", MessageType.Info);

                if (_execLog.Length > 0)
                {
                    EditorGUILayout.HelpBox(_execLog.ToString(), MessageType.Info);
                }

                EUtil.DrawSplitter();
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(0.1f * position.width);
                    EUtil.PushGUIEnable(_bones[(int)Bones.Root] != null);
                    if (EUtil.Button("Add Constraints", "If not all bones are set up, constraints will be only setup on those qualified", isAllFound ? Color.green : Color.yellow, GUILayout.Height(50f)))
                    {
                        _ExecuteAddConstraints();
                    }
                    if (EUtil.Button("Clear Constraints", "Clear all constraints on qualified joints", Color.white, GUILayout.Height(50f)))
                    {
                        _ExecuteDelConstraints();
                    }
                    EUtil.PopGUIEnable();
                    GUILayout.Space(0.1f * position.width);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion "unity methods"

        #region "public methods"
        #endregion "public methods"

        #region "private methods"

        private void _CollectBonesByHumanoidAvatar(Animator am)
        {
            _bones[(int)Bones.Pelvis] = am.GetBoneTransform(HumanBodyBones.Hips);
            _bones[(int)Bones.UpperArmR] = am.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _bones[(int)Bones.UpperArmL] = am.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _bones[(int)Bones.LowerArmR] = am.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _bones[(int)Bones.LowerArmL] = am.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            _bones[(int)Bones.HandR] = am.GetBoneTransform(HumanBodyBones.RightHand);
            _bones[(int)Bones.HandL] = am.GetBoneTransform(HumanBodyBones.LeftHand);
            _bones[(int)Bones.UpperLegR] = am.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            _bones[(int)Bones.UpperLegL] = am.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            _bones[(int)Bones.LowerLegR] = am.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            _bones[(int)Bones.LowerLegL] = am.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            _bones[(int)Bones.FootR] = am.GetBoneTransform(HumanBodyBones.RightFoot);
            _bones[(int)Bones.FootL] = am.GetBoneTransform(HumanBodyBones.LeftFoot);
        }

        private void _ClearBonesRefExceptRoot()
        {
            for (int i = 1; i < (int)Bones.END; ++i)
                _bones[i] = null;
            _execLog.Remove(0, _execLog.Length);
        }

        private void _TryFindBones()
        {
            if (_bones[(int)Bones.Root] == null)
                return;

            Animator am = _bones[(int)Bones.Root].GetComponent<Animator>();
            if( am && am.isHuman )
            {
                _CollectBonesByHumanoidAvatar(am);
            }
            else
            {
                _TryCollectBones();
            }

        }

        private void _TryCollectBones()
        {
            Transform root = _bones[(int)Bones.Root];

            Dictionary<Transform, bool> validBones = new Dictionary<Transform, bool>();
            Transform[] allTransforms = root.GetComponentsInChildren<Transform>();
            foreach(var b in allTransforms){ validBones[b] = true; }

            Dictionary<int, Transform> outDict = new Dictionary<int, Transform>();
            outDict = (Dictionary<int, Transform>)RCall.CallMtd1("UnityEditor.AvatarAutoMapper", "MapBones", 
                new Type[] { typeof(Transform), typeof(Dictionary<Transform, bool>) }, 
                null, root, validBones);

            _bones[(int)Bones.Pelvis] = outDict.TryGet((int)HumanBodyBones.Hips);
            _bones[(int)Bones.UpperArmR] = outDict.TryGet((int)HumanBodyBones.RightUpperArm);
            _bones[(int)Bones.UpperArmL] = outDict.TryGet((int)HumanBodyBones.LeftUpperArm);
            _bones[(int)Bones.LowerArmR] = outDict.TryGet((int)HumanBodyBones.RightLowerArm);
            _bones[(int)Bones.LowerArmL] = outDict.TryGet((int)HumanBodyBones.LeftLowerArm);
            _bones[(int)Bones.HandR] = outDict.TryGet((int)HumanBodyBones.RightHand);
            _bones[(int)Bones.HandL] = outDict.TryGet((int)HumanBodyBones.LeftHand);
            _bones[(int)Bones.UpperLegR] = outDict.TryGet((int)HumanBodyBones.RightUpperLeg);
            _bones[(int)Bones.UpperLegL] = outDict.TryGet((int)HumanBodyBones.LeftUpperLeg);
            _bones[(int)Bones.LowerLegR] = outDict.TryGet((int)HumanBodyBones.RightLowerLeg);
            _bones[(int)Bones.LowerLegL] = outDict.TryGet((int)HumanBodyBones.LeftLowerLeg);
            _bones[(int)Bones.FootR] = outDict.TryGet((int)HumanBodyBones.RightFoot);
            _bones[(int)Bones.FootL] = outDict.TryGet((int)HumanBodyBones.LeftFoot);

            //foreach( var pr in outDict )
            //{
            //    Dbg.Log(((HumanBodyBones)pr.Key) + ":" + pr.Value);
            //}
        }

        private bool _IsAllFound()
        {
            return _bones.TrueForAll(x => x != null);
        }

        private void _ExecuteAddConstraints()
        {
            _execLog.Remove(0, _execLog.Length);
            _AddConstraintForArm(Bones.HandL, Bones.LowerArmL, Bones.UpperArmL);
            _AddConstraintForArm(Bones.HandR, Bones.LowerArmR, Bones.UpperArmR);
            _AddConstraintForLeg(Bones.FootL, Bones.LowerLegL, Bones.UpperLegL);
            _AddConstraintForLeg(Bones.FootR, Bones.LowerLegR, Bones.UpperLegR);

            Transform root = _bones[(int)Bones.Root];
            Transform lfTarget = root.Find("LFTarget");
            Transform rfTarget = root.Find("RFTarget");
            Transform pelvis = _bones[(int)Bones.Pelvis];
            if(_addPelvisFollow && lfTarget && rfTarget && pelvis)
            {
                // clear old constraints if exists

                ConstraintStack cs = pelvis.ForceGetComponent<ConstraintStack>();
                cs.RemoveByType(typeof(CopyPosition));
                cs.ExecOrder = 50;

                // add new ones
                var copyL = pelvis.gameObject.AddComponent<CopyPosition>();
                var copyR = pelvis.gameObject.AddComponent<CopyPosition>();
                copyL.Target = lfTarget;
                copyL.Influence = 1f;
                copyL.Affect = EAxisD.X | EAxisD.Z;
                copyL.UseOffset = false;
                copyR.Target = rfTarget;
                copyR.Influence = 0.5f;
                copyR.Affect = EAxisD.X | EAxisD.Z;
                copyR.UseOffset = false;
            }
        }

        private void _ExecuteDelConstraints()
        {
            _execLog.Remove(0, _execLog.Length);
            _DelConstraintForArm(Bones.HandL, Bones.LowerArmL, Bones.UpperArmL);
            _DelConstraintForArm(Bones.HandR, Bones.LowerArmR, Bones.UpperArmR);
            _DelConstraintForLeg(Bones.FootL, Bones.LowerLegL, Bones.UpperLegL);
            _DelConstraintForLeg(Bones.FootR, Bones.LowerLegR, Bones.UpperLegR);

            Transform pelvis = _bones[(int)Bones.Pelvis];
            if( pelvis )
            {
                var cs = pelvis.GetComponent<ConstraintStack>();
                if( cs )
                {
                    cs.RemoveAll();
                    MUndo.DestroyObj(cs);
                }
            }

        }

        private void _DelConstraintForLeg(Bones eFoot, Bones eLowerLeg, Bones eUpperLeg)
        {
            Transform root = _bones[(int)Bones.Root];
            Transform foot = _bones[(int)eFoot];
            Transform lleg = _bones[(int)eLowerLeg];
            Transform uleg = _bones[(int)eUpperLeg];

            string targetName = eFoot == Bones.FootL ? "LFTarget" : "RFTarget";
            Transform target = root.Find(targetName);
            if( target != null )
            {
                var cstack = foot.GetComponent<ConstraintStack>();
                if (cstack != null)
                {
                    cstack.RemoveAll();
                    MUndo.DestroyObj(cstack);
                }
                MUndo.DestroyObj(target.gameObject);
                _execLog.AppendFormat("removed IKTarget of {0}\n", targetName);
            }

            if ( foot != null )
            {
                var cstack = foot.GetComponent<ConstraintStack>();
                if( cstack != null )
                {
                    cstack.RemoveAll();
                    MUndo.DestroyObj(cstack);
                    _execLog.AppendFormat("removed constraints on {0}\n", foot.name);
                }                
            }

            if ( lleg != null )
            {
                var cp = lleg.GetComponent<AngleConstraintMB>();
                if( cp != null )
                {
                    MUndo.DestroyObj(cp);
                    _execLog.AppendFormat("removed constraint on {0}\n", lleg.name);
                }                
            }

            if ( uleg != null )
            {
                var cp = uleg.GetComponent<ConeConstraintMB>();
                if (cp != null)
                {
                    MUndo.DestroyObj(cp);
                    _execLog.AppendFormat("removed constraint on {0}\n", uleg.name);
                }
            }
        }

        private void _DelConstraintForArm(Bones eHand, Bones eLowerArm, Bones eUpperArm)
        {
            Transform pelvis = _bones[(int)Bones.Pelvis];
            Transform hand = _bones[(int)eHand];
            Transform larm = _bones[(int)eLowerArm];
            Transform uarm = _bones[(int)eUpperArm];

            string targetName = eHand == Bones.HandL ? "LHTarget" : "RHTarget";
            Transform target = pelvis.Find(targetName);
            if (target != null)
            {
                MUndo.DestroyObj(target.gameObject);
                _execLog.AppendFormat("removed IKTarget of {0}\n", targetName);
            }

            if (hand != null)
            {
                var cp = hand.GetComponent<CCDSolverMB>();
                if (cp != null)
                {
                    MUndo.DestroyObj(cp);

                    var cstack = hand.GetComponent<ConstraintStack>();
                    MUndo.DestroyObj(cstack);

                    _execLog.AppendFormat("removed IKSolver on {0}\n", hand.name);
                }
            }

            if (larm != null)
            {
                var cp = larm.GetComponent<AngleConstraintMB>();
                if (cp != null)
                {
                    MUndo.DestroyObj(cp);
                    _execLog.AppendFormat("removed constraint on {0}\n", larm.name);
                }
            }

            if (uarm != null)
            {
                var cp = uarm.GetComponent<ConeConstraintMB>();
                if (cp != null)
                {
                    MUndo.DestroyObj(cp);
                    _execLog.AppendFormat("removed constraint on {0}\n", uarm.name);
                }
            }
        }

        private void _AddConstraintForLeg(Bones eFoot, Bones eLowerLeg, Bones eUpperLeg)
        {
            Transform root = _bones[(int)Bones.Root];
            Transform foot = _bones[(int)eFoot];
            Transform lleg = _bones[(int)eLowerLeg];
            Transform uleg = _bones[(int)eUpperLeg];

            if (foot == null || lleg == null || uleg == null)
            {
                if (foot == null) _execLog.AppendFormat("{0} is null, giving up adding constraint on this leg\n", eFoot.ToString());
                if (lleg == null) _execLog.AppendFormat("{0} is null, giving up adding constraint on this leg\n", eLowerLeg.ToString());
                if (uleg == null) _execLog.AppendFormat("{0} is null, giving up adding constraint on this leg\n", eUpperLeg.ToString());
                return;
            }

            lleg.ForceGetComponent<AngleConstraintMB>();
            uleg.ForceGetComponent<ConeConstraintMB>();

            if(_addIKTarget)
            {
                Transform target = ((GameObject)PrefabUtility.InstantiatePrefab(_pfMarker)).transform;
                target.name = eFoot == Bones.FootL ? "LFTarget" : "RFTarget";
                target.SetParent(root);
                target.position = foot.position;
                target.localScale = MarkerScale;
                target.GetComponent<EmptyMarker>().jumpTo = foot;
                //target.rotation = foot.rotation;

                var solver = foot.ForceGetComponent<CCDSolverMB>();
                solver.Target = target;
                var jointLst = solver.jointList;
                jointLst.Clear();
                jointLst.AddMulti(uleg, lleg, foot);
                solver._RenewInitInfoAndSolver();

                // floor
                if (_addFloor && _floorObj != null)
                {
                    var floor = target.ForceGetComponent<Floor>();
                    floor.Target = _floorObj;
                    floor.UseRaycast = true;
                    floor.UseOffset = true;
                    floor.Offset = 0.1f;
                }
            }            

            _execLog.AppendFormat("Successfully added constraints for {0}\n", eFoot == Bones.FootL ? "LeftLeg" : "RightLeg");
        }

        private void _AddConstraintForArm(Bones eHand, Bones eLowerArm, Bones eUpperArm)
        {
            Transform pelvis = _bones[(int)Bones.Pelvis];
            Transform hand = _bones[(int)eHand];
            Transform larm = _bones[(int)eLowerArm];
            Transform uarm = _bones[(int)eUpperArm];

            if ( hand == null || larm == null || uarm == null )
            {
                if (hand == null) _execLog.AppendFormat("{0} is null, giving up adding constraint on this arm\n", eHand.ToString());
                if (larm == null) _execLog.AppendFormat("{0} is null, giving up adding constraint on this arm\n", eLowerArm.ToString());
                if (uarm == null) _execLog.AppendFormat("{0} is null, giving up adding constraint on this arm\n", eUpperArm.ToString());
                return;
            }



            larm.ForceGetComponent<AngleConstraintMB>();
            uarm.ForceGetComponent<ConeConstraintMB>();

            if( _addIKTarget )
            {
                Transform target = ((GameObject)PrefabUtility.InstantiatePrefab(_pfMarker)).transform;
                target.name = eHand == Bones.HandL ? "LHTarget" : "RHTarget";
                target.SetParent(pelvis);
                target.position = hand.position;
                target.localScale = MarkerScale;
                target.GetComponent<EmptyMarker>().jumpTo = hand;

                var solver = hand.ForceGetComponent<CCDSolverMB>();
                solver.Target = target;
                var jointLst = solver.jointList;
                jointLst.Clear();
                jointLst.AddMulti(uarm, larm, hand);
                solver._RenewInitInfoAndSolver();
            }            

            _execLog.AppendFormat("Successfully added constraints for {0}\n", eHand == Bones.HandL ? "LeftArm" : "RightArm");
        }

        #endregion "private methods"

        #region "constants"

        private enum Bones
        {
            Root,
            Pelvis,
            UpperArmR, UpperArmL,
            LowerArmR, LowerArmL,
            HandR, HandL,
            UpperLegR, UpperLegL,
            LowerLegR, LowerLegL,
            FootR, FootL,
            END,
        }

        private const string MarkerMaterialPath = "Assets/Skele/Common/Res/Marker/Materials/DefaultMarker.mat";
        private const string MarkerRectFrame = "Assets/Skele/Common/Res/Marker/MarkerRectFrame.prefab";
        private static readonly Vector3 MarkerScale = new Vector3(0.2f, 1f, 0.2f);

        #endregion "constants"
    }
}
