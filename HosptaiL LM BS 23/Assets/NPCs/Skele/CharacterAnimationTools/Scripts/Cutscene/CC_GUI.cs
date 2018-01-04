using System;
using System.Collections.Generic;
using UnityEngine;


namespace MH
{

/// <summary>
/// As a dialog display utility
/// </summary>
public class CC_GUI : CC_EvtActions
{
	#region "configurable data"
    // configurable data

    public GameObject m_DialogGO = null;

    #endregion "configurable data"

	#region "data"
    // data

    #endregion "data"

	#region "unity event handlers"
    // unity event handlers

	void Start()
	{
        // NO, I should set it inactive when I add it as child of the CC_GUI
        //m_DialogGO.SetActive(false); //ensure it's not active
    }

	void Update()
	{}

    #endregion "unity event handlers"

	#region "public method"
    // public method

    public override void OnAnimEvent()
    {
        if (m_DialogGO == null)
        {
            Dbg.LogWarn("CC_GUI.OnAnimEvent: no dialog GO specified: {0}", name);
            return;
        }
        m_DialogGO.SendMessage("StartDialog", this);
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
