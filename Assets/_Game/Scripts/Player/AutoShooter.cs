using UnityEngine;

namespace Duck
{
    /// <summary>Duck auto-fire at the nearest enemy with muzzle flash, gun recoil and crits.</summary>
    public class AutoShooter : MonoBehaviour
    {
        public float damage = 9f;
        public float fireRate = 3.5f;
        [Tooltip("Kept smaller than half the portrait screen width so the range ring is fully visible.")]
        public float range = 5.5f;
        [Range(0f, 1f)] public float critChance = 0.12f;
        public float critMultiplier = 2f;
        public float knockback = 3f;
        public float bulletSpeed = 30f;
        public int bulletsPerShot = 1;
        public int pierce;
        public float spreadDegrees = 8f;
        public float damageMultiplier = 1f;

        public Transform muzzle;
        public Transform gun;
        public GameObject muzzleFlash;

        DuckController duck;
        Enemy target;
        float fireTimer, retarget, flashTimer, recoil;
        Vector3 gunRest;
        RangeRing ring;

        void Start()
        {
            // Always-visible gun radius on the ground; brighter while an enemy is inside.
            ring = RangeRing.Create(transform, "GunRange", new Color(1f, 0.92f, 0.45f, 1f), false, 0.22f);
        }

        void Awake()
        {
            duck = GetComponent<DuckController>();
            if (gun != null) gunRest = gun.localPosition;
            if (muzzleFlash != null) muzzleFlash.SetActive(false);
        }

        void OnDisable()
        {
            if (ring != null) ring.Hide();
            if (duck != null) duck.aimTarget = null;
            if (muzzleFlash != null) muzzleFlash.SetActive(false);
            target = null;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            fireTimer -= dt;

            retarget -= dt;
            if (retarget <= 0f || target == null || !target.IsAlive)
            {
                retarget = 0.1f;
                target = EnemyManager.Instance != null ? EnemyManager.Instance.Nearest(transform.position, range) : null;
                duck.aimTarget = target != null ? target.transform : null;
            }
            if (ring != null) ring.Show(range, target != null ? 0.95f : 0.6f);

            if (target != null && fireTimer <= 0f)
            {
                Vector3 to = target.transform.position - transform.position;
                to.y = 0f;
                if (Vector3.Angle(transform.forward, to) < 30f) Fire(to);
            }

            // Recoil and flash decay.
            recoil = Mathf.MoveTowards(recoil, 0f, dt * 10f);
            if (gun != null) gun.localPosition = gunRest - Vector3.forward * (0.18f * recoil);
            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                if (flashTimer <= 0f && muzzleFlash != null) muzzleFlash.SetActive(false);
            }
        }

        void Fire(Vector3 direction)
        {
            fireTimer = 1f / fireRate;
            Vector3 from = muzzle != null ? muzzle.position : transform.position + Vector3.up;
            for (int i = 0; i < bulletsPerShot; i++)
            {
                float angle = bulletsPerShot > 1 ? (i - (bulletsPerShot - 1) * 0.5f) * spreadDegrees : Random.Range(-2f, 2f);
                bool crit = Random.value < critChance;
                float dmg = damage * damageMultiplier * (crit ? critMultiplier : 1f);
                Bullet.Fire(from, Quaternion.Euler(0f, angle, 0f) * direction, dmg, crit, knockback * (crit ? 1.6f : 1f), bulletSpeed, pierce);
            }

            recoil = 1f;
            if (muzzleFlash != null)
            {
                muzzleFlash.SetActive(true);
                muzzleFlash.transform.localScale = Vector3.one * Random.Range(0.35f, 0.55f);
                muzzleFlash.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                flashTimer = 0.05f;
            }
            Sfx.Play(SfxId.Shot, 0.45f, 0.12f, 0.04f);
            CameraFollow.Shake(0.025f);
        }
    }
}
