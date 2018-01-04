using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using MH.Skele;

namespace MH
{
    using MatchMap = System.Collections.Generic.Dictionary<string, MatchBone>;

    public enum Axis
    {
        XZ,
        XY,
        YZ,
    }

	public class MirrorCtrl
	{
	    #region "data"
        // data

        private Axis m_SymBoneAxis = Axis.XY;
        private Axis m_NonSymBoneAxis = Axis.XY;

        private MatchMap m_MatchMap;
        private List<Regex> m_REs;
        private List<string> m_ReplaceStrs;

        private Transform m_AnimRoot;

        #endregion "data"

	    #region "public method"
        // public method

        public Axis SymBoneAxis
        {
            get { return m_SymBoneAxis; }
            set { m_SymBoneAxis = value;}
        }

        public Axis NonSymBoneAxis
        {
            get { return m_NonSymBoneAxis; }
            set { m_NonSymBoneAxis = value;}
        }

        public MirrorCtrl() {
            m_MatchMap = new MatchMap();
        }

        public void Init(Transform animRoot){
            m_AnimRoot = animRoot;
            _InitRE();
            _GenMatchMap();
        }

        public void Fini()
        {
            _Clear();
        }

        /// <summary>
        /// given the rootTr, try best to find out the symBoneAxis and nonSymBoneAxis,
        /// return true iff succeeds
        /// </summary>
        public bool AutoDetectAxis()
        {
            return true;
            // sweep all bones and count 
            //throw new NotImplementedException();
        }

        public static MirrorNameRegex InitMirrorNameRegex()
        {
            var res = AssetDatabase.LoadAssetAtPath(RELstPath, typeof(MirrorNameRegex)) as MirrorNameRegex;
            if (res == null)
            {
                res = ScriptableObject.CreateInstance<MirrorNameRegex>();
                var lst = res.m_REPrLst = new List<MirrorNameRegex.REPair>();
                lst.Add(new MirrorNameRegex.REPair("Left", "Right")); //mixamo 
                lst.Add(new MirrorNameRegex.REPair("_L\\b", "_R"));
                lst.Add(new MirrorNameRegex.REPair("\\.L\\b", ".R"));
                lst.Add(new MirrorNameRegex.REPair(" L ", " R "));  //bip
                lst.Add(new MirrorNameRegex.REPair("Right", "Left"));
                lst.Add(new MirrorNameRegex.REPair("_R\\b", "_L"));
                lst.Add(new MirrorNameRegex.REPair("\\.R\\b", ".L"));
                lst.Add(new MirrorNameRegex.REPair(" R ", " L "));
                AssetDatabase.CreateAsset(res, RELstPath);
            }
            return res;
        }

        public int IsMatchRE(string p)
        {
            for (int idx = 0; idx < m_REs.Count; ++idx)
            {
                Regex r = m_REs[idx];
                if (r.IsMatch(p))
                {
                    return idx;
                }
            }

            return -1;
        }

        public string GetMatchPath(string thisPath, int REidx)
        {
            Regex r = m_REs[REidx];
            string repl = m_ReplaceStrs[REidx];
            string newStr = r.Replace(thisPath, repl);
            return newStr;
        }

        /// <summary>
        /// try to get mirrored bone, if not found or is deactivated, return null
        /// </summary>
        public Transform GetMirrorBone(Transform thisBone)
        {
            string thisTrPath = AnimationUtility.CalculateTransformPath(thisBone, m_AnimRoot);
            MatchBone matchBone;
            if( m_MatchMap.TryGetValue(thisTrPath, out matchBone) )
            {
                string thatTrPath = matchBone.path;
                Transform thatTr = m_AnimRoot.Find(thatTrPath);
                return thatTr;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// mirror thisBone's data to corresponding 
        /// </summary>
        public void ApplyMirror(Transform thisBone)
        {
            Transform thatBone = GetMirrorBone(thisBone);
            if (thatBone == null)
                return;

            Undo.RecordObject(thatBone, "Apply Mirror");

            Axis selfAxisValue = _GetAxisValue(thisBone);
            Axis selfParentAxisValue = _GetAxisValueForParent(thisBone);

            //rot
            {
                Vector3 planeNormal = Vector3.right;
                Quaternion oldQ = thisBone.localRotation;
                Quaternion newQ = oldQ;
                if (selfAxisValue != selfParentAxisValue)
                {
                    planeNormal = _GetPlaneNormal(selfParentAxisValue);
                    newQ = _ReflectQ(oldQ, selfAxisValue, planeNormal);
                }
                else
                {
                    switch (selfAxisValue)
                    {
                        case Axis.XY:
                            {
                                newQ.x = -newQ.x;
                                newQ.y = -newQ.y;
                            }
                            break;
                        case Axis.XZ:
                            {
                                newQ.x = -newQ.x;
                                newQ.z = -newQ.z;
                            }
                            break;
                        case Axis.YZ:
                            {
                                newQ.y = -newQ.y;
                                newQ.z = -newQ.z;
                            }
                            break;
                        default:
                            Dbg.LogErr("MirrorCtrl.ApplyMirror: unexpected parentAxisValue: {0}", selfAxisValue);
                            break;
                    }
                }
                thatBone.localRotation = newQ;
            }
            
            //pos
            {
                Vector3 oldP = thisBone.localPosition;
                Vector3 newP = oldP;
                switch (selfParentAxisValue)
                {
                    case Axis.XZ:
                        {
                            newP.y = -newP.y;
                        }
                        break;
                    case Axis.XY:
                        {
                            newP.z = -newP.z;
                        }
                        break;
                    case Axis.YZ:
                        {
                            newP.x = -newP.x;
                        }
                        break;
                    default:
                        Dbg.LogErr("MirrorCtrl.ApplyMirror: unexpected mirror axis value (2nd): {0}", selfParentAxisValue);
                        break;
                }
                thatBone.localPosition = newP;
            }            

            //scale
            {
                thatBone.localScale = thisBone.localScale;
            }

            HotFix.FixRotation(thatBone);
        }

        #endregion "public method"

	    #region "private method"
        // private method

        private void _Clear()
        {
            m_REs = null;
            m_ReplaceStrs = null;
            m_MatchMap.Clear();
        }

        private void _InitRE()
        {
            MirrorNameRegex res = InitMirrorNameRegex();

            m_REs = new List<Regex>();
            m_ReplaceStrs = new List<string>();
            foreach (var REPair in res.m_REPrLst)
            {
                m_REs.Add(new Regex(REPair.fromBoneRE));
                m_ReplaceStrs.Add(REPair.replaceString);
            }
        }

        private void _GenMatchMap()
        {
            SkinnedMeshRenderer smr = SMREditor.GetSMR();
            Dbg.Assert(smr != null, "MirrorCtrl.__GenMatchMap: failed to get SMR");

            Transform[] bones = smr.bones;

            // build trPath map
            Dictionary<string, bool> trPathMap = new Dictionary<string, bool>();
            for (int idx = 0; idx < bones.Length; ++idx )
            {
                Transform thisBone = bones[idx];
                string thisPath = AnimationUtility.CalculateTransformPath(thisBone, m_AnimRoot);
                trPathMap[thisPath] = true;
            }

            // gen match map
            for (int idx = 0; idx < bones.Length; ++idx)
            {
                Transform thisBone = bones[idx];
                string thisPath = AnimationUtility.CalculateTransformPath(thisBone, m_AnimRoot);

                int matchREIdx = IsMatchRE(thisPath);
                if (matchREIdx >= 0)
                {
                    string thatPath = GetMatchPath(thisPath, matchREIdx);
                    bool bFound = trPathMap.ContainsKey(thatPath);

                    m_MatchMap[thisPath] = new MatchBone(thatPath, bFound);
                }
            }
        }

        private Axis _GetAxisValue(Transform tr)
        {
            string trPath = AnimationUtility.CalculateTransformPath(tr, m_AnimRoot);

            if (m_MatchMap.ContainsKey(trPath))
            {
                return m_SymBoneAxis;
            }
            else
            {
                return m_NonSymBoneAxis;
            }
        }

        /// <summary>
        /// get the axis value for the parent joint of the specified bindingPath
        /// </summary>
        private Axis _GetAxisValueForParent(Transform tr)
        {
            if (tr == m_AnimRoot)
            {
                return m_NonSymBoneAxis; //if bindingPath points to AnimRoot, then just return non-sym bone axis, should be OK?
            }

            Transform parentBone = tr.parent;

            string trPath = AnimationUtility.CalculateTransformPath(parentBone, m_AnimRoot);

            if (m_MatchMap.ContainsKey(trPath))
            {
                return m_SymBoneAxis;
            }
            else
            {
                return m_NonSymBoneAxis;
            }
        }

        private static Vector3 _GetPlaneNormal(Axis selfParentAxisValue)
        {
            Vector3 planeNormal = Vector3.zero;
            switch (selfParentAxisValue)
            {
                case Axis.XY: planeNormal = Vector3.forward; break;
                case Axis.XZ: planeNormal = Vector3.up; break;
                case Axis.YZ: planeNormal = Vector3.right; break;
                default: Dbg.LogErr("MirrorCtrl.ApplyMirror: unexpected parentAxisValue: {0}", selfParentAxisValue); break;
            }
            return planeNormal;
        }

        private Quaternion _ReflectQ(Quaternion rot, Axis axisValue, Vector3 planeNormal)
        {
            switch (axisValue)
            {
                case Axis.XY: return QUtil.Reflect_XY(rot, planeNormal);
                case Axis.XZ: return QUtil.Reflect_XZ(rot, planeNormal);
                case Axis.YZ: return QUtil.Reflect_YZ(rot, planeNormal);

                default:
                    Dbg.LogErr("MirrorCtrl._ReflectQ: unexpected axisValue: {0}", axisValue);
                    return Quaternion.identity;
            }
        }

        #endregion "private method"

	    #region "constant data"
        // constant data

        public const string RELstPath = "Assets/Skele/CharacterAnimationTools/Res/TheBoneManipulator/BoneMirrorRELst.asset";

        #endregion "constant data"
	}


    public class MatchBone
    {
        public string path;
        public bool found;
        public MatchBone(string p, bool found)
        {
            path = p;
            this.found = found;
        }
    }
}
