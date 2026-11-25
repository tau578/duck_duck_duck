using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    public class Pooled : MonoBehaviour
    {
        public GameObject prefab;
    }

    /// <summary>Minimal prefab pool. Survives scene reloads (destroyed entries are skipped).</summary>
    public static class Pool
    {
        static readonly Dictionary<GameObject, Stack<GameObject>> free = new Dictionary<GameObject, Stack<GameObject>>();

        public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            if (!free.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<GameObject>();
                free[prefab] = stack;
            }

            GameObject go = null;
            while (go == null && stack.Count > 0) go = stack.Pop();

            if (go == null)
            {
                go = Object.Instantiate(prefab, pos, rot);
                go.AddComponent<Pooled>().prefab = prefab;
            }
            else
            {
                go.transform.SetPositionAndRotation(pos, rot);
                go.SetActive(true);
            }
            return go;
        }

        public static void Despawn(GameObject go)
        {
            var p = go.GetComponent<Pooled>();
            if (p == null || p.prefab == null)
            {
                Object.Destroy(go);
                return;
            }
            go.SetActive(false);
            if (!free.TryGetValue(p.prefab, out var stack))
            {
                stack = new Stack<GameObject>();
                free[p.prefab] = stack;
            }
            stack.Push(go);
        }
    }
}
