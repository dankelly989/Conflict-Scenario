using System;
using System.Collections.Generic;

using UnityEngine;

using System.Xml.Serialization;
using System.IO;
using System.Linq;

using grendgine_collada;
using System.Text;
using UnityEditor;
using System.Xml;

#pragma warning disable 618

namespace MH
{

    /// <summary>
    /// Animation & Mesh exporter
    /// </summary>
    public class DaeExporter
    {
        private HashSet<Transform> m_NeedExportHierarchyNodes = new HashSet<Transform>();

        // for MF
        private MeshFilter[] m_MFs;

        // for SMR
        private SkinnedMeshRenderer[] m_SMRs;
        private List<WBoneWeights> m_BWLst;
        private int m_BWcnt = 0;
        private List<Transform> m_ExtendedJoints = new List<Transform>();
        private List<AnimationClip> m_Clips = new List<AnimationClip>();
        private Transform m_RootBone = null; //the root bone user specified
        private Transform m_AnimRoot = null; //the GO with Animation/Animator

        //private AnimationNodeData m_RootMotionNodeData = null; //used to 
        //private bool m_RootBoneAlreadyOccupied = false; //if true, means the rootmotion target bone already has curves

        // debug usage
        private bool m_Debug = false;
        private StreamWriter m_DbgSW;

        /// <summary>
        /// the rootBone must be the topmost object
        /// </summary>
        public DaeExporter(SkinnedMeshRenderer[] smrs, MeshFilter[] mfs, Transform rootGO)
        {
            m_SMRs = smrs;
            m_MFs = mfs;
            Dbg.Assert(m_SMRs.Length > 0 || m_MFs.Length > 0, "DaeExporter.ctor: no renderer specified");

            m_ExtendedJoints.Clear();

            if (m_SMRs.Length > 0)
            {
                m_RootBone = rootGO;

                m_AnimRoot = m_RootBone.transform; //look for the Animator/Animation component from here
                while (m_AnimRoot != null)
                {
                    if (m_AnimRoot.GetComponent<Animation>() != null || m_AnimRoot.GetComponent<Animator>() != null)
                        break;
                    m_AnimRoot = m_AnimRoot.parent;
                }
                Dbg.Assert(m_AnimRoot != null, "DaeExporter.ctor: failed to find a GO with Animation/Animator");
                _InitExtendedJoints();
            }
        }

        public void Export(AnimationClip clip, string outPath = "export.dae")
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            clips.Add(clip);
            Export(clips, outPath);
        }

        public void Export(List<AnimationClip> clips, string outPath = "export.dae")
        {
            //Matrix4x4 A = Matrix4x4.TRS(Vector3.right, Quaternion.Euler(0, 90, 0), Vector3.one);
            //Matrix4x4 B = Matrix4x4.TRS(Vector3.forward, Quaternion.Euler(0, 90, 0), Vector3.one);
            //Matrix4x4 AB = A * B;
            //Matrix4x4 BRAR = _ReflectXMatrix(B) * _ReflectXMatrix(A);
            //Matrix4x4 AB_R = _ReflectXMatrix(A * B);
            //Matrix4x4 ARBR = _ReflectXMatrix(A) * _ReflectXMatrix(B);

            //Dbg.Log("A: {0}\nB:{1}\nAB: {2}\nBRAR:{3}\nAB_R:{4}\nARBR: {5}", A, B, AB, BRAR, AB_R, ARBR);
            if (m_Debug)
            {
                m_DbgSW = new StreamWriter("DaeExporterDbg.log");
            }

            m_Clips.Clear();
            if (clips != null)
            {
                m_Clips.AddRange(clips);
            }

            //m_RootBoneAlreadyOccupied = false;
            //m_RootMotionNodeData = null;

            Grendgine_Collada col_scene = _MakeColScene();

            // library_geometry
            _Export_Lib_Geometry(col_scene);

            // library_controllers
            _Export_Lib_Controllers(col_scene);

            // export clips
            if( m_Clips.Count > 0 )
            {
                //if (m_Clips.Count == 1)
                //    _Export_Lib_Animation_Clips(col_scene, outPath);
                //else
                //    _Export_Clips_In_Separate_Archives(outPath);

                _Export_Clips_In_Separate_Archives(outPath);
            }

            // library_visual_scenes
            _Export_Lib_VisualScene(col_scene);

            Grendgine_Collada.Grendgine_Save_File(outPath, col_scene);

            Dbg.Log("Exported Archive: {0}", outPath);

            if (m_Debug)
            {
                m_DbgSW.Close();
                m_DbgSW = null;
            }
        }

        private Grendgine_Collada _MakeColScene()
        {
            var col_scene = new Grendgine_Collada();

            // asset (required for max)
            var asset = col_scene.Asset = new Grendgine_Collada_Asset();
            asset.Created = DateTime.Now;
            asset.Modified = DateTime.Now;
            asset.Revision = "1.0";
            asset.Up_Axis = "Y_UP";
            asset.Contributor = _ContributorInfo();
            var unit = asset.Unit = new Grendgine_Collada_Asset_Unit();
            unit.Name = "meter"; unit.Meter = 1;

            // version (required for blender)
            col_scene.Collada_Version = "1.4.1";
            return col_scene;
        }

        private Grendgine_Collada_Asset_Contributor[] _ContributorInfo()
        {
            var contributors = new Grendgine_Collada_Asset_Contributor[1];
            var d = contributors[0] = new Grendgine_Collada_Asset_Contributor();
            d.Authoring_Tool = "Skele: Character Animation Tools";
            d.Comments = "https://www.assetstore.unity3d.com/en/#!/content/16899";
            return contributors;
        }

        private void _Export_Lib_Animation_Clips(Grendgine_Collada col_scene, string outPath)
        {
            if (m_Clips.Count == 0)
                return;

            var lib_animations = col_scene.Library_Animations = new Grendgine_Collada_Library_Animations();
            lib_animations.Animation = new List<Grendgine_Collada_Animation>();
            var lib_clips = col_scene.Library_Animation_Clips = new Grendgine_Collada_Library_Animation_Clips();
            lib_clips.Animation_Clip = new List<Grendgine_Collada_Animation_Clip>();

            float clipStartTime = 0;

            for (int clipIdx = 0; clipIdx < m_Clips.Count; ++clipIdx)
            {
                AnimationClip clip = m_Clips[clipIdx];
                _ExportClip(clip, clipIdx, ref clipStartTime, lib_animations, lib_clips);
            }
        }

        private void _Export_Clips_In_Separate_Archives(string outPath)
        {
            for( int i=0; i<m_Clips.Count; ++i)
            {
                var clip = m_Clips[i];
                string newOutPath = PathUtil.AppendFileNameWithoutChangeExtension(outPath, "@" + clip.name);
                var newDoc = _MakeColScene();

                var lib_animations = newDoc.Library_Animations = new Grendgine_Collada_Library_Animations();
                lib_animations.Animation = new List<Grendgine_Collada_Animation>();
                var lib_clips = newDoc.Library_Animation_Clips = new Grendgine_Collada_Library_Animation_Clips();
                lib_clips.Animation_Clip = new List<Grendgine_Collada_Animation_Clip>();

                float clipStartTime = 0;

                //_Export_Lib_Controllers(newDoc); // not necessary
                _ExportClip(clip, 0, ref clipStartTime, lib_animations, lib_clips);
                _Export_Lib_VisualScene(newDoc, clip);

                Grendgine_Collada.Grendgine_Save_File(newOutPath, newDoc);

                Dbg.Log("Export separate clip archive: {0}", newOutPath);
            }
        }

        private void _ExportClip(AnimationClip clip, int clipIdx, ref float clipStartTime,
            Grendgine_Collada_Library_Animations lib_animations, Grendgine_Collada_Library_Animation_Clips lib_clips)
        {
            var anims = lib_animations.Animation;
            float deltaTime = 1f / clip.frameRate;
            float clipLength = clip.length;
            int frameCnt = 0;
            string timeString = _TimeToStringArray(clipLength, deltaTime, clipStartTime, out frameCnt);

            //int frameCnt = Mathf.CeilToInt(totalTime * m_Clip.frameRate) + 1; // due to float precision, will be one frame more in some cases

            var curves = AnimationUtility.GetAllCurves(clip, true);
            //var bindings = AnimationUtility.GetCurveBindings(m_Clip);
            List<AnimationNodeData> animNodesDatum = _GatherAnimationNodeDataFromCurves(curves);

            ///////////////
            // clip info export
            //////////////
            var topAnimNode = new Grendgine_Collada_Animation();
            topAnimNode.Name = clip.name;
            topAnimNode.ID = topAnimNode.Name + "_" + clipIdx;
            anims.Add(topAnimNode);
            var subAnims = topAnimNode.Animation = new List<Grendgine_Collada_Animation>();

            var clipNode = new Grendgine_Collada_Animation_Clip();
            clipNode.Name = clip.name;
            clipNode.Start = clipStartTime;
            clipNode.End = clipStartTime + clipLength;
            lib_clips.Animation_Clip.Add(clipNode);
            var instance_animations = clipNode.Instance_Animation = new List<Grendgine_Collada_Instance_Animation>();
            clipStartTime += clipLength; //update the clipStartTime

            var oneAnimInstance = new Grendgine_Collada_Instance_Animation();
            oneAnimInstance.URL = topAnimNode.ID;
            instance_animations.Add(oneAnimInstance);
            
            ///////////////////////
            // export animation curves data
            ///////////////////////
            int idx = 0;
            foreach (var oneNodeData in animNodesDatum)
            {
                //Dbg.Log("path: {0}, prop: {1}", oneNodeData.path,
                //    string.Join("|", oneNodeData.curves.Select(x => x.propertyName).ToArray()) );

                var animNode = new Grendgine_Collada_Animation();
                subAnims.Add(animNode);

                string rawName = _ExtractNameFromPath(oneNodeData.path);
                animNode.Name = rawName + "_" + clipIdx + "_" + (idx++); //animation node name
                animNode.ID = animNode.Name + "-anim";
                animNode.Source = new Grendgine_Collada_Source[3];

                ////////////////////
                // Animation curve data
                var timeSource = animNode.Source[0] = new Grendgine_Collada_Source();
                { // time-source
                    timeSource.ID = animNode.Name + "-Matrix-animation-input";
                    var timeSourceArr = timeSource.Float_Array = new Grendgine_Collada_Float_Array();
                    timeSourceArr.ID = timeSource.ID + "-array";
                    timeSourceArr.Count = frameCnt;
                    timeSourceArr.Value_As_String = timeString;

                    var tech = timeSource.Technique_Common = new Grendgine_Collada_Technique_Common_Source();
                    var techAcc = tech.Accessor = new Grendgine_Collada_Accessor();
                    techAcc.Source = "#" + timeSourceArr.ID;
                    techAcc.Count = (uint)frameCnt;
                    techAcc.Param = new Grendgine_Collada_Param[1];
                    var techAccParam = techAcc.Param[0] = new Grendgine_Collada_Param();
                    techAccParam.Name = "TIME";
                    techAccParam.Type = "float";
                }

                var matSource = animNode.Source[1] = new Grendgine_Collada_Source();
                { // output-source
                    matSource.ID = animNode.Name + "-Matrix-animation-output-transform";
                    var matSourceArr = matSource.Float_Array = new Grendgine_Collada_Float_Array();
                    matSourceArr.ID = matSource.ID + "-array";
                    matSourceArr.Count = frameCnt * 16;
                    matSourceArr.Value_As_String = _AnimNodeToStringArray(oneNodeData, clipLength, deltaTime);

                    var tech = matSource.Technique_Common = new Grendgine_Collada_Technique_Common_Source();
                    var techAcc = tech.Accessor = new Grendgine_Collada_Accessor();
                    techAcc.Source = "#" + matSourceArr.ID;
                    techAcc.Count = (uint)frameCnt;
                    techAcc.Stride = 16;
                    techAcc.Param = new Grendgine_Collada_Param[1];
                    var techAccParam = techAcc.Param[0] = new Grendgine_Collada_Param();
                    techAccParam.Type = "float4x4";
                }

                var intSource = animNode.Source[2] = new Grendgine_Collada_Source();
                { // interpolation-source
                    intSource.ID = animNode.Name + "-Interpolations";
                    var intSourceArr = intSource.Name_Array = new Grendgine_Collada_Name_Array();
                    intSourceArr.ID = intSource.ID + "-array";
                    intSourceArr.Count = frameCnt;
                    intSourceArr.Value_Pre_Parse = String.Join("\n", Enumerable.Repeat("LINEAR", frameCnt).ToArray());

                    var tech = intSource.Technique_Common = new Grendgine_Collada_Technique_Common_Source();
                    var techAcc = tech.Accessor = new Grendgine_Collada_Accessor();
                    techAcc.Source = "#" + intSourceArr.ID;
                    techAcc.Count = (uint)frameCnt;
                    techAcc.Param = new Grendgine_Collada_Param[1];
                    var techAccParam = techAcc.Param[0] = new Grendgine_Collada_Param();
                    techAccParam.Type = "name";
                }

                Grendgine_Collada_Sampler sampler = null;
                { //sampler
                    animNode.Sampler = new Grendgine_Collada_Sampler[1];
                    sampler = animNode.Sampler[0] = new Grendgine_Collada_Sampler();
                    sampler.ID = animNode.Name + "-Matrix-animation-transform";

                    var inputs = sampler.Input = new Grendgine_Collada_Input_Unshared[3];
                    var input = inputs[0] = new Grendgine_Collada_Input_Unshared();
                    input.Semantic = Grendgine_Collada_Input_Semantic.INPUT;
                    input.source = "#" + timeSource.ID;
                    var output = inputs[1] = new Grendgine_Collada_Input_Unshared();
                    output.Semantic = Grendgine_Collada_Input_Semantic.OUTPUT;
                    output.source = "#" + matSource.ID;
                    var inter = inputs[2] = new Grendgine_Collada_Input_Unshared();
                    inter.Semantic = Grendgine_Collada_Input_Semantic.INTERPOLATION;
                    inter.source = "#" + intSource.ID;
                }

                { //channel
                    animNode.Channel = new Grendgine_Collada_Channel[1];
                    var channel = animNode.Channel[0] = new Grendgine_Collada_Channel();
                    channel.Source = "#" + sampler.ID;
                    channel.Target = rawName + "/" + MATRIX_ID;
                }
            }
        }

        private void _Export_Lib_Controllers(grendgine_collada.Grendgine_Collada col_scene)
        {
            var lib_ctrl = col_scene.Library_Controllers = new Grendgine_Collada_Library_Controllers();
            lib_ctrl.Controller = new Grendgine_Collada_Controller[m_SMRs.Length];

            for (int idx = 0; idx < m_SMRs.Length; ++idx)
            {
                SkinnedMeshRenderer smr = m_SMRs[idx];
                Transform smrtr = smr.transform;

                var ctrler = lib_ctrl.Controller[idx] = new Grendgine_Collada_Controller();
                ctrler.ID = CONTROLLER_ID + idx;
                var skin = ctrler.Skin = new Grendgine_Collada_Skin();
                skin.sID = "#" + _GenGeometryID(smrtr, idx); //this must conform to Geometry's id

                skin.Source = new Grendgine_Collada_Source[3];

                string src_Joints_ID = "Joints" + "_" + idx;
                string src_Weights_ID = "Weights" + "_" + idx;
                string src_InvBind_ID = "Inv_Bind_Mats" + "_" + idx;

                // bone weight data struct
                m_BWLst = new List<WBoneWeights>();
                BoneWeight[] bws = smr.sharedMesh.boneWeights;
                for (int bidx = 0; bidx < bws.Length; ++bidx)
                {
                    WBoneWeights wbw = new WBoneWeights(bws[bidx]);
                    m_BWLst.Add(wbw);
                    m_BWcnt += wbw.m_validWeights;
                }

                //bind_shape_matrix
                {
                    var bsmat = skin.Bind_Shape_Matrix = new Grendgine_Collada_Float_Array_String();
                    //Matrix4x4 m = Matrix4x4.TRS(smrtr.localPosition, smrtr.localRotation, smrtr.localScale);
                    bsmat.Value_As_String = _Matrix2String(Matrix4x4.identity);
                }

                //source_joints
                {
                    // joints name array
                    var joints = skin.Source[0] = new Grendgine_Collada_Source();
                    joints.ID = src_Joints_ID;
                    var joints_name_array = joints.Name_Array = new Grendgine_Collada_Name_Array();
                    joints_name_array.ID = "Joints_name_array" + "_" + idx;
                    joints_name_array.Count = smr.bones.Length;
                    joints_name_array.Value_Pre_Parse = _SMR2NameArray(smr);

                    // tech_common
                    var tech_common = joints.Technique_Common = new Grendgine_Collada_Technique_Common_Source();
                    var accessor = tech_common.Accessor = new Grendgine_Collada_Accessor();
                    accessor.Source = "#" + joints_name_array.ID;
                    accessor.Count = (uint)joints_name_array.Count;
                    accessor.Stride = 1;
                    accessor.Param = new Grendgine_Collada_Param[1];
                    var param = accessor.Param[0] = new Grendgine_Collada_Param(); param.Type = "name";
                }

                //source_weights
                {
                    // weight float array
                    var weights = skin.Source[1] = new Grendgine_Collada_Source();
                    weights.ID = src_Weights_ID;
                    var float_array = weights.Float_Array = new Grendgine_Collada_Float_Array();
                    float_array.ID = "Weights_float_array" + "_" + idx;
                    float_array.Count = m_BWcnt;
                    float_array.Value_As_String = _SMRWeights2FloatArray();

                    // tech_common
                    var tech_common = weights.Technique_Common = new Grendgine_Collada_Technique_Common_Source();
                    var accessor = tech_common.Accessor = new Grendgine_Collada_Accessor();
                    accessor.Source = "#" + float_array.ID;
                    accessor.Count = (uint)float_array.Count;
                    accessor.Stride = 1;
                    accessor.Param = new Grendgine_Collada_Param[1];
                    var param = accessor.Param[0] = new Grendgine_Collada_Param(); param.Type = "float";
                }

                // source_inv_bind_mat
                {
                    // inv-bind-mat array
                    var invbinds = skin.Source[2] = new Grendgine_Collada_Source();
                    invbinds.ID = src_InvBind_ID;
                    var float_array = invbinds.Float_Array = new Grendgine_Collada_Float_Array();
                    float_array.ID = "Inv_Bind_Mats_Float_Array" + "_" + idx;
                    float_array.Count = smr.bones.Length * 16;
                    float_array.Value_As_String = _SMRInvBind2Array(smr);

                    // tech_common
                    var tech_common = invbinds.Technique_Common = new Grendgine_Collada_Technique_Common_Source();
                    var accessor = tech_common.Accessor = new Grendgine_Collada_Accessor();
                    accessor.Source = "#" + float_array.ID;
                    accessor.Count = (uint)smr.bones.Length;
                    accessor.Stride = 16;
                    accessor.Param = new Grendgine_Collada_Param[1];
                    var param = accessor.Param[0] = new Grendgine_Collada_Param(); param.Type = "float4x4";
                }

                // Joints
                {
                    var js = skin.Joints = new Grendgine_Collada_Joints();
                    js.Input = new Grendgine_Collada_Input_Unshared[2];
                    var js_input_JOINT = js.Input[0] = new Grendgine_Collada_Input_Unshared();
                    js_input_JOINT.Semantic = Grendgine_Collada_Input_Semantic.JOINT;
                    js_input_JOINT.source = "#" + src_Joints_ID;
                    var js_input_INVBIND = js.Input[1] = new Grendgine_Collada_Input_Unshared();
                    js_input_INVBIND.Semantic = Grendgine_Collada_Input_Semantic.INV_BIND_MATRIX;
                    js_input_INVBIND.source = "#" + src_InvBind_ID;
                }

                //vertex_weights
                {
                    var vw = skin.Vertex_Weights = new Grendgine_Collada_Vertex_Weights();
                    vw.Input = new Grendgine_Collada_Input_Shared[2];
                    vw.Count = smr.sharedMesh.vertexCount;
                    var vw_input_JOINT = vw.Input[0] = new Grendgine_Collada_Input_Shared();
                    vw_input_JOINT.Semantic = Grendgine_Collada_Input_Semantic.JOINT;
                    vw_input_JOINT.source = "#" + src_Joints_ID;
                    vw_input_JOINT.Offset = 0;
                    var vw_input_WEIGHT = vw.Input[1] = new Grendgine_Collada_Input_Shared();
                    vw_input_WEIGHT.Semantic = Grendgine_Collada_Input_Semantic.WEIGHT;
                    vw_input_WEIGHT.source = "#" + src_Weights_ID;
                    vw_input_WEIGHT.Offset = 1;

                    var vcount = vw.VCount = new Grendgine_Collada_Int_Array_String();
                    vcount.Value_As_String = _SMR2VCountIntArray();

                    var v = vw.V = new Grendgine_Collada_Int_Array_String();
                    v.Value_As_String = _SMR2V_IntArray(smr);
                }
            } //end of for(int idx...

        }

        private void _Export_Lib_Geometry(Grendgine_Collada col_scene)
        {
            // establish MeshList
            List<MeshData> meshLst = new List<MeshData>();
            foreach (var smr in m_SMRs)
            {
                MeshData d = new MeshData();
                d.mesh = smr.sharedMesh;
                d.trans = smr.transform;
                meshLst.Add(d);
            }
            foreach (var mf in m_MFs)
            {
                MeshData d = new MeshData();
                d.mesh = mf.sharedMesh;
                d.trans = mf.transform;
                meshLst.Add(d);
            }

            // create lib_geo node
            var lib_geo = col_scene.Library_Geometries = new Grendgine_Collada_Library_Geometries();
            var geos = lib_geo.Geometry = Misc.CreateList<Grendgine_Collada_Geometry>(meshLst.Count);

            // foreach geo
            for (int mIdx = 0; mIdx < meshLst.Count; ++mIdx)
            {
                Mesh m = meshLst[mIdx].mesh;
                var geo = geos[mIdx] = new Grendgine_Collada_Geometry();
                geo.ID = _GenGeometryID(meshLst[mIdx].trans, mIdx);
                geo.Name = m.name; //need check? is this name same with the GO's name //YES, it is
                //Dbg.Log("GeoName{0}: {1}", mIdx, geo.Name);

                var onemesh = geo.Mesh = new Grendgine_Collada_Mesh();

                onemesh.Source = new Grendgine_Collada_Source[3];
                var vertsrc = onemesh.Source[0] = new Grendgine_Collada_Source();
                vertsrc.ID = "src" + mIdx;
                var texsrc = onemesh.Source[1] = new Grendgine_Collada_Source();
                texsrc.ID = "texsrc" + mIdx;
                var nomsrc = onemesh.Source[2] = new Grendgine_Collada_Source();
                nomsrc.ID = "normalSrc" + mIdx;

                // vert source float Array
                var vertSrcArray = vertsrc.Float_Array = new Grendgine_Collada_Float_Array();
                vertSrcArray.ID = vertsrc.ID + "arr";
                vertSrcArray.Count = m.vertexCount * 3;
                vertSrcArray.Value_As_String = _Vertices2String(m.vertices);

                // vert source technique common
                var vertSrcTech = vertsrc.Technique_Common = new Grendgine_Collada_Technique_Common_Source();
                var vertSrcTechAcc = vertSrcTech.Accessor = new Grendgine_Collada_Accessor();
                vertSrcTechAcc.Source = "#" + vertSrcArray.ID;
                vertSrcTechAcc.Count = (uint)m.vertexCount;
                vertSrcTechAcc.Stride = 3;
                vertSrcTechAcc.Param = new Grendgine_Collada_Param[3];
                {
                    var paramX = vertSrcTechAcc.Param[0] = new Grendgine_Collada_Param(); paramX.Name = "X"; paramX.Type = "float";
                    var paramY = vertSrcTechAcc.Param[1] = new Grendgine_Collada_Param(); paramY.Name = "Y"; paramY.Type = "float";
                    var paramZ = vertSrcTechAcc.Param[2] = new Grendgine_Collada_Param(); paramZ.Name = "Z"; paramZ.Type = "float";
                }

                // texcoord source float array
                var texSrcArray = texsrc.Float_Array = new Grendgine_Collada_Float_Array();
                texSrcArray.ID = texsrc.ID + "arr";
                texSrcArray.Count = m.vertexCount * 2;
                texSrcArray.Value_As_String = _Texcoord2String(m.uv);

                // texcoord source technique common
                var texSrcTech = texsrc.Technique_Common = new Grendgine_Collada_Technique_Common_Source();
                var texSrcTechAcc = texSrcTech.Accessor = new Grendgine_Collada_Accessor();
                texSrcTechAcc.Source = "#" + texSrcArray.ID;
                texSrcTechAcc.Count = (uint)m.vertexCount;
                texSrcTechAcc.Stride = 2;
                texSrcTechAcc.Param = new Grendgine_Collada_Param[2];
                {
                    var paramX = texSrcTechAcc.Param[0] = new Grendgine_Collada_Param(); paramX.Name = "S"; paramX.Type = "float";
                    var paramY = texSrcTechAcc.Param[1] = new Grendgine_Collada_Param(); paramY.Name = "T"; paramY.Type = "float";
                }

                // normal source float array
                var nomSrcArray = nomsrc.Float_Array = new Grendgine_Collada_Float_Array();
                nomSrcArray.ID = nomsrc.ID + "arr";
                nomSrcArray.Count = m.vertexCount * 3;
                nomSrcArray.Value_As_String = _Normal2String(m.normals);

                // normal source technique common
                var nomSrcTech = nomsrc.Technique_Common = new Grendgine_Collada_Technique_Common_Source();
                var nomSrcTechAcc = nomSrcTech.Accessor = new Grendgine_Collada_Accessor();
                nomSrcTechAcc.Source = "#" + nomSrcArray.ID;
                nomSrcTechAcc.Count = (uint)m.vertexCount;
                nomSrcTechAcc.Stride = 3;
                nomSrcTechAcc.Param = new Grendgine_Collada_Param[3];
                {
                    var paramX = nomSrcTechAcc.Param[0] = new Grendgine_Collada_Param(); paramX.Name = "X"; paramX.Type = "float";
                    var paramY = nomSrcTechAcc.Param[1] = new Grendgine_Collada_Param(); paramY.Name = "Y"; paramY.Type = "float";
                    var paramZ = nomSrcTechAcc.Param[2] = new Grendgine_Collada_Param(); paramZ.Name = "Z"; paramZ.Type = "float";
                }

                // verts
                var vertices = onemesh.Vertices = new Grendgine_Collada_Vertices();
                vertices.ID = "meshVtx" + mIdx;
                vertices.Input = new Grendgine_Collada_Input_Unshared[1];
                var oneInput = vertices.Input[0] = new Grendgine_Collada_Input_Unshared();
                oneInput.Semantic = Grendgine_Collada_Input_Semantic.POSITION;
                oneInput.source = "#" + vertsrc.ID;

                // triangles
                onemesh.Triangles = new Grendgine_Collada_Triangles[1];
                var triangles = onemesh.Triangles[0] = new Grendgine_Collada_Triangles();
                triangles.Count = m.triangles.Length / 3;
                triangles.Input = new Grendgine_Collada_Input_Shared[3];
                triangles.P = new Grendgine_Collada_Int_Array_String();
                triangles.P.Value_As_String = _Triangles2String(m.triangles);

                var vertInputTri = triangles.Input[0] = new Grendgine_Collada_Input_Shared();
                vertInputTri.Semantic = Grendgine_Collada_Input_Semantic.VERTEX;
                vertInputTri.source = "#" + vertices.ID;
                vertInputTri.Offset = 0;

                var uvInputTri = triangles.Input[1] = new Grendgine_Collada_Input_Shared();
                uvInputTri.Semantic = Grendgine_Collada_Input_Semantic.TEXCOORD;
                uvInputTri.source = "#" + texsrc.ID;
                uvInputTri.Offset = 1;

                var nomInputTri = triangles.Input[2] = new Grendgine_Collada_Input_Shared();
                nomInputTri.Semantic = Grendgine_Collada_Input_Semantic.NORMAL;
                nomInputTri.source = "#" + nomsrc.ID;
                nomInputTri.Offset = 2;
            }
        }

        private void _Export_Lib_VisualScene(Grendgine_Collada col_scene, AnimationClip clip = null)
        {
            _PrepareToExportAsHierarchyNodeList();

            // scene
            var scene = col_scene.Scene = new Grendgine_Collada_Scene();
            scene.Visual_Scene = new Grendgine_Collada_Instance_Visual_Scene();
            scene.Visual_Scene.URL = "#DefaultScene";

            // library_VisualScenes
            var lib_vs = col_scene.Library_Visual_Scene = new Grendgine_Collada_Library_Visual_Scenes();
            var vss = lib_vs.Visual_Scene = new Grendgine_Collada_Visual_Scene[1];
            var oneVisualScene = vss[0] = new Grendgine_Collada_Visual_Scene();
            oneVisualScene.ID = "DefaultScene";

            // set frame rate( Max/Unity specific )
            if( clip != null )
            {
                var extras = oneVisualScene.Extra = new Grendgine_Collada_Extra[1];
                var oneExtra = extras[0] = new Grendgine_Collada_Extra();
                var techs = oneExtra.Technique = new Grendgine_Collada_Technique[1];
                var oneTech = techs[0] = new Grendgine_Collada_Technique();
                oneTech.profile = "MAX3D";

                XmlDocument tmpDoc = new XmlDocument();
                XmlElement elemFps = tmpDoc.CreateElement("frame_rate");
                var oneTechDatas = oneTech.Data = new XmlElement[1];
                var oneTechData = oneTechDatas[0] = elemFps;
                oneTechData.InnerText = string.Format("{0:F2}", clip.frameRate);
            }            

            // nodes
            oneVisualScene.Node = new List<Grendgine_Collada_Node>();

            // export the hierarchy
            if (m_AnimRoot != null)
            {
                foreach (Transform childTr in m_AnimRoot)
                {
                    if (m_NeedExportHierarchyNodes.Contains(childTr))
                    {
                        Grendgine_Collada_Node childNode = new Grendgine_Collada_Node();
                        oneVisualScene.Node.Add(childNode);
                        _ExportHierarchyNodes(childNode, childTr);
                    }
                }
            }

            // special case: only MFs and no root specified,
            // just export the MFs one by one
            if (m_MFs.Length > 0 && m_AnimRoot == null)
            {
                for (int idx = 0; idx < m_MFs.Length; ++idx)
                {
                    MeshFilter mf = m_MFs[idx];
                    if (!m_NeedExportHierarchyNodes.Contains(mf.transform))
                    { //if export this node at the rootNode, not in joints hierarchy
                        int globalIdx = m_SMRs.Length + idx;

                        var geoNode = new Grendgine_Collada_Node();
                        oneVisualScene.Node.Add(geoNode);
                        geoNode.ID = SCENE_NODE_ID + globalIdx;
                        geoNode.Name = mf.name;
                        geoNode.Type = Grendgine_Collada_Node_Type.NODE;

                        geoNode.Instance_Geometry = new Grendgine_Collada_Instance_Geometry[1];
                        var inst_geo = geoNode.Instance_Geometry[0] = new Grendgine_Collada_Instance_Geometry();
                        inst_geo.URL = "#" + _GenGeometryID(mf.transform, globalIdx); //must be same with corresponding Geometry's name

                    }
                }
            }

        }

        private void _ExportHierarchyNodes(Grendgine_Collada_Node curNode, Transform nodeTr)
        {

            curNode.ID = _FixName(nodeTr.name);
            curNode.Name = nodeTr.name;
            curNode.sID = _FixName(nodeTr.name);

            bool bIsJoint = m_ExtendedJoints.Contains(nodeTr);
            curNode.Type = bIsJoint ? Grendgine_Collada_Node_Type.JOINT : Grendgine_Collada_Node_Type.NODE;

            // add mf & smr ref
            MeshFilter mf = nodeTr.GetComponent<MeshFilter>();
            SkinnedMeshRenderer smr = nodeTr.GetComponent<SkinnedMeshRenderer>();
            if (mf != null)
            {
                int idx = ArrayUtility.IndexOf(m_MFs, mf);
                if (idx > -1)
                {
                    int globalIdx = m_SMRs.Length + idx;
                    curNode.ID = SCENE_NODE_ID + globalIdx;
                    curNode.Instance_Geometry = new Grendgine_Collada_Instance_Geometry[1];
                    var inst_geo = curNode.Instance_Geometry[0] = new Grendgine_Collada_Instance_Geometry();
                    inst_geo.URL = "#" + _GenGeometryID(mf.transform, globalIdx); //must be same with corresponding Geometry's name
                }
            }
            if (smr != null)
            {
                int idx = ArrayUtility.IndexOf(m_SMRs, smr);
                if (idx > -1)
                {
                    int globalIdx = idx;
                    curNode.ID = SCENE_NODE_ID + globalIdx;
                    curNode.Instance_Controller = new Grendgine_Collada_Instance_Controller[1];
                    var inst_ctrl = curNode.Instance_Controller[0] = new Grendgine_Collada_Instance_Controller();
                    inst_ctrl.URL = "#" + CONTROLLER_ID + globalIdx;
                }
            }

            // export matrix
            curNode.Matrix = new Grendgine_Collada_Matrix[1];
            var m = curNode.Matrix[0] = new Grendgine_Collada_Matrix(); m.sID = MATRIX_ID;
            //if (nodeTr == m_ExtendedJoints[0]) // flip the x to -x for right-handed system
            //{
            //    m.Value_As_String = _Matrix2String_FlipX(Matrix4x4.TRS(nodeTr.localPosition, nodeTr.localRotation, nodeTr.localScale));
            //}
            //else
            //    m.Value_As_String = _Matrix2String(Matrix4x4.TRS(nodeTr.localPosition, nodeTr.localRotation, nodeTr.localScale));

            var localMat = _LocalTRS(nodeTr);
            var reflectLocalMat = _ReflectXMatrix(localMat);
            m.Value_As_String = _Matrix2String(reflectLocalMat);
            //Dbg.Log("name: {0}\reflectLocalMat:{1}", nodeTr.name, _MatInfo(reflectLocalMat));


            // recursive
            foreach (Transform childTr in nodeTr)
            {
                if (m_NeedExportHierarchyNodes.Contains(childTr))
                {
                    Grendgine_Collada_Node childNode = null;
                    if (curNode.node == null)
                    {
                        curNode.node = Misc.CreateList<Grendgine_Collada_Node>(1);
                        childNode = curNode.node[0] = new Grendgine_Collada_Node();
                    }
                    else
                    {
                        childNode = new Grendgine_Collada_Node();
                        curNode.node.Add(childNode);
                    }

                    _ExportHierarchyNodes(childNode, childTr);
                }
            }
        }

        /// <summary>
        /// for each mf/smr, going upwards to AnimRoot
        /// </summary>
        private void _PrepareToExportAsHierarchyNodeList()
        {
            m_NeedExportHierarchyNodes.Clear();

            if( m_AnimRoot != null )
            {
                foreach( var b in m_ExtendedJoints)
                {
                    m_NeedExportHierarchyNodes.Add(b);
                }

                foreach (var mf in m_MFs)
                {
                    Transform tr = mf.transform;
                    while (tr != m_AnimRoot)
                    {
                        m_NeedExportHierarchyNodes.Add(tr);
                        tr = tr.parent;
                    }
                }
                foreach (var smr in m_SMRs)
                {
                    Transform tr = smr.transform;
                    while (tr != m_AnimRoot)
                    {
                        m_NeedExportHierarchyNodes.Add(tr);
                        tr = tr.parent;
                    }
                }
            }            
        }

        ///// <summary>
        ///// start from each MeshFilter, goes up to rootBone, add all to the toExportNodeList
        ///// </summary>
        //private void _PrepareToExportAsHierarchyNodeList()
        //{
        //    m_NeedExportHierarchyNodes.AddRange(m_ExtendedJoints);

        //    foreach (var mf in m_MFs)
        //    {
        //        Transform tr = mf.transform;

        //        bool bValid = false;
        //        while (tr != null)
        //        {
        //            if (m_NeedExportHierarchyNodes.Contains(tr))
        //            {
        //                bValid = true;
        //                break;
        //            }
        //            tr = tr.parent;
        //        }

        //        if (bValid)
        //        {
        //            tr = mf.transform;
        //            while (!m_NeedExportHierarchyNodes.Contains(tr))
        //            {
        //                m_NeedExportHierarchyNodes.Add(tr);
        //                tr = tr.parent;
        //            }
        //        }
        //    }

        //    foreach (var smr in m_SMRs)
        //    {
        //        Transform tr = smr.transform;

        //        bool bValid = false;
        //        while (tr != null)
        //        {
        //            if (m_NeedExportHierarchyNodes.Contains(tr))
        //            {
        //                bValid = true;
        //                break;
        //            }
        //            tr = tr.parent;
        //        }

        //        if (bValid)
        //        {
        //            tr = smr.transform;
        //            while (!m_NeedExportHierarchyNodes.Contains(tr))
        //            {
        //                m_NeedExportHierarchyNodes.Add(tr);
        //                tr = tr.parent;
        //            }
        //        }
        //    }
        //}

        private string _GenGeometryID(Transform tr, int idx)
        {
            //string s = AnimationUtility.CalculateTransformPath(tr, null) + "_" + idx;
            string s = string.Format("mesh_{0}", idx);
            return s;
        }

        private string _ExtractNameFromPath(string curve_path)
        {
            if (curve_path.Length == 0) return string.Empty;
            int idx = curve_path.LastIndexOf('/');
            string s = curve_path.Substring(idx + 1);
            return s.Replace(' ', '_');
        }

        /// <summary>
        /// foreach AnimationCurve, 
        /// 1. if the curve specify a GO not present yet, create one new AnimationNodeData instance
        /// 1.1 retrieve the original pos/rot/scale of the GO
        /// 2. get the AnimationNodeData instance, add the curve into the curves list
        /// </summary>
        private List<AnimationNodeData> _GatherAnimationNodeDataFromCurves(AnimationClipCurveData[] curves)
        {
            List<AnimationNodeData> lst = new List<AnimationNodeData>();

            Dictionary<string, AnimationNodeData> dict = new Dictionary<string, AnimationNodeData>();

            Transform rootNode = m_AnimRoot;

            //bool bHasRootMotionCurve = false;
            //bool bHasCurveForRootBone = false;

            //string rootBonePath = AnimationUtility.CalculateTransformPath(m_RootBone, m_AnimRoot);

            foreach (var curveData in curves)
            {
                //Dbg.Log("curves: path: {0}, prop: {1}", curveData.path, curveData.propertyName);

                if (curveData.type != typeof(Transform))
                {
                    continue;
                    //if (curveData.type == typeof(Animator) &&
                    //    (curveData.propertyName.StartsWith("MotionT.") || curveData.propertyName.StartsWith("MotionQ."))
                    //  )
                    //{
                    //    _PrepareRootMotionCurves(curveData);
                    //    bHasRootMotionCurve = true;
                    //}
                    //else
                    //{
                    //    continue;
                    //}
                }

                string path = curveData.path;
                //if (path == rootBonePath)
                //{
                //    bHasCurveForRootBone = true;
                //}

                // 1
                AnimationNodeData nodeData = null;
                if (dict.ContainsKey(path))
                {
                    nodeData = dict[path];
                }
                else
                {
                    Transform tr = rootNode.Find(path);
                    if (tr == null)
                    {
                        Dbg.LogWarn("DaeExporter._GatherAnimationNodeDataFromCurves: failed to find Transform: {0}", path);
                        continue;
                    }

                    nodeData = new AnimationNodeData();
                    dict[path] = nodeData;
                    lst.Add(nodeData);

                    nodeData.pos = tr.localPosition;
                    nodeData.rot = tr.localRotation;
                    nodeData.tmpEuler = tr.localEulerAngles;
                    nodeData.scale = tr.localScale;
                    nodeData.path = path;
                }

                // 2
                nodeData.curves.Add(curveData);
            }

            //// check root-motion curves overlap 
            //m_RootBoneAlreadyOccupied = (bHasCurveForRootBone && bHasRootMotionCurve);
            //if (m_RootBoneAlreadyOccupied)
            //{
            //    // will combine matrix
            //}
            //else
            //{
            //    if (m_RootMotionNodeData != null)
            //        lst.Add(m_RootMotionNodeData); //directly output onto the bone
            //}

            return lst;
        }

        ///// <summary>
        ///// the root motion curves needs special treatment
        ///// </summary>
        //private void _PrepareRootMotionCurves(AnimationClipCurveData curveData)
        //{
        //    if (m_RootMotionNodeData == null)
        //    {
        //        var data = m_RootMotionNodeData = new AnimationNodeData();
        //        data.path = AnimationUtility.CalculateTransformPath(m_RootBone, m_AnimRoot);
        //        data.pos = m_RootBone.localPosition;
        //        data.rot = m_RootBone.localRotation;
        //        data.scale = m_RootBone.localScale;
        //    }

        //    m_RootMotionNodeData.curves.Add(curveData);
        //}

        /// <summary>
        /// 
        /// </summary>
        private bool _ApplyCurveDataToTarget(AnimationClipCurveData curveData, float v,
            ref Vector3 pos, ref Quaternion rot, ref Vector3 euler, ref Vector3 scale, ref int useQuaternion)
        {
            string prop = curveData.propertyName;
            string[] parts = prop.Split('.');
            if (parts.Length != 2)
                return false;

            string main = parts[0];
            string acc = parts[1];

            int idx = 0;
            if (acc == "x")
            {
                idx = 0;
            }
            else if (acc == "y")
            {
                idx = 1;
            }
            else if (acc == "z")
            {
                idx = 2;
            }
            else if (acc == "w")
            {
                idx = 3;
            }
            else
            {
                Dbg.LogWarn("_ApplyCurveDataToTarget: prop: invalid prop {0}", prop);
                return false;
            }

            if (main == "m_LocalPosition" || main == "MotionT")
            {
                pos[idx] = v;
            }
            else if (main == "m_LocalRotation" || main == "MotionQ")
            {
                rot[idx] = v;
                if( useQuaternion < 0 )
                    Dbg.LogWarn("DaeExporter._ApplyCurveDataToTarget: already has euler curves, but found another quaternion curve: {0}.{1}  ", curveData.path, prop);
                useQuaternion++;
            }
            else if(main == "localEulerAnglesRaw" || main == "localEulerAnglesBaked")
            {
                euler[idx] = v;
                if (useQuaternion > 0)
                    Dbg.LogWarn("DaeExporter._ApplyCurveDataToTarget: already has quaternion curves, but found another euler curve: {0}.{1}  ", curveData.path, prop);
                useQuaternion--;

            }
            else if (main == "m_LocalScale")
            {
                scale[idx] = v;
            }
            else
            {
                Dbg.LogWarn("_ApplyCurveDataToTarget: prop: inv prop {0}", prop);
                return false;
            }

            return true;
        }

        private string _AnimNodeToStringArray(AnimationNodeData nodeData, float totalTime, float deltaTime)
        {
            StringBuilder bld = new StringBuilder();

            Transform tr = m_AnimRoot.Find(nodeData.path);
            Dbg.Assert(tr != null, "DaeExporter._AnimNodeToStringArray: failed to find transform path: {0}", nodeData.path);

            if(m_Debug)
            {
                m_DbgSW.WriteLine("Bone: {0}", nodeData.path);
            }

            for (float t = 0; t <= totalTime; ) //for each frame
            {
                if( m_Debug )
                {
                    m_DbgSW.Write("Time: {0:F4}, ", t);
                }

                Matrix4x4 m = _CalcMatrixFromCurves(nodeData, t);
                m = _ReflectXMatrix(m);

                //// combine root motion curves if they overlap with these curves
                //// ignore rootmotion if rootbone is occupied by other curves
                //if (m_RootBoneAlreadyOccupied && tr == m_RootBone)
                //{
                //    //Matrix4x4 rootMotionMat = _CalcMatrixFromCurves(m_RootMotionNodeData, t);
                //    //rootMotionMat = _ReflectXMatrix(rootMotionMat);
                //    //m = rootMotionMat * m; //combine these two matrices //WTF? Why? rootmotion mat should be first...
                //}

                bld.Append(_Matrix2String(m));

                if (Mathf.Approximately(t, totalTime))
                    break;
                t = Mathf.Min(totalTime, t + deltaTime);
            }
            return bld.ToString();
        }

        private Matrix4x4 _CalcMatrixFromCurves(AnimationNodeData nodeData, float t)
        {
            List<AnimationClipCurveData> curves = nodeData.curves;
            //Vector3 pos = Vector3.zero;
            //Quaternion rot = Quaternion.identity;
            //Vector3 scale = Vector3.one;
            Vector3 pos = nodeData.pos;
            Quaternion rot = nodeData.rot;
            Vector3 euler = nodeData.tmpEuler;
            Vector3 scale = nodeData.scale;
            int useQuaternion = 0;

            foreach (AnimationClipCurveData curveData in curves) // for each related curve
            {
                AnimationCurve c = curveData.curve;
                float v = c.Evaluate(t);

                _ApplyCurveDataToTarget(curveData, v, ref pos, ref rot, ref euler, ref scale, ref useQuaternion);
            }

            rot = MH.QUtil.Normalize(rot);

            if( m_Debug )
            {
                m_DbgSW.WriteLine("pos:<{0:F3},{1:F3},{2:F3}>, rot:<{3:F3},{4:F3},{5:F3},{6:F3}>, scale:<{7:F3}>",
                    pos.x, pos.y, pos.z,
                    rot.x, rot.y, rot.z, rot.w,
                    scale);
            }

            if (useQuaternion < 0)
                rot = Quaternion.Euler(euler);
            Matrix4x4 m = Matrix4x4.TRS(pos, rot, scale);
            return m;
        }

        private string _TimeToStringArray(float totalTime, float deltaTime, float clipStartTime, out int frameCnt)
        {
            frameCnt = 0;
            StringBuilder bld = new StringBuilder();

            for (float t = 0; t <= totalTime; )
            {
                frameCnt++;
                bld.AppendFormat("{0}\n", t + clipStartTime);

                if (Mathf.Approximately(t, totalTime))
                    break;
                else
                    t = Mathf.Min(totalTime, t + deltaTime);
            }

            return bld.ToString();
        }

        private string _Vertices2String(Vector3[] vertices)
        {
            StringBuilder bld = new StringBuilder();

            bld.Append("\n");
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                bld.AppendFormat("{0} {1} {2}", -v.x, v.y, v.z);
                if (i < vertices.Length - 1)
                    bld.Append('\n');
            }

            return bld.ToString();
        }

        private string _Normal2String(Vector3[] normals)
        {
            StringBuilder bld = new StringBuilder();
            bld.Append("\n");
            for (int i = 0; i < normals.Length; ++i)
            {
                Vector3 n = normals[i];
                bld.AppendFormat("{0} {1} {2}", -n.x, n.y, n.z);
                if (i < normals.Length - 1)
                    bld.Append('\n');
            }

            return bld.ToString();
        }

        private string _Texcoord2String(Vector2[] uv)
        {
            StringBuilder bld = new StringBuilder();

            bld.Append("\n");
            for (int i = 0; i < uv.Length; ++i)
            {
                Vector2 v = uv[i];
                bld.AppendFormat("{0} {1}", v.x, v.y);
                if (i < uv.Length - 1)
                    bld.Append('\n');
            }

            return bld.ToString();
        }

        private string _Triangles2String(int[] tris)
        {
            StringBuilder bld = new StringBuilder();

            bld.Append("\n");
            for (int i = 0; i < tris.Length; i += 3)
            {
                bld.AppendFormat("{0} {0} {0} {1} {1} {1} {2} {2} {2}",
                    tris[i], tris[i + 2], tris[i + 1]);
                if (i < tris.Length - 1)
                    bld.Append('\n');
            }

            return bld.ToString();
        }

        private string _Matrix2String_FlipX(Matrix4x4 m)
        {
            StringBuilder bld = new StringBuilder();

            Vector4 v = m.GetRow(0);
            v *= -1f;
            m.SetRow(0, v);

            var mt = m.transpose;
            for (int i = 0; i < 16; i++)
            {
                bld.AppendFormat("{0} ", mt[i]);
                if ((i + 1) % 4 == 0)
                    bld.Append('\n');
            }

            return bld.ToString();
        }

        private string _Matrix2String(Matrix4x4 m)
        {
            StringBuilder bld = new StringBuilder();
            var mt = m.transpose;
            for (int i = 0; i < 16; i++)
            {
                bld.AppendFormat("{0} ", mt[i]);
                if ((i + 1) % 4 == 0)
                    bld.Append('\n');
            }

            return bld.ToString();
        }

        private string _SMR2NameArray(SkinnedMeshRenderer smr)
        {
            StringBuilder bld = new StringBuilder();

            Transform[] bones = smr.bones;

            for (int i = 0; i < bones.Length; i++)
            {
                bld.AppendFormat("{0}\n", _FixName(bones[i].name));
            }

            return bld.ToString();
        }

        private string _SMRInvBind2Array(SkinnedMeshRenderer smr)
        {
            StringBuilder bld = new StringBuilder();

            Matrix4x4[] mats = smr.sharedMesh.bindposes;
            //Transform[] bones = smr.bones;

            for (int i = 0; i < mats.Length; i++)
            {
                Matrix4x4 m = mats[i];
                //m = m.inverse;

                //_DebugInvBindInfo(smr, i, ref m);
                m = _ReflectXMatrix(m);

                //m = m.inverse;

                bld.Append(_Matrix2String(m));
            }

            return bld.ToString();
        }

        private void _DebugInvBindInfo(SkinnedMeshRenderer smr, int i, ref Matrix4x4 m)
        {
            Transform bone = smr.bones[i];
            Vector3 pos = m.GetColumn(3);
            Vector3 z = m.GetColumn(2);
            Vector3 y = m.GetColumn(1);
            Vector3 x = m.GetColumn(0);

            Dbg.Log("InvBind: SMR: {0}, bone:{1}, pos:{2}, z:{3}, y:{4}, x:{5}", smr.name, bone.name, pos, z, y, x);
        }

        //private static Matrix4x4 _ReflectXMatrix(Matrix4x4 m)
        //{
        //    Vector3 pos = new Vector3(m[12], m[13], m[14]);
        //    pos.x = -pos.x;

        //    Vector3 vZ = m.GetColumn(2);
        //    Vector3 vY = m.GetColumn(1);
        //    Vector3 vX = m.GetColumn(0);

        //    Vector3 newVZ = Vector3.Reflect(vZ, Vector3.right);
        //    Vector3 newVY = Vector3.Reflect(vY, Vector3.right);

        //    Vector3 scale = new Vector3(vX.magnitude, vY.magnitude, vZ.magnitude);
        //    Dbg.Assert(!Mathf.Approximately(scale.x, 0) && !Mathf.Approximately(scale.y, 0) && !Mathf.Approximately(scale.z, 0),
        //        "_ReflectXMatrix: scale is 0");

        //    m = Matrix4x4.TRS(pos, Quaternion.LookRotation(newVZ, newVY), scale);

        //    //Vector3 newVX = Vector3.Cross(newVY, newVZ);
        //    return m;
        //}

        private static Matrix4x4 _ReflectXMatrix(Matrix4x4 m)
        {
            //Vector3 pos = new Vector3(m[12], m[13], m[14]);
            //pos.x = -pos.x;
            m[12] = -m[12];

            Vector3 vZ = m.GetColumn(2);
            Vector3 vY = m.GetColumn(1);
            Vector3 vX = m.GetColumn(0);
            float xmag = vX.magnitude;

            Vector3 newVZ = Vector3.Reflect(vZ, Vector3.right);
            Vector3 newVY = Vector3.Reflect(vY, Vector3.right);
            Vector3 newVX = Vector3.Cross(newVY, newVZ).normalized * xmag;

            m.SetColumn(0, new Vector4(newVX.x, newVX.y, newVX.z, m[3]) );
            m.SetColumn(1, new Vector4(newVY.x, newVY.y, newVY.z, m[7]) );
            m.SetColumn(2, new Vector4(newVZ.x, newVZ.y, newVZ.z, m[11]));

            return m;
        }

        private string _SMRWeights2FloatArray()
        {
            StringBuilder bld = new StringBuilder();

            for (int i = 0; i < m_BWLst.Count; i++)
            {
                var wbw = m_BWLst[i];
                for (int j = 0; j < wbw.m_validWeights; ++j)
                {
                    switch (j)
                    {
                        case 0: bld.AppendFormat("{0} ", wbw.m_weight.weight0); break;
                        case 1: bld.AppendFormat("{0} ", wbw.m_weight.weight1); break;
                        case 2: bld.AppendFormat("{0} ", wbw.m_weight.weight2); break;
                        case 3: bld.AppendFormat("{0} ", wbw.m_weight.weight3); break;
                    }
                }
                bld.AppendFormat("\n");
            }

            return bld.ToString();
        }

        private string _SMR2VCountIntArray()
        {
            StringBuilder bld = new StringBuilder();

            for (int i = 0; i < m_BWLst.Count; i++)
            {
                var wbw = m_BWLst[i];
                bld.AppendFormat("{0}\n", wbw.m_validWeights);
            }

            return bld.ToString();
        }

        private string _SMR2V_IntArray(SkinnedMeshRenderer smr)
        {
            int curIdx = 0;
            StringBuilder bld = new StringBuilder();

            for (int i = 0; i < m_BWLst.Count; i++)
            {
                var wbw = m_BWLst[i];

                for (int j = 0; j < wbw.m_validWeights; ++j)
                {
                    switch (j)
                    {
                        case 0: bld.AppendFormat("{0} {1} ", wbw.m_weight.boneIndex0, curIdx + j); break;
                        case 1: bld.AppendFormat("{0} {1} ", wbw.m_weight.boneIndex1, curIdx + j); break;
                        case 2: bld.AppendFormat("{0} {1} ", wbw.m_weight.boneIndex2, curIdx + j); break;
                        case 3: bld.AppendFormat("{0} {1} ", wbw.m_weight.boneIndex3, curIdx + j); break;
                    }
                }
                bld.AppendFormat("\n");

                curIdx += wbw.m_validWeights;
            }

            return bld.ToString();
        }

        /// <summary>
        /// include all bones from all smr, and all transforms upwards to the AnimRoot
        /// </summary>
        private void _InitExtendedJoints()
        {
            HashSet<Transform> joints = new HashSet<Transform>();

            foreach(var smr in m_SMRs)
            {
                var bones = smr.bones;
                foreach( var b in bones )
                {
                    Transform oneBone = b;
                    while( oneBone != m_AnimRoot )
                    {
                        joints.Add(oneBone);
                        oneBone = oneBone.parent;
                    }
                }
            }

            m_ExtendedJoints.Clear();
            m_ExtendedJoints.AddRange(joints);
        }

        //private void _InitExtendedJoints()
        //{
        //    _IncludeExtendedJoints(m_RootBone); //recursively include joints into m_ExtendedJoints
        //}

        //private void _IncludeExtendedJoints(Transform joint)
        //{
        //    // whether include
        //    bool bInclude = false;
        //    //if (joint == m_RootBone)
        //    //    bInclude = true; 
        //    //else 
        //    if (joint.GetComponent<Animation>() != null
        //        || joint.GetComponent<Animator>() != null
        //        || joint.GetComponent<SkinnedMeshRenderer>() != null
        //        || joint.GetComponent<MeshFilter>() != null)
        //        bInclude = false;
        //    else
        //        bInclude = true;

        //    if (bInclude)
        //    {
        //        //Dbg.Log("Include: {0}", joint.name);
        //        m_ExtendedJoints.Add(joint);
        //    }

        //    // recursive
        //    for (int idx = 0; idx < joint.childCount; ++idx)
        //    {
        //        Transform child = joint.GetChild(idx);

        //        _IncludeExtendedJoints(child);
        //    }
        //}

        private UnityEngine.Matrix4x4 _LocalTRS(Transform tr)
        {
            return Matrix4x4.TRS(tr.localPosition, tr.localRotation, tr.localScale);
        }


        private string _MatInfo(Matrix4x4 m)
        {
            Vector3 pos = new Vector3(m[12], m[13], m[14]);
            Vector3 z = m.GetColumn(2);
            Vector3 y = m.GetColumn(1);

            return string.Format("p:<{0},{1},{2}>, z:{3}, y:{4}", pos.x, pos.y, pos.z, z, y);
        }

        /// <summary>
        /// no space in name, replace ' ' with '_'
        /// </summary>
        private string _FixName(string name)
        {
            return name.Replace(' ', '_');
        }

        #region "Constants"
        // "Constants" 

        private const string GEO_ID = "base_mesh";
        private const string CONTROLLER_ID = "controller";
        private const string SCENE_NODE_ID = "scene_node";

        private const string MATRIX_ID = "matrix";

        #endregion "Constants"

        #region "Inner struct"
        // "Inner struct" 

        public class MeshData
        {
            public Mesh mesh;
            public Transform trans;
        }

        public class WBoneWeights
        {
            public int m_validWeights;
            public BoneWeight m_weight;

            public WBoneWeights(BoneWeight bw)
            {
                m_weight = bw;
                m_validWeights = 0;

                if (bw.weight0 <= 0) return;
                ++m_validWeights;
                if (bw.weight1 <= 0) return;
                ++m_validWeights;
                if (bw.weight2 <= 0) return;
                ++m_validWeights;
                if (bw.weight3 <= 0) return;
                ++m_validWeights;
            }
        }

        /// <summary>
        /// animation node data
        /// gather multiple animation curves and create one AnimationNodeData
        /// </summary>
        public class AnimationNodeData
        {
            public string path; //the path from AnimationClipCurveData
            public List<AnimationClipCurveData> curves = new List<AnimationClipCurveData>(); //all curves related to this GO
            public Vector3 pos;
            public Quaternion rot;
            public Vector3 tmpEuler;
            public Vector3 scale;
        }

        #endregion "Inner struct"
    }

}