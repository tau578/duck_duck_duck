using System.Collections.Generic;
using UnityEngine;

namespace Duck
{
    /// <summary>Shared materials and prefabs, created by SceneBuilder and referenced from the scene.</summary>
    [CreateAssetMenu(menuName = "Duck/Game Assets")]
    public class GameAssets : ScriptableObject
    {
        public static GameAssets I;

        public Material litTemplate;
        public Material flashMaterial;
        public GameObject bulletPrefab;
        public GameObject enemyPrefab;
        public GameObject coinPrefab;

        static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

        public static Material Lit(Color c)
        {
            if (cache.TryGetValue(c, out var m) && m != null) return m;
            m = new Material(I.litTemplate);
            m.SetColor("_BaseColor", c);
            m.enableInstancing = true;
            cache[c] = m;
            return m;
        }
    }
}
