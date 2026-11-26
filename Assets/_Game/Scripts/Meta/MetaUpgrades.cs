namespace Duck
{
    public class MetaUpgradeDef
    {
        public string id;
        public int[] costs;
        public int MaxLevel => costs.Length;
        public string Name => Loc.T($"meta.{id}.name");
        public string Desc => Loc.T($"meta.{id}.desc");
    }

    /// <summary>Workshop: permanent upgrades bought with gears earned in chapters.</summary>
    public static class MetaUpgrades
    {
        public static readonly MetaUpgradeDef[] All =
        {
            new MetaUpgradeDef { id = "duck_power", costs = new[] { 30, 60, 120 } },
            new MetaUpgradeDef { id = "fortify", costs = new[] { 30, 60, 120 } },
            new MetaUpgradeDef { id = "supplies", costs = new[] { 40, 80 } },
            new MetaUpgradeDef { id = "airstrike", costs = new[] { 40, 80 } },
        };

        public static int Level(int i) => SaveSystem.Upgrade(i);

        /// <summary>All Workshop levels bought, for enemy auto-leveling.</summary>
        public static int TotalLevels
        {
            get { int n = 0; for (int i = 0; i < All.Length; i++) n += Level(i); return n; }
        }

        public static float DuckDamageMultiplier => 1f + 0.08f * Level(0);
        public static float BuildingHpMultiplier => 1f + 0.15f * Level(1);
        public static int BonusStartCoins => 2 * Level(2);
        public static int BonusAirstrikeLevels => Level(3);

        public static bool TryBuy(int i)
        {
            var def = All[i];
            int lvl = Level(i);
            if (lvl >= def.MaxLevel) return false;
            int cost = def.costs[lvl];
            if (SaveSystem.Data.gears < cost) return false;
            SaveSystem.Data.gears -= cost;
            SaveSystem.SetUpgrade(i, lvl + 1);
            SaveSystem.Save();
            return true;
        }
    }
}
