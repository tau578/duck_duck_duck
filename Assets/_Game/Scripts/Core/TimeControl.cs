namespace Duck
{
    /// <summary>Single owner of Time.timeScale: pause (skill choice) wins over hit-stop.</summary>
    public static class TimeControl
    {
        static int pauseCount;
        static bool hitStop;

        public static bool Paused => pauseCount > 0;

        public static void Pause() { pauseCount++; Apply(); }
        public static void Resume() { if (pauseCount > 0) pauseCount--; Apply(); }
        public static void SetHitStop(bool on) { hitStop = on; Apply(); }
        public static void Reset() { pauseCount = 0; hitStop = false; Apply(); }

        static void Apply()
        {
            UnityEngine.Time.timeScale = pauseCount > 0 ? 0f : hitStop ? 0.05f : 1f;
        }
    }
}
