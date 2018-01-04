using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MH;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class AMGroup : ScriptableObject {
		

		public string group_name;
	   	public int group_id;
		public List<int> elements = new List<int>();			// holds track ids (positive integers) and group ids (negative integers)
		public bool foldout = true;
		
		public void init(int group_id = 0, string group_name = null) 
		{
			// set group id
			this.group_id = group_id;
			// set group name
			if(group_name == null) this.group_name = "Group"+Mathf.Abs(this.group_id);
			else this.group_name = group_name;
		
		}
		public void destroy() {
            //Object.DestroyImmediate(this);
            AMUtil.destroyObjImm(this);
		}

#if UNITY_EDITOR
        public void SaveAsset(AnimatorData mb, AMTake take)
        {
            AMTakeSav.AddObjectToAsset(this, take);
        }
#endif
}
