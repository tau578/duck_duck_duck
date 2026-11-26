using UnityEngine;

namespace Duck
{
    /// <summary>Fixed place for one building type. Build on an empty slot, upgrade on the same slot (price grows).</summary>
    public class BuildSlot : MonoBehaviour
    {
        public BuildingConfig config;
        public int startLevel;
        [Tooltip("Slot appears from this chapter on.")]
        public int minChapter = 1;
        public GameObject marker;
        [Tooltip("Barracks only: where the squad stands guard.")]
        public Vector3 rallyPoint;

        public int Level { get; private set; }
        public Building Current { get; private set; }
        public bool CanUpgrade => Level < config.MaxLevel;
        /// <summary>Price of the next level (levels[Level] is level Level+1).</summary>
        public int NextCost => CanUpgrade ? config.levels[Level].cost : 0;

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
            Refresh();
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
