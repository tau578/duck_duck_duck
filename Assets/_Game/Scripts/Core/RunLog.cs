using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Duck
{
    /// <summary>
    /// Playtest journal for balancing: one line per night (how long it took, how close the base and the duck
    /// came to falling, what was built and picked) and one per finished chapter. Appended to runs.log in
    /// Application.persistentDataPath and mirrored to the console with the [DuckRun] tag.
    /// </summary>
    public class RunLog : MonoBehaviour
    {
        const string FileName = "runs.log";

        GameManager gm;
        bool inNight, wasGhost;
        int night, coins, deaths;
        float baseMin, duckMin, chapterStart, fightTime;
        string built;

        void Awake()
        {
            gm = GetComponent<GameManager>();
            gm.PhaseChanged += OnPhase;
        }

        void OnDestroy()
        {
            if (gm != null) gm.PhaseChanged -= OnPhase;
        }

        /// <summary>Leaving or restarting in the middle of a night still leaves a trace.</summary>
        public void Abandon()
        {
            if (inNight) EndNight("abandoned");
        }

        void OnPhase(GamePhase phase)
        {
            if (phase == GamePhase.Night) { BeginNight(); return; }
            if (inNight) EndNight(phase == GamePhase.Defeat ? "lost" : "won");
            else if (phase == GamePhase.Day) chapterStart = Time.realtimeSinceStartup; // the first day
            if (phase == GamePhase.Victory || phase == GamePhase.Defeat)
                Write($"chapter={gm.ChapterNumber} END={(phase == GamePhase.Victory ? "victory" : "defeat")}"
                    + $" night={night} fight={Num(fightTime)}s total={Num(Time.realtimeSinceStartup - chapterStart)}s");
        }

        void BeginNight()
        {
            inNight = true;
            wasGhost = false;
            night = gm.WaveIndex + 1;
            deaths = 0;
            baseMin = duckMin = 1f;
            coins = Wallet.Instance != null ? Wallet.Instance.Coins : 0;
            built = Buildings();
        }

        void Update()
        {
            if (!inNight) return;
            var home = gm.baseSlot != null ? gm.baseSlot.Current : null;
            baseMin = Mathf.Min(baseMin, home != null ? home.Health.Normalized : 0f);
            var duck = PlayerHealth.Instance;
            if (duck == null) return;
            duckMin = Mathf.Min(duckMin, duck.Normalized);
            if (duck.IsGhost && !wasGhost) deaths++;
            wasGhost = duck.IsGhost;
        }

        void EndNight(string result)
        {
            Update(); // the blow that ended the night counts too
            inNight = false;
            fightTime += gm.NightTime;
            var home = gm.baseSlot != null ? gm.baseSlot.Current : null;
            var skills = SkillSystem.Instance;
            string picked = skills == null ? "" : string.Join(",", skills.All.Where(s => skills.Stars(s) > 0).Select(s => s.id + ":" + skills.Stars(s)));
            string workshop = string.Join("/", Enumerable.Range(0, MetaUpgrades.All.Length).Select(MetaUpgrades.Level));
            Write($"chapter={gm.ChapterNumber} night={night}/{gm.WaveCount} result={result} time={Num(gm.NightTime)}s"
                + $" enemies={gm.spawner.NightEnemyCount} summoned={gm.spawner.Summoned} baseEnd={Percent(home != null ? home.Health.Normalized : 0f)} baseMin={Percent(baseMin)}"
                + $" duckMin={Percent(duckMin)} duckDeaths={deaths}"
                + $" airstrikeUsed={(Airstrike.Instance != null && Airstrike.Instance.Used ? 1 : 0)}"
                + $" rescueChests={(skills != null ? skills.RescueChests : 0)} duckLevel={(skills != null ? skills.Level : 1)}"
                + $" coins={coins} workshop={workshop} built=[{built}] standing=[{Buildings()}] skills=[{picked}]");
        }

        static string Buildings() => string.Join(",", Building.All.Where(b => b.IsAlive)
            .Select(b => b.Config.type + ":" + b.Level).OrderBy(s => s));

        static string Num(float v) => v.ToString("0", CultureInfo.InvariantCulture);
        static string Percent(float v) => Num(v * 100f) + "%";

        static void Write(string text)
        {
            var line = new StringBuilder(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Append(' ').Append(text).ToString();
            Debug.Log("[DuckRun] " + line);
            try { File.AppendAllText(Path.Combine(Application.persistentDataPath, FileName), line + "\n"); }
            catch (Exception e) { Debug.LogWarning("[DuckRun] cannot write the journal: " + e.Message); }
        }
    }
}
