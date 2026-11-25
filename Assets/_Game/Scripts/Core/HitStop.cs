using System.Collections;
using UnityEngine;

namespace Duck
{
    /// <summary>Short freeze-frame for heavy kills.</summary>
    public static class HitStop
    {
        static bool active;

        public static void Do(float realSeconds)
        {
            if (active || GameManager.Instance == null) return;
            GameManager.Instance.StartCoroutine(Run(realSeconds));
        }

        static IEnumerator Run(float t)
        {
            active = true;
            float prev = Time.timeScale;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(t);
            Time.timeScale = prev <= 0.05f ? 1f : prev;
            active = false;
        }
    }
}
