using UnityEngine;

namespace Duck
{
    /// <summary>Coin on the ground: pops out, spins, flies to the duck when it is near.</summary>
    public class CoinPickup : MonoBehaviour
    {
        public int value = 1;
        public float magnetRadius = 3f;
        public float collectRadius = 0.6f;

        Vector3 velocity;
        float age;
        bool magnet;

        public static void Drop(Vector3 pos, int value)
        {
            if (GameAssets.I == null || GameAssets.I.coinPrefab == null) return;
            var go = Pool.Spawn(GameAssets.I.coinPrefab, pos + Vector3.up * 0.5f, Quaternion.identity);
            var c = go.GetComponent<CoinPickup>();
            c.value = value;
            c.age = 0f;
            c.magnet = false;
            c.velocity = new Vector3(Random.Range(-2f, 2f), 5f, Random.Range(-2f, 2f));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            transform.Rotate(0f, 360f * dt, 0f, Space.World);

            var duck = DuckController.Instance;
            Vector3 p = transform.position;

            if (!magnet && age > 0.4f && duck != null)
            {
                Vector3 d = duck.transform.position - p;
                d.y = 0f;
                if (d.sqrMagnitude < magnetRadius * magnetRadius) magnet = true;
            }

            if (magnet && duck != null)
            {
                Vector3 to = duck.transform.position + Vector3.up * 0.8f - p;
                velocity = Vector3.Lerp(velocity, to.normalized * 14f, dt * 10f);
                p += velocity * dt;
                transform.position = p;
                if (to.sqrMagnitude < collectRadius * collectRadius)
                {
                    Wallet.Instance?.Add(value);
                    Sfx.Play(SfxId.Coin, 0.6f, 0.1f, 0.02f);
                    Pool.Despawn(gameObject);
                }
                return;
            }

            // Pop out and settle with a small bounce.
            velocity.y -= 20f * dt;
            p += velocity * dt;
            if (p.y < 0.35f)
            {
                p.y = 0.35f;
                velocity.y = Mathf.Abs(velocity.y) * 0.3f;
                velocity.x *= 0.5f;
                velocity.z *= 0.5f;
            }
            transform.position = p;
        }
    }
}
