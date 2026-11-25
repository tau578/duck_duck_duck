using UnityEngine;

namespace Duck
{
    /// <summary>Global runtime settings applied once when the game scene starts.</summary>
    public class GameBootstrap : MonoBehaviour
    {
        public int targetFrameRate = 60;

        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}
