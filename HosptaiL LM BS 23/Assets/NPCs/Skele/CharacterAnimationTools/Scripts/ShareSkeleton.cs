using System;
using System.Collections.Generic;
using UnityEngine;

using ExtMethods;

namespace MH
{

/// <summary>
/// a helper for making skinned meshes sharing skeleton
/// </summary>
public class ShareSkeleton
{
    /// <summary>
    /// make `fromSMR' to use the skeleton in `targetSMR'
    /// </summary>
    public static void ShareSkele(SkinnedMeshRenderer targetSMR, SkinnedMeshRenderer fromSMR)
    {
        Transform targetRoot = targetSMR.transform.parent;

        Transform[] boneArray = fromSMR.bones;
        for( int idx = 0; idx < boneArray.Length; ++ idx)
        {
            string boneName = boneArray[idx].name;

            Transform targetCorresponding = targetRoot.FindByName(boneName);
            if( null == targetCorresponding )
            {
                Dbg.LogWarn("ShareSkeleton.ShareSkele: the bone \"{0}\" is not found in targetSMR", boneName);
            }
            boneArray[idx] = targetCorresponding;
        }

        fromSMR.bones = boneArray; //take effect

        //change fromSMR's rootBone
        Transform newRootBone = _FindNewRootbone(targetRoot, boneArray[0]);
        if (newRootBone == null)
        {
            Dbg.LogWarn("ShareSkeleton.ShareSkele: failed to find the rootBone for targetSMR");
        }
        else
        {
            fromSMR.rootBone = newRootBone;
        }
    }

    private static Transform _FindNewRootbone(Transform targetRoot, Transform oneBone)
    {
        while( oneBone != null && oneBone.parent != targetRoot )
        {
            oneBone = oneBone.parent;
        }
        return oneBone;
    }
}

}
