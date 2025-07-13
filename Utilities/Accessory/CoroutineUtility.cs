using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BitterCMS.UnityIntegration.Utility
{
    public class CoroutineUtility : MonoBehaviour
    {
        private static CoroutineUtility _instance;
        private static CoroutineUtility Instance {
            get {
                if (_instance != null)
                    return _instance;

                var manager = new GameObject("[CoroutineUtility]").AddComponent<CoroutineUtility>();
                DontDestroyOnLoad(manager.gameObject);
                _instance = manager;

                return _instance;
            }
        }

        private readonly Dictionary<IEnumerator, Coroutine> _activeCoroutines = new Dictionary<IEnumerator, Coroutine>();
        private readonly List<Action> _allStopCallbacks = new List<Action>();
        private readonly List<Action<IEnumerator>> _singleStopCallbacks = new List<Action<IEnumerator>>();

        #region [Public Methods]
        public static Coroutine Run(IEnumerator coroutine)
        {
            return Instance.CRun(coroutine);
        }

        public static void Stop(IEnumerator coroutine)
        {
            Instance.CStop(coroutine);
        }

        public static void StopAll()
        {
            Instance.CStopAll();
        }

        public static void SubscribeToAllStop(Action callback)
        {
            Instance.CSubscribeToAllStop(callback);
        }

        public static void SubscribeToStopCoroutine(Action<IEnumerator> callback)
        {
            Instance.CSubscribeToStopCoroutine(callback);
        }

        public static void UnsubscribeAllCoroutine()
        {
            Instance.CUnsubscribeAllCoroutine();
        }
        #endregion
        
        #region [Private Methods]
        private Coroutine CRun(IEnumerator coroutine)
        {
            var coroutineInstance = StartCoroutine(ExecuteCoroutine(coroutine));
            _activeCoroutines.Add(coroutine, coroutineInstance);
            return coroutineInstance;
        }

        private void CStop(IEnumerator coroutine)
        {
            if (!_activeCoroutines.TryGetValue(coroutine, out var coroutineInstance))
                return;

            StopCoroutine(coroutineInstance);
            _activeCoroutines.Remove(coroutine);
            NotifyCoroutineStopped(coroutine);
        }

        private void CStopAll()
        {
            foreach (var coroutine in _activeCoroutines.Values)
            {
                StopCoroutine(coroutine);
            }

            var stoppedCoroutines = new List<IEnumerator>(_activeCoroutines.Keys);
            _activeCoroutines.Clear();

            foreach (var coroutine in stoppedCoroutines)
            {
                NotifyCoroutineStopped(coroutine);
            }

            NotifyAllCoroutinesStopped();
        }

        private void CSubscribeToAllStop(Action callback)
        {
            if (callback == null) return;
            _allStopCallbacks.Add(callback);
        }

        private void CSubscribeToStopCoroutine(Action<IEnumerator> callback)
        {
            if (callback == null) return;
            _singleStopCallbacks.Add(callback);
        }

        private void CUnsubscribeAllCoroutine()
        {
            _allStopCallbacks.Clear();
            _singleStopCallbacks.Clear();
        }

        private IEnumerator ExecuteCoroutine(IEnumerator coroutine)
        {
            yield return coroutine;

            if (!_activeCoroutines.ContainsKey(coroutine))
                yield break;

            _activeCoroutines.Remove(coroutine);
            NotifyCoroutineStopped(coroutine);
        }

        private void NotifyCoroutineStopped(IEnumerator coroutine)
        {
            foreach (var callback in _singleStopCallbacks)
            {
                callback?.Invoke(coroutine);
            }

            if (_activeCoroutines.Count == 0)
            {
                NotifyAllCoroutinesStopped();
            }
        }

        private void NotifyAllCoroutinesStopped()
        {
            foreach (var callback in _allStopCallbacks)
            {
                callback?.Invoke();
            }
        }
        #endregion
    }
}
