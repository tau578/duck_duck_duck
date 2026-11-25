using UnityEngine;

namespace Duck
{
    /// <summary>Day-time warning: red skull marker with the enemy count and the road drawn on the ground.</summary>
    public class SpawnIndicator : MonoBehaviour
    {
        public EnemyPath path;
        public GameObject marker;
        LineRenderer line;
        WorldLabel label;
        float t;
        int pending = -1;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
        }

        void Start()
        {
            if (line != null && path != null)
            {
                line.positionCount = path.points.Length;
                for (int i = 0; i < path.points.Length; i++) line.SetPosition(i, path.points[i] + Vector3.up * 0.05f);
            }
            label = WorldLabel.Create(marker != null ? marker.transform : transform, Vector3.up * 1.6f, 80, new Color(1f, 0.3f, 0.25f));
            if (pending >= 0) Show(pending);
        }

        public void Show(int count)
        {
            pending = count;
            bool on = count > 0;
            if (marker != null) marker.SetActive(on);
            if (line != null) line.enabled = on;
            if (label != null)
            {
                label.SetVisible(on);
                label.Set(count.ToString());
            }
        }

        public void Hide() => Show(0);

        void Update()
        {
            if (line == null || !line.enabled) return;
            t += Time.deltaTime;
            var c = new Color(1f, 0.15f, 0.1f, 0.45f + 0.25f * Mathf.Sin(t * 4f));
            line.startColor = c;
            line.endColor = c;
            if (marker != null) marker.transform.localScale = Vector3.one * (1f + 0.1f * Mathf.Sin(t * 5f));
        }
    }
}
