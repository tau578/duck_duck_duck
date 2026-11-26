using UnityEngine;

namespace Duck
{
    /// <summary>
    /// Procedural background music (no audio files): a soft 4-chord loop with bass and a plucked arpeggio,
    /// synthesized once at startup. Volume follows GameSettings.MusicVolume.
    /// </summary>
    public class Music : MonoBehaviour
    {
        const int Rate = 22050;
        const float Bpm = 100f;
        public float baseVolume = 0.22f;

        AudioSource source;
        static AudioClip clip;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            if (clip == null) clip = Build();
            source.clip = clip;
            Apply();
            source.Play();
            GameSettings.Changed += Apply;
        }

        void OnDestroy() { GameSettings.Changed -= Apply; }

        void Apply() { if (source != null) source.volume = baseVolume * GameSettings.MusicVolume; }

        static AudioClip Build()
        {
            // Am - F - C - G, one bar (4 beats) each.
            int[][] chords = { new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 48, 52, 55 }, new[] { 55, 59, 62 } };
            float beat = 60f / Bpm;
            float bar = beat * 4f;
            int total = Mathf.RoundToInt(bar * chords.Length * Rate);
            var data = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = i / (float)Rate;
                int ci = Mathf.Min(chords.Length - 1, (int)(t / bar));
                var ch = chords[ci];
                float inBar = t - ci * bar;

                // Bass: root an octave down, on every beat with a short decay.
                float bt = inBar % beat;
                float bass = Mathf.Sin(2f * Mathf.PI * Freq(ch[0] - 12) * t) * Mathf.Exp(-bt * 3f) * 0.5f;

                // Pad: quiet sustained chord with slow swell.
                float pad = 0f;
                for (int k = 0; k < 3; k++) pad += Mathf.Sin(2f * Mathf.PI * Freq(ch[k]) * t);
                pad *= 0.08f * (0.6f + 0.4f * Mathf.Sin(Mathf.PI * inBar / bar));

                // Arpeggio: eighth notes up the chord, plucked.
                float eighth = beat * 0.5f;
                int step = (int)(inBar / eighth);
                float at = inBar - step * eighth;
                int note = ch[step % 3] + (step % 6 >= 3 ? 12 : 0);
                float arp = Tri(Freq(note) * t) * Mathf.Exp(-at * 9f) * 0.22f;

                data[i] = Mathf.Clamp((bass + pad + arp) * 0.8f, -1f, 1f);
            }
            var c = AudioClip.Create("music_loop", total, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static float Freq(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        static float Tri(float phase) { float p = phase - Mathf.Floor(phase); return 4f * Mathf.Abs(p - 0.5f) - 1f; }
    }
}
