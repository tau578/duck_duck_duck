using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Duck.EditorTools
{
    /// <summary>
    /// Builds materials, prefabs, balance data and the Game scene from code, so the AI (via Unity MCP or batch mode)
    /// can regenerate everything without manual clicking.
    ///   Duck/Build Game Scene                  - keeps existing balance assets (tweaks in the Inspector survive)
    ///   Duck/Reset Data To Defaults And Build  - overwrites balance assets with the defaults below
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Game.unity";
        const string MatFolder = "Assets/_Game/Art/Materials";
        const string DataFolder = "Assets/_Game/Data";
        const string PrefabFolder = "Assets/_Game/Prefabs";

        [MenuItem("Duck/Build Game Scene")]
        public static void BuildGameScene() => Build(false);

        [MenuItem("Duck/Reset Data To Defaults And Build")]
        public static void ResetDataAndBuild() => Build(true);

        class Data
        {
            public BuildingConfig baseCfg, mine, tower, wall;
            public EnemyConfig zombie, runner, bat, brute, ogre;
            public ChapterConfig chapter1;
        }

        static void Build(bool resetData)
        {
            EnsureFolder("Assets/_Game/Scenes");
            EnsureFolder(MatFolder);
            EnsureFolder(DataFolder);
            EnsureFolder(PrefabFolder);

            GameAssets assets = CreateGameAssets();
            Data data = CreateData(resetData);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Managers.
            var game = new GameObject("Game");
            game.AddComponent<GameBootstrap>();
            var gm = game.AddComponent<GameManager>();
            game.AddComponent<Wallet>();
            game.AddComponent<EnemyManager>();
            var spawner = game.AddComponent<WaveSpawner>();
            game.AddComponent<Sfx>();
            gm.assets = assets;
            gm.chapter = data.chapter1;
            gm.spawner = spawner;

            gm.sun = CreateLight();
            CreateEnvironment();

            // Roads: 0 = north, 1 = east (used from chapter 2).
            var north = CreatePath("Path_North", new[] { new Vector3(0, 0, 22f), new Vector3(0, 0, 12f), new Vector3(0, 0, 0f) });
            var east = CreatePath("Path_East", new[] { new Vector3(15f, 0, 15f), new Vector3(7f, 0, 15f), new Vector3(1.5f, 0, 12.5f), new Vector3(0, 0, 0f) });
            spawner.paths = new List<EnemyPath> { north, east };
            gm.indicators = new List<SpawnIndicator> { CreateIndicator(north), CreateIndicator(east) };

            // Building slots (fixed places like in the original).
            var slotsRoot = new GameObject("Slots").transform;
            gm.baseSlot = CreateSlot(slotsRoot, data.baseCfg, new Vector3(0, 0, 0), 1);
            CreateSlot(slotsRoot, data.mine, new Vector3(-4.5f, 0, -1.5f), 0);
            CreateSlot(slotsRoot, data.mine, new Vector3(4.5f, 0, -1.5f), 0);
            CreateSlot(slotsRoot, data.tower, new Vector3(-3f, 0, 4.5f), 0);
            CreateSlot(slotsRoot, data.tower, new Vector3(3f, 0, 4.5f), 0);
            CreateSlot(slotsRoot, data.tower, new Vector3(-3f, 0, 8f), 0);
            CreateSlot(slotsRoot, data.tower, new Vector3(3f, 0, 8f), 0);
            CreateSlot(slotsRoot, data.wall, new Vector3(0, 0, 10.5f), 0);

            GameObject duck = CreateDuck();
            CreateCamera(duck.transform);
            VirtualJoystick stick = CreateUI();
            duck.GetComponent<DuckController>().joystick = stick;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.75f, 0.8f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.55f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.28f, 0.25f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[Duck] Game scene built: {ScenePath} (data reset: {resetData})");
        }

        // ================= assets =================

        static GameAssets CreateGameAssets()
        {
            var assets = LoadOrCreate<GameAssets>($"{DataFolder}/GameAssets.asset", true, a => { });
            assets.litTemplate = Mat("Template_Lit", Color.white);
            assets.flashMaterial = Mat("FX_Flash", Color.white, "Universal Render Pipeline/Unlit");
            assets.bulletPrefab = CreateBulletPrefab();
            assets.enemyPrefab = CreateEnemyPrefab();
            assets.coinPrefab = CreateCoinPrefab();
            EditorUtility.SetDirty(assets);
            return assets;
        }

        static GameObject CreateBulletPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Bullet";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.22f;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Mat("FX_Bullet", new Color(1f, 0.9f, 0.35f), "Universal Render Pipeline/Unlit");
            r.shadowCastingMode = ShadowCastingMode.Off;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.07f;
            trail.widthMultiplier = 0.2f;
            trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            trail.sharedMaterial = LineMat();
            trail.startColor = new Color(1f, 0.9f, 0.4f, 0.9f);
            trail.endColor = new Color(1f, 0.6f, 0.1f, 0f);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<Bullet>();
            return SavePrefab(go, "Bullet");
        }

        static GameObject CreateEnemyPrefab()
        {
            var root = new GameObject("Enemy");
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            var body = new GameObject("Default").transform;
            body.SetParent(visual, false);

            Material skin = Mat("Enemy_Body", new Color(0.45f, 0.65f, 0.35f));
            Material head = Mat("Enemy_Head", new Color(0.62f, 0.72f, 0.55f));
            Material eyes = Mat("Enemy_Eyes", new Color(0.9f, 0.1f, 0.1f), "Universal Render Pipeline/Unlit");

            var b = Prim(PrimitiveType.Capsule, "Body", body, skin, false);
            b.transform.localPosition = new Vector3(0, 0.55f, 0);
            b.transform.localScale = new Vector3(0.7f, 0.55f, 0.6f);
            var h = Prim(PrimitiveType.Sphere, "Head", body, head, false);
            h.transform.localPosition = new Vector3(0, 1.25f, 0.05f);
            h.transform.localScale = Vector3.one * 0.55f;
            var e = Prim(PrimitiveType.Cube, "Eyes", body, eyes, false);
            e.transform.localPosition = new Vector3(0, 1.3f, 0.3f);
            e.transform.localScale = new Vector3(0.35f, 0.08f, 0.05f);
            e.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            foreach (float x in new[] { -0.35f, 0.35f })
            {
                var arm = Prim(PrimitiveType.Cube, "Arm", body, skin, false);
                arm.transform.localPosition = new Vector3(x, 0.85f, 0.3f);
                arm.transform.localScale = new Vector3(0.15f, 0.15f, 0.6f);
                arm.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            var enemy = root.AddComponent<Enemy>();
            enemy.visual = visual;
            return SavePrefab(root, "Enemy");
        }

        static GameObject CreateCoinPrefab()
        {
            var root = new GameObject("Coin");
            var mesh = Prim(PrimitiveType.Cylinder, "Mesh", root.transform, Mat("Coin", new Color(1f, 0.8f, 0.15f)), false);
            mesh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            mesh.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            root.AddComponent<CoinPickup>();
            return SavePrefab(root, "Coin");
        }

        static GameObject SavePrefab(GameObject go, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ================= balance data =================

        static Data CreateData(bool reset)
        {
            var d = new Data();

            d.baseCfg = LoadOrCreate<BuildingConfig>($"{DataFolder}/Building_Base.asset", reset, c =>
            {
                c.type = BuildingType.Base; c.displayName = "База"; c.displayNameEn = "Base"; c.halfExtents = new Vector2(1.5f, 1.4f); c.wallOnly = false;
                c.levels = new[]
                {
                    new BuildingLevel { cost = 0, hp = 400, income = 4 },
                    new BuildingLevel { cost = 8, hp = 600, income = 7 },
                    new BuildingLevel { cost = 12, hp = 850, income = 12 },
                };
            });
            d.mine = LoadOrCreate<BuildingConfig>($"{DataFolder}/Building_Mine.asset", reset, c =>
            {
                c.type = BuildingType.Mine; c.displayName = "Шахта"; c.displayNameEn = "Gold Mine"; c.halfExtents = new Vector2(0.8f, 0.8f); c.wallOnly = false;
                c.levels = new[]
                {
                    new BuildingLevel { cost = 3, hp = 150, income = 3 },
                    new BuildingLevel { cost = 6, hp = 220, income = 5 },
                };
            });
            d.tower = LoadOrCreate<BuildingConfig>($"{DataFolder}/Building_Tower.asset", reset, c =>
            {
                c.type = BuildingType.Tower; c.displayName = "Башня"; c.displayNameEn = "Tower"; c.halfExtents = new Vector2(0.6f, 0.6f); c.wallOnly = false;
                c.levels = new[]
                {
                    new BuildingLevel { cost = 1, hp = 150, damage = 8, fireRate = 2f, range = 8f, shots = 1 },
                    new BuildingLevel { cost = 3, hp = 220, damage = 14, fireRate = 2.5f, range = 8.5f, shots = 1 },
                    new BuildingLevel { cost = 6, hp = 320, damage = 14, fireRate = 2.5f, range = 9f, shots = 3 },
                };
            });
            d.wall = LoadOrCreate<BuildingConfig>($"{DataFolder}/Building_Wall.asset", reset, c =>
            {
                c.type = BuildingType.Wall; c.displayName = "Стена"; c.displayNameEn = "Wall"; c.halfExtents = new Vector2(3f, 0.45f); c.wallOnly = true;
                c.levels = new[]
                {
                    new BuildingLevel { cost = 1, hp = 300 },
                    new BuildingLevel { cost = 3, hp = 600 },
                    new BuildingLevel { cost = 6, hp = 1000 },
                };
            });

            d.zombie = MakeEnemy("Enemy_Zombie", reset, "Зомби", "Zombie", 30, 1.6f, 8, 1f, 0.9f, 1f, new Color(0.45f, 0.65f, 0.35f), 0.3f, 1);
            d.runner = MakeEnemy("Enemy_Runner", reset, "Бегун", "Runner", 18, 3.2f, 5, 0.8f, 0.9f, 0.8f, new Color(0.85f, 0.55f, 0.25f), 0.2f, 1);
            d.bat = MakeEnemy("Enemy_Bat", reset, "Летучая мышь", "Bat", 15, 2.6f, 6, 1f, 0.9f, 0.7f, new Color(0.5f, 0.3f, 0.6f), 0.2f, 1, c => c.flying = true);
            d.brute = MakeEnemy("Enemy_Brute", reset, "Громила в бочке", "Barrel Brute", 400, 1.1f, 20, 1.2f, 1.2f, 1.6f, new Color(0.4f, 0.4f, 0.45f), 1f, 3, c => { c.isElite = true; c.knockResist = 0.7f; });
            d.ogre = MakeEnemy("Enemy_Ogre", reset, "Огр", "Ogre", 1800, 1.0f, 45, 1.5f, 1.6f, 2.6f, new Color(0.3f, 0.45f, 0.25f), 1f, 5, c => { c.isBoss = true; c.knockResist = 0.95f; c.aggroRange = 3.5f; });

            d.chapter1 = LoadOrCreate<ChapterConfig>($"{DataFolder}/Chapter_1.asset", reset, c =>
            {
                c.displayName = "Глава 1. Пустошь";
                c.displayNameEn = "Chapter 1. Wasteland";
                c.startCoins = 7;
                c.waves = new[]
                {
                    new WaveConfig { kind = WaveKind.Normal, groups = new[]
                    {
                        new SpawnGroup { enemy = d.zombie, count = 12, pathIndex = 0, interval = 0.9f },
                    }},
                    new WaveConfig { kind = WaveKind.Normal, groups = new[]
                    {
                        new SpawnGroup { enemy = d.zombie, count = 20, pathIndex = 0, interval = 0.5f },
                        new SpawnGroup { enemy = d.zombie, count = 10, pathIndex = 0, interval = 0.4f, delay = 8f },
                    }},
                    new WaveConfig { kind = WaveKind.Boss, groups = new[]
                    {
                        new SpawnGroup { enemy = d.zombie, count = 15, pathIndex = 0, interval = 0.5f },
                        new SpawnGroup { enemy = d.ogre, count = 1, pathIndex = 0, interval = 0f, delay = 4f },
                        new SpawnGroup { enemy = d.zombie, count = 15, pathIndex = 0, interval = 0.4f, delay = 10f },
                    }},
                };
            });

            AssetDatabase.SaveAssets();
            return d;
        }

        static EnemyConfig MakeEnemy(string file, bool reset, string name, string nameEn, float hp, float speed, float dmg, float interval,
            float range, float scale, Color color, float coinChance, int coinValue, Action<EnemyConfig> extra = null)
        {
            return LoadOrCreate<EnemyConfig>($"{DataFolder}/{file}.asset", reset, c =>
            {
                c.displayName = name; c.displayNameEn = nameEn; c.maxHp = hp; c.speed = speed; c.damage = dmg; c.attackInterval = interval;
                c.attackRange = range; c.scale = scale; c.bodyColor = color; c.coinChance = coinChance; c.coinValue = coinValue;
                c.flying = false; c.isElite = false; c.isBoss = false; c.knockResist = 0f; c.aggroRange = 2.5f; c.xp = 1;
                extra?.Invoke(c);
            });
        }

        static T LoadOrCreate<T>(string path, bool overwrite, Action<T> fill) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                fill(asset);
                AssetDatabase.CreateAsset(asset, path);
            }
            else if (overwrite)
            {
                fill(asset);
                EditorUtility.SetDirty(asset);
            }
            return asset;
        }

        // ================= scene parts =================

        static Light CreateLight()
        {
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            return light;
        }

        static void CreateEnvironment()
        {
            var root = new GameObject("Environment").transform;

            var ground = Prim(PrimitiveType.Plane, "Ground", root, Mat("Ground", new Color(0.55f, 0.62f, 0.38f)), false);
            ground.transform.localScale = new Vector3(3.4f, 1f, 5f); // 34 x 50 units

            Material trunk = Mat("Trunk", new Color(0.4f, 0.27f, 0.17f));
            Material leaves = Mat("Leaves", new Color(0.25f, 0.5f, 0.25f));
            Material rock = Mat("Rock", new Color(0.5f, 0.5f, 0.48f));
            Vector3[] trees =
            {
                new Vector3(-12f, 0, 18f), new Vector3(-13f, 0, 6f), new Vector3(-11f, 0, -14f), new Vector3(12f, 0, -17f),
                new Vector3(-9f, 0, 21f), new Vector3(11f, 0, 22f), new Vector3(13f, 0, -5f), new Vector3(-14f, 0, -4f),
                new Vector3(9f, 0, 2f), new Vector3(-8f, 0, 13f),
            };
            foreach (var t in trees)
            {
                var tree = new GameObject("Tree").transform;
                tree.SetParent(root);
                tree.position = t;
                float s = UnityEngine.Random.Range(0.8f, 1.3f);
                tree.localScale = Vector3.one * s;
                var tr = Prim(PrimitiveType.Cylinder, "Trunk", tree, trunk, false);
                tr.transform.localPosition = new Vector3(0, 0.6f, 0);
                tr.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
                var lv = Prim(PrimitiveType.Sphere, "Leaves", tree, leaves, false);
                lv.transform.localPosition = new Vector3(0, 1.8f, 0);
                lv.transform.localScale = Vector3.one * 1.8f;
            }
            Vector3[] rocks = { new Vector3(-6f, 0, 17f), new Vector3(7f, 0, -10f), new Vector3(-7f, 0, -9f), new Vector3(10f, 0, 8f) };
            foreach (var r in rocks)
            {
                var go = Prim(PrimitiveType.Sphere, "Rock", root, rock, false);
                go.transform.position = r + Vector3.up * 0.2f;
                go.transform.localScale = new Vector3(1.4f, 0.7f, 1.1f);
            }
        }

        static EnemyPath CreatePath(string name, Vector3[] points)
        {
            var go = new GameObject(name);
            var path = go.AddComponent<EnemyPath>();
            path.points = points;

            Material dirt = Mat("Path", new Color(0.6f, 0.5f, 0.36f));
            for (int i = 0; i + 1 < points.Length; i++)
            {
                Vector3 a = points[i], b = points[i + 1];
                Vector3 mid = (a + b) * 0.5f;
                float len = Vector3.Distance(a, b);
                var strip = Prim(PrimitiveType.Cube, "Road", go.transform, dirt, false);
                strip.transform.position = mid + Vector3.up * 0.01f;
                strip.transform.rotation = Quaternion.LookRotation(b - a);
                strip.transform.localScale = new Vector3(3f, 0.02f, len + 3f);
                strip.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            return path;
        }

        static SpawnIndicator CreateIndicator(EnemyPath path)
        {
            var go = new GameObject($"Indicator_{path.name}");
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.alignment = LineAlignment.TransformZ;
            line.widthMultiplier = 0.45f;
            line.sharedMaterial = LineMat();
            line.numCornerVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off;

            var marker = new GameObject("Skull").transform;
            marker.position = path.points[0] + Vector3.up * 1.2f;
            var skull = Prim(PrimitiveType.Sphere, "Head", marker, Mat("Skull", new Color(0.85f, 0.15f, 0.12f), "Universal Render Pipeline/Unlit"), false);
            skull.transform.localScale = Vector3.one * 1.1f;
            foreach (float x in new[] { -0.22f, 0.22f })
            {
                var eye = Prim(PrimitiveType.Sphere, "Eye", marker, Mat("Duck_Eye", new Color(0.08f, 0.08f, 0.08f)), false);
                eye.transform.localPosition = new Vector3(x, 0.1f, -0.45f);
                eye.transform.localScale = Vector3.one * 0.28f;
            }

            var ind = go.AddComponent<SpawnIndicator>();
            ind.path = path;
            ind.marker = marker.gameObject;
            return ind;
        }

        static BuildSlot CreateSlot(Transform parent, BuildingConfig cfg, Vector3 pos, int startLevel)
        {
            var go = new GameObject($"Slot_{cfg.displayName}");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var marker = new GameObject("Marker").transform;
            marker.SetParent(go.transform, false);
            float size = Mathf.Max(cfg.halfExtents.x, cfg.halfExtents.y) * 2f + 0.4f;
            var ring = Prim(PrimitiveType.Cylinder, "Ring", marker, Mat("Slot", new Color(1f, 0.85f, 0.3f)), false);
            ring.transform.localPosition = Vector3.up * 0.03f;
            ring.transform.localScale = cfg.type == BuildingType.Wall
                ? new Vector3(cfg.halfExtents.x * 2f, 0.03f, 1f)
                : new Vector3(size, 0.03f, size);
            ring.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var plusA = Prim(PrimitiveType.Cube, "Plus", marker, Mat("Slot_Plus", new Color(1f, 1f, 1f)), false);
            plusA.transform.localPosition = Vector3.up * 0.07f;
            plusA.transform.localScale = new Vector3(0.6f, 0.04f, 0.15f);
            var plusB = Prim(PrimitiveType.Cube, "Plus", marker, Mat("Slot_Plus", new Color(1f, 1f, 1f)), false);
            plusB.transform.localPosition = Vector3.up * 0.07f;
            plusB.transform.localScale = new Vector3(0.15f, 0.04f, 0.6f);

            var slot = go.AddComponent<BuildSlot>();
            slot.config = cfg;
            slot.startLevel = startLevel;
            slot.marker = marker.gameObject;
            return slot;
        }

        static GameObject CreateDuck()
        {
            var duck = new GameObject("Duck");
            duck.transform.position = new Vector3(0f, 0f, -3.5f);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(duck.transform, false);

            Material yellow = Mat("Duck_Body", new Color(1f, 0.85f, 0.2f));
            Material orange = Mat("Duck_Beak", new Color(1f, 0.5f, 0.1f));
            Material black = Mat("Duck_Eye", new Color(0.08f, 0.08f, 0.08f));
            Material steel = Mat("Duck_Gun", new Color(0.25f, 0.3f, 0.35f));
            Material helmet = Mat("Duck_Helmet", new Color(0.3f, 0.45f, 0.25f));

            var body = Prim(PrimitiveType.Sphere, "Body", visual, yellow, false);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale = new Vector3(1f, 0.9f, 1.15f);
            var head = Prim(PrimitiveType.Sphere, "Head", visual, yellow, false);
            head.transform.localPosition = new Vector3(0f, 1.25f, 0.25f);
            head.transform.localScale = Vector3.one * 0.7f;
            var cap = Prim(PrimitiveType.Sphere, "Helmet", visual, helmet, false);
            cap.transform.localPosition = new Vector3(0f, 1.42f, 0.22f);
            cap.transform.localScale = new Vector3(0.75f, 0.4f, 0.75f);
            var beak = Prim(PrimitiveType.Cube, "Beak", visual, orange, false);
            beak.transform.localPosition = new Vector3(0f, 1.18f, 0.66f);
            beak.transform.localScale = new Vector3(0.38f, 0.12f, 0.3f);
            foreach (float x in new[] { -0.17f, 0.17f })
            {
                var eye = Prim(PrimitiveType.Sphere, "Eye", visual, black, false);
                eye.transform.localPosition = new Vector3(x, 1.32f, 0.53f);
                eye.transform.localScale = Vector3.one * 0.12f;
            }

            var gun = new GameObject("Gun").transform;
            gun.SetParent(visual, false);
            gun.localPosition = new Vector3(0.45f, 0.65f, 0.4f);
            var barrel = Prim(PrimitiveType.Cube, "Barrel", gun, steel, false);
            barrel.transform.localScale = new Vector3(0.2f, 0.2f, 0.8f);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(gun, false);
            muzzle.localPosition = new Vector3(0f, 0f, 0.5f);
            var flash = Prim(PrimitiveType.Sphere, "MuzzleFlash", muzzle, Mat("FX_Muzzle", new Color(1f, 0.95f, 0.6f), "Universal Render Pipeline/Unlit"), false);
            flash.transform.localScale = new Vector3(0.45f, 0.45f, 0.8f);
            flash.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var ctrl = duck.AddComponent<DuckController>();
            ctrl.visual = visual;
            var shooter = duck.AddComponent<AutoShooter>();
            shooter.gun = gun;
            shooter.muzzle = muzzle;
            shooter.muzzleFlash = flash;
            duck.AddComponent<PlayerHealth>();
            return duck;
        }

        static Camera CreateCamera(Transform target)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.35f, 0.45f, 0.3f);
            go.AddComponent<AudioListener>();
            var follow = go.AddComponent<CameraFollow>();
            follow.target = target;
            go.transform.rotation = Quaternion.Euler(follow.pitch, 0f, 0f);
            go.transform.position = target.position + follow.offset;
            return cam;
        }

        // ================= UI =================

        static VirtualJoystick CreateUI()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            var canvasGo = new GameObject("HUD", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform c = canvasGo.transform;
            var hud = canvasGo.AddComponent<HUD>();

            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // Red vignette (behind everything).
            var vig = UIRect("DamageVignette", c, Vector2.zero, Vector2.one);
            vig.gameObject.AddComponent<Image>();
            vig.gameObject.AddComponent<DamageVignette>();

            // Damage numbers layer.
            var nums = UIRect("DamageNumbers", c, Vector2.zero, Vector2.one);
            nums.gameObject.AddComponent<DamageNumbers>();

            // Joystick area: lower 45% of the screen.
            var area = UIRect("JoystickArea", c, new Vector2(0f, 0f), new Vector2(1f, 0.45f));
            area.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            var stickBase = UIRect("StickBase", area, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            stickBase.sizeDelta = new Vector2(280f, 280f);
            stickBase.anchoredPosition = new Vector2(0f, -150f);
            var baseImg = stickBase.gameObject.AddComponent<Image>();
            baseImg.sprite = knob;
            baseImg.color = new Color(1f, 1f, 1f, 0.3f);
            baseImg.raycastTarget = false;
            var handle = UIRect("Handle", stickBase, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            handle.sizeDelta = new Vector2(130f, 130f);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.sprite = knob;
            handleImg.color = new Color(1f, 1f, 1f, 0.85f);
            handleImg.raycastTarget = false;
            var stick = area.gameObject.AddComponent<VirtualJoystick>();
            stick.area = area;
            stick.stickBase = stickBase;
            stick.handle = handle;
            stick.radius = 120f;

            // Dark band behind the top HUD so text never sits on top of the battlefield.
            var band = UIRect("TopBand", c, new Vector2(0f, 1f), new Vector2(1f, 1f));
            band.pivot = new Vector2(0.5f, 1f);
            band.sizeDelta = new Vector2(0f, 300f);
            var bandImg = band.gameObject.AddComponent<Image>();
            bandImg.color = new Color(0f, 0f, 0f, 0.45f);
            bandImg.raycastTarget = false;

            // Top-left: duck HP and base HP.
            var hpBar = Bar(c, "DuckHP", new Vector2(40f, -60f), new Vector2(420f, 56f), new Color(0.3f, 0.85f, 0.3f), rounded, out var hpFill, out var hpFillImg);
            hud.playerHpFill = hpFill;
            hud.playerHpFillImage = hpFillImg;
            hud.playerHpText = Label(hpBar, "Text", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 30, TextAnchor.MiddleCenter, Color.white);
            hud.playerHpText.text = "Утка 100/100";

            var baseBar = Bar(c, "BaseHP", new Vector2(40f, -126f), new Vector2(420f, 40f), new Color(0.95f, 0.65f, 0.2f), rounded, out var baseFill, out _);
            hud.baseHpFill = baseFill;
            var baseLabel = Label(baseBar, "Text", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 26, TextAnchor.MiddleCenter, Color.white);
            baseLabel.text = "База";
            baseLabel.gameObject.AddComponent<LocalizedText>().key = "hud.base";

            // Top-right: coins.
            var coinIcon = UIRect("CoinIcon", c, new Vector2(1f, 1f), new Vector2(1f, 1f));
            coinIcon.pivot = new Vector2(1f, 1f);
            coinIcon.sizeDelta = new Vector2(70f, 70f);
            coinIcon.anchoredPosition = new Vector2(-230f, -55f);
            var coinImg = coinIcon.gameObject.AddComponent<Image>();
            coinImg.sprite = knob;
            coinImg.color = new Color(1f, 0.8f, 0.15f);
            var coinsHolder = UIRect("Coins", c, new Vector2(1f, 1f), new Vector2(1f, 1f));
            coinsHolder.pivot = new Vector2(1f, 1f);
            coinsHolder.sizeDelta = new Vector2(200f, 80f);
            coinsHolder.anchoredPosition = new Vector2(-30f, -50f);
            hud.coinsText = Label(coinsHolder, "Text", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 60, TextAnchor.MiddleLeft, Color.white);
            hud.coinsText.text = "0";

            // Top-center: wave track and phase.
            hud.waveText = Label(c, "Wave", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -195f), new Vector2(900f, 64f), 42, TextAnchor.MiddleCenter, Color.white);
            hud.phaseText = Label(c, "Phase", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -252f), new Vector2(900f, 50f), 30, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f));

            // FPS (for device testing).
            var fps = Label(c, "FPS", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(170f, -185f), new Vector2(260f, 44f), 28, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.6f));
            fps.gameObject.AddComponent<FpsCounter>().label = fps;

            // Banner and base alert.
            hud.bannerText = Label(c, "Banner", new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(1000f, 260f), 64, TextAnchor.MiddleCenter, Color.white);
            hud.baseAlertText = Label(c, "BaseAlert", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(900f, 60f), 40, TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.25f));
            hud.baseAlertText.text = "База под атакой!";
            hud.baseAlertText.gameObject.AddComponent<LocalizedText>().key = "hud.base_attack";

            // Buttons.
            hud.buildButton = MakeButton(c, "BuildButton", new Vector2(0.5f, 0f), new Vector2(0f, 700f), new Vector2(760f, 130f), new Color(1f, 0.8f, 0.2f), rounded, "Построить", 38, new Color(0.2f, 0.12f, 0f), out hud.buildButtonText, out hud.buildButtonImage);
            hud.fightButton = MakeButton(c, "FightButton", new Vector2(1f, 0f), new Vector2(-190f, 880f), new Vector2(320f, 150f), new Color(0.85f, 0.2f, 0.15f), rounded, "В БОЙ!", 52, Color.white, out var fightLabel, out _);
            fightLabel.gameObject.AddComponent<LocalizedText>().key = "hud.fight";

            // Dev: language toggle RU/EN.
            var langBtn = MakeButton(c, "LanguageToggle", new Vector2(1f, 1f), new Vector2(-150f, -165f), new Vector2(240f, 70f), new Color(0.2f, 0.2f, 0.25f, 0.8f), rounded, "RU → EN", 30, Color.white, out var langLabel, out _);
            langBtn.gameObject.AddComponent<LanguageToggle>().label = langLabel;

            // Result panel.
            var panel = UIRect("ResultPanel", c, Vector2.zero, Vector2.one);
            panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            hud.resultPanel = panel.gameObject;
            hud.resultTitle = Label(panel, "Title", new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(1000f, 160f), 110, TextAnchor.MiddleCenter, Color.white);
            hud.resultSubtitle = Label(panel, "Subtitle", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(1000f, 80f), 44, TextAnchor.MiddleCenter, Color.white);
            hud.restartButton = MakeButton(panel, "Restart", new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(520f, 140f), new Color(1f, 0.8f, 0.2f), rounded, "Ещё раз", 50, new Color(0.2f, 0.12f, 0f), out var restartLabel, out _);
            restartLabel.gameObject.AddComponent<LocalizedText>().key = "hud.restart";

            return stick;
        }

        static RectTransform Bar(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite,
            out RectTransform fill, out Image fillImage)
        {
            var bg = UIRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f));
            bg.pivot = new Vector2(0f, 1f);
            bg.anchoredPosition = pos;
            bg.sizeDelta = size;
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = sprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0f, 0f, 0f, 0.6f);
            bgImg.raycastTarget = false;

            fill = UIRect("Fill", bg, Vector2.zero, Vector2.one);
            fill.offsetMin = new Vector2(5f, 5f);
            fill.offsetMax = new Vector2(-5f, -5f);
            fill.pivot = new Vector2(0f, 0.5f);
            fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = sprite;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = color;
            fillImage.raycastTarget = false;
            return bg;
        }

        static Text Label(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size,
            int fontSize, TextAnchor align, Color color)
        {
            var rt = UIRect(name, parent, anchorMin, anchorMax);
            if (anchorMin == anchorMax)
            {
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;
            }
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.8f);
            o.effectDistance = new Vector2(3f, -3f);
            return t;
        }

        static Button MakeButton(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite,
            string text, int fontSize, Color textColor, out Text label, out Image image)
        {
            var rt = UIRect(name, parent, anchor, anchor);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = image;
            label = Label(rt, "Text", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fontSize, TextAnchor.MiddleCenter, textColor);
            label.text = text;
            var outline = label.GetComponent<Outline>();
            if (outline != null) Object.DestroyImmediate(outline);
            return btn;
        }

        // ================= helpers =================

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        static RectTransform UIRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Material mat, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static Material LineMat()
        {
            string path = $"{MatFolder}/FX_Line.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        static Material Mat(string name, Color color, string shaderName = "Universal Render Pipeline/Lit")
        {
            string path = $"{MatFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
