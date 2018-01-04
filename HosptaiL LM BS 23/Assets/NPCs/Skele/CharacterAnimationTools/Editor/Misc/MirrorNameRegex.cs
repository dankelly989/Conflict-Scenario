using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{

using REPairLst = System.Collections.Generic.List<MirrorNameRegex.REPair>;

/// <summary>
/// this SO contains a group of regex used to find mirror-able joints
/// </summary>
public class MirrorNameRegex : ScriptableObject
{
	#region "configurable data"
    // configurable data

    public REPairLst m_REPrLst;

    #endregion "configurable data"

	#region "inner struct"
	// "inner struct" 

    [Serializable]
    public class REPair
    {
        public string fromBoneRE;
        public string replaceString;

        public REPair(string l, string r)
        {
            fromBoneRE = l;
            replaceString = r;
        }
    }
	
	#endregion "inner struct"
}

}
