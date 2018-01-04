using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MH
{
    public class EDOString : EDataObj
    {
        public string val;

        public override string ToString()
        {
            return val;
        }

        public static EDOString DFGet(string id, string defVal)
        {
            bool isNew = true;
            var edo = EData.FGet<EDOString>(id, out isNew);
            if (isNew) edo.val = defVal;
            return edo;
        }
    }
}
