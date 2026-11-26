using UnityEngine;

namespace Duck
{
    /// <summary>
    /// Duck HP with clear feedback: bar over the head, HUD bar, red edge flash, low-HP pulse.
    /// Death is not game over, as in the original: the duck turns into a grey ghost that can still walk but not
    /// fight, a grey bar drains under it, then it revives on the spot with full HP. Enemies ignore the ghost.
    /// Once the base falls (or the chapter is won) the run is over: no revival, duck frozen.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth Instance { get; private set; }

        public float maxHp = 100f;
        [Tooltip("Seconds the duck stays a ghost before reviving where it stands.")]
        public float ghostDuration = 6f;
        public float invulnerableAfterRevive = 1.5f;

        Health hp;
        DuckController controller;
        AutoShooter shooter;
        WorldBar bar;
        float ghostTimer, invulnTimer;
        WorldBar ghostBar;
        Material ghostMaterial;
        Renderer[] renderers;
        Material[] materials;

        public bool IsAlive => hp != null && hp.IsAlive;
        public float Normalized => hp != null ? hp.Normalized : 0f;
        public float Current => hp != null ? hp.Current : 0f;
        public float Max => hp != null ? hp.max : maxHp;
        public bool IsGhost => ghostTimer > 0f;
        public float RespawnIn => ghostTimer;
        public bool RunOver { get; private set; }

        /// <summary>Base destroyed or chapter won: cancel any revival, stop moving and shooting.</summary>
        public void EndRun()
        {
            RunOver = true;
            ghostTimer = 0f;
            invulnTimer = 0f;
            SetGhostLook(false);
            hp.invulnerable = true;
            controller.inputEnabled = false;
            if (shooter != null) shooter.enabled = false;
        }

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
            // Grey countdown over the ghost, in place of the health bar.
            ghostBar = WorldBar.Create(transform, 2.1f, 1.4f, new Color(0.85f, 0.87f, 0.9f), false);
            ghostBar.plain = true;
            ghostBar.gameObject.SetActive(false);
        }

        public void TakeDamage
(float amount) => hp.TakeDamage(amount);

        /// <summary>New max HP (Vitality skill). Heals to full if alive.</summary>
        public void SetMaxHp(float value)
        {
            maxHp = value;
            if (hp.IsAlive)
            {
                hp.ResetTo(value);
                bar.Set(1f);
                DamageVignette.SetHealth(1f);
            }
            else hp.max = value;
        }

        public void HealFull()
        {
            if (IsGhost) { Revive(); return; } // dawn brings the duck back at once
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
            if (RunOver) return;
            ghostTimer = ghostDuration;
            invulnTimer = 0f;
            controller.visual.gameObject.SetActive(true);
            if (shooter != null) shooter.enabled = false; // the ghost keeps walking but cannot shoot
            SetGhostLook(true);
            DamageVignette.SetHealth(1f);
            DamageVignette.Flash(0.9f);
            CameraFollow.Shake(0.5f);
            Sfx.Play(SfxId.Explosion, 0.6f, 0.05f, 0f);
            if (HUD.Instance != null) HUD.Instance.Banner(Loc.F("banner.duck_down", Mathf.CeilToInt(ghostDuration)), new Color(1f, 0.4f, 0.3f), 2.5f);
        }

        void Update()
        {
            if (RunOver) return;
            if (IsGhost)
            {
                ghostTimer -= Time.deltaTime;
                if (ghostTimer <= 0f) Revive();
                else ghostBar.Set(ghostTimer / ghostDuration);
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

        void SetGhostLook(bool ghost)
        {
            if (ghostBar != null)
            {
                ghostBar.gameObject.SetActive(ghost);
                if (ghost) ghostBar.Set(1f);
            }
            if (ghost)
            {
                renderers = controller.visual.GetComponentsInChildren<Renderer>(true);
                materials = new Material[renderers.Length];
                // Unlit, so the ghost stays pale and readable in the night lighting.
                if (ghostMaterial == null)
                {
                    ghostMaterial = new Material(GameAssets.I.flashMaterial);
                    ghostMaterial.SetColor("_BaseColor", new Color(0.66f, 0.69f, 0.72f));
                }
                for (int i = 0; i < renderers.Length; i++)
                {
                    materials[i] = renderers[i].sharedMaterial;
                    renderers[i].sharedMaterial = ghostMaterial;
                }
            }
            else if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].sharedMaterial = materials[i];
                renderers = null;
            }
        }

        void Revive()
        {
            ghostTimer = 0f;
            SetGhostLook(false);
            hp.HealFull();
            hp.invulnerable = true;
            invulnTimer = invulnerableAfterRevive;
            bar.Set(1f);
            DamageVignette.SetHealth(1f);
            if (shooter != null) shooter.enabled = true;
            DamageNumbers.ShowText(transform.position + Vector3.up, "+" + Mathf.RoundToInt(hp.max), new Color(0.4f, 1f, 0.4f), 60);
            DamageNumbers.ShowText(transform.position, Loc.T("fx.back"), new Color(0.5f, 1f, 0.5f), 44);
            CameraFollow.Shake(0.3f);
            Sfx.Play(SfxId.Build, 0.9f, 0f, 0f);
        }
    }
}
