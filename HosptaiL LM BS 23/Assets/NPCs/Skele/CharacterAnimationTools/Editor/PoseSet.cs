using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using ExtMethods;

using PoseDataDict = System.Collections.Generic.Dictionary<string, MH.XformData>; // boneTrPath, xformData
using System.Text;

namespace MH
{

/// <summary>
/// contains the description of the skeleton, starting from root bone, include even the hidden bones
/// </summary>
public class SkeletonDesc
{
    public SkeletonDescNode m_RootBone;
}
public class SkeletonDescNode
{
    //public Transform m_Transform; //could be null;
    public string m_BoneName;
    public List<SkeletonDescNode> m_ChildBones;
}

public class PoseDesc
{
    public string m_PoseName;
    public PoseDataDict m_PoseData; //map< boneName, transformData>
}

/// <summary>
/// encapsulate all the Poses for a given skeleton
/// </summary>
public class PoseSet
{
	#region "data"
    // data
    [SerializeField]
    private string m_FileName = string.Empty; //base: Project root, e.g.: Assets/Skele/Poses/xx.poses
    [SerializeField]
    private SkeletonDesc m_SkeletonDesc; //the description of skeleton structure
    [SerializeField]
    private List<PoseDesc> m_Poses; //all the poses that belong to this skeleton

    #endregion "data"

	#region "public method"
    // public method
    public static PoseSet Load(string pathFromPrjRoot, Transform rootJoint, Transform[] joints)
    {
        //TextAsset assetFile = AssetDatabase.LoadAssetAtPath(pathFromPrjRoot, typeof(TextAsset)) as TextAsset;
        if( !File.Exists(pathFromPrjRoot) )
        {
            Dbg.LogErr("PoseSet.Load: unable to load poseFile: {0}", pathFromPrjRoot);
            return null;

        }
        string content = File.ReadAllText(pathFromPrjRoot);

        PoseSet pf = Json.ToObj<PoseSet>(content);

        bool bSuccess = pf._Match(joints);

        if( bSuccess )
        {
            Dbg.Log("PoseSet.Load: loaded pose file from: {0}", pathFromPrjRoot);
            return pf;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// get/set the filename
    /// </summary>
    public string FileName
    {
        get { return m_FileName; }
        set { m_FileName = value; }
    }

    public int Count
    {
        get { return m_Poses.Count; }
    }

    public List<PoseDesc> Poses
    {
        get { return m_Poses; }
        set { m_Poses = value; }
    }

    public SkeletonDesc SkeleDesc
    {
        get { return m_SkeletonDesc; }
        set { m_SkeletonDesc = value; }
    }

    /// <summary>
    /// specify file saving location,
    /// create SkeletonDesc from extendedJoints from SMREditor
    /// </summary>
    public PoseSet(Transform rootJoint, Transform[] joints)
    {
        m_Poses = new List<PoseDesc>();

        _InitSkeletonDesc(rootJoint, joints);
    }

    /// <summary>
    /// used by LitJson's deserialization
    /// </summary>
    public PoseSet()
    {

    }

    /// <summary>
    /// save the data to the specified file
    /// </summary>
    public void Save()
    {
        Dbg.Assert(m_FileName != null, "PoseSet.Save: m_FileName is null");

        string pathFile = m_FileName;

        string str = Json.ToStr(this);

        Directory.CreateDirectory(Path.GetDirectoryName(pathFile));
        File.WriteAllText(pathFile, str, Encoding.UTF8);

        Dbg.Log("PoseSet.Save: Save poses to file: {0}", m_FileName);
    }

    /// <summary>
    /// find out if there's the pose
    /// </summary>
    public bool HasPose(string poseName)
    {
        for (int idx = 0; idx < m_Poses.Count; ++idx)
        {
            PoseDesc desc = m_Poses[idx];
            if (desc.m_PoseName == poseName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// get pose data by the name
    /// if not found, return null
    /// </summary>
    public PoseDesc GetPose(string poseName)
    {
        for (int idx = 0; idx < m_Poses.Count; ++idx)
        {
            PoseDesc desc = m_Poses[idx];
            if (desc.m_PoseName == poseName)
            {
                return desc;
            }
        }
        return null;
    }

    /// <summary>
    /// get pose data by idx
    /// </summary>
    public PoseDesc GetPose(int idx)
    {
        Dbg.Assert(idx < m_Poses.Count, "PoseSet.GetPose: the idx beyond range: {0}", idx);
        return m_Poses[idx];
    }

    /// <summary>
    /// add a pose, if a pose with same name presents, overwrite it;
    /// 
    /// `joints' are the joints belonging to the pose
    /// </summary>
    public void AddPose(string poseName, Transform[] joints, Transform animRoot)
    {
        DelPose(poseName); //del existing same name pose first if there is one

        PoseDesc newDesc = new PoseDesc();
        newDesc.m_PoseName = poseName;
        var data = newDesc.m_PoseData = new PoseDataDict();

        for( int idx = 0; idx < joints.Length; ++idx )
        {
            Transform j = joints[idx];

            XformData trData = new XformData();
            trData.CopyFrom(j);

            string boneTrPath = AnimationUtility.CalculateTransformPath(j, animRoot);

            if( !data.ContainsKey(boneTrPath) )
            {
                data.Add(boneTrPath, trData);
            }
            else
            {
                Dbg.LogWarn("PoseSet.AddPose: Found duplicate bone transform path: {0}", boneTrPath);
            }
        }

        m_Poses.Add(newDesc);
    }

    public bool DelPose(string poseName)
    {
        for( int idx = 0; idx < m_Poses.Count; ++idx )
        {
            PoseDesc desc = m_Poses[idx];
            if( desc.m_PoseName == poseName )
            {
                m_Poses.RemoveAt(idx);
                return true;
            }
        }

        return false;
    }
    public void DelPose(int idx)
    {
        Dbg.Assert(idx < m_Poses.Count, "PoseSet.DelPose: idx beyond range: {0}", idx);
        m_Poses.RemoveAt(idx);
    }

    #endregion "public method"

	#region "private method"
    // private method

    public bool _Match(Transform[] extendedJoints)
    {
        if (m_SkeletonDesc.m_RootBone.m_BoneName != extendedJoints[0].name)
        {
            Dbg.LogWarn("PoseSet._Match: current root \"{0}\" is not matched in pose file root: \"{1}\"", 
                extendedJoints[0].name, m_SkeletonDesc.m_RootBone.m_BoneName);
            return false;
        }

        return _RecursiveMatch(m_SkeletonDesc.m_RootBone, extendedJoints[0], extendedJoints);
    }

    private bool _RecursiveMatch(SkeletonDescNode node, Transform curJoint, Transform[] joints)
    {
        //node.m_Transform = curJoint;

        for (int idx = 0; idx < node.m_ChildBones.Count; ++idx)
        {
            SkeletonDescNode cnode = node.m_ChildBones[idx]; //child desc_node
            Transform cJoint = _FindJointByName(joints, cnode.m_BoneName); //child_joint
            if (cJoint == null)
            {
                Dbg.LogWarn("PoseSet._Match: current skeleton doesn't have bone named: {0}", cnode.m_BoneName);
                return false;
            }

            if (!_RecursiveMatch(cnode, cJoint, joints))
                return false;
        }

        return true;
    }

    private Transform _FindJointByName(Transform[] joints, string name)
    {
        for (int idx = 0; idx < joints.Length; ++idx)
        {
            if (joints[idx].name == name)
            {
                return joints[idx];
            }
        }
        return null;
    }

    private void _InitSkeletonDesc(Transform rootJoint, Transform[] joints)
    {
        m_SkeletonDesc = new SkeletonDesc();
        var rootNode = m_SkeletonDesc.m_RootBone = new SkeletonDescNode();

        _InitSkeletonDescNode(rootNode, rootJoint, joints);
    }

    private void _InitSkeletonDescNode( SkeletonDescNode node, Transform curJoint, Transform[] joints )
    {
        node.m_BoneName = curJoint.name;
        //node.m_Transform = curJoint;
        var children = node.m_ChildBones = new List<SkeletonDescNode>();

        for( int idx = 0; idx < curJoint.childCount; ++idx )
        {
            Transform cTr = curJoint.GetChild(idx);
            if( joints.Contains(cTr)) 
            {
                SkeletonDescNode newNode = new SkeletonDescNode();
                _InitSkeletonDescNode(newNode, cTr, joints);
                children.Add(newNode);
            }
        }
    }

    #endregion "private method"

	#region "constant data"
    // constant data

    #endregion "constant data"
   

    

    
}

}
