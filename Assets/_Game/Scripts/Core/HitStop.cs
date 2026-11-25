using System.Collections;
using UnityEngine;

namespace Duck
{
    /// <summary>Short freeze-frame for heavy kills.</summary>
    public static class HitStop
    {
        static bool active;

        /// <summary>Called on scene start: a coroutine killed by a scene reload must not leave the flag stuck.</summary>
        public static void ResetState() { active = false; }

        public static void Do(float realSeconds)
        {
            if (active || TimeControl.Paused || GameManager.Instance == null) return;
            GameManager.Instance.StartCoroutine(Run(realSeconds));
        }

        static IEnumerator Run(float t)
        {
            active = true;
            TimeControl.SetHitStop(true);
            yield return new WaitForSecondsRealtime(t);
            TimeControl.SetHitStop(false);
            active = false;
        }
    }
}
