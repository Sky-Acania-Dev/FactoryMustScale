using System;
using System.Collections;
using UnityEngine;

namespace FactoryMustScale.Visualization
{
    /// <summary>
    /// Unity adapter that invokes a callback once every fixed interval using a coroutine.
    /// Intended for driving one simulation tick every 0.25 seconds in PlayMode.
    /// </summary>
    public sealed class MinimalFactoryTickCoroutineDriver : MonoBehaviour
    {
        [SerializeField]
        private float _tickIntervalSeconds = 0.25f;

        private Action _onTick;
        private Coroutine _tickLoopCoroutine;

        public float TickIntervalSeconds => _tickIntervalSeconds;

        public bool IsRunning => _tickLoopCoroutine != null;

        public void StartTickLoop(Action onTick)
        {
            _onTick = onTick;

            if (_tickLoopCoroutine != null)
            {
                StopCoroutine(_tickLoopCoroutine);
            }

            _tickLoopCoroutine = StartCoroutine(TickLoop());
        }

        public void StopTickLoop()
        {
            if (_tickLoopCoroutine == null)
            {
                return;
            }

            StopCoroutine(_tickLoopCoroutine);
            _tickLoopCoroutine = null;
        }

        private IEnumerator TickLoop()
        {
            float interval = _tickIntervalSeconds > 0f ? _tickIntervalSeconds : 0.001f;
            float nextTickTime = Time.realtimeSinceStartup;

            while (enabled)
            {
                float now = Time.realtimeSinceStartup;
                while (now >= nextTickTime)
                {
                    _onTick?.Invoke();
                    nextTickTime += interval;
                }

                yield return null;
            }

            _tickLoopCoroutine = null;
        }

        private void OnDisable()
        {
            StopTickLoop();
        }
    }
}
