//using System;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;
//using System.Text;
//using System.IO;


///// <summary>
///// used to find out the bones from imported archive
///// </summary>
//public class FindBonePP : AssetPostprocessor
//{
//    #region "configurable data"
//    // configurable data

//    #endregion "configurable data"

//    #region "data"
//    // data

//    #endregion "data"

//    #region "unity event handlers"
//    // unity event handlers

//    void OnPreprocessModel()
//    {
//        var importer = (ModelImporter)assetImporter;
//        var bones = importer.transformPaths;
//        StringBuilder bld = new StringBuilder();
//        foreach (string bone in bones)
//        {
//            bld.AppendLine(bone);
//        }
//        File.WriteAllText("F:/aaa.txt", bld.ToString());
//    }

//    void OnPostprocessModel(GameObject go)
//    {
//        var importer = (ModelImporter)assetImporter;
//        var bones = importer.transformPaths;
//        StringBuilder bld = new StringBuilder();
//        foreach( string bone in bones )
//        {
//            bld.AppendLine(bone);
//        }
//        Dbg.Log(bld.ToString());
//    }

//    #endregion "unity event handlers"

//    #region "public method"
//    // public method

//    #endregion "public method"

//    #region "private method"
//    // private method

//    #endregion "private method"

//    #region "constant data"
//    // constant data

//    #endregion "constant data"
//}

