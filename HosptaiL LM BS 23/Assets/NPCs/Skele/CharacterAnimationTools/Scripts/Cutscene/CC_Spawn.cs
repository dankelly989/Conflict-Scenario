using System;
using System.Collections.Generic;
using UnityEngine;


namespace MH
{

/// <summary>
/// used to execute Spawning operation
/// </summary>
public class CC_Spawn : CC_EvtActions
{
	#region "configurable data"
    // configurable data

    public List<CCSpawnData> m_SpawnDataLst;

    #endregion "configurable data"

	#region "data"
    // data

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

	void Start()
	{}

    #endregion "unity event handlers"

	#region "public method"
    // public method

    public override void OnAnimEvent()
    {
        Transform ccroot = m_CC.transform;
        for( int idx = 0; idx < m_SpawnDataLst.Count; ++idx )
        {
            CCSpawnData sd = m_SpawnDataLst[idx];
            GameObject go = sd.Spawn(ccroot);

            Transform newTr = go.transform;
            if( newTr.parent == null )
                Misc.AddChild(transform, go);
        }
    }

    #endregion "public method"

	#region "private method"
    // private method

    #endregion "private method"

	#region "constant data"
    // constant data

    #endregion "constant data"


	#region "Inner struct"
	// "Inner struct" 


	
	#endregion "Inner struct"
}

}