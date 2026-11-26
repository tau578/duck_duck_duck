using System;
using UnityEngine;

namespace Duck
{
    /// <summary>
    /// Player settings (PlayerPrefs): sound volume plus accessibility options.
    /// Read by Sfx, TimeControl, CameraFollow, DamageVignette, HitStop, text pop-ups and bars.
    /// </summary>
    public static class GameSettings
    {
        public static event Action Changed;

        static readonly float[] VolumeSteps = { 1f, 0.75f, 0.5f, 0.25f, 0f };

        public static float SfxVolume { get; private set; } = 1f;
        /// <summary>Bigger pop-up numbers, slot labels and banners.</summary>
        public static bool LargeText { get; private set; }
        /// <summary>Blue/orange instead of green/red on bars and range rings.</summary>
        public static bool ColorBlind { get; private set; }
        /// <summary>Almost no camera shake, no hit-stop, softer red screen flashes.</summary>
        public static bool ReduceMotion { get; private set; }
        /// <summary>Whole game runs at 75% speed: more time to react.</summary>
        public static bool SlowMode { get; private set; }

        public static float TextScale => LargeText ? 1.35f : 1f;
        public static float GameSpeed => SlowMode ? 0.75f : 1f;
        public static float ShakeScale => ReduceMotion ? 0.15f : 1f;
        public static float FlashScale => ReduceMotion ? 0.3f : 1f;

        static bool loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() => Load();

        public static void Load()
        {
            loaded = true;
            SfxVolume = PlayerPrefs.GetFloat("set_sfx", 1f);
            LargeText = PlayerPrefs.GetInt("set_large", 0) == 1;
            ColorBlind = PlayerPrefs.GetInt("set_cb", 0) == 1;
            ReduceMotion = PlayerPrefs.GetInt("set_motion", 0) == 1;
            SlowMode = PlayerPrefs.GetInt("set_slow", 0) == 1;
        }

        static void Save()
        {
            PlayerPrefs.SetFloat("set_sfx", SfxVolume);
            PlayerPrefs.SetInt("set_large", LargeText ? 1 : 0);
            PlayerPrefs.SetInt("set_cb", ColorBlind ? 1 : 0);
            PlayerPrefs.SetInt("set_motion", ReduceMotion ? 1 : 0);
            PlayerPrefs.SetInt("set_slow", SlowMode ? 1 : 0);
            PlayerPrefs.Save();
            TimeControl.Refresh();
            Changed?.Invoke();
        }

        static float NextVolume(float v)
        {
            for (int i = 0; i < VolumeSteps.Length; i++)
                if (Mathf.Abs(VolumeSteps[i] - v) < 0.01f) return VolumeSteps[(i + 1) % VolumeSteps.Length];
            return 1f;
        }

        public static void CycleSfx() { SfxVolume = NextVolume(SfxVolume); Save(); }
        public static void ToggleLargeText() { LargeText = !LargeText; Save(); }
        public static void ToggleColorBlind() { ColorBlind = !ColorBlind; Save(); }
        public static void ToggleReduceMotion() { ReduceMotion = !ReduceMotion; Save(); }
        public static void ToggleSlowMode() { SlowMode = !SlowMode; Save(); }

        /// <summary>Color-blind safe palette: greens become blue, reds become orange.</summary>
        public static Color Safe(Color c)
        {
            if (!loaded) Load();
            if (!ColorBlind) return c;
            if (c.g > c.r * 1.2f && c.g >= c.b) return new Color(0.25f, 0.6f, 1f, c.a);
            if (c.r > c.g * 1.5f && c.r > c.b) return new Color(1f, 0.55f, 0.05f, c.a);
            return c;
        }
    }
}
