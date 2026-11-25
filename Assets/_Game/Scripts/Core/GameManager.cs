using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Duck
{
    public enum GamePhase { Menu, Day, Night, Victory, Defeat }

    /// <summary>
    /// Battle loop of one chapter: Day (collect coins, build) -> Night (wave) -> Day ... -> Victory after the last wave.
    /// Defeat when the base is destroyed.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameAssets assets;
        public ChapterConfig[] chapters;
        public ChapterConfig chapter;
        public WaveSpawner spawner;
        public BuildSlot baseSlot;
        public List<SpawnIndicator> indicators = new List<SpawnIndicator>();
        public Light sun;
        public float buildReach = 2.2f;
        [Tooltip("Seconds of night before enemies enrage (+50% speed).")]
        public float rageAfter = 100f;

        public GamePhase Phase { get; private set; }
        public int ChapterIndex { get; private set; }
        public int ChapterNumber => ChapterIndex + 1;
        public bool HasNextChapter => chapters != null && ChapterIndex + 1 < chapters.Length;
        public int LastReward { get; private set; }
        public int WaveIndex { get; private set; }
        public int WaveCount => chapter != null && chapter.waves != null ? chapter.waves.Length : 0;
        public float NightTime { get; private set; }
        public WaveConfig CurrentWave => WaveIndex < WaveCount ? chapter.waves[WaveIndex] : null;

        public event Action<GamePhase> PhaseChanged;

        readonly List<BuildSlot> slots = new List<BuildSlot>();
        Coroutine lightRoutine;

        void Awake()
        {
            Instance = this;
            GameAssets.I = assets;
            HitStop.ResetState();
            Enemy.SpeedMultiplier = 1f;
            TimeControl.Reset();
        }

        void Start()
        {
            slots.AddRange(FindObjectsByType<BuildSlot>(FindObjectsSortMode.None));
            foreach (var ind in indicators) ind.Hide();
            int pending = GameSession.PendingChapter;
            GameSession.PendingChapter = -1;
            if (pending >= 0 && chapters != null && pending < chapters.Length) BeginChapter(pending);
            else ShowMenu();
        }

        void ShowMenu()
        {
            SetPhase(GamePhase.Menu);
            foreach (var s in slots) s.SetDayMode(false);
            if (MenuUI.Instance != null) MenuUI.Instance.Show();
        }

        /// <summary>Called by the menu: reload the scene clean and start the chapter.</summary>
        public void PlayChapter(int index)
        {
            GameSession.PendingChapter = index;
            TimeControl.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void BeginChapter(int index)
        {
            ChapterIndex = index;
            chapter = chapters[index];
            spawner.hpMultiplier = chapter.enemyHpMultiplier;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i].minChapter <= ChapterNumber) continue;
                slots[i].gameObject.SetActive(false);
                slots.RemoveAt(i);
            }
            Wallet.Instance.Set(chapter.startCoins + MetaUpgrades.BonusStartCoins);
            EnterDay(true);
        }

        void Update()
        {
            if (Phase == GamePhase.Day)
            {
                UpdateBuildTarget();
            }
            else if (Phase == GamePhase.Night)
            {
                NightTime += Time.deltaTime;
                if (Enemy.SpeedMultiplier < 1.5f && NightTime > rageAfter)
                {
                    Enemy.SpeedMultiplier = 1.5f;
                    HUD.Instance?.Banner(Loc.T("banner.rage"), new Color(1f, 0.25f, 0.1f), 2.5f);
                    CameraFollow.Shake(0.4f);
                }
                if (NightTime > 1f && !spawner.IsSpawning && EnemyManager.Instance.AliveCount == 0)
                    WaveCleared();
            }
        }

        // ---------- phases ----------

        void EnterDay(bool first)
        {
            SetPhase(GamePhase.Day);
            Enemy.SpeedMultiplier = 1f;
            foreach (var s in slots) s.SetDayMode(true);
            ShowIndicators(CurrentWave);
            Lighting(true);

            if (first)
            {
                string news = string.IsNullOrEmpty(chapter.News) ? "" : "\n" + Loc.F("banner.news", chapter.News);
                HUD.Instance?.Banner(Loc.F("banner.start", chapter.Name) + news, Color.white, 4.5f);
                return;
            }

            int income = 0;
            foreach (var s in slots)
            {
                if (s.Current == null) continue;
                s.OnDawn();
                int inc = s.Current.Income;
                if (inc > 0)
                {
                    income += inc;
                    DamageNumbers.ShowText(s.transform.position + Vector3.up * 2f, $"+{inc}", new Color(1f, 0.85f, 0.2f), 56);
                }
            }
            Wallet.Instance.Add(income);
            PlayerHealth.Instance?.HealFull();
            Sfx.Play(SfxId.Coin, 1f, 0f, 0f);
            HUD.Instance?.Banner(Loc.F("banner.dawn", income, WaveIndex + 1, WaveCount), new Color(1f, 0.9f, 0.4f), 3f);
        }

        public void StartNight()
        {
            if (Phase != GamePhase.Day || CurrentWave == null) return;
            SetPhase(GamePhase.Night);
            NightTime = 0f;
            foreach (var s in slots) s.SetDayMode(false);
            foreach (var ind in indicators) ind.Hide();
            Lighting(false);
            spawner.StartWave(CurrentWave);
            HUD.Instance?.SetBuildTarget(null);

            switch (CurrentWave.kind)
            {
                case WaveKind.Boss:
                    HUD.Instance?.Banner(Loc.T("banner.boss"), new Color(1f, 0.2f, 0.2f), 2.5f);
                    CameraFollow.Shake(0.5f);
                    break;
                case WaveKind.Elite:
                    HUD.Instance?.Banner(Loc.T("banner.elite"), new Color(0.8f, 0.4f, 1f), 2.5f);
                    break;
                default:
                    HUD.Instance?.Banner(Loc.T("banner.attack"), new Color(1f, 0.4f, 0.3f), 2f);
                    break;
            }
        }

        void WaveCleared()
        {
            WaveIndex++;
            if (WaveIndex >= WaveCount)
            {
                SetPhase(GamePhase.Victory);
                Lighting(true);
                GrantReward(true);
                Sfx.Play(SfxId.Build, 1f, 0f, 0f);
                HUD.Instance?.ShowResult(true);
                return;
            }
            EnterDay(false);
        }

        public void OnBaseDestroyed()
        {
            if (Phase == GamePhase.Victory || Phase == GamePhase.Defeat) return;
            SetPhase(GamePhase.Defeat);
            spawner.StopAll();
            GrantReward(false);
            CameraFollow.Shake(1f);
            HitStop.Do(0.25f);
            HUD.Instance?.ShowResult(false);
        }

        void GrantReward(bool victory)
        {
            var save = SaveSystem.Data;
            int reward = victory
                ? chapter.rewardGears
                : Mathf.Max(5, Mathf.RoundToInt(chapter.rewardGears * 0.5f * WaveIndex / Mathf.Max(1, WaveCount)));
            LastReward = reward;
            save.gears += reward;
            if (victory && save.unlockedChapter < ChapterNumber + 1) save.unlockedChapter = ChapterNumber + 1;
            if (save.bestWaves.Length < chapters.Length) System.Array.Resize(ref save.bestWaves, chapters.Length);
            int waves = victory ? WaveCount : WaveIndex;
            if (waves > save.bestWaves[ChapterIndex]) save.bestWaves[ChapterIndex] = waves;
            SaveSystem.Save();
        }

        public void Restart() => PlayChapter(ChapterIndex);
        public void NextChapter() => PlayChapter(HasNextChapter ? ChapterIndex + 1 : ChapterIndex);

        public void ToMenu()
        {
            GameSession.PendingChapter = -1;
            TimeControl.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void SetPhase(GamePhase p)
        {
            Phase = p;
            PhaseChanged?.Invoke(p);
        }

        // ---------- building ----------

        BuildSlot buildTarget;

        void UpdateBuildTarget()
        {
            var duck = DuckController.Instance;
            BuildSlot best = null;
            if (duck != null && PlayerHealth.Instance != null && PlayerHealth.Instance.IsAlive)
            {
                float bestSq = buildReach * buildReach;
                foreach (var s in slots)
                {
                    if (!s.CanUpgrade) continue;
                    Vector3 d = s.transform.position - duck.transform.position;
                    d.y = 0f;
                    float reach = s.Current != null ? s.Current.Config.halfExtents.magnitude : 0f;
                    float sq = Mathf.Max(0f, d.magnitude - reach);
                    sq *= sq;
                    if (sq < bestSq) { bestSq = sq; best = s; }
                }
            }
            buildTarget = best;
            HUD.Instance?.SetBuildTarget(best);
        }

        public void BuildPressed()
        {
            if (Phase != GamePhase.Day || buildTarget == null) return;
            if (!buildTarget.TryUpgrade())
            {
                Sfx.Play(SfxId.Denied, 0.8f, 0f, 0.1f);
                HUD.Instance?.NotEnoughCoins();
            }
        }

        // ---------- presentation ----------

        void ShowIndicators(WaveConfig wave)
        {
            for (int i = 0; i < indicators.Count; i++)
                indicators[i].Show(wave != null ? wave.CountOnPath(i) : 0);
        }

        void Lighting(bool day)
        {
            if (lightRoutine != null) StopCoroutine(lightRoutine);
            lightRoutine = StartCoroutine(LerpLighting(day));
        }

        IEnumerator LerpLighting(bool day)
        {
            Color sunFrom = sun != null ? sun.color : Color.white;
            float intFrom = sun != null ? sun.intensity : 1f;
            Color skyFrom = RenderSettings.ambientSkyColor;
            Color eqFrom = RenderSettings.ambientEquatorColor;

            Color sunTo = day ? new Color(1f, 0.96f, 0.88f) : new Color(0.55f, 0.6f, 1f);
            float intTo = day ? 1.2f : 0.55f;
            Color skyTo = day ? new Color(0.75f, 0.8f, 0.9f) : new Color(0.2f, 0.22f, 0.45f);
            Color eqTo = day ? new Color(0.55f, 0.55f, 0.5f) : new Color(0.15f, 0.15f, 0.3f);

            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.5f)
            {
                if (sun != null)
                {
                    sun.color = Color.Lerp(sunFrom, sunTo, t);
                    sun.intensity = Mathf.Lerp(intFrom, intTo, t);
                }
                RenderSettings.ambientSkyColor = Color.Lerp(skyFrom, skyTo, t);
                RenderSettings.ambientEquatorColor = Color.Lerp(eqFrom, eqTo, t);
                yield return null;
            }
            if (sun != null) { sun.color = sunTo; sun.intensity = intTo; }
            RenderSettings.ambientSkyColor = skyTo;
            RenderSettings.ambientEquatorColor = eqTo;
        }
    }
}
