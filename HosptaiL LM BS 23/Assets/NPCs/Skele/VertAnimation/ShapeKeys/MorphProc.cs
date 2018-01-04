using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
namespace VertAnim
{
    /// <summary>
    /// used to process and calculate the mesh based on the ShapeKey data and weights
    /// the 0 idx is for the base mesh, all the others will be based on that
    /// 
    /// weight's domain is [0f, 100f]
    /// </summary>
    [ExecuteInEditMode]
    public class MorphProc : MonoBehaviour
    {
	    #region "configurable data"
        // configurable data

        public float m_Weight0; 
        public float m_Weight1;
        public float m_Weight2;
        public float m_Weight3;
        public float m_Weight4;
        public float m_Weight5;
        public float m_Weight6;
        public float m_Weight7;
        public float m_Weight8;
        public float m_Weight9;

        [SerializeField]
        [Tooltip("the shape-key deform sequences")]
        private List<ShapeKeyMorphSO> m_Deforms;

        [SerializeField]
        private EditableMesh m_Mesh;

        [SerializeField]
        [Tooltip("if need fix, and the frame lapse has exceeded this limit, then execute Fix")]
        private int m_AutoFixFrameLimit = 1;

        [SerializeField]
        [Tooltip("This is a very important performance optimization, it stores and updates the mesh's data to greatly reduce GC amount, check this option as long as you don't manipulate this mesh yourself")]
        private bool m_UseMeshCache = true;

        //[SerializeField]
        //[Tooltip("if true, normals of the mesh will be re-calculated when mesh is modified")]
        //private bool m_RecalcNormal = false;

        #endregion "configurable data"

	    #region "data"
        // data
        
        private float[] m_CmpWeights; //used to compare with m_WeightX to test change
        private ShapeKeyDataDiff m_OutData; //used to contain lerp-ed data

        private MeshCacheRT m_MeshCache;

        private Renderer m_Renderer;

        // auto fix normal & bounds
        private int m_FrameToFix = -1;

        #endregion "data"

	    #region "unity event handlers"
        // unity event handlers

        void Awake()
        {
            //Dbg.Log("MorphProc.Awake: {0}, {1}", name, GetInstanceID());

            if (m_Mesh == null)
            {
                m_Mesh = EditableMesh.New(gameObject);
            }

            var m = m_Mesh.mesh;
            MeshUtil.MarkDynamic(m);

            if (m_Deforms == null)
            { //should be created by MeshManipulator
                m_Deforms = new List<ShapeKeyMorphSO>();
            }
            else
            { //other circumstances, switch playMode, reload assembly, etc.
                Dbg.Assert(m_Deforms.Count > 0, "MorphProc.Awake: the MorphProc is not null but empty?!");
                m_OutData = ShapeKeyDataDiff.TempVarNew(m_Deforms[BASIS_MORPH_IDX]);
            }

            if (m_CmpWeights == null)
            {
                m_CmpWeights = new float[MAX_SHAPEKEY_CNT];
            }
            if( m_MeshCache == null )
            {
                //Dbg.Log("MorphProc.Awake: init MeshCache: {0}", name);  
                m_MeshCache = new MeshCacheRT();
            }

            // force apply weight on mesh, to fix the mesh deforming leftover when switch between playMode and editMode            
            if (m_Deforms.Count > 0)
            {
                ResetToBasisShape();
                for (int i = 1; i < m_Deforms.Count; ++i) //don't check the first base shape
                {
                    float animWeight = Mathf.Clamp(_GetAnimWeight(i), 0, 100f);
                    _ApplyWeightChange(i, animWeight); 
                }

            }

        }

        void LateUpdate()
        {
            if (!_GetRenderer().isVisible)
                return;

            int shapeCnt = m_Deforms.Count;

            //Dbg.Assert(m_CmpWeights != null, "MorphProc.LateUpdate: m_CmpWeights == null: {0}", name);
            //Dbg.Assert(m_OutData != null, "MorphProc.LateUpdate: m_OutData == null: {0}", name);
            //Dbg.Assert(m_MeshCache != null, "MorphProc.LateUpdate: m_MeshCache == null: {0}", name);

            for (int i = 1; i < shapeCnt; ++i) //don't check the first base shape
            {
                float animWeight = Mathf.Clamp(_GetAnimWeight(i), 0, 100f);
                float curWeight = m_CmpWeights[i];
                if (!Mathf.Approximately(animWeight, curWeight))
                {
                    _ApplyWeightChange(i, animWeight);
                }
            }

            //if( ! Application.isPlaying || m_RecalcNormal ) //in runtime, if not specifiy "RecalcNormal", then should directly use the interpolated normals
            _CheckForAutoFix();
        }

        void OnBecameVisible()
        {

        }

        void OnBecameInvisible()
        {

        }

//#if UNITY_EDITOR
//        /// <summary>
//        /// the normals blink like crazy if fix here
//        /// </summary>
//        void OnRenderObject()
//        {
//            if (!Application.isPlaying)
//            {
//                _CheckForAutoFix();
//            }
//        }
//#endif

        #endregion "unity event handlers"

	    #region "public method"
        // public method

        public int MorphCount
        {
            get { return m_Deforms.Count; }
        }

        // for editor
        public EditableMesh GetMesh()
        {
            return m_Mesh;
        }

        public ShapeKeyMorphSO GetMorphAt(int idx)
        {
            Dbg.Assert(idx < m_Deforms.Count, "MorphProc.GetMorphAt: idx beyond range: {0}, {1}", idx, m_Deforms.Count);

            return m_Deforms[idx];
        }

        public void SetMorphAt(int idx, ShapeKeyMorphSO shape)
        {
            Dbg.Assert(m_Deforms != null, "SetMorphAt: m_Deforms == null");

            if( idx < m_Deforms.Count )
            {
                m_Deforms[idx] = shape;
            }
            else if( idx == m_Deforms.Count )
            {
                m_Deforms.Add(shape);
            }
            else if( idx >= MAX_SHAPEKEY_CNT )
            {
                Dbg.LogErr("MorphProc.SetMorphAt: current only support up to 10 shape keys");
            }
            else
            {
                Dbg.LogErr("MorphProc.SetMorphAt: idx beyond range: {0}, {1}", idx, m_Deforms.Count);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// called only by MeshManipulator
        /// </summary>
        public void InitWhenCreatedByEditor()
        {
            Dbg.Assert(m_Deforms.Count == 0, "MorphProc.InitWhenCreatedByEditor: m_Deforms not empty!?");

            ShapeKeyMorphSO newShape = ShapeKeyMorphSO.NewAsBasis(m_Mesh.mesh);
            SetMorphAt(BASIS_MORPH_IDX, newShape);

            m_CmpWeights[BASIS_MORPH_IDX] = FULL_WEIGHT;
            _SetAnimWeight(BASIS_MORPH_IDX, FULL_WEIGHT);

            m_OutData = ShapeKeyDataDiff.TempVarNew(m_Deforms[BASIS_MORPH_IDX]);
        }
#endif

        /// <summary>
        /// add a new deform using current mesh status
        /// </summary>
        public void AddCurrentMeshAsNewShapeKeyMorph()
        {
            if (m_Deforms.Count >= MAX_SHAPEKEY_CNT)
                return;

            int newIdx = m_Deforms.Count;
            ShapeKeyMorphSO newShape = ShapeKeyMorphSO.NewAsNonBasis(m_Deforms[BASIS_MORPH_IDX]);
            SetMorphAt(newIdx, newShape);

            m_CmpWeights[newIdx] = FULL_WEIGHT;
            _SetAnimWeight(newIdx, FULL_WEIGHT);
        }

        public void RemoveShapeKeyMorphAt(int idx)
        {
            Dbg.Assert(idx < m_Deforms.Count, "MorphProc.RemoveShapeKeyMorphAt: idx beyond range: {0}, {1}", idx, m_Deforms.Count);

            if (m_CmpWeights[idx] > 0)
            {
                _ApplyWeightChange(idx, 0); //revert the deform's effect first
            }

            // shift the values forward
            for (int i = idx; i < m_Deforms.Count-1; ++i )
            {
                m_CmpWeights[i] = m_CmpWeights[i + 1];
                _SetAnimWeight(i, _GetAnimWeight(i + 1));
            }
            m_CmpWeights[m_Deforms.Count - 1] = 0;
            _SetAnimWeight(m_Deforms.Count - 1, 0);

            //remove the deform
            m_Deforms.RemoveAt(idx);

            _FixNormalAndBound();
        }

        /// <summary>
        /// revert all other deform's effects,
        /// apply only this specified deform weight as 100%
        /// </summary>
        public void ApplyOnlyMorphAt(int deformIdx, float weight = FULL_WEIGHT)
        {
            Dbg.Assert(deformIdx < m_Deforms.Count, "MorphProc.ApplyOnlyMorphAt: idx beyond range: {0}, {1}", deformIdx, m_Deforms.Count);

            for( int i=1; i<m_Deforms.Count; ++i) //ignore the basis, keep it at 100%
            {
                if(i != deformIdx)
                {
                    _ApplyWeightChange(i, 0);
                }
                else
                {
                    _ApplyWeightChange(i, weight);
                }
            }

            _FixNormalAndBound();
        }

        /// <summary>
        /// reset to basis deform
        /// </summary>
        public void ResetToBasisShape()
        {
            if (m_Deforms.Count == 0)
                return;

            m_CmpWeights[0] = 100f;
            _SetAnimWeight(0, 100f);

            for (int i = 1; i < m_Deforms.Count; ++i ) //to save performance, we dont call _ApplyWeightChange, directly set weights to 0
            {
                m_CmpWeights[i] = 0;
                _SetAnimWeight(i, 0);
            }

            _ForceSetMeshAsBasisWithoutChangeAllWeights();

            _MarkNeedFix();
            _FixNormalAndBound();
        }

        #endregion "public method"

	    #region "private method"

        /// <summary>
        /// for specified shape, 
        /// 1. apply mesh changes based on weight
        /// 2. ensure animWeight == cmpWeight
        /// </summary>
        private void _ApplyWeightChange(int shapeIdx, float newWeight)
        {
            float curWeight = m_CmpWeights[shapeIdx];
            _SetAnimWeight(shapeIdx, newWeight);
            if (Mathf.Approximately(curWeight, newWeight))
                return;

            Mesh m = m_Mesh.mesh;

            ShapeKeyMorphSO tarMorph = m_Deforms[shapeIdx];
            tarMorph.LerpDiff(_GetBasisShapeKey(), curWeight, newWeight, m_OutData);

            if( m_UseMeshCache )
            {
                m_OutData.ApplyToMeshAsSubtract(m, m_MeshCache);
            }
            else
            {
                m_OutData.ApplyToMeshAsSubtract(m);
            }

            m_CmpWeights[shapeIdx] = newWeight;

            _MarkNeedFix();
        }

        private void _MarkNeedFix()
        {
            m_FrameToFix = m_AutoFixFrameLimit;
            MeshModifyEvt.FireEvent(); //notify mesh modified, used by editors
        }

        private ShapeKeyDataDiff _GetBasisShapeKey()
        {
            Dbg.Assert( m_Deforms.Count > 0, "MorphProc._GetBasisShapeKey: no deforms defined");
            return m_Deforms[BASIS_MORPH_IDX].GetShapeKeyDataDiff(0);
        }

        private float _GetAnimWeight(int idx)
        {
            switch(idx)
            {
                case 0: return m_Weight0;
                case 1: return m_Weight1;
                case 2: return m_Weight2;
                case 3: return m_Weight3;
                case 4: return m_Weight4;
                case 5: return m_Weight5;
                case 6: return m_Weight6;
                case 7: return m_Weight7;
                case 8: return m_Weight8;
                case 9: return m_Weight9;
                default:
                    Dbg.LogErr("MorphProc._GetAnimWeight: unexpected weight index: {0}", idx);
                    return 0;
            }
        }

        private void _SetAnimWeight(int idx, float w)
        {
            switch (idx)
            {
                case 0: m_Weight0 = w; break;
                case 1: m_Weight1 = w; break;
                case 2: m_Weight2 = w; break;
                case 3: m_Weight3 = w; break;
                case 4: m_Weight4 = w; break;
                case 5: m_Weight5 = w; break;
                case 6: m_Weight6 = w; break;
                case 7: m_Weight7 = w; break;
                case 8: m_Weight8 = w; break;
                case 9: m_Weight9 = w; break;
                default:
                    Dbg.LogErr("MorphProc._SetAnimWeight: unexpected weight index: {0}", idx);
                    break;
            }
        }

        private void _FixNormalAndBound()
        {
            MeshUtil.FixNormalAndBound(m_Mesh.mesh);
        }

        private void _CheckForAutoFix()
        {
            if (m_FrameToFix > 0)
            {
                --m_FrameToFix;
                if (m_FrameToFix == 0)
                {
                    _FixNormalAndBound();
                }
            }
        }

        private void _ForceSetMeshAsBasisWithoutChangeAllWeights()
        {
            ShapeKeyDataDiff basisShapeKey = _GetBasisShapeKey();
            if (m_UseMeshCache)
                basisShapeKey.ApplyToMesh(m_Mesh.mesh, m_MeshCache);
            else
                basisShapeKey.ApplyToMesh(m_Mesh.mesh);
        }

        private Renderer _GetRenderer()
        {
            if( m_Renderer == null )
            {
                m_Renderer = GetComponent<Renderer>();
                Dbg.Assert(m_Renderer != null, "MorphProc._GetRenderer: there's no renderer on {0}", name);
            }
            return m_Renderer;
        }

        #endregion "private method"

	    #region "constant data"
        // constant data

        public const int BASIS_MORPH_IDX = 0;
        public const int MAX_SHAPEKEY_CNT = 10;

        public const float FULL_WEIGHT = 100f;

        #endregion "constant data"


    }
}
}
