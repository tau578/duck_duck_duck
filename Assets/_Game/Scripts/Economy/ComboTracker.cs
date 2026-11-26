using UnityEngine;

namespace Duck
{
    /// <summary>
    /// Coins come from the base and mines at dawn, like in the original. The only extra is a small reward
    /// for a combo: 3+ kills by the duck's own gun in one quick burst. Airstrike, skills and towers don't count,
    /// and combo coins are capped per night so dawn income stays the main source.
    /// </summary>
    public class ComboTracker : MonoBehaviour
    {
        [Tooltip("A burst ends when no enemy dies for this long.")]
        public float window = 0.45f;
        public int minCombo = 3;
        [Tooltip("Max coins from combos per night.")]
        public int maxCoinsPerNight = 0;

        int coinsThisNight;
        GamePhase lastPhase;

        int count;
        float timer;
        Vector3 lastPos;

        void Start() { EnemyManager.EnemyKilled += OnKill; }
        void OnDestroy() { EnemyManager.EnemyKilled -= OnKill; }

        void OnKill(Enemy e)
        {
            if (!e.KilledByDuckGun) return;
            count++;
            timer = window;
            lastPos = e.transform.position;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Phase != lastPhase)
            {
                lastPhase = gm.Phase;
                if (lastPhase == GamePhase.Night) coinsThisNight = 0;
            }
            if (count == 0) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            int n = count;
            count = 0;
            if (n < minCombo) return;

            var ch = gm != null ? gm.chapter : null;
            int cap = ch != null ? ch.comboCoinsPerNight : maxCoinsPerNight;
            int coins = Mathf.Min(n >= 5 ? 2 : 1, cap - coinsThisNight);
            if (coins <= 0) return;
            coinsThisNight += coins;
            for (int i = 0; i < coins; i++) CoinPickup.Drop(lastPos, 1);
            Color c = n >= 5 ? new Color(1f, 0.6f, 0.2f) : new Color(1f, 0.85f, 0.25f);
            DamageNumbers.ShowText(lastPos + Vector3.up * 1.5f, Loc.F("fx.combo", n, coins), c, n >= 5 ? 64 : 52);
            Sfx.Play(SfxId.Coin, 1f, 0f, 0f);
            if (n >= 5) CameraFollow.Shake(0.15f);
        }
    }
}
