using System;
using System.Collections.Generic;
using UnityEngine;

using WindowCont = System.Collections.Generic.Dictionary<int, MH.GUIWindow>;
using WindowList = System.Collections.Generic.List<MH.GUIWindow>;
using IDList = System.Collections.Generic.List<int>;

namespace MH
{

/// <summary>
/// used to draw GUI.Window/ModalWindow
/// </summary>
public class GUIWindowMgr
{
	#region "data"
    // data

    private static GUIWindowMgr ms_Instance = null;

    private WindowCont m_Windows;
    private IDList m_toDel;
    private WindowList m_toAdd;

    private int m_uid; //the unique window id

    #endregion "data"

	#region "public method"
    // public method

    public static GUIWindowMgr Instance 
    {
        get {
            if( ms_Instance == null )
            {
                ms_Instance = new GUIWindowMgr();
            }
            return ms_Instance;
        }
    }

    private GUIWindowMgr()
    {
        m_Windows = new WindowCont();
        m_toDel = new IDList();
        m_toAdd = new WindowList();
    }

    /// <summary>
    /// return true to mean there is modal window
    /// </summary>
    public bool OnGUI()
    {
        for(var ie = m_Windows.GetEnumerator(); ie.MoveNext(); )
        {
            var pr = ie.Current;
            GUIWindow wndctrl = pr.Value;

            // execute onGUI
            GUIWindow.EReturn eRet = wndctrl.OnGUI();

            // record those need deleting
            if( eRet == GUIWindow.EReturn.STOP )
            {
                m_toDel.Add(pr.Key);
            }
            else if(eRet == GUIWindow.EReturn.MODAL)
            {
                return true;
            }
        }

        for (var ie = m_toAdd.GetEnumerator(); ie.MoveNext(); )
        {
            GUIWindow wndctrl = ie.Current;
            int idx = wndctrl.Index;
            m_Windows.Add(idx, wndctrl);
        }
        for (var ie = m_toDel.GetEnumerator(); ie.MoveNext(); )
        {
            int id = ie.Current;
            m_Windows.Remove(id);
        }
        m_toAdd.Clear();
        m_toDel.Clear();

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    public int Add(GUIWindow wndctrl)
    {
        int id = m_uid++;

        wndctrl.Index = id;

        m_toAdd.Add(wndctrl);

        return id;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Remove(int idx)
    {
        m_toDel.Add(idx);
    }

    #endregion "public method"

	#region "private method"
    // private method

    #endregion "private method"

	#region "constant data"
    // constant data

    #endregion "constant data"
}

public class GUIWindow
{
	#region "data"
	// "data" 

    public int m_Index;
	
	#endregion "data"

	#region "public method"
    // public method

    public int Index
    {
        get { return m_Index; }
        set { m_Index = value; }
    }

    public virtual EReturn OnGUI()
    {
        return EReturn.GOON;
    }

    #endregion "public method"

	#region "constant data"
    // constant data

    public enum EReturn
    {
        GOON,
        STOP,
        MODAL,
    }

    #endregion "constant data"
}

}