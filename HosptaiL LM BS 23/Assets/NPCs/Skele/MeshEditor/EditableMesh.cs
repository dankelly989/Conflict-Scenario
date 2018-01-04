using System;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor;

namespace MH
{
    /// <summary>
    /// a container to shield the diff of MF & SMR
    /// </summary>
    [Serializable]
    public class EditableMesh
    {
        public bool m_independentMesh = false;
        public MeshFilter m_MF;
        public SkinnedMeshRenderer m_SMR;

        private bool m_MeshOK = false;
        private bool m_createdNewMeshRuntime = false; //runtime flag indicated if we have created the mesh when m_useSharedMesh is false
        private Transform m_Tr;

        //private EditableMesh(){}

        public static EditableMesh New(MeshFilter mf)
        {
            EditableMesh newOne = new EditableMesh();
            newOne.m_MF = mf;
            return newOne;
        }
        public static EditableMesh New(SkinnedMeshRenderer smr)
        {
            EditableMesh newOne = new EditableMesh();
            newOne.m_SMR = smr;
            return newOne;
        }
        public static EditableMesh New(GameObject go)
        {
            if (go == null)
                return null;

            MeshFilter[] mfs = go.GetComponents<MeshFilter>();
            if (mfs.Length > 0)
            {
                return New(mfs[0]);
            }

            SkinnedMeshRenderer[] smrs = go.GetComponents<SkinnedMeshRenderer>();
            if (smrs.Length > 0)
            {
                //foreach (var oneSmr in smrs)
                //{
                //    if (oneSmr.sharedMesh.blendShapeCount > 0)
                //    {
                //        return New(oneSmr);
                //    }
                //}

                return New(smrs[0]); //if no SMR has blendshape, take the first one
            }

            Dbg.LogErr("EditableMesh.New: ain't be here");
            return null;
        }

        public bool valid
        {
            get { return m_MF || m_SMR; }
        }

        public GameObject gameObject
        {
            get {
                if (m_MF != null) return m_MF.gameObject;
                else if (m_SMR != null) return m_SMR.gameObject;
                else
                {
                    Dbg.LogErr("EditableMesh.gameObject: empty EditableMesh");
                    return null;
                }
            }
        }

        public Renderer renderer
        {
            get {
                return gameObject.GetComponent<Renderer>();
            }
        }

        public Transform transform
        {
            get {
                if( m_Tr == null )
                {
                    if (m_MF != null) m_Tr = m_MF.transform;
                    else if (m_SMR != null) m_Tr = m_SMR.transform;
                }

                Dbg.Assert(m_Tr != null, "EditableMesh.transform.get: failed to get transform");
                return m_Tr;                
            }
        }

        public Mesh mesh
        {
            get {
                if (m_MF != null) return _GetMFMesh();
                else if (m_SMR != null) return _GetSMRMesh();
                else
                {
                    Dbg.LogErr("EditableMesh.mesh: no mesh to get");
                    return null;
                }
            }
        }

        private Mesh _GetSMRMesh()
        {
            if( !m_MeshOK )
            {
                Mesh m = m_SMR.sharedMesh;
                if( !m.name.EndsWith(MAGIC_POSTFIX) )
                {
                    Mesh newMesh = (Mesh)Mesh.Instantiate(m);
                    newMesh.name = m.name + MAGIC_POSTFIX;
                    m_SMR.sharedMesh = newMesh;
                }

                _CreateIndividualSkinnedMeshRuntimeIfNeeded();

                m_MeshOK = true;
            }

            _CreateIndividualSkinnedMeshRuntimeIfNeeded();

            return m_SMR.sharedMesh;
        }

        private void _CreateIndividualSkinnedMeshRuntimeIfNeeded()
        {
            if( Application.isPlaying && m_independentMesh && !m_createdNewMeshRuntime )
            {
                Mesh newRTMesh = (Mesh)Mesh.Instantiate(m_SMR.sharedMesh);
                m_SMR.sharedMesh = newRTMesh;
                m_createdNewMeshRuntime = true;
            }            
        }

        private Mesh _GetMFMesh()
        {
            if (!m_MeshOK)
            {
                Mesh m = m_MF.sharedMesh;
                if (!m.name.EndsWith(MAGIC_POSTFIX))
                {
                    Mesh newMesh = (Mesh)Mesh.Instantiate(m);
                    newMesh.name = m.name + MAGIC_POSTFIX;
                    m_MF.sharedMesh = newMesh;
                }

                _CreateIndividualMeshRuntimeIfNeeded();

                m_MeshOK = true;
            }

            _CreateIndividualMeshRuntimeIfNeeded();

            return m_MF.sharedMesh;
        }

        private void _CreateIndividualMeshRuntimeIfNeeded()
        {
            if (Application.isPlaying && m_independentMesh && !m_createdNewMeshRuntime)
            {
                var m = m_MF.mesh;
                m_createdNewMeshRuntime = (m!=null);
            }
        }

        public static bool HasAvailTarget(GameObject go)
        {
            if (go == null)
                return false;

            MeshFilter[] mfs = go.GetComponents<MeshFilter>();
            if (mfs.Length > 0)
            {
                return true;
            }

            SkinnedMeshRenderer[] smrs = go.GetComponents<SkinnedMeshRenderer>();
            if (smrs.Length > 0)
            {
                return true;
                //foreach (var oneSmr in smrs)
                //{
                //    if (oneSmr.sharedMesh.blendShapeCount > 0)
                //    {
                //        return true;
                //    }
                //}
            }

            return false;
        }


        private const string MAGIC_POSTFIX = "_SKELE_MM";
    }
}
