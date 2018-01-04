using UnityEngine;
using System.Collections;

using MH;

using Object = UnityEngine.Object;

public class ResMgr : NonBehaviourSingleton<ResMgr> 
{
	#region "configurable data"
    // configurable data

    #endregion

	#region "data"
    // data

    private bool m_EnableDropRes = true; //if disabled, DropRes will not be effective

    #endregion

	#region "unity event handlers"
    // unity event handlers

    public override void Init()
    {
        
    }

    public override void Fini()
    {
        base.Fini();
    }

    #endregion

	#region "public method"
    // public method

    /// <summary>
    /// the default empty resource
    /// </summary>
    public Res EmptyRes 
    {
        get { return m_Empty; }
    }

    /// <summary>
    /// control whether DropRes will be active;
    /// e.g.: if we're going to load a BIG bunch of resource, we would rather call UnloadUnusedAssets() at last;
    /// </summary>
    public bool EnableDropRes
    {
        get { return m_EnableDropRes; }
        set { m_EnableDropRes = value; }
    }

    /// <summary>
    /// given a uniform id, try to acquire specified resource,
    /// if failed, the Res.m_Resource == null
    /// </summary>
    public Res GetRes(string uni)
    {
        Object o = Resources.Load(PathUtil.StripExtension(uni));
        if( null == o )
            return EmptyRes;

        return new Res(o, RESTYPE.Resource);
    }

    /// <summary>
    /// when done using a Res, call this to release underlying resource if there is
    /// </summary>
    public void DropRes(ref Res res)
    {
        if( m_EnableDropRes )
        {
            //Resources.UnloadAsset(res.m_Resource); //?? UnloadAsset cannot work on Gameobject prefab? then how should I do it?
            Resources.UnloadUnusedAssets();
        }        
    }

    #endregion

	#region "private method"
    // private method

    #endregion

	#region "constant data"
    // constant data

    public enum RESTYPE {
        File,
        Resource,
        Bundle,
        TYPE_END
    };

    private Res m_Empty = new Res(null, RESTYPE.TYPE_END);

    #endregion

    #region "Inner struct"
    // "Inner struct" 

    public struct Res
    {
        public Object m_Resource;
        public RESTYPE m_Type;

        public Res(Object r, RESTYPE tp)
        {
            m_Resource = r;
            m_Type = tp;
        }

        public bool Valid {
            get { return m_Type != RESTYPE.TYPE_END; }
        }

        public T Get<T>() where T : Object
        {
            Dbg.Assert( Valid, "ResMgr.Res.Get<T>: the resource is not valid");
            Dbg.Assert( m_Resource is T, "ResMgr.Res.Get<T>: the resource type mismatch");
            return (T)m_Resource;
        }
    }

    #endregion
	
}
