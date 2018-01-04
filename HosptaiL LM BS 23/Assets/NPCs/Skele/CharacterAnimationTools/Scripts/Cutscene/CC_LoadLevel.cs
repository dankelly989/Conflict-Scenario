using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH
{

public class CC_LoadLevel : CC_EvtActions
{
	#region "configurable data"
    // configurable data

    public string m_ToLoadLevel;

    #endregion "configurable data"

	#region "data"
    // data

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

    void Update(){}

    #endregion "unity event handlers"

	#region "public method"
    // public method

    public override void OnAnimEvent()
    {
        //Camera.current.enabled = false;

        var allCam = Camera.allCameras;
        for (int idx = 0; idx < allCam.Length; ++idx )
        {
            allCam[idx].enabled = false;
        }

        //Application.LoadLevel(m_ToLoadLevel);
        Levels.LoadLevel(m_ToLoadLevel);
    }

    #endregion "public method"

	#region "private method"
    // private method

    #endregion "private method"

	#region "constant data"
    // constant data

    #endregion "constant data"
}

}
