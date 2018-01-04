using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using ExtMethods;
#pragma warning disable 618
namespace MH
{

    public class AnimCompressEditorWindow : EditorWindow
	{
	    #region "data"
        // data

        private static AnimCompressEditorWindow ms_Instance = null;

        private AnimationClip m_Clip;

        // pos / rot / scale error threshold
        private float m_PosErr = 0.5f;
        private float m_RotErr = 0.5f;
        private float m_ScaErr = 0.5f;
        private float m_FloatErr = 0.5f;

        private bool m_Verbose = false;

        #endregion "data"

	    #region "unity event handlers"
        // unity event handlers

        [MenuItem("Window/Skele/Anim Compressor")]
        public static void OpenWindow()
        {
            if (ms_Instance == null)
            {
                var inst = ms_Instance = (AnimCompressEditorWindow)GetWindow(typeof(AnimCompressEditorWindow));
                EditorApplication.playmodeStateChanged += inst.OnPlayModeChanged;

                EUtil.SetEditorWindowTitle(inst, "Compressor");
                inst.minSize = new Vector2(300, 150);
                inst.Show();
            }
        }

        void OnInspectorUpdate()
        {
            if (EditorApplication.isCompiling)
            {
                if( ms_Instance != null )
                    ms_Instance.Close();
                return;
            }
        }

        void OnPlayModeChanged()
        {
            if(ms_Instance != null)
            {
                ms_Instance.Close();
            }
        }

        void OnDestroy()
        {
            EditorUtility.ClearProgressBar();
            ms_Instance = null;
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            m_Clip = (AnimationClip)EditorGUILayout.ObjectField("Compress clip:", m_Clip, typeof(AnimationClip), false);
            m_PosErr = EditorGUILayout.FloatField(new GUIContent("Position Threshold:", "raising this value could reduce more keys"), m_PosErr);
            m_RotErr = EditorGUILayout.FloatField(new GUIContent("Rotation Threshold:", "raising this value could reduce more keys"), m_RotErr);
            m_ScaErr = EditorGUILayout.FloatField(new GUIContent("Scale Threshold:", "raising this value could reduce more keys"), m_ScaErr);
            m_FloatErr = EditorGUILayout.FloatField(new GUIContent("Float Threshold:", "raising this value could reduce more keys"), m_FloatErr);
            m_Verbose = EditorGUILayout.ToggleLeft("Verbose", m_Verbose);
            if (EditorGUI.EndChangeCheck()){ }

            EUtil.PushGUIEnable(m_Clip != null);
            if( EUtil.Button("Execute!", "Start the work", Color.green, GUILayout.ExpandHeight(true)))
            {
                string newPath = EditorUtility.SaveFilePanelInProject("Save new clip", m_Clip.name + "_optimized", "anim", "Select new optimized clip save location");
                if( !string.IsNullOrEmpty(newPath))
                {
                    string oldPath = AssetDatabase.GetAssetPath(m_Clip);
                    if(oldPath == newPath)
                    {
                        bool bOK = EditorUtility.DisplayDialog("Are you sure?", "You're going to overwrite old animation file", "Go Ahead", "No-No");
                        if( bOK )
                        {
                            var comp = new AnimCompress();
                            comp.Verbose = m_Verbose;
                            comp.ReduceKeyframes(m_Clip, m_PosErr, m_RotErr, m_ScaErr, m_FloatErr);
                        }
                    }
                    else
                    {
                        AnimationClip newClip = AnimationClip.Instantiate(m_Clip) as AnimationClip;
                        if (newClip == null)
                        {
                            Dbg.LogErr("cannot load new clip at path: {0}", newPath);
                            return;
                        }
                        var comp = new AnimCompress();
                        comp.Verbose = m_Verbose;
                        comp.ReduceKeyframes(newClip, m_PosErr, m_RotErr, m_ScaErr, m_FloatErr);

                        newClip.name = Path.GetFileNameWithoutExtension(newPath);
                        EUtil.SaveAnimClip(newClip, newPath);
                    }
                }
            }
            EUtil.PopGUIEnable();

        }
	    
        #endregion "unity event handlers"

	    #region "private method"
        // private method

        #endregion "private method"

	    #region "constant data"
        // constant data

        #endregion "constant data"
	}

    /// <summary>
    /// execute the animation compression task here
    /// </summary>
    public class AnimCompress
    {
        public delegate bool ErrorFunc<T>(T val, T reduceVal, float min);

        public const float kPositionMinValue = 0.00001F;
        public const float kQuaternionNormalizationError = 0.001F;/// The sampled quaternion must be almost normalized.

        private HashSet<string> m_checkedCurves;
        private AnimationClip m_Clip;
        private bool m_Verbose = false;

        private int m_CurveCnt = 0;
        private int m_FinishedCurveCnt = 0;

        public bool Verbose {
            get { return m_Verbose; }
            set { m_Verbose = value; }
        }

        public int CurveCount {
            get {
                return m_CurveCnt;
            }
        }

        public int FinishedCurveCount {
            get {
                return m_FinishedCurveCnt;
            }
        }

        public void ReduceKeyframes(AnimationClip clip, 
            float posErr, float rotErr, float scaleErr, float floatErr)
        {
            m_Clip = clip;

            posErr /= 100f;
            scaleErr /= 100f;
            floatErr /= 100f;
            rotErr = Mathf.Cos(rotErr * Mathf.Deg2Rad / 2f);

            var allCurveData = AnimationUtility.GetAllCurves(clip, false);
            int curveCnt = m_CurveCnt = allCurveData.Length;
            m_FinishedCurveCnt = 0;

            if( curveCnt == 0 )
            {
                Dbg.LogWarn("There is no curves in given clip: {0}", clip.name);
                return;
            }

            m_checkedCurves = new HashSet<string>();

            double time = EditorApplication.timeSinceStartup;
            int oldKeyCnt = 0;
            int newKeyCnt = 0;
            float compRatio = 0f;

            float sampleRate = clip.frameRate;

            // execute
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            foreach(var oneBinding in bindings)
            {
                oldKeyCnt += _GetKeyCnt(oneBinding);

                if (!_HasBinding(oneBinding))
                {
                    string pName = oneBinding.propertyName;
                    if (pName.StartsWith(PROP_LPOS))
                    {
                        // gather all other curves to form a position curve
                        _AnimCurve newCurve = _GatherPosCurve(oneBinding);
                        _ReduceKeyFrames(newCurve, sampleRate, posErr);
                    }
                    else if (pName.StartsWith(PROP_LROT))
                    {
                        // gather all other curves to form a rotation curve
                        _AnimCurve newCurve = _GatherRotCurve(oneBinding);
                        _ReduceKeyFrames(newCurve, sampleRate, rotErr);
                    }
                    else if (pName.StartsWith(PROP_LSCA))
                    {
                        // gather all other curves to form a scale curve
                        _AnimCurve newCurve = _GatherScaleCurve(oneBinding);
                        _ReduceKeyFrames(newCurve, sampleRate, scaleErr);
                    }
                    else
                    {
                        Dbg.Log("Ignore curve: {0}", pName);
                    }
                }

                newKeyCnt += _GetKeyCnt(oneBinding);
                m_FinishedCurveCnt++;

                if( m_FinishedCurveCnt % 10 == 0 )
                {
                    EditorUtility.DisplayProgressBar("Crunching curves...", 
                        "Working hard to compress clip..." + m_FinishedCurveCnt + "/" + m_CurveCnt,
                        m_FinishedCurveCnt / (float)m_CurveCnt
                    );
                }
            }

            EditorUtility.ClearProgressBar();

            if( oldKeyCnt != 0 )
                compRatio = (float)newKeyCnt / (float)oldKeyCnt;
            time = EditorApplication.timeSinceStartup - time;

            Dbg.Log("ReduceKeyFrames: compress ratio: {0:F2}%, time: {1:F4}", compRatio*100f, time);

            m_checkedCurves.Clear();
            m_checkedCurves = null;
        }

        /// <summary>
        /// get the key count of the specified curve
        /// </summary>
        private int _GetKeyCnt(EditorCurveBinding oneBinding)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(m_Clip, oneBinding);
            return curve.length;
        }

        private float _ReduceKeyFrames(_AnimCurve curve, float sampleRate, float allowedError)
        {
            int keycnt = curve.KeyCnt;
            if (keycnt <= 2)
                return 100f;

            float delta = 1f / sampleRate;
            List<_Keyframe> keyLst = new List<_Keyframe>(keycnt);
            _Keyframe[] keys = curve.keys;
            _Keyframe kfStart = keys[0].Clone();
            _Keyframe kfEnd = keys[keycnt - 1].Clone();

            // at first try reducing to const curve
            kfStart.inTangent = kfStart.outTangent = 0f;
            kfEnd.inTangent = kfEnd.outTangent = 0f;
            kfEnd.value = kfStart.value;

            bool bCanReduceToConstCurve = _CanReduce(kfStart, kfEnd, curve, allowedError, delta, 0, keycnt - 1, false);
            if( bCanReduceToConstCurve )
            {
                keyLst.Add(kfStart);
                keyLst.Add(kfEnd);
            }
            else
            {
                keyLst.Capacity = keycnt;
                keyLst.Add(keys[0]);

                int lastUsedKey = 0;
       
                for (int i=1; i<keycnt-1; i++)
                {
                    _Keyframe fromKey = keys[lastUsedKey];
                    _Keyframe toKey = keys[i + 1];
 
                    //FitTangentsToCurve(curve, fromKey, toKey);

                    bool canReduce = _CanReduce(fromKey, toKey, curve, allowedError, delta, lastUsedKey + 1, i + 1, true);
 
                    if (!canReduce)
                    {
                        keyLst.Add(keys[i]);
                        // fitting tangents between last two keys
                        //FitTangentsToCurve(curve, *(output.end() - 2), output.back());
 
                        lastUsedKey = i;
                    }
                }
       
                // We always add the last key
                keyLst.Add(keys[keycnt - 1]);
                // fitting tangents between last and the one before last keys
                //FitTangentsToCurve(curve, *(output.end() - 2), output.back());
            }

            float compressRatio = (float)keyLst.Count / (float)keycnt * 100.0F;
            if( m_Verbose )
                Dbg.Log("compressRatio = {0}%, {1}/{2}\t{3} : {4}", compressRatio, keyLst.Count, keycnt, curve.Binding0.path, curve.Binding0.propertyName);

            curve.keys = keyLst.ToArray();

            return compressRatio;
        }

        private bool _CanReduce(_Keyframe fromKey, _Keyframe toKey, _AnimCurve curve,
            float allowedError, float delta,
            int firstKey, int lastKey, bool useKeyframeLimit)
        {
            float beginTime = fromKey.time;
            float endTime = toKey.time;

            bool canReduce = true;

            for (float t = beginTime + delta; t < endTime; t += delta)
            {
                if (!(canReduce = _CanReduce(curve, fromKey, toKey, t, allowedError)))
                    break;
            }

            // we need to check that all keys can be reduced, because keys might be closer to each other than delta
            // this happens when we have steps in curve
            // TODO : we could skip the loop above if keyframes are close enough

            float lastTime = beginTime;

            for (int j = firstKey; canReduce && j < lastKey; ++j)
            {
                float time = curve.Get(j).time;

                // validates point at keyframe (j) and point between (j) and and (j-1) keyframes
                // TODO : For checking point at "time" it could just use keys[j].value instead - that would be faster than sampling the curve
                canReduce =
                    _CanReduce(curve, fromKey, toKey, time, allowedError) &&
                    _CanReduce(curve, fromKey, toKey, (lastTime + time) / 2, allowedError);

                lastTime = time;
            }

            if (canReduce)
            {
                // validate point between last two keyframes
                float time = curve.Get(lastKey).time;
                canReduce = _CanReduce(curve, fromKey, toKey, (lastTime + time) / 2, allowedError);
            }

            // Don't reduce if we are about to reduce more than 50 samples at
            // once to prevent n^2 performance impact
            canReduce = canReduce && (!useKeyframeLimit || (endTime - beginTime < 50.0F * delta));

            return canReduce;
        }

        private bool _CanReduce(_AnimCurve curve, _Keyframe key0, _Keyframe key1, float time,
            float allowedError)
        {
            float invT = Mathf.InverseLerp(key0.time, key1.time, time);
            switch (curve.KeyframeType)
            {
                case _AnimCurve.KFType.Position:
                case _AnimCurve.KFType.Scale:
                    {
                        Vector3 value = curve.Evaluate_Vector3(time);
                        Vector3 v3Key0 = (Vector3)key0;
                        Vector3 v3Key1 = (Vector3)key1;
                        Vector3 interpolatedVal = Vector3.Lerp(v3Key0, v3Key1, invT);
                        return PositionDistanceError(value, interpolatedVal, allowedError);
                    }
                case _AnimCurve.KFType.Rotation:
                    {
                        Quaternion value = curve.Evaluate_Quaternion(time);
                        Quaternion qKey0 = (Quaternion)key0;
                        Quaternion qKey1 = (Quaternion)key1;
                        Quaternion interpolatedVal = Quaternion.Lerp(qKey0, qKey1, invT);
                        return QuaternionDistanceError(value, interpolatedVal, allowedError);
                    }
                default:
                    Dbg.LogErr("AnimCompress._CanReduce: unexpected KFtype: {0}", curve.KeyframeType);
                    return false;
            }

        }

        private _AnimCurve _GatherPosCurve(EditorCurveBinding oneBinding)
        {
            AnimationCurve[] curves = new AnimationCurve[3];
            EditorCurveBinding[] bindings = new EditorCurveBinding[3];

            for(int i=0; i<3; ++i)
            {
                bindings[i] = oneBinding;
                bindings[i].propertyName = PROP_LPOS + POS_POSTFIX[i];
                curves[i] = AnimationUtility.GetEditorCurve(m_Clip, bindings[i]);
                if( curves[i] == null )
                {
                    Dbg.LogWarn("AnimCompress._GatherPosCurve: failed to get curve on binding: {0}", bindings[i]);
                    return null;
                }

                _RegBinding(bindings[i]);
            }

            _AnimCurve newCompCurve = new _AnimCurve(curves, _AnimCurve.KFType.Position, bindings, m_Clip);

            return newCompCurve;
        }

        private _AnimCurve _GatherRotCurve(EditorCurveBinding oneBinding)
        {
            AnimationCurve[] curves = new AnimationCurve[4];
            EditorCurveBinding[] bindings = new EditorCurveBinding[4];

            for (int i = 0; i < 4; ++i)
            {
                bindings[i] = oneBinding;
                bindings[i].propertyName = PROP_LROT + ROT_POSTFIX[i];
                curves[i] = AnimationUtility.GetEditorCurve(m_Clip, bindings[i]);
                if (curves[i] == null)
                {
                    Dbg.LogWarn("AnimCompress._GatherRotCurve: failed to get curve on binding: {0}", bindings[i]);
                    return null;
                }

                _RegBinding(bindings[i]);
            }

            _AnimCurve newCompCurve = new _AnimCurve(curves, _AnimCurve.KFType.Rotation, bindings, m_Clip);

            return newCompCurve;
        }

        private _AnimCurve _GatherScaleCurve(EditorCurveBinding oneBinding)
        {
            AnimationCurve[] curves = new AnimationCurve[3];
            EditorCurveBinding[] bindings = new EditorCurveBinding[3];

            for (int i = 0; i < 3; ++i)
            {
                bindings[i] = oneBinding;
                bindings[i].propertyName = PROP_LSCA + SCA_POSTFIX[i];
                curves[i] = AnimationUtility.GetEditorCurve(m_Clip, bindings[i]);
                if (curves[i] == null)
                {
                    Dbg.LogWarn("AnimCompress._GatherScaleCurve: failed to get curve on binding: {0}", bindings[i]);
                    return null;
                }

                _RegBinding(bindings[i]);
            }

            _AnimCurve newCompCurve = new _AnimCurve(curves, _AnimCurve.KFType.Scale, bindings, m_Clip);

            return newCompCurve;
        }

        private void _RegBinding(EditorCurveBinding oneBinding)
        {
            string key = oneBinding.path + "__" + oneBinding.propertyName + "__" + oneBinding.type;
            m_checkedCurves.Add(key);
        }

        private bool _HasBinding(EditorCurveBinding oneBinding)
        {
            string key = oneBinding.path + "__" + oneBinding.propertyName + "__" + oneBinding.type;
            return m_checkedCurves.Contains(key);
        }


        /// - We allow reduction if the reduced magnitude doesn't go off very far
        /// - And the angle between the two rotations is similar
        bool QuaternionDistanceError (Quaternion value, Quaternion reduced, float quaternionDotError)
        {
            float magnitude = QUtil.Magnitude(reduced);
            if (Mathf.Abs(1.0F - magnitude) > kQuaternionNormalizationError)
            {
                return false;
            }
 
            value = QUtil.Normalize(value);

            reduced.x /= magnitude;
            reduced.y /= magnitude;
            reduced.z /= magnitude;
            reduced.w /= magnitude;
   
            // float angle = Rad2Deg(acos (Dot(value, reduced))) * 2.0F;
            // if (dot > kQuaternionAngleError)
            //        return false;
            if (Quaternion.Dot(value, reduced) < quaternionDotError)
            {
                return false;
            }
 
            return true;
        }
 
        /// We allow reduction
        // - the distance of the two vectors is low
        // - the distance of each axis is low
        bool PositionDistanceError (Vector3 value, Vector3 reduced, float distancePercentageError)
        {
            float percentage = distancePercentageError;
            float minValue = kPositionMinValue * percentage;
 
            // Vector3 distance as a percentage
            float distance = (value - reduced).sqrMagnitude;
            float length = (value).sqrMagnitude;
            float lengthReduced = (reduced).sqrMagnitude;
            //if (distance > length * Sqr(percentage))
            if (DeltaError(length, lengthReduced, distance, percentage*percentage, minValue*minValue))
                return false;
 
            // Distance of each axis
            float distanceX = Mathf.Abs(value.x - reduced.x);
            float distanceY = Mathf.Abs(value.y - reduced.y);
            float distanceZ = Mathf.Abs(value.z - reduced.z);
   
            //if (distanceX > Abs(value.x) * percentage)
            if (DeltaError(value.x, reduced.x, distanceX, percentage, minValue))
                return false;
            //if (distanceY > Abs(value.y) * percentage)
            if (DeltaError(value.y, reduced.y, distanceY, percentage, minValue))
                return false;
            //if (distanceZ > Abs(value.z) * percentage)
            if (DeltaError(value.z, reduced.z, distanceZ, percentage, minValue))
                return false;
 
            return true;
        }
 
        /// We allow reduction if the distance between the two values is low
        bool FloatDistanceError (float value, float reduced, float distancePercentageError)
        {
            float percentage = distancePercentageError;
            float minValue = kPositionMinValue * percentage;
 
            float distance = Mathf.Abs(value - reduced);
            //if (distance > Abs(value) * percentage)
            if (DeltaError(value, reduced, distance, percentage, minValue))
                return false;
   
            return true;
        }

        bool DeltaError(float value, float reducedValue, float delta, float percentage, float minValue)
        {
            float absValue = Mathf.Abs(value);
            // (absValue > minValue || Abs(reducedValue) > minValue) part is necessary for reducing values which have tiny fluctuations around 0
            return (absValue > minValue || Mathf.Abs(reducedValue) > minValue) && (delta > absValue * percentage);
        }
 
        public const string PROP_LPOS = "m_LocalPosition";
        public const string PROP_LROT = "m_LocalRotation";
        public const string PROP_LSCA = "m_LocalScale";

        public static readonly string[] POS_POSTFIX = { ".x", ".y", ".z" };
        public static readonly string[] ROT_POSTFIX = { ".x", ".y", ".z", ".w" };
        public static readonly string[] SCA_POSTFIX = { ".x", ".y", ".z" };
        
    }

    // animation curve
    // the keys cnt in each curves must be the same
    class _AnimCurve
    {
        private AnimationCurve[] m_Curves;
        private Keyframe[][] m_Kf4Curves;
        private int m_KeyCnt = 0;
        private EditorCurveBinding[] m_Bindings;
        private AnimationClip m_Clip;

        private KFType m_KfType;

        private _Keyframe[] m_Keys;

        public _AnimCurve(AnimationCurve[] curves, KFType tp, EditorCurveBinding[] bindings, AnimationClip clip)
        {
            Dbg.Assert(curves.Length > 0, "_AnimCurve.ctor: curves[] has zero element");

            m_Curves = curves;
            m_Clip = clip;

            m_KfType = tp;
            m_KeyCnt = m_Curves[0].length;

            //Dbg.Log("{0}:{1}    keyCnt:{2}", binding.path, binding.propertyName, m_KeyCnt);

            m_Bindings = bindings;

            // check key count
            for (int i = 0; i < m_Curves.Length; ++i )
            {
                if( m_Curves[i].length != m_KeyCnt )
                {
                    Dbg.LogWarn("_AnimCurve.ctor: the key count not match other curves: {0} != {1}, {2}:{3}", m_Curves[i], m_KeyCnt, m_Bindings[0].path, m_Bindings[0].propertyName);
                }
            }

            // assign keys
            m_Kf4Curves = new Keyframe[m_Curves.Length][];
            for(int i=0; i<m_Curves.Length; ++i)
            {
                m_Kf4Curves[i] = m_Curves[i].keys;
            }

            Dbg.Assert(m_KeyCnt >= 2, "_AnimCurve.ctor: curve has less than 2 keys: {0}", m_Curves[0]);

            // init _KeyFrame array
            _InitKeyframes();
        }

        public _Keyframe[] keys
        { 
            get { return m_Keys; } 
            set { 
                m_Keys = value;
                m_KeyCnt = m_Keys.Length;
                ApplyToUnderlyingCurves();
            }
        }

        public KFType KeyframeType { get { return m_KfType; } }

        public int KeyCnt { get { return m_KeyCnt; } }

        public EditorCurveBinding Binding0 { get { return m_Bindings[0]; } }

        // apply the data from _Keyframe[] to stored AnimationCurve[]
        public void ApplyToUnderlyingCurves()
        {
            for(int curveIdx = 0; curveIdx < m_Curves.Length; ++curveIdx )
            {
                AnimationCurve ac = m_Curves[curveIdx];

                Keyframe[] tmpKeys = new Keyframe[m_KeyCnt];
                for( int keyIdx = 0; keyIdx < m_KeyCnt; ++keyIdx )
                {
                    _Keyframe compKey = m_Keys[keyIdx];
                    tmpKeys[keyIdx] = compKey.Get(curveIdx);
                }

                ac.keys = tmpKeys;
                //Dbg.Log("ac.length = {0}", ac.length);
                AnimationUtility.SetEditorCurve(m_Clip, m_Bindings[curveIdx], ac); //set back
            }
        }

        public _Keyframe Get(int idx)
        {
            Dbg.Assert(idx < m_KeyCnt, "_AnimCurve.Get: idx > keyCnt: {0}, {1}", idx, m_KeyCnt);
            return m_Keys[idx];
        }

        public Vector3 Evaluate_Vector3 (float time)
        {
            float x = m_Curves[0].Evaluate(time);
            float y = m_Curves[1].Evaluate(time);
            float z = m_Curves[2].Evaluate(time);

            return new Vector3(x, y, z);
        }

        public Quaternion Evaluate_Quaternion(float time)
        {
            float x = m_Curves[0].Evaluate(time);
            float y = m_Curves[1].Evaluate(time);
            float z = m_Curves[2].Evaluate(time);
            float w = m_Curves[3].Evaluate(time);

            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// create an array of _Keyframe
        /// </summary>
        private void _InitKeyframes()
        {
            m_Keys = new _Keyframe[m_KeyCnt];

            for(int i=0; i<m_KeyCnt; ++i) //each key
            {
                var k = m_Keys[i] = new _Keyframe(m_Curves.Length);
                
                for( int cidx = 0; cidx < m_Curves.Length; ++cidx ) //aggregate keys from each curve
                {
                    k.Set(cidx, m_Kf4Curves[cidx][i]);
                }
            }
        }

        public enum KFType {
            Position,
            Rotation,
            Scale,
        }


    }

    // key frame
    class _Keyframe
    {
        public Keyframe[] m_Kfs;

        public _Keyframe(int cnt)
        {
            m_Kfs = new Keyframe[cnt];
        }

        public _Keyframe Clone()
        {
            int len = m_Kfs.Length;
            _Keyframe newKf = new _Keyframe(len);
            for( int i=0; i<len; ++i )
            {
                newKf.m_Kfs[i] = m_Kfs[i];
            }
            return newKf;
        }

        public void Set(int idx, Keyframe kf)
        {
            m_Kfs[idx] = kf;
        }

        public Keyframe Get(int idx)
        {
            return m_Kfs[idx];
        }

        public Keyframe[] value
        {
            get { 
                Keyframe[] newKfs = new Keyframe[m_Kfs.Length];
                for( int i=0; i<m_Kfs.Length; ++i)
                {
                    newKfs[i] = m_Kfs[i];
                }
                return newKfs;
            }
            set{
                for(int i=0; i<m_Kfs.Length; ++i)
                {
                    m_Kfs[i].value = value[i].value;
                }
            }
        }

        public float time
        {
            get { return m_Kfs[0].time; }
        }

        public float inTangent 
        {
            get { return m_Kfs[0].inTangent; }
            set { 
                for( int i=0; i<m_Kfs.Length; ++i )
                {
                    m_Kfs[i].inTangent = value;
                }
            }
        }

        public float outTangent
        {
            get { return m_Kfs[0].outTangent; }
            set
            {
                for (int i = 0; i < m_Kfs.Length; ++i)
                {
                    m_Kfs[i].outTangent = value;
                }
            }
        }

        public static explicit operator Vector3(_Keyframe x) 
        {
            var d = x.m_Kfs;
            return new Vector3(d[0].value, d[1].value, d[2].value);
        }

        public static explicit operator Quaternion(_Keyframe x)
        {
            var d = x.m_Kfs;
            return new Quaternion(d[0].value, d[1].value, d[2].value, d[3].value);
        }
    }


}
