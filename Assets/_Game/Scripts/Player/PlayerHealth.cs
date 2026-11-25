using UnityEngine;

namespace Duck
{
    /// <summary>
    /// Duck HP with clear feedback: bar over the head, HUD bar, red edge flash, low-HP pulse.
    /// Death is not game over: the duck respawns at the base after a delay.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth Instance { get; private set; }

        public float maxHp = 100f;
        public float respawnDelay = 5f;
        public float invulnerableAfterRespawn = 1.5f;
        public Vector3 respawnPoint = new Vector3(0f, 0f, -3.5f);

        Health hp;
        DuckController controller;
        AutoShooter shooter;
        WorldBar bar;
        float respawnTimer, invulnTimer;

        public bool IsAlive => hp != null && hp.IsAlive;
        public float Normalized => hp != null ? hp.Normalized : 0f;
        public float Current => hp != null ? hp.Current : 0f;
        public float Max => hp != null ? hp.max : maxHp;
        public float RespawnIn => respawnTimer;

        void Awake()
        {
            Instance = this;
            hp = GetComponent<Health>();
            hp.ResetTo(maxHp);
            hp.Damaged += OnDamaged;
            hp.Died += OnDied;
            controller = GetComponent<DuckController>();
            shooter = GetComponent<AutoShooter>();
        }

        void Start()
        {
            bar = WorldBar.Create(transform, 2.1f, 1.4f, new Color(0.3f, 0.9f, 0.3f), false);
            bar.Set(1f);
        }

        public void TakeDamage(float amount) => hp.TakeDamage(amount);

        public void HealFull()
        {
            if (!hp.IsAlive) return;
            hp.HealFull();
            bar.Set(1f);
            DamageVignette.SetHealth(1f);
        }

        void OnDamaged(float amount)
        {
            bar.Set(hp.Normalized);
            DamageVignette.Flash(0.55f);
            DamageVignette.SetHealth(hp.Normalized);
            CameraFollow.Shake(0.22f);
            Sfx.Play(SfxId.Hurt, 0.7f, 0.1f, 0.08f);
            DamageNumbers.ShowText(transform.position + Vector3.up * 0.5f, "-" + Mathf.CeilToInt(amount), new Color(1f, 0.3f, 0.3f), 44);
        }

        void OnDied()
        {
            respawnTimer = respawnDelay;
            controller.inputEnabled = false;
            if (shooter != null) shooter.enabled = false;
            controller.visual.gameObject.SetActive(false);
            DamageVignette.SetHealth(1f);
            DamageVignette.Flash(0.9f);
            CameraFollow.Shake(0.5f);
            Sfx.Play(SfxId.Explosion, 0.6f, 0.05f, 0f);
            if (HUD.Instance != null) HUD.Instance.Banner(Loc.F("banner.duck_down", Mathf.CeilToInt(respawnDelay)), new Color(1f, 0.4f, 0.3f), 2.5f);
        }

        void Update()
        {
            if (!hp.IsAlive && respawnTimer > 0f)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f) Respawn();
                return;
            }

            if (invulnTimer > 0f)
            {
                invulnTimer -= Time.deltaTime;
                bool blinkOn = invulnTimer <= 0f || Mathf.Repeat(invulnTimer, 0.2f) > 0.1f;
                controller.visual.gameObject.SetActive(blinkOn);
                if (invulnTimer <= 0f) hp.invulnerable = false;
            }
        }

        void Respawn()
        {
            respawnTimer = 0f;
            transform.position = respawnPoint;
            hp.HealFull();
            hp.invulnerable = true;
            invulnTimer = invulnerableAfterRespawn;
            bar.Set(1f);
            controller.visual.gameObject.SetActive(true);
            controller.inputEnabled = true;
            if (shooter != null) shooter.enabled = true;
            DamageNumbers.ShowText(transform.position, Loc.T("fx.back"), new Color(0.5f, 1f, 0.5f), 44);
        }
    }
}
