using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    public class EDOBool : EDataObj
    {
        public bool val;

        public override string ToString()
        {
            return val.ToString();
        }

        public static EDOBool DFGet(string id, bool defVal)
        {
            bool isNew = true;
            EDOBool edo = EData.FGet<EDOBool>(id, out isNew);
            if (isNew)
                edo.val = defVal;
            return edo;
        }
    }
}
