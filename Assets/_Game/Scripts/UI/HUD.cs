using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>Battle HUD: coins, wave track, phase/timer, duck and base HP, banners, build and fight buttons, result.</summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        public Text coinsText;
        public Text waveText;
        public Text phaseText;
        public Text playerHpText;
        public RectTransform playerHpFill;
        public Image playerHpFillImage;
        public RectTransform baseHpFill;
        public Text bannerText;
        public Button fightButton;
        public Button buildButton;
        public Text buildButtonText;
        public Image buildButtonImage;
        public GameObject resultPanel;
        public Text resultTitle;
        public Text resultSubtitle;
        public Button restartButton;
        public Button menuButton;
        public Button nextButton;
        public Text rewardText;
        public Text baseAlertText;
        public RectTransform xpFill;
        public Text levelText;

        float bannerTimer, bannerDuration;
        float coinPunch, coinDenied, baseAlert;
        int shownCoins = -1;
        BuildSlot buildTarget;

        void Awake()
        {
            Instance = this;
            if (resultPanel != null) resultPanel.SetActive(false);
            if (bannerText != null) bannerText.gameObject.SetActive(false);
            if (baseAlertText != null) baseAlertText.gameObject.SetActive(false);
        }

        void Start()
        {
            fightButton.onClick.AddListener(() => GameManager.Instance.StartNight());
            buildButton.onClick.AddListener(() => GameManager.Instance.BuildPressed());
            restartButton.onClick.AddListener(() => GameManager.Instance.Restart());
            menuButton.onClick.AddListener(() => GameManager.Instance.ToMenu());
            nextButton.onClick.AddListener(() => GameManager.Instance.NextChapter());
            Wallet.Instance.Changed += OnCoins;
            Loc.Changed += OnLanguage;
            OnCoins(Wallet.Instance.Coins);
            buildButton.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (Wallet.Instance != null) Wallet.Instance.Changed -= OnCoins;
            Loc.Changed -= OnLanguage;
        }

        void OnLanguage()
        {
            if (buildTarget != null) SetBuildTarget(buildTarget);
        }

        void OnCoins(int coins)
        {
            if (shownCoins >= 0 && coins > shownCoins) coinPunch = 1f;
            shownCoins = coins;
            coinsText.text = coins.ToString();
            if (buildTarget != null) SetBuildTarget(buildTarget);
        }

        public void Banner(string text, Color color, float duration = 2.2f)
        {
            bannerText.text = text;
            bannerText.color = color;
            bannerText.gameObject.SetActive(true);
            bannerTimer = duration;
            bannerDuration = duration;
        }

        public void BaseUnderAttack()
        {
            baseAlert = 1.5f;
        }

        public void NotEnoughCoins()
        {
            coinDenied = 1f;
        }

        public void SetBuildTarget(BuildSlot slot)
        {
            buildTarget = slot;
            bool on = slot != null;
            if (buildButton.gameObject.activeSelf != on) buildButton.gameObject.SetActive(on);
            if (!on) return;
            bool afford = Wallet.Instance.Coins >= slot.NextCost;
            string name = slot.config.Name;
            buildButtonText.text = slot.Level == 0
                ? Loc.F("hud.build", name, slot.NextCost)
                : Loc.F("hud.upgrade", name, slot.Level + 1, slot.NextCost);
            buildButtonImage.color = afford ? new Color(1f, 0.8f, 0.2f) : new Color(0.55f, 0.55f, 0.55f);
        }

        public void ShowResult(bool victory)
        {
            resultPanel.SetActive(true);
            resultTitle.text = Loc.T(victory ? "hud.victory" : "hud.defeat");
            resultTitle.color = victory ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.35f, 0.3f);
            var gm = GameManager.Instance;
            resultSubtitle.text = victory
                ? Loc.F("hud.victory_sub", gm.WaveCount)
                : Loc.F("hud.defeat_sub", gm.WaveIndex, gm.WaveCount);
            rewardText.text = Loc.F("result.reward", gm.LastReward, SaveSystem.Data.gears);
            nextButton.gameObject.SetActive(victory && gm.HasNextChapter);
            buildButton.gameObject.SetActive(false);
            fightButton.gameObject.SetActive(false);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Phase == GamePhase.Menu) return;
            float dt = Time.unscaledDeltaTime;

            // Wave track and phase.
            int shownWave = Mathf.Min(gm.WaveIndex + 1, gm.WaveCount);
            var wave = gm.CurrentWave;
            string kind = wave == null ? "" : wave.kind == WaveKind.Boss ? Loc.T("hud.wave_boss") : wave.kind == WaveKind.Elite ? Loc.T("hud.wave_elite") : "";
            waveText.text = Loc.F("hud.wave", shownWave, gm.WaveCount, kind);

            if (gm.Phase == GamePhase.Day)
                phaseText.text = Loc.F("hud.day", gm.WaveIndex + 1);
            else if (gm.Phase == GamePhase.Night)
            {
                int t = Mathf.FloorToInt(gm.NightTime);
                int alive = EnemyManager.Instance != null ? EnemyManager.Instance.AliveCount : 0;
                phaseText.text = Loc.F("hud.night", gm.WaveIndex + 1, t / 60, t % 60, alive);
            }
            else phaseText.text = "";

            bool showFight = gm.Phase == GamePhase.Day;
            if (fightButton.gameObject.activeSelf != showFight) fightButton.gameObject.SetActive(showFight);

            // Duck HP.
            var ph = PlayerHealth.Instance;
            if (ph != null)
            {
                float n = ph.Normalized;
                playerHpFill.localScale = new Vector3(n, 1f, 1f);
                playerHpFillImage.color = n < 0.3f
                    ? Color.Lerp(new Color(0.9f, 0.15f, 0.1f), new Color(1f, 0.5f, 0.4f), Mathf.PingPong(Time.unscaledTime * 3f, 1f))
                    : new Color(0.3f, 0.85f, 0.3f);
                playerHpText.text = ph.IsAlive
                    ? Loc.F("hud.duck_hp", Mathf.CeilToInt(ph.Current), Mathf.CeilToInt(ph.Max))
                    : Loc.F("hud.duck_respawn", Mathf.CeilToInt(ph.RespawnIn));
            }

            // XP / hero level.
            var sk = SkillSystem.Instance;
            if (sk != null && xpFill != null)
            {
                bool max = sk.Level >= sk.maxLevel;
                xpFill.localScale = new Vector3(max ? 1f : Mathf.Clamp01(sk.Xp / (float)sk.XpToNext), 1f, 1f);
                levelText.text = max ? $"Lv. MAX" : Loc.F("hud.level", sk.Level, sk.Xp, sk.XpToNext);
            }

            // Base HP.
            var baseB = gm.baseSlot != null ? gm.baseSlot.Current : null;
            baseHpFill.localScale = new Vector3(baseB != null ? baseB.Health.Normalized : 0f, 1f, 1f);

            // Coin punch / denied shake.
            coinPunch = Mathf.MoveTowards(coinPunch, 0f, dt * 4f);
            coinDenied = Mathf.MoveTowards(coinDenied, 0f, dt * 3f);
            coinsText.rectTransform.localScale = Vector3.one * (1f + coinPunch * 0.35f);
            coinsText.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 60f) * 12f * coinDenied, 0f);
            coinsText.color = Color.Lerp(Color.white, new Color(1f, 0.3f, 0.3f), coinDenied);

            // Base alert.
            if (baseAlertText != null)
            {
                baseAlert = Mathf.MoveTowards(baseAlert, 0f, dt);
                bool on = baseAlert > 0f;
                if (baseAlertText.gameObject.activeSelf != on) baseAlertText.gameObject.SetActive(on);
                if (on) baseAlertText.color = new Color(1f, 0.3f, 0.25f, 0.6f + 0.4f * Mathf.PingPong(Time.unscaledTime * 4f, 1f));
            }

            // Banner fade.
            if (bannerTimer > 0f)
            {
                bannerTimer -= dt;
                float k = bannerTimer / bannerDuration;
                var c = bannerText.color;
                c.a = Mathf.Clamp01(bannerTimer / 0.4f);
                bannerText.color = c;
                bannerText.rectTransform.localScale = Vector3.one * (k > 0.9f ? Mathf.Lerp(1f, 1.3f, (k - 0.9f) * 10f) : 1f);
                if (bannerTimer <= 0f) bannerText.gameObject.SetActive(false);
            }
        }
    }
}
