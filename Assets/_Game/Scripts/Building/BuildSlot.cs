using UnityEngine;

namespace Duck
{
    /// <summary>Fixed place for one building type. Build on an empty slot, upgrade on the same slot (price grows).</summary>
    public class BuildSlot : MonoBehaviour
    {
        public BuildingConfig config;
        public int startLevel;
        [Tooltip("Per chapter: the day this spot opens (1 = from the start), 0 = not in that chapter.")]
        public int[] unlockDays = { 1, 1, 1 };
        public int UnlockDay(int chapterIndex) => unlockDays != null && chapterIndex < unlockDays.Length ? unlockDays[chapterIndex] : 1;
        public GameObject marker;
        [Tooltip("Barracks only: where the squad stands guard.")]
        public Vector3 rallyPoint;

        public int Level { get; private set; }
        public Building Current { get; private set; }
        public bool CanUpgrade => Level < Mathf.Min(config.MaxLevel, ChapterLevelCap);
        static int ChapterLevelCap
        {
            get
            {
                var gm = GameManager.Instance;
                return gm != null && gm.chapter != null ? gm.chapter.maxBuildingLevel : 99;
            }
        }
        /// <summary>Price of the next level (levels[Level] is level Level+1).</summary>
        public int NextCost
        {
            get
            {
                if (!CanUpgrade) return 0;
                var gm = GameManager.Instance;
                var costs = gm != null && gm.chapter != null ? gm.chapter.buildCosts : null;
                int type = (int)config.type;
                return Level == 0 && costs != null && type < costs.Length && costs[type] > 0 ? costs[type] : config.levels[Level].cost;
            }
        }

        [Tooltip("Price label is shown only when the duck is this close (keeps the screen clean).")]
        public float labelRadius = 7f;

        WorldLabel label;
        RangeRing currentRing, nextRing;
        bool isDay = true;
        bool labelNear = true;

        void Update()
        {
            UpdateRangeRings();
            var duck = DuckController.Instance;
            if (duck == null || label == null) return;
            Vector3 d = duck.transform.position - transform.position;
            d.y = 0f;
            bool near = d.sqrMagnitude < labelRadius * labelRadius;
            if (near != labelNear)
            {
                labelNear = near;
                Refresh();
            }
        }

        void OnEnable() { Loc.Changed += Refresh; }
        void OnDisable() { Loc.Changed -= Refresh; }

        void Start()
        {
            label = WorldLabel.Create(transform, Vector3.up * 1.2f, 56, new Color(1f, 0.88f, 0.25f));
            if (config.type == BuildingType.Tower)
            {
                currentRing = RangeRing.Create(transform, "RangeNow", new Color(1f, 1f, 1f, 1f), false, 0.12f);
                nextRing = RangeRing.Create(transform, "RangeNext", new Color(0.45f, 1f, 0.45f, 1f), true, 0.14f);
            }
            if (startLevel > 0) SetLevel(startLevel, false);
            Refresh();
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade) return false;
            if (Wallet.Instance == null || !Wallet.Instance.TrySpend(NextCost)) return false;
            SetLevel(Level + 1, true);
            return true;
        }

        void SetLevel(int level, bool fx)
        {
            if (Current != null) Destroy(Current.gameObject);
            Level = level;
            Current = BuildingFactory.Create(config, level, this);
            if (fx)
            {
                Sfx.Play(SfxId.Build, 0.8f, 0.05f, 0f);
                CameraFollow.Shake(0.12f);
                DamageNumbers.ShowText(transform.position + Vector3.up, level == 1 ? Loc.T("fx.built") : Loc.F("fx.level", level), new Color(0.5f, 1f, 0.5f), 48);
            }
            if (fx) PushDuckOut();
            Refresh();
        }

        /// <summary>A new building must not swallow the duck standing on its slot: step her out towards the base.</summary>
        void PushDuckOut()
        {
            var duck = DuckController.Instance;
            if (duck == null || config.type == BuildingType.Wall || config.type == BuildingType.Base) return;
            const float clearRadius = 1.7f;
            Vector3 d = duck.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude >= clearRadius * clearRadius) return;
            Vector3 dir = d.sqrMagnitude > 0.04f ? d.normalized : -new Vector3(transform.position.x, 0f, transform.position.z).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.back;
            Vector3 p = transform.position + dir * clearRadius;
            p.y = duck.transform.position.y;
            duck.transform.position = p;
        }

        public void OnBuildingDestroyed(Building b)
        {
            if (b != Current) return;
            Current = null;
            Level = 0;
            Destroy(b.gameObject, 0.05f);
            if (config.type == BuildingType.Base && GameManager.Instance != null) GameManager.Instance.OnBaseDestroyed();
            Refresh();
        }

        /// <summary>
        /// Day, duck at this tower slot: solid ring = current range, dashed green ring = range after building/upgrading.
        /// Hidden at night so the battlefield stays readable.
        /// </summary>
        void UpdateRangeRings()
        {
            if (currentRing == null) return;
            bool selected = isDay && HUD.Instance != null && HUD.Instance.BuildTarget == this;
            float now = Level > 0 ? config.levels[Level - 1].range : 0f;
            float next = CanUpgrade ? config.levels[Level].range : 0f;
            if (selected && now > 0f) currentRing.Show(now, 0.6f); else currentRing.Hide();
            if (selected && next > 0f) nextRing.Show(next, 0.85f); else nextRing.Hide();
        }

        public void SetDayMode(bool day)
        {
            isDay = day;
            Refresh();
        }

        public void OnDawn()
        {
            if (Current == null) return;
            Current.Health.HealFull();
            Current.RefreshBar();
            var barracks = Current.GetComponent<Barracks>();
            if (barracks != null) barracks.OnDawn();
        }

        void Refresh()
        {
            if (marker != null) marker.SetActive(Level == 0);
            if (label == null) return;
            label.SetVisible(isDay && CanUpgrade && labelNear);
            // Below the slot (towards the camera) so it never covers the ring or the roof.
            label.offset = new Vector3(0f, 0.3f, -(config.halfExtents.y + 0.9f));
            label.Set(Level == 0 ? Loc.F("slot.empty", config.Name, NextCost) : Loc.F("slot.upgrade", Level + 1, NextCost));
        }
    }
}
