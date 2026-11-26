using UnityEngine;

namespace Duck
{
    /// <summary>Builds placeholder low-poly building models from primitives (one look per level).</summary>
    public static class BuildingFactory
    {
        static readonly Color Wood = new Color(0.55f, 0.38f, 0.22f);
        static readonly Color Canvas = new Color(0.85f, 0.78f, 0.6f);
        static readonly Color Stone = new Color(0.6f, 0.6f, 0.62f);
        static readonly Color Steel = new Color(0.35f, 0.4f, 0.45f);
        static readonly Color Gold = new Color(1f, 0.8f, 0.2f);
        static readonly Color Red = new Color(0.8f, 0.2f, 0.2f);
        static readonly Color Dark = new Color(0.15f, 0.15f, 0.17f);

        public static float Height(BuildingType type, int level)
        {
            switch (type)
            {
                case BuildingType.Base: return 1.6f + level * 0.5f;
                case BuildingType.Tower: return 1.6f + level * 0.3f;
                case BuildingType.Wall: return 0.8f + level * 0.3f;
                default: return 1.2f;
            }
        }

        public static Building Create(BuildingConfig cfg, int level, BuildSlot slot)
        {
            var root = new GameObject($"{cfg.name}_L{level}");
            root.transform.SetParent(slot.transform, false);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            var b = root.AddComponent<Building>();
            b.visual = visual;

            switch (cfg.type)
            {
                case BuildingType.Base: BuildBase(visual, level); break;
                case BuildingType.Mine: BuildMine(visual, level); break;
                case BuildingType.Tower: BuildTower(visual, level, b); break;
                case BuildingType.Wall: BuildWall(visual, level, cfg); break;
                case BuildingType.Barracks: BuildBarracks(visual, level); break;
            }

            b.Init(cfg, level, slot, Height(cfg.type, level) + 0.5f);
            return b;
        }

        static void BuildBase(Transform v, int level)
        {
            if (level <= 1)
            {
                P(PrimitiveType.Cube, v, Canvas, new Vector3(0, 0.6f, 0), new Vector3(2.4f, 1.2f, 2f));
                var roof = P(PrimitiveType.Cube, v, Red, new Vector3(0, 1.3f, 0), new Vector3(1.6f, 1.6f, 2.1f));
                roof.transform.localRotation = Quaternion.Euler(0, 0, 45f);
                roof.transform.localScale = new Vector3(1.3f, 1.3f, 2.1f);
            }
            else
            {
                float h = level == 2 ? 1.6f : 2.2f;
                P(PrimitiveType.Cube, v, level == 2 ? Wood : Stone, new Vector3(0, h * 0.5f, 0), new Vector3(2.8f, h, 2.6f));
                P(PrimitiveType.Cube, v, Red, new Vector3(0, h + 0.25f, 0), new Vector3(3f, 0.5f, 2.8f));
                P(PrimitiveType.Cube, v, Dark, new Vector3(0, 0.5f, -1.31f), new Vector3(0.8f, 1f, 0.05f));
                if (level >= 3)
                {
                    foreach (var c in new[] { new Vector3(-1.5f, 0, -1.4f), new Vector3(1.5f, 0, -1.4f), new Vector3(-1.5f, 0, 1.4f), new Vector3(1.5f, 0, 1.4f) })
                        P(PrimitiveType.Cylinder, v, Stone, c + Vector3.up * 1.4f, new Vector3(0.7f, 1.4f, 0.7f));
                }
            }
            P(PrimitiveType.Cylinder, v, Dark, new Vector3(1f, 2.4f + level * 0.4f, 0.8f), new Vector3(0.05f, 0.8f, 0.05f));
            P(PrimitiveType.Cube, v, Gold, new Vector3(1.25f, 3f + level * 0.4f, 0.8f), new Vector3(0.5f, 0.3f, 0.03f));
        }

        static void BuildMine(Transform v, int level)
        {
            P(PrimitiveType.Cube, v, Wood, new Vector3(0, 0.4f, 0), new Vector3(1.6f, 0.8f, 1.6f));
            P(PrimitiveType.Cylinder, v, Dark, new Vector3(0, 0.81f, 0), new Vector3(0.9f, 0.02f, 0.9f));
            for (int i = 0; i < 2 + level * 2; i++)
            {
                float a = i * 2.4f;
                P(PrimitiveType.Sphere, v, Gold, new Vector3(Mathf.Cos(a) * 0.55f, 0.95f, Mathf.Sin(a) * 0.55f), Vector3.one * 0.3f);
            }
            P(PrimitiveType.Cube, v, Wood, new Vector3(-0.7f, 1.1f, 0), new Vector3(0.12f, 1.4f, 0.12f));
            P(PrimitiveType.Cube, v, Wood, new Vector3(0.7f, 1.1f, 0), new Vector3(0.12f, 1.4f, 0.12f));
            P(PrimitiveType.Cube, v, Wood, new Vector3(0, 1.8f, 0), new Vector3(1.5f, 0.12f, 0.12f));
        }

        static void BuildTower(Transform v, int level, Building b)
        {
            float h = Height(BuildingType.Tower, level) - 0.6f;
            Color body = level >= 3 ? Steel : level == 2 ? Stone : Wood;
            P(PrimitiveType.Cylinder, v, body, new Vector3(0, h * 0.5f, 0), new Vector3(1.1f, h * 0.5f, 1.1f));
            var head = new GameObject("Head").transform;
            head.SetParent(v, false);
            head.localPosition = new Vector3(0, h + 0.3f, 0);
            P(PrimitiveType.Cube, head, Steel, Vector3.zero, new Vector3(0.8f, 0.5f, 0.8f));
            int barrels = Mathf.Clamp(level, 1, 3);
            for (int i = 0; i < barrels; i++)
            {
                float x = (i - (barrels - 1) * 0.5f) * 0.22f;
                P(PrimitiveType.Cube, head, Dark, new Vector3(x, 0.05f, 0.6f), new Vector3(0.12f, 0.12f, 0.8f));
            }
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(head, false);
            muzzle.localPosition = new Vector3(0, 0.05f, 1.05f);
            b.turretHead = head;
            b.muzzle = muzzle;
        }

        static void BuildBarracks(Transform v, int level)
        {
            Color body = level >= 2 ? Stone : Wood;
            P(PrimitiveType.Cube, v, body, new Vector3(0, 0.55f, 0), new Vector3(1.8f, 1.1f, 1.4f));
            var roof = P(PrimitiveType.Cube, v, new Color(0.25f, 0.4f, 0.75f), new Vector3(0, 1.25f, 0), new Vector3(1.2f, 1.2f, 1.5f));
            roof.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            P(PrimitiveType.Cube, v, Dark, new Vector3(0, 0.4f, 0.71f), new Vector3(0.5f, 0.8f, 0.05f));
            P(PrimitiveType.Cylinder, v, Dark, new Vector3(-0.7f, 1.9f, 0.5f), new Vector3(0.05f, 0.6f, 0.05f));
            P(PrimitiveType.Cube, v, new Color(0.35f, 0.6f, 1f), new Vector3(-0.5f, 2.3f, 0.5f), new Vector3(0.4f, 0.25f, 0.03f));
        }

        static void BuildWall(Transform v, int level, BuildingConfig cfg)
        {
            float w = cfg.halfExtents.x * 2f;
            float h = Height(BuildingType.Wall, level);
            Color c = level >= 3 ? Steel : level == 2 ? Stone : Wood;
            P(PrimitiveType.Cube, v, c, new Vector3(0, h * 0.5f, 0), new Vector3(w, h, cfg.halfExtents.y * 2f));
            int posts = Mathf.RoundToInt(w / 1.2f) + 1;
            for (int i = 0; i < posts; i++)
            {
                float x = -w * 0.5f + i * (w / (posts - 1));
                P(PrimitiveType.Cube, v, level >= 2 ? Dark : Wood, new Vector3(x, h + 0.15f, 0), new Vector3(0.25f, 0.3f + level * 0.1f, cfg.halfExtents.y * 2.2f));
            }
        }

        static GameObject P(PrimitiveType type, Transform parent, Color color, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = GameAssets.Lit(color);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }
    }
}
