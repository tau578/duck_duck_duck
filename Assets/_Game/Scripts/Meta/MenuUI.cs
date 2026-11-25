using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>Main menu between battles: chapters (with what's new and best result) and the Workshop.</summary>
    public class MenuUI : MonoBehaviour
    {
        public static MenuUI Instance { get; private set; }

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
            Loc.Changed += Refresh;
        }

        void OnDestroy() { Loc.Changed -= Refresh; }

        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
        }

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
