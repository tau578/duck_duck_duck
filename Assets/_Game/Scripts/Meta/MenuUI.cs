using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>
    /// Main menu: New game (level select with 3 chapters), Workshop (permanent upgrades) and Settings
    /// (sound, language, accessibility). One page is visible at a time.
    /// </summary>
    public class MenuUI : MonoBehaviour
    {
        public static MenuUI Instance { get; private set; }

        [Header("Pages")]
        public GameObject mainPage;
        public GameObject levelsPage;
        public GameObject workshopPage;
        public GameObject settingsPage;
        public Button playButton;
        public Button workshopButton;
        public Button settingsButton;
        public Button[] backButtons;
        public Button unlockAllButton;

        [Header("Settings rows: sfx, language, large text, color-blind, reduce motion, slow mode")]
        public Button[] settingButtons;
        public Text[] settingLabels;
        public Text accessibilityHeader;

        [Header("Content")]
        public Text title;
        public Text gearsText;
        public Text chaptersHeader;
        public Text workshopHeader;
        public Button[] chapterButtons;
        public Image[] chapterImages;
        public Text[] chapterNames;
        public Text[] chapterInfo;
        public Button[] upgradeButtons;
        public Image[] upgradeImages;
        public Text[] upgradeNames;
        public Text[] upgradeInfo;
        public Button resetButton;
        public Button cheatButton;

        void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);
        }

        void Start()
        {
            for (int i = 0; i < chapterButtons.Length; i++)
            {
                int index = i;
                chapterButtons[i].onClick.AddListener(() => OnChapter(index));
            }
            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                int index = i;
                upgradeButtons[i].onClick.AddListener(() => OnUpgrade(index));
            }
            resetButton.onClick.AddListener(() => { SaveSystem.ResetAll(); Refresh(); });
            cheatButton.onClick.AddListener(() => { SaveSystem.Data.gears += 100; SaveSystem.Save(); Refresh(); });

            playButton.onClick.AddListener(() => ShowPage(levelsPage));
            workshopButton.onClick.AddListener(() => ShowPage(workshopPage));
            settingsButton.onClick.AddListener(() => ShowPage(settingsPage));
            foreach (var b in backButtons) b.onClick.AddListener(() => ShowPage(mainPage));
            unlockAllButton.onClick.AddListener(() =>
            {
                SaveSystem.Data.unlockedChapter = Mathf.Max(SaveSystem.Data.unlockedChapter, GameManager.Instance.chapters.Length);
                SaveSystem.Save();
                Refresh();
            });

            System.Action[] actions =
            {
                GameSettings.CycleSfx, Loc.Toggle, GameSettings.ToggleLargeText,
                GameSettings.ToggleColorBlind, GameSettings.ToggleReduceMotion, GameSettings.ToggleSlowMode,
            };
            for (int i = 0; i < settingButtons.Length && i < actions.Length; i++)
            {
                var action = actions[i];
                settingButtons[i].onClick.AddListener(() => { action(); Sfx.Play(SfxId.Coin, 0.6f, 0f, 0f); });
            }

            Loc.Changed += Refresh;
            GameSettings.Changed += Refresh;
        }

        void OnDestroy()
        {
            Loc.Changed -= Refresh;
            GameSettings.Changed -= Refresh;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            // The in-game dev language button would cover the title; the language lives in Settings here.
            var lang = FindFirstObjectByType<LanguageToggle>();
            if (lang != null) lang.gameObject.SetActive(false);
            ShowPage(mainPage);
        }

        void ShowPage(GameObject page)
        {
            mainPage.SetActive(page == mainPage);
            levelsPage.SetActive(page == levelsPage);
            workshopPage.SetActive(page == workshopPage);
            settingsPage.SetActive(page == settingsPage);
            Refresh();
        }

        static string OnOff(bool v) => Loc.T(v ? "set.on" : "set.off");
        static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";

        void OnChapter(int index)
        {
            if (index + 1 > SaveSystem.Data.unlockedChapter)
            {
                Sfx.Play(SfxId.Denied, 0.8f, 0f, 0.1f);
                return;
            }
            GameManager.Instance.PlayChapter(index);
        }

        void OnUpgrade(int index)
        {
            if (MetaUpgrades.TryBuy(index)) Sfx.Play(SfxId.Build, 0.9f, 0f, 0f);
            else Sfx.Play(SfxId.Denied, 0.8f, 0f, 0.1f);
            Refresh();
        }

        void Refresh()
        {
            if (!gameObject.activeInHierarchy) return;
            var save = SaveSystem.Data;
            var chapters = GameManager.Instance.chapters;
            title.text = Loc.T("menu.title");
            gearsText.text = Loc.F("menu.gears", save.gears);
            chaptersHeader.text = Loc.T("menu.chapters");
            workshopHeader.text = Loc.T("menu.workshop");
            if (accessibilityHeader != null) accessibilityHeader.text = Loc.T("set.access");

            string[] settingTexts =
            {
                Loc.F("set.sfx", Percent(GameSettings.SfxVolume)),
                Loc.T("set.lang"),
                Loc.F("set.large", OnOff(GameSettings.LargeText)),
                Loc.F("set.colorblind", OnOff(GameSettings.ColorBlind)),
                Loc.F("set.motion", OnOff(GameSettings.ReduceMotion)),
                Loc.F("set.slow", OnOff(GameSettings.SlowMode)),
            };
            for (int i = 0; i < settingLabels.Length && i < settingTexts.Length; i++) settingLabels[i].text = settingTexts[i];

            for (int i = 0; i < chapterButtons.Length; i++)
            {
                bool exists = chapters != null && i < chapters.Length;
                chapterButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;
                var ch = chapters[i];
                bool unlocked = i + 1 <= save.unlockedChapter;
                int best = i < save.bestWaves.Length ? save.bestWaves[i] : 0;
                int waves = ch.waves.Length;
                chapterNames[i].text = ch.Name;
                string status = !unlocked ? Loc.F("menu.locked", i)
                    : best > 0 ? Loc.F("menu.best", best, waves) : Loc.T("menu.new");
                string news = unlocked && !string.IsNullOrEmpty(ch.News) ? "\n" + Loc.F("banner.news", ch.News) : "";
                chapterInfo[i].text = status + news;
                chapterImages[i].color = !unlocked ? new Color(0.25f, 0.25f, 0.28f)
                    : best >= waves ? new Color(0.25f, 0.45f, 0.3f) : new Color(0.75f, 0.45f, 0.15f);
            }

            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                bool exists = i < MetaUpgrades.All.Length;
                upgradeButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;
                var def = MetaUpgrades.All[i];
                int lvl = MetaUpgrades.Level(i);
                bool max = lvl >= def.MaxLevel;
                int cost = max ? 0 : def.costs[lvl];
                upgradeNames[i].text = $"{def.Name}  {Loc.F("meta.level", lvl, def.MaxLevel)}";
                upgradeInfo[i].text = def.Desc + "\n" + (max ? Loc.T("meta.max") : Loc.F("meta.cost", cost));
                upgradeImages[i].color = max ? new Color(0.25f, 0.45f, 0.3f)
                    : save.gears >= cost ? new Color(0.2f, 0.35f, 0.6f) : new Color(0.25f, 0.25f, 0.28f);
            }
        }
    }
}
