using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace MH
{
    using VGroup = System.Collections.Generic.List<int>; //represent a group of verts
    using VGroupCont = System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int> >; // vertIdx -> vertGroup

    /// <summary>
    /// this is used to pre-process given mesh, 
    /// and find out those verts that are overlapped,
    /// </summary>
	class OverlapChecker
	{

	    #region "data"
        // data
        private VGroupCont m_VGCont = new VGroupCont();
        private float m_VertDistThres = 0.001f;

        #endregion "data"

	    #region "public method"
        // public method

        public OverlapChecker() { }

        public void Init(Mesh m)
        {
            Init(m, DEF_SAME_VERT_DIST_THRES);
        }
        public void Init(Mesh m, float sameThres)
        {
            m_VertDistThres = sameThres;
            Vector3[] verts = m.vertices;
            List<_VertUnit> vunits = verts.Select((v, idx) => { return new _VertUnit(v, idx); }).ToList();
            vunits.Sort(_VertUnit.Compare);

            _FillVGroupCont(vunits);
        }

        public void Fini()
        {
            m_VGCont.Clear();
        }

        public bool IsInVGroup(int vidx)
        {
            return m_VGCont.ContainsKey(vidx);
        }

        public bool GetVGroup(int vidx, out VGroup grp)
        {
            return m_VGCont.TryGetValue(vidx, out grp);
        }

        #endregion "public method"

	    #region "private method"

        private void _FillVGroupCont(List<_VertUnit> vunits)
        {
            m_VGCont.Clear();
            if (vunits.Count == 0)
                return;

            float sqrDistThres = m_VertDistThres * m_VertDistThres;

            _VertUnit guard = vunits[0];
            for(int i=1; i<vunits.Count; ++i)
            {
                _VertUnit one = vunits[i];
                Vector3 diff = guard.pos - one.pos;
                //Dbg.Log("dist: {0}<->{1}, {2:F6}", guard.idx, one.idx, diff.magnitude);
                if (Vector3.SqrMagnitude(diff) < sqrDistThres)
                {
                    VGroup grp = _ForceGetVGroup(guard);
                    _AddToVGroup(one.idx, grp); //add `one' into grp
                }
                else
                {
                    guard = one; //advance guard to `one'
                }
            }
        }

        private VGroup _ForceGetVGroup(_VertUnit v)
        {
            int idx = v.idx;
            VGroup grp = null;
            if( !m_VGCont.TryGetValue(idx, out grp) )
            {
                grp = new VGroup();
                grp.Add(idx);
                m_VGCont.Add(idx, grp);
            }

            return grp;
        }

        private void _AddToVGroup(int idx, VGroup grp)
        {
            Dbg.Assert(!m_VGCont.ContainsKey(idx), "OverlapChecker._AddToGroup: vert already in VGCont: {0}", idx);
            Dbg.Assert(!grp.Contains(idx), "OverlapChecker._AddToGroup: vert already in group: {0}", idx);

            m_VGCont.Add(idx, grp);
            grp.Add(idx);
        }

        #endregion "private method"

	    #region "inner struct"
	    // "inner struct" 

        class _VertUnit
        {
            public Vector3 pos;
            public int idx;

            public _VertUnit(Vector3 p, int i) { pos = p; idx = i; }
            public static int Compare(_VertUnit lhs, _VertUnit rhs)
            {
                Vector3 l = lhs.pos;
                Vector3 r = rhs.pos;
                if (l.x < r.x) return -1;
                else if (l.x > r.x) return 1;

                if (l.y < r.y) return -1;
                else if (l.y > r.y) return 1;

                if (l.z < r.z) return -1;
                else if (l.z > r.z) return 1;

                return 0;
            }
        }
	
	    #endregion "inner struct"

	    #region "constant data"
        // constant data

        private const float DEF_SAME_VERT_DIST_THRES = 0.001f;

        #endregion "constant data"
	}
}
