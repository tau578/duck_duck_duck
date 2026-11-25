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

        public int Level { get; private set; }
        public Building Current { get; private set; }
        public bool CanUpgrade => Level < config.MaxLevel;
        /// <summary>Price of the next level (levels[Level] is level Level+1).</summary>
        public int NextCost => CanUpgrade ? config.levels[Level].cost : 0;

        [Tooltip("Price label is shown only when the duck is this close (keeps the screen clean).")]
        public float labelRadius = 7f;

        WorldLabel label;
        bool isDay = true;
        bool labelNear = true;

        void Update()
        {
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
