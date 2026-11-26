using UnityEngine;

namespace Duck
{
    /// <summary>Top-down angled camera that smoothly follows the target and stays inside the map.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 20f, -12f);
        public float pitch = 58f;
        public float smoothTime = 0.15f;
        public Vector2 clampHalfSize = new Vector2(8f, 14f);

        public float maxShake = 0.6f;
        [Tooltip("Shifts the view forward so the duck sits lower on screen and the top HUD covers less of the battlefield.")]
        public float lookAhead = 2f;
        public static CameraFollow Instance { get; private set; }

        Vector3 velocity;
        Vector3 smoothPos;
        float trauma;

        void Awake() { Instance = this; }

        /// <summary>Adds screen shake (0..1). Strength grows with the square of trauma.</summary>
        public static void Shake(float amount)
        {
            if (Instance != null) Instance.trauma = Mathf.Min(1f, Instance.trauma + amount * GameSettings.ShakeScale);
        }

        void Start()
        {
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            if (target != null) transform.position = Desired();
            smoothPos = transform.position;
        }

        void LateUpdate()
        {
            if (target == null) return;
            smoothPos = Vector3.SmoothDamp(smoothPos, Desired(), ref velocity, smoothTime);
            trauma = Mathf.MoveTowards(trauma, 0f, Time.unscaledDeltaTime * 1.8f);
            Vector3 shake = trauma > 0f ? Random.insideUnitSphere * (trauma * trauma * maxShake) : Vector3.zero;
            transform.position = smoothPos + shake;
        }

        Vector3 Desired()
        {
            Vector3 focus = target.position;
            focus.x = Mathf.Clamp(focus.x, -clampHalfSize.x, clampHalfSize.x);
            focus.z = Mathf.Clamp(focus.z, -clampHalfSize.y, clampHalfSize.y);
            return focus + Vector3.forward * lookAhead + offset;
        }
    }
}
