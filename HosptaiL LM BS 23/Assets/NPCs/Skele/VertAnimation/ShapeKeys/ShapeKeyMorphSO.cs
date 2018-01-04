using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{
namespace VertAnim
{
    /// <summary>
    /// used to store a bunch of ShapeKeys created by Skele MeshManipulator in Assets
    /// 
    /// 
    /// </summary>
    public class ShapeKeyMorphSO : ScriptableObject
    {
	    #region "data"
        // data

        [SerializeField]
        private Mesh m_Mesh; //the related mesh
        [SerializeField]
        private List<ShapeKeyDataDiff> m_Keys; //the shape keys
        [SerializeField]
        private ShapeKeyMorphSO m_BasisSO; //could be self if this is basis

        private ShapeKeyDataDiff m_tmpShape0; //tmp data
        private ShapeKeyDataDiff m_tmpShape1;

        private bool m_hasNormals;
        private bool m_hasTangents;

        #endregion "data"

        public static ShapeKeyMorphSO NewAsBasis(Mesh m)
        {
            ShapeKeyMorphSO deform = ScriptableObject.CreateInstance<ShapeKeyMorphSO>();
            deform.name = "Deform" + deform.GetInstanceID();
            deform.m_Mesh = m;
            deform.m_Keys = new List<ShapeKeyDataDiff>();

            deform.m_BasisSO = deform;

            var data = ShapeKeyDataDiff.New(deform, MorphProc.FULL_WEIGHT, true);
            deform.m_Keys.Add(data);

            deform._PrepareTmpVar();

            deform.m_hasNormals = data.normals.Count > 0;
            deform.m_hasTangents = data.tangents.Count > 0;

            return deform;
        }

        public static ShapeKeyMorphSO NewAsNonBasis(ShapeKeyMorphSO basis)
        {
            ShapeKeyMorphSO deform = ScriptableObject.CreateInstance<ShapeKeyMorphSO>();
            deform.name = "Deform" + deform.GetInstanceID();
            Mesh m = basis.m_Mesh;
            deform.m_Mesh = m;
            deform.m_Keys = new List<ShapeKeyDataDiff>();

            deform.m_BasisSO = basis;

            var data = ShapeKeyDataDiff.New(basis, MorphProc.FULL_WEIGHT, false);
            deform.m_Keys.Add(data);

            deform._PrepareTmpVar();

            deform.m_hasNormals = data.normals.Count > 0;
            deform.m_hasTangents = data.tangents.Count > 0; 

            return deform;
        }

        /// <summary>
        /// there're 3 circumstances:
        /// 1. just create the SO with Editor, no m_Mesh is set;
        /// 2. playmode, the mesh is serialized, so available
        /// 3. back from playmode, need to re-init again, m_Mesh is set already, available
        /// </summary>
        public void OnEnable()
        {
            _PrepareTmpVar(); //will use m_Mesh, which is not available when just created by Editor
        }

        public bool IsValid()
        {
            if (m_Mesh == null)
                return false;

            if (m_Keys.Count == 0)
                return false;

            return true;
        }

        public int ShapeKeyCnt
        {
            get { return m_Keys.Count; }
        }

        public bool HasNormals
        {
            get { return m_hasNormals; }
        }

        public bool HasTangents
        {
            get { return m_hasTangents; }
        }

        public Mesh GetMesh()
        {
            return m_Mesh;
        }

        /// <summary>
        /// return the shape key data
        /// </summary>
        public ShapeKeyDataDiff GetShapeKeyDataDiff(int idx)
        {
            Dbg.Assert(idx < m_Keys.Count, "ShapeKeyMorphSO.GetShapeKeyDataDiff: idx beyond range: idx: {0}, keycnt:{1}", idx, m_Keys.Count);
            return m_Keys[idx];
        }

        public void DelShapeKeyDataDiff(int idx)
        {
            Dbg.Assert(idx < m_Keys.Count, "ShapeKeyMorphSO.DelShapeKeyDataDiff: idx beyond range: {0}, {1}", idx, m_Keys.Count);
            m_Keys.RemoveAt(idx);
        }

        /// <summary>
        /// copy the mesh's current status into specified ShapeKeyData, weight not changed
        /// </summary>
        public void SetMeshCurrentDataToShapeKey(int idx)
        {
            Dbg.Assert(idx < m_Keys.Count, "ShapeKeyMorphSO.SetMeshCurrentDataToShapeKey: idx beyond range: {0}, {1}", idx, m_Keys.Count);

            m_Keys[idx].UpdateDiffDataWithCurrentMesh();
        }

        public void AddNewShapeKeyByMeshCurrentData(float weight = MorphProc.FULL_WEIGHT * 2f)
        {
            ShapeKeyDataDiff newData = ShapeKeyDataDiff.New(m_BasisSO, weight, false);
            m_Keys.Add(newData);
            _SortShapeKeys();
        }

        public void SetShapeKeyWeight(int keyIdx, float weight)
        {
            Dbg.Assert(keyIdx < m_Keys.Count, "ShapeKeyMorphSO.SetShapeKeyWeight: key idx beyond range: {0}, {1}", keyIdx, m_Keys.Count);
            m_Keys[keyIdx].weight = weight;
            _SortShapeKeys();
        }

        /// <summary>
        /// given a weight, calculate the diff with basisKeyData
        /// </summary>
        /// <param name="w">[0, MorphProc.FULL_WEIGHT]</param>
        public void GetWeightedData(ShapeKeyDataDiff basisKeyData, float w, ShapeKeyDataDiff outData)
        {
            if( w <= 0 )
            {
                outData.ResetEmptyDiff();
            }
            if( w >= MorphProc.FULL_WEIGHT )
            {
                outData.Copy(m_Keys[m_Keys.Count - 1]);
            }
            else
            {
                // find the enclosing two shapekeyData and lerp
                float leftWeight = 0;
                ShapeKeyDataDiff lhs = basisKeyData;
                for (int i = 0; i < m_Keys.Count; ++i)
                {
                    ShapeKeyDataDiff rhs = m_Keys[i];
                    if (w <= rhs.weight)
                    {
                        ShapeKeyDataDiff.Lerp(lhs, rhs, leftWeight, w, outData);
                        break;
                    }

                    lhs = rhs;
                    leftWeight = lhs.weight;
                }
            }
        }

        /// <summary>
        /// outData = shape(wEnd) - shape(wStart)
        /// </summary>
        public void LerpDiff(ShapeKeyDataDiff basis, float wStart, float wEnd, ShapeKeyDataDiff outData)
        {
            GetWeightedData(basis, wStart, m_tmpShape0);
            GetWeightedData(basis, wEnd, m_tmpShape1);
            ShapeKeyDataDiff.Subtract(m_tmpShape1, m_tmpShape0, outData);
        }	    


	    #region "private method"
	    // "private method" 

        private void _SortShapeKeys()
        {
            m_Keys.Sort((lhs, rhs) => { return Math.Sign(lhs.weight - rhs.weight); });
            for (int i = 1; i < m_Keys.Count; ++i)
            {
                ShapeKeyDataDiff lhs = m_Keys[i - 1];
                ShapeKeyDataDiff rhs = m_Keys[i];

                if (lhs.weight > rhs.weight || // e.g.: <50, 50, 50>, and [1] will be 50.1 and bigger than [2]
                    Mathf.Approximately(lhs.weight, rhs.weight))
                {
                    rhs.weight = lhs.weight + 0.1f;
                }
            }
        }

        private void _PrepareTmpVar()
        {
            if (m_Mesh != null)
            {
                Dbg.Assert(m_BasisSO != null, "ShapeKeyMorphSO._PrepareTmpVar: m_BasisSO is null");
                m_tmpShape0 = ShapeKeyDataDiff.TempVarNew(m_BasisSO);
                m_tmpShape1 = ShapeKeyDataDiff.TempVarNew(m_BasisSO);
            }
        }
	
	    #endregion "private method"

        

    }

    
}
}
