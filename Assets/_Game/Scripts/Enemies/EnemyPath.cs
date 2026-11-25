using UnityEngine;

namespace Duck
{
    /// <summary>Road from a spawn point to the base. Points are in world space; first point = spawn.</summary>
    public class EnemyPath : MonoBehaviour
    {
        public Vector3[] points;
        public float laneHalfWidth = 1.2f;

        public Vector3 Spawn => points[0];

        void OnDrawGizmos()
        {
            if (points == null) return;
            Gizmos.color = Color.red;
            for (int i = 0; i + 1 < points.Length; i++) Gizmos.DrawLine(points[i], points[i + 1]);
        }
    }
}
