using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    public enum SfxId { Shot, Hit, EnemyDie, Coin, Hurt, Build, Explosion, Heartbeat, TowerShot, Denied }

    /// <summary>
    /// Procedurally generated sound effects (no audio files needed): noise bursts and chirps
    /// synthesized at startup. Plays through a small pool of AudioSources with pitch variation.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        static Sfx instance;
        const int Rate = 44100;

        readonly Dictionary<SfxId, AudioClip> clips = new Dictionary<SfxId, AudioClip>();
        readonly Dictionary<SfxId, float> lastPlayed = new Dictionary<SfxId, float>();
        AudioSource[] sources;
        int next;

        public float masterVolume = 0.7f;

        void Awake()
        {
            instance = this;
            sources = new AudioSource[12];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 0f;
            }

            clips[SfxId.Shot] = Make("shot", 0.09f, (t, n) => (Noise() * 0.6f + Mathf.Sin(t * 2f * Mathf.PI * Mathf.Lerp(180f, 60f, n)) * 0.8f) * Mathf.Pow(1f - n, 3f));
            clips[SfxId.TowerShot] = Make("tower", 0.07f, (t, n) => (Noise() * 0.5f + Square(t, Mathf.Lerp(420f, 200f, n)) * 0.3f) * Mathf.Pow(1f - n, 2.5f));
            clips[SfxId.Hit] = Make("hit", 0.05f, (t, n) => Noise() * Mathf.Pow(1f - n, 4f) * 0.7f);
            clips[SfxId.EnemyDie] = Make("die", 0.18f, (t, n) => (Noise() * 0.4f + Mathf.Sin(t * 2f * Mathf.PI * Mathf.Lerp(300f, 80f, n))) * (1f - n) * 0.6f);
            clips[SfxId.Coin] = Make("coin", 0.12f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * (n < 0.4f ? 1320f : 1760f)) * (1f - n) * 0.5f);
            clips[SfxId.Hurt] = Make("hurt", 0.2f, (t, n) => Square(t, Mathf.Lerp(240f, 110f, n)) * (1f - n) * 0.45f);
            clips[SfxId.Build] = Make("build", 0.25f, (t, n) => (Mathf.Sin(t * 2f * Mathf.PI * Mathf.Lerp(300f, 900f, n)) * 0.6f + Noise() * 0.15f * (1f - n)) * (1f - n));
            clips[SfxId.Explosion] = Make("boom", 0.6f, LowNoise(0.6f));
            clips[SfxId.Heartbeat] = Make("heart", 0.45f, (t, n) =>
            {
                float a = Mathf.Exp(-Mathf.Pow((t - 0.05f) * 40f, 2f)) + 0.7f * Mathf.Exp(-Mathf.Pow((t - 0.25f) * 40f, 2f));
                return Mathf.Sin(t * 2f * Mathf.PI * 55f) * a;
            });
            clips[SfxId.Denied] = Make("denied", 0.15f, (t, n) => Square(t, 140f) * (1f - n) * 0.35f);
        }

        public static void Play(SfxId id, float volume = 1f, float pitchJitter = 0.08f, float minInterval = 0.03f)
        {
            if (instance == null) return;
            instance.PlayInternal(id, volume, pitchJitter, minInterval);
        }

        void PlayInternal(SfxId id, float volume, float pitchJitter, float minInterval)
        {
            if (!clips.TryGetValue(id, out var clip)) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < minInterval) return;
            lastPlayed[id] = now;

            var src = sources[next];
            next = (next + 1) % sources.Length;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(clip, volume * masterVolume);
        }

        // ---------- synthesis ----------

        delegate float Synth(float t, float normalized);

        static float Noise() => Random.value * 2f - 1f;
        static float Square(float t, float freq) => Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * freq));

        static Synth LowNoise(float length)
        {
            float prev = 0f;
            return (t, n) =>
            {
                prev = Mathf.Lerp(prev, Noise(), 0.08f);
                return (prev * 3f + Mathf.Sin(t * 2f * Mathf.PI * 45f) * 0.5f * (1f - n)) * Mathf.Pow(1f - n, 2f);
            };
        }

        static AudioClip Make(string name, float seconds, Synth synth)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                data[i] = Mathf.Clamp(synth(t, i / (float)count), -1f, 1f);
            }
            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
