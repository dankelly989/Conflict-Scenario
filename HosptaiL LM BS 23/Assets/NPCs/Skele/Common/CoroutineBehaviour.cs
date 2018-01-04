using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtMethods;

namespace MH
{
    /// <summary>
    /// helpers to install coroutine on other GO
    /// </summary>
    public class CoroutineBehaviour : MonoBehaviour
    {
        public static Coroutine StartCoroutine(GameObject go, IEnumerator coFunc, float delay = 0)
        {
            var co = go.ForceGetComponent<CoroutineBehaviour>();
            return co.StartCoroutine(_DelayWrapper(coFunc, delay));
        }

        public static Coroutine StartCoroutine(GameObject go, Func<bool> normalFunc, bool continueWithTrue = true)
        {
            var co = go.ForceGetComponent<CoroutineBehaviour>();
            return co.StartCoroutine(_LoopWrapper(normalFunc, continueWithTrue));
        }

        
        public static Coroutine StartCoroutineDelay(GameObject go, System.Action normalFunc, float delay = 0)
        {
            var co = go.ForceGetComponent<CoroutineBehaviour>();
            return co.StartCoroutine(_DelayWrapper(normalFunc, delay));
        }

        public static Coroutine StartCoroutineDelay(GameObject go, System.Action<GameObject> normalFunc, float delay = 0)
        {
            var co = go.ForceGetComponent<CoroutineBehaviour>();
            return co.StartCoroutine(_DelayWrapper(normalFunc, go, delay));
        }

        public static void StopAllCoroutines(GameObject go)
        {
            var cp = go.GetComponent<CoroutineBehaviour>();
            cp.StopAllCoroutines();
        }

        public static void StopCoroutine(GameObject go, Coroutine co)
        {
            var cp = go.GetComponent<CoroutineBehaviour>();
            if (cp != null)
            {
                cp.StopCoroutine(co);
            }
        }


        private static IEnumerator _DelayWrapper(System.Action func, float delay = 0)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);
            func();
        }

        private static IEnumerator _DelayWrapper(System.Action<GameObject> func, GameObject go, float delay = 0)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);
            func(go);
        }

        private static IEnumerator _DelayWrapper(IEnumerator func, float delay = 0)
        {
            if( delay > 0 )
                yield return new WaitForSeconds(delay);
            yield return func;
        }

        private static IEnumerator _LoopWrapper(Func<bool> normalFunc, bool continueWithTrue = true, float delay = 0f)
        {
            if( delay > 0 )
                yield return new WaitForSeconds(delay);

            bool ret = false;
            do
            {
                ret = normalFunc();
                yield return 0;
            } while (ret == continueWithTrue);
        }

    }
}
