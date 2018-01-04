using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
namespace MeshOp
{

    using VLst = System.Collections.Generic.List<int>;

    /// <summary>
    /// used when Soft-selection is activated
    /// </summary>
    public class SoftSelection
    {
	    #region "data"
        // data

        private _Data[] m_Cont;
        private float m_Range;
        private AnimationCurve m_Atten;

        private EditableMesh m_Mesh;
        private MeshSelection m_Selection;
        private Pivotor m_Pivot;

        private Mode m_Mode;
        private PrepareMode m_PrepareMode;

        private VLst m_EffectVertIdxLst; //the indices of verts under effect > 0

        private Vector3[] m_CachedVertPos; //used in dragging handle, cache the starting position of verts

        #endregion "data"

	    #region "public method"
        // public method

        public void Init(EditableMesh m, MeshSelection sel, Pivotor p)
        {
            m_Mesh = m;
            m_Selection = sel;
            m_Pivot = p;
            Dbg.Assert(m_Pivot != null, "SoftSelection.Init: pivotor is null");

            m_Range = 1f;

            m_Cont = new _Data[m.mesh.vertexCount];
            for (int i = 0; i < m_Cont.Length; ++i)
            {
                m_Cont[i] = new _Data();
            }

            m_EffectVertIdxLst = new VLst();

            m_Mode = Mode.Off;
            m_PrepareMode = PrepareMode.Always;

            // atten curve
            var res = (SoftSelectionRes)AssetDatabase.LoadAssetAtPath(SOFTSEL_ATTEN_CURVE_PATH, typeof(SoftSelectionRes));
            if( res == null )
            {
                res = ScriptableObject.CreateInstance<SoftSelectionRes>();
                AssetDatabase.CreateAsset(res, SOFTSEL_ATTEN_CURVE_PATH);
                res = (SoftSelectionRes)AssetDatabase.LoadAssetAtPath(SOFTSEL_ATTEN_CURVE_PATH, typeof(SoftSelectionRes));
                Dbg.Assert(res != null, "SoftSelection.Init: failed to create curve asset for SoftSelection");
            }
            m_Atten = res.attenCurve;
            
            MeshManipulator.evtHandleDraggingStateChanged += this._OnHandleDraggingStateChanged;
        }

        public void Fini()
        {
            MeshManipulator.evtHandleDraggingStateChanged -= this._OnHandleDraggingStateChanged;
        }

        public AnimationCurve Atten
        {
            get { return m_Atten; }
            set { m_Atten = value; }
        }

        public float Range
        {
            get { return m_Range; }
            set { 
                if( !Mathf.Approximately(m_Range, value) )
                {
                    m_Range = value;
                    _CalcPercentage();
                }
            }
        }

        public Vector3[] CachedVertPos
        {
            get { return m_CachedVertPos; }
            set { m_CachedVertPos = value; }
        }

        // only for debug
        //public PrepareMode PMode
        //{
        //    get { return m_PrepareMode; }
        //    set { m_PrepareMode = value;}
        //}

        public bool Activated
        {
            get { return m_Mode != Mode.Off; }
        }

        public Mode CurMode
        {
            get { return m_Mode;}
            set { 
                if(m_Mode != value )
                {
                    m_Mode = value;
                }
            }
        }

        public VLst EffectVerts
        {
            get { return m_EffectVertIdxLst; }
        }

        public void Prepare()
        {
            if (m_PrepareMode == PrepareMode.Stop)
                return;
            else if (m_PrepareMode == PrepareMode.OnlyOnce)
                m_PrepareMode = PrepareMode.Stop;

            m_CachedVertPos = m_Mesh.mesh.vertices; //cache as starting pos, useful when dynamically change range
            VLst selectedVerts = m_Selection.GetVertices();

            //1. set the initial effect percentage
            Reset();
            //1.1 set the seeds
            for (int i = 0; i < selectedVerts.Count; ++i)
            {
                int vidx = selectedVerts[i];
                m_Cont[vidx].AsSeed();
            }

            //2. calculate nearest distance for every vert, 
            _CalcDist(selectedVerts);
            
            //3. calc the percentage
            _CalcPercentage();
        }


        public VLst GetEffectVerts()
        {
            return m_EffectVertIdxLst;
        }

     

        public void Reset()
        {
            for(int i=0; i<m_Cont.Length; ++i)
            {
                m_Cont[i].Reset();
            }
        }

        public float GetPercentage(int vidx)
        {
            Dbg.Assert(vidx < m_Cont.Length, "SoftSelection.GetPercentage: vidx: {0} beyond range {1}", vidx, m_Cont.Length);
            return m_Cont[vidx].percentage;
        }

        public void Set(int vidx, float perc)
        {
            m_Cont[vidx].percentage = perc;
        }

        public bool Has(int vidx)
        {
            return m_Cont[vidx].percentage > 0;
        }

        #endregion "public method"

	    #region "private method"

        /// <summary>
        /// calculate for each vert, the minimum distance to a seed vert
        /// </summary>
        private void _CalcDist(VLst selectedVerts)
        {
            Transform meshTr = m_Mesh.transform;

            Mesh m = m_Mesh.mesh;
            Vector3[] oriVerts = m.vertices; //original verts pos

            // generate the transformed verts
            Vector3[] verts = new Vector3[oriVerts.Length]; //the verts transformed by matrix
            for(int i=0; i<m.vertexCount; ++i)
            {
                verts[i] = meshTr.TransformPoint(oriVerts[i]);
            }

            // calc distances
            int found = 0;
            for(int i=0; i<m_Cont.Length; ++i )
            {
                _Data lhs = m_Cont[i];
                if (lhs.percentage == 1f)
                    continue;

                Vector3 lhsPos = verts[i];

                for(int j=0; j<selectedVerts.Count; ++j)
                {
                    int seedVertIdx = selectedVerts[j];
                    Vector3 seedPos = verts[seedVertIdx];

                    float d = Vector3.Distance(seedPos, lhsPos);

                    lhs.dist = Mathf.Min(lhs.dist, d);
                }

                if (lhs.dist < m_Range)
                    ++found;
            }

            //Dbg.Log("SoftSelection._CalcDist: found {0}", found);
        }

        /// <summary>
        /// must be performed after distance is ready
        /// </summary>
        private void _CalcPercentage()
        {
            m_EffectVertIdxLst.Clear();

            for(int i=0; i< m_Cont.Length; ++i)
            {
                _Data data = m_Cont[i];
                float dist = data.dist;

                if (dist > m_Range)
                {
                    data.percentage = 0;
                }
                else
                {
                    float t = dist / m_Range;
                    float percentage = m_Atten.Evaluate(t); // the curve would be better a [1, 0] curve, decreasing
                    data.percentage = percentage;
                    m_EffectVertIdxLst.Add(i);
                    //Dbg.Log("_CalcPercentage: i:{0}, p:{1}", i, percentage);
                }
            }

            //Dbg.Log("SoftSelection._CalcPercentage");
        }

        private void _OnHandleDraggingStateChanged(bool bDragging)
        {
            if (bDragging)
            {
                m_PrepareMode = PrepareMode.OnlyOnce;
            }
            else
            {
                m_PrepareMode = PrepareMode.Always;
            }
        }

        #endregion "private method"

	    #region "inner struct"
	    // "inner struct" 

        class _Data
        {
            public float percentage; // the percentage of effect [0,1] 
            public float dist; //the nearest distance of all seeds

            public _Data() { percentage = 0f; }
            public void Reset()
            {
                dist = float.MaxValue * 0.1f;
                percentage = 0f;
            }
            public void AsSeed()
            {
                dist = 0f;
                percentage = 1f;
            }
        }
	
	    #endregion "inner struct"

	    #region "constant data"
        // constant data

        public enum Mode
        {
            Off,
            Space3D,
        }

        public enum PrepareMode
        {
            Always,
            OnlyOnce,
            Stop,
        }

        private const string SOFTSEL_ATTEN_CURVE_PATH = "Assets/Skele/MeshEditor/Editor/Res/AttenCurve.asset";

        #endregion "constant data"


    }
}
}
