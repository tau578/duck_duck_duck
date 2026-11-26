using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    public class SkillDef
    {
        public string id;
        public int maxStars;
        public bool passive;
        /// <summary>Star level that "awakens" the skill (big visual change). 0 = none.</summary>
        public int awakenAt;
        /// <summary>Skill joins the pool from this chapter on.</summary>
        public int minChapter = 1;
        public Color color;

        public string Name => Loc.T($"skill.{id}.name");
        public string Desc(int star) => Loc.T($"skill.{id}.{star}");
    }

    /// <summary>
    /// Roguelike layer inside a battle: kills give XP, each level offers 3 random skills, the pick is applied at once.
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        public static SkillSystem Instance { get; private set; }
        /// <summary>Building HP multiplier from the "Fortify" skill (read by Building on creation).</summary>
        public static float BuildingHpMultiplier = 1f;

        public SkillPicker picker;
        public int maxLevel = 15;
        public int MaxLevel => Chapter != null && Chapter.maxDuckLevel > 0 ? Chapter.maxDuckLevel : maxLevel;
        static ChapterConfig Chapter => GameManager.Instance != null ? GameManager.Instance.chapter : null;

        /// <summary>The elite chest: its free picks come after its own animation and before level-up picks.</summary>
        bool eliteChestOpened, chestToReveal;
        int chestPicks, chestTotal;
        ChestReveal reveal;

        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        /// <summary>The chapter's XP table if it has one, otherwise first level after ~10 kills, then x1.25 per level.</summary>
        public int XpToNext
        {
            get
            {
                var ch = Chapter;
                float scale = ch != null ? ch.xpScale : 1f;
                var table = ch != null ? ch.xpPerLevel : null;
                float need = table != null && table.Length > 0
                    ? table[Mathf.Min(Level - 1, table.Length - 1)]
                    : 10f * Mathf.Pow(1.25f, Level - 1);
                return Mathf.Max(1, Mathf.RoundToInt(need * scale));
            }
        }

        public readonly List<SkillDef> All = new List<SkillDef>
        {
            new SkillDef { id = "double_shot", maxStars = 3, awakenAt = 3, color = new Color(1f, 0.75f, 0.2f) },
            new SkillDef { id = "blades", maxStars = 3, awakenAt = 3, minChapter = 2, color = new Color(0.75f, 0.8f, 0.9f) },
            new SkillDef { id = "meteor", maxStars = 3, minChapter = 3, color = new Color(1f, 0.45f, 0.2f) },
            new SkillDef { id = "rapid", maxStars = 3, passive = true, color = new Color(0.4f, 0.8f, 1f) },
            new SkillDef { id = "power", maxStars = 3, passive = true, color = new Color(1f, 0.35f, 0.35f) },
            new SkillDef { id = "fortify", maxStars = 2, passive = true, color = new Color(0.6f, 0.5f, 0.35f) },
            new SkillDef { id = "vitality", maxStars = 2, passive = true, color = new Color(0.4f, 0.9f, 0.4f) },
        };

        readonly Dictionary<string, int> stars = new Dictionary<string, int>();
        int pendingChoices;
        bool showing;
        AutoShooter shooter;
        float baseFireRate;

        void Awake()
        {
            Instance = this;
            BuildingHpMultiplier = MetaUpgrades.BuildingHpMultiplier;
        }

        void Start()
        {
            EnemyManager.EnemyKilled += OnEnemyKilled;
            var duck = DuckController.Instance;
            shooter = duck != null ? duck.GetComponent<AutoShooter>() : null;
            if (shooter != null)
            {
                baseFireRate = shooter.fireRate;
                shooter.damageMultiplier = MetaUpgrades.DuckDamageMultiplier;
            }
        }

        void OnDestroy()
        {
            EnemyManager.EnemyKilled -= OnEnemyKilled;
        }

        public int Stars(SkillDef s) => stars.TryGetValue(s.id, out int n) ? n : 0;

        void OnEnemyKilled(Enemy e)
        {
            if (e.Config.isElite && !eliteChestOpened && Chapter != null && Chapter.eliteChestPicks > 0)
            {
                eliteChestOpened = true;
                chestPicks = chestTotal = Chapter.eliteChestPicks;
                chestToReveal = true;
            }
            if (Level < MaxLevel)
            {
                Xp += Mathf.Max(1, e.Config.xp);
                while (Xp >= XpToNext && Level < MaxLevel)
                {
                    Xp -= XpToNext;
                    Level++;
                    pendingChoices++;
                    Sfx.Play(SfxId.Build, 0.7f, 0f, 0f);
                }
            }
            TryShowChoice();
        }

        void Update()
        {
            // The last kill can level the duck up in the same frame the chapter ends: never keep a skill choice over the result screen.
            if (!showing) return;
            var gm = GameManager.Instance;
            if (gm == null || (gm.Phase != GamePhase.Victory && gm.Phase != GamePhase.Defeat)) return;
            if (picker != null) picker.Cancel();
            if (reveal != null) reveal.Close();
            showing = false;
            pendingChoices = chestPicks = 0;
            chestToReveal = false;
            TimeControl.Resume();
        }

        void TryShowChoice()
        {
            if (showing || pendingChoices + chestPicks <= 0 || picker == null) return;
            var gm = GameManager.Instance;
            if (gm != null && (gm.Phase == GamePhase.Victory || gm.Phase == GamePhase.Defeat)) return;

            var options = PickOptions(3);
            if (options.Count == 0) { pendingChoices = chestPicks = 0; chestToReveal = false; return; }
            showing = true;
            TimeControl.Pause();

            if (chestToReveal)
            {
                chestToReveal = false;
                var card = picker.cardImages != null && picker.cardImages.Length > 0 ? picker.cardImages[0].sprite : null;
                reveal = ChestReveal.Show(picker.transform.parent, card, chestTotal, () =>
                {
                    reveal = null;
                    showing = false;
                    TimeControl.Resume();
                    TryShowChoice();
                });
                return;
            }

            string heading;
            if (chestPicks > 0)
            {
                heading = Loc.F("chest.pick", chestTotal - chestPicks + 1, chestTotal);
                chestPicks--;
            }
            else
            {
                heading = Loc.F("skill.title", Level);
                pendingChoices--;
            }
            picker.Show(options, this, OnPicked, heading);
        }

        List<SkillDef> PickOptions(int count)
        {
            var pool = new List<SkillDef>();
            int chapter = GameManager.Instance != null ? GameManager.Instance.ChapterNumber : 1;
            foreach (var s in All) if (Stars(s) < s.maxStars && s.minChapter <= chapter) pool.Add(s);
            var result = new List<SkillDef>();
            while (result.Count < count && pool.Count > 0)
            {
                int i = Random.Range(0, pool.Count);
                result.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return result;
        }

        void OnPicked(SkillDef s)
        {
            int n = Stars(s) + 1;
            stars[s.id] = n;
            Apply(s, n);
            showing = false;
            TimeControl.Resume();
            var duck = DuckController.Instance;
            if (duck != null)
            {
                bool awaken = s.awakenAt > 0 && n == s.awakenAt;
                DamageNumbers.ShowText(duck.transform.position + Vector3.up, awaken ? Loc.T("skill.awaken") + "!" : s.Name,
                    awaken ? new Color(1f, 0.4f, 0.2f) : s.color, awaken ? 64 : 48);
                if (awaken) CameraFollow.Shake(0.4f);
            }
            TryShowChoice();
        }

        void Apply(SkillDef s, int n)
        {
            var duck = DuckController.Instance;
            switch (s.id)
            {
                case "double_shot":
                    // Every extra bullet is weaker: x1.4 / x1.8 damage, then the awakening pierces one enemy.
                    shooter.bulletsPerShot = 1 + Mathf.Min(n, 2);
                    shooter.bulletDamageShare = n <= 1 ? 0.7f : 0.6f;
                    if (n >= 3) shooter.pierce = 1;
                    break;
                case "blades":
                    Get<SpinningBlades>(duck).SetLevel(n);
                    break;
                case "meteor":
                    Get<MeteorSkill>(duck).SetLevel(n);
                    break;
                case "rapid":
                    shooter.fireRate = baseFireRate * (1f + 0.1f * n);
                    break;
                case "power":
                    shooter.damageMultiplier = MetaUpgrades.DuckDamageMultiplier * (1f + 0.1f * n);
                    break;
                case "fortify":
                    BuildingHpMultiplier = MetaUpgrades.BuildingHpMultiplier * (1f + 0.25f * n);
                    foreach (var b in Building.All) if (b.IsAlive) b.ApplyHpMultiplier();
                    break;
                case "vitality":
                    PlayerHealth.Instance?.SetMaxHp(100f + 25f * n);
                    break;
            }
        }

        static T Get<T>(Component host) where T : Component
        {
            var c = host.GetComponent<T>();
            return c != null ? c : host.gameObject.AddComponent<T>();
        }
    }
}
