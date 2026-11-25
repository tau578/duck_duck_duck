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

        Vector3 velocity;

        void Start()
        {
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            if (target != null) transform.position = Desired();
        }

        void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
        }

        Vector3 Desired()
        {
            Vector3 focus = target.position;
            focus.x = Mathf.Clamp(focus.x, -clampHalfSize.x, clampHalfSize.x);
            focus.z = Mathf.Clamp(focus.z, -clampHalfSize.y, clampHalfSize.y);
            return focus + offset;
        }
    }
}
