using System.IO;
using PassTheLight.Core;
using PassTheLight.Interaction;
using PassTheLight.Optics;
using PassTheLight.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace PassTheLight.EditorTools
{
    public static class ProjectBuilder
    {
        const string Root = "Assets/_Project";
        const string MaterialsDir = Root + "/Materials";
        const string PrefabsDir = Root + "/Prefabs";
        const string ScenePath = Root + "/Scenes/Lab.unity";

        static readonly Color Navy = new(0.06f, 0.09f, 0.16f);
        static readonly Color PanelColor = new(0.09f, 0.12f, 0.2f, 0.92f);
        static readonly Color Gold = new(1f, 0.78f, 0.25f);
        static readonly Color TextColor = new(0.93f, 0.94f, 0.97f);
        static readonly Color ButtonColor = new(0.2f, 0.27f, 0.42f);

        static Font font;
        static Font displayFont;
        static Sprite roundedSprite;

        class Materials
        {
            public Material Bench, Floor, Mirror, MirrorBase, MirrorCap, Ring, Glass, Wall, LaserBody, Slot, Beam, RelayBeam, Spark, BeamGlow, RelayGlow, Confetti;
        }

        class Prefabs
        {
            public GameObject Laser, RelayBeam, Mirror, Glass, Desk, Wall, Slot;
        }

        [MenuItem("Pass the Light/Rebuild Content")]
        public static void BuildAll()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            displayFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Art/Kenney/Fonts/KenneyFuture.ttf");
            circleSprite = CircleSprite();

            var mats = CreateMaterials();
            var prefabs = CreatePrefabs(mats);
            BuildScene(prefabs, mats);
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("Pass the Light: content rebuilt");
        }

        static Materials CreateMaterials()
        {
            return new Materials
            {
                Bench = Textured(Lit("M_Bench", Color.white, 0.25f), GridTexture(), new Vector2(11.6f, 7.6f)),
                Floor = Textured(Lit("M_Floor", Color.white, 0.15f), PlankTexture(), new Vector2(12f, 12f)),
                Mirror = Lit("M_Mirror", new Color(0.8f, 0.92f, 1f), 0.95f, 0.85f),
                MirrorBase = Lit("M_MirrorBase", new Color(0.16f, 0.18f, 0.24f), 0.6f, 0.3f),
                MirrorCap = Unlit("M_MirrorCap", new Color(0.5f, 1.6f, 2.2f)),
                Ring = Unlit("M_SelectionRing", new Color(2.2f, 1.6f, 0.4f)),
                Glass = Transparent(Lit("M_Glass", new Color(0.55f, 0.85f, 1f, 0.35f), 0.95f)),
                Wall = Lit("M_Wall", new Color(0.22f, 0.24f, 0.3f), 0.2f),
                LaserBody = Lit("M_LaserBody", new Color(0.18f, 0.18f, 0.2f), 0.6f, 0.5f),
                Slot = Unlit("M_Slot", new Color(0.2f, 0.55f, 0.6f)),
                Beam = Unlit("M_Beam", new Color(3f, 0.35f, 0.25f)),
                RelayBeam = Unlit("M_RelayBeam", new Color(2.6f, 1.8f, 0.45f)),
                Spark = SparkMaterial("M_Spark", Gold),
                BeamGlow = SparkMaterial("M_BeamGlow", new Color(2.5f, 0.9f, 0.7f)),
                RelayGlow = SparkMaterial("M_RelayGlow", new Color(2.4f, 1.9f, 0.8f)),
                Confetti = Transparent(LoadOrCreate("M_Confetti", "Universal Render Pipeline/Particles/Unlit")),
            };
        }

        static Material Lit(string name, Color color, float smoothness, float metallic = 0f)
        {
            var mat = LoadOrCreate(name, "Universal Render Pipeline/Lit");
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            return mat;
        }

        static Material Textured(Material mat, Texture2D texture, Vector2 tiling)
        {
            mat.SetTexture("_BaseMap", texture);
            mat.SetTextureScale("_BaseMap", tiling);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Texture2D GridTexture() => GeneratedTexture("bench_grid", (x, y, n) =>
        {
            var wood = new Color(0.47f, 0.33f, 0.22f);
            float grain = 0.025f * Mathf.Sin(y * 0.35f + x * 0.01f);
            var c = wood * (1f + grain + n * 0.05f);
            bool line = x < 3 || y < 3;
            return line ? Color.Lerp(c, new Color(0.75f, 0.6f, 0.42f), 0.6f) : c;
        });

        static Texture2D PlankTexture() => GeneratedTexture("floor_planks", (x, y, n) =>
        {
            var wood = new Color(0.2f, 0.14f, 0.11f);
            bool seam = y % 64 < 2 || (x + (y / 64) * 97) % 256 < 2;
            var c = wood * (1f + n * 0.08f + 0.05f * Mathf.Sin(x * 0.05f + y));
            return seam ? c * 0.6f : c;
        });

        static Texture2D GeneratedTexture(string name, System.Func<int, int, float, Color> pixel)
        {
            string path = $"{Root}/Art/UI/{name}.png";
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var random = new System.Random(name.GetHashCode());
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float noise = (float)random.NextDouble() - 0.5f;
                var c = pixel(x, y, noise);
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Unlit(string name, Color color)
        {
            var mat = LoadOrCreate(name, "Universal Render Pipeline/Unlit");
            mat.SetColor("_BaseColor", color);
            return mat;
        }

        static Material SparkMaterial(string name, Color color)
        {
            var mat = LoadOrCreate(name, "Universal Render Pipeline/Particles/Unlit");
            mat.SetColor("_BaseColor", color);
            var texture = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (texture) mat.SetTexture("_BaseMap", texture);
            Transparent(mat, additive: true);
            return mat;
        }

        static Material Transparent(Material mat, bool additive = false)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", additive ? 2f : 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material LoadOrCreate(string name, string shaderName)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var shader = Shader.Find(shaderName);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Prefabs CreatePrefabs(Materials m)
        {
            return new Prefabs
            {
                Laser = SavePrefab(BuildLaser(m), "Laser"),
                RelayBeam = SavePrefab(BuildRelayBeam(m), "RelayBeam"),
                Mirror = SavePrefab(BuildMirror(m), "Mirror"),
                Glass = SavePrefab(BuildGlass(m), "GlassBlock"),
                Desk = SavePrefab(BuildDesk(m), "StudentDesk"),
                Wall = SavePrefab(BuildWall(m), "Wall"),
                Slot = SavePrefab(BuildSlot(m), "PlacementSlot"),
            };
        }

        static GameObject SavePrefab(GameObject go, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabsDir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject BuildLaser(Materials m)
        {
            var root = new GameObject("Laser");
            AddBeam(root, m.Beam, 0.07f, m.BeamGlow);
            var body = Primitive(PrimitiveType.Cube, "Body", root.transform, m.LaserBody, keepCollider: true);
            body.transform.localPosition = new Vector3(0f, 0f, -0.4f);
            body.transform.localScale = new Vector3(0.45f, 0.35f, 0.7f);
            return root;
        }

        static GameObject BuildRelayBeam(Materials m)
        {
            var root = new GameObject("RelayBeam");
            AddBeam(root, m.RelayBeam, 0.055f, m.RelayGlow);
            return root;
        }

        static void AddBeam(GameObject go, Material material, float width, Material glow)
        {
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            go.AddComponent<LaserEmitter>();
            var effects = go.AddComponent<BeamEffects>();
            Set(effects, "glowMaterial", glow);
            Set(effects, "glowMesh", Resources.GetBuiltinResource<Mesh>("Quad.fbx"));
        }

        static GameObject BuildMirror(Materials m)
        {
            var root = new GameObject("Mirror");
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(0.14f, 1f, 1.5f);
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            root.AddComponent<Mirror>();
            var grabbable = root.AddComponent<Grabbable>();

            var face = Primitive(PrimitiveType.Cube, "Face", root.transform, m.Mirror);
            face.transform.localScale = new Vector3(0.1f, 1f, 1.5f);
            var cap = Primitive(PrimitiveType.Cube, "Cap", root.transform, m.MirrorCap);
            cap.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            cap.transform.localScale = new Vector3(0.16f, 0.05f, 1.55f);
            var stand = Primitive(PrimitiveType.Cylinder, "Stand", root.transform, m.MirrorBase);
            stand.transform.localPosition = new Vector3(0f, -0.48f, 0f);
            stand.transform.localScale = new Vector3(0.95f, 0.025f, 0.95f);

            Set(grabbable, "body", face.GetComponent<Renderer>());
            return root;
        }

        static GameObject BuildGlass(Materials m)
        {
            var root = Primitive(PrimitiveType.Cube, "GlassBlock", null, m.Glass, keepCollider: true);
            root.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            root.AddComponent<GlassBlock>();
            return root;
        }

        static GameObject BuildDesk(Materials m)
        {
            var root = new GameObject("StudentDesk");
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(1.1f, 0.7f, 0.8f);
            var desk = root.AddComponent<StudentDesk>();

            FitBox(FurnitureDir + "/desk.fbx", root.transform, Vector3.zero, new Vector3(1.1f, 0.7f, 0.8f));

            var seat = new GameObject("StudentSeat").transform;
            seat.SetParent(root.transform, false);
            seat.localPosition = new Vector3(0f, -0.35f, 0.85f);

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(root.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.9f, 0.5f);
            var glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = Gold;
            glow.range = 2.5f;
            glow.intensity = 3f;
            glow.enabled = false;

            var sparkGo = new GameObject("Sparkles");
            sparkGo.transform.SetParent(root.transform, false);
            sparkGo.transform.localPosition = new Vector3(0f, 0.9f, 0.6f);
            var sparkles = sparkGo.AddComponent<ParticleSystem>();
            sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparkles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = 0.9f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(Gold, Color.white);
            main.gravityModifier = 0.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            var emission = sparkles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 35) });
            var shape = sparkles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            sparkGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = m.Spark;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Canvas));
            labelGo.transform.SetParent(root.transform, false);
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.sizeDelta = new Vector2(400f, 80f);
            labelRect.localPosition = new Vector3(0f, 1.6f, 0.6f);
            labelRect.localRotation = Quaternion.Euler(60f, 0f, 0f);
            labelRect.localScale = Vector3.one * 0.0065f;
            labelGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var label = Display(Label("Text", labelGo.transform, "", 40, Gold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero));

            var markerGo = new GameObject("WaitingMarker", typeof(RectTransform), typeof(Canvas));
            markerGo.transform.SetParent(root.transform, false);
            var markerRect = (RectTransform)markerGo.transform;
            markerRect.sizeDelta = new Vector2(100f, 100f);
            markerRect.localPosition = new Vector3(0f, 1.75f, 0.6f);
            markerRect.localRotation = Quaternion.Euler(60f, 0f, 0f);
            markerRect.localScale = Vector3.one * 0.006f;
            markerGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            Dot("Bubble", markerRect, Half, Vector2.zero, 100);
            IconImage("Icon", markerRect, "exclamation", new Color(0.1f, 0.12f, 0.2f), Half, Vector2.zero, 60);

            Set(desk, "glow", glow);
            Set(desk, "sparkles", sparkles);
            Set(desk, "label", label);
            Set(desk, "waitingMarker", markerRect);
            return root;
        }

        static GameObject BuildWall(Materials m) =>
            Primitive(PrimitiveType.Cube, "Wall", null, m.Wall, keepCollider: true);

        static GameObject BuildSlot(Materials m)
        {
            var root = new GameObject("PlacementSlot");
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 1f, 0f);
            box.size = new Vector3(1.3f, 2f, 1.3f);
            var slot = root.AddComponent<PlacementSlot>();

            var pad = Primitive(PrimitiveType.Cylinder, "Pad", root.transform, m.Slot);
            pad.transform.localPosition = new Vector3(0f, 0.006f, 0f);
            pad.transform.localScale = new Vector3(1.2f, 0.005f, 1.2f);
            pad.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            Set(slot, "pad", pad.GetComponent<Renderer>());
            return root;
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Material material, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent) go.transform.SetParent(parent, false);
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static void BuildScene(Prefabs p, Materials mats)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.5f, 0.55f);
            RenderSettings.skybox = null;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            var bump = camGo.AddComponent<CameraBump>();
            AddPostProcessing(cam);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.065f, 0.055f);
            cam.fieldOfView = 50f;
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 10.8f, -7.0f), Quaternion.Euler(60f, 0f, 0f));

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.95f, 0.87f);
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

            var environment = new GameObject("Environment").transform;
            var floor = Primitive(PrimitiveType.Plane, "Floor", environment, AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/M_Floor.mat"));
            floor.transform.position = new Vector3(0f, -0.9f, 0f);
            floor.transform.localScale = new Vector3(6f, 1f, 6f);
            var bench = Primitive(PrimitiveType.Cube, "Bench", environment, AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/M_Bench.mat"), keepCollider: true);
            bench.transform.localScale = new Vector3(11.6f, 0.2f, 7.6f);
            AddDecor(environment);

            nextCharacter = 0;
            var levelsRoot = new GameObject("Levels").transform;
            var levels = new[] { BuildLevel1(p, levelsRoot), BuildLevel2(p, levelsRoot), BuildLevel3(p, levelsRoot) };

            var systems = new GameObject("Systems").transform;
            var interactor = new GameObject("PointerInteractor").AddComponent<PointerInteractor>();
            interactor.transform.SetParent(systems);
            Set(interactor, "cam", cam);

            var ringGo = new GameObject("SelectionRing");
            ringGo.transform.SetParent(systems);
            var ringLine = ringGo.AddComponent<LineRenderer>();
            ringLine.sharedMaterial = mats.Ring;
            ringLine.widthMultiplier = 0.07f;
            ringLine.shadowCastingMode = ShadowCastingMode.Off;
            Set(ringGo.AddComponent<SelectionRing>(), "interactor", interactor);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(systems);
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var sfxGo = new GameObject("Sfx");
            sfxGo.transform.SetParent(systems);
            var sources = new AudioSource[3];
            for (int i = 0; i < 3; i++)
            {
                sources[i] = sfxGo.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
            }
            sources[2].volume = 0.25f;
            sources[2].clip = MusicLoop();
            var sfx = sfxGo.AddComponent<Sfx>();
            Set(sfx, "effects", sources[0]);
            Set(sfx, "pitched", sources[1]);
            Set(sfx, "music", sources[2]);
            Set(sfx, "star", Tone("star", 0.25f, 1047f));
            Set(sfx, "pop", Tone("pop", 0.06f, 660f, 990f));
            Set(sfx, "click", Tone("click", 0.04f, 1400f));
            Set(sfx, "pickUp", Tone("pick_up", 0.07f, 520f, 780f));
            Set(sfx, "drop", Tone("drop", 0.18f, 110f, 80f));
            Set(sfx, "deskLit", Tone("desk_lit", 0.12f, 660f, 880f, 1320f));
            Set(sfx, "levelLit", Tone("level_lit", 0.14f, 523f, 659f, 784f, 1047f));
            Set(sfx, "correct", Tone("correct", 0.12f, 784f, 1047f));
            Set(sfx, "wrong", Tone("wrong", 0.18f, 220f, 175f));

            var ui = BuildUI();

            var game = new GameObject("GameManager").AddComponent<GameManager>();
            game.transform.SetParent(systems);
            Set(game, "levels", levels);
            Set(game, "ui", ui);
            Set(game, "interactor", interactor);
            Set(game, "confetti", Confetti(mats.Confetti));
            Set(game, "cameraBump", bump);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static LevelRoot NewLevel(Transform parent, string name, string title, string tip, int par, string lesson,
                                  string question, string[] options, int correct, string explanation)
        {
            var level = new GameObject(name).AddComponent<LevelRoot>();
            level.transform.SetParent(parent);
            Set(level, "title", title);
            Set(level, "tip", tip);
            Set(level, "parMoves", par);
            Set(level, "lesson", lesson);
            Set(level, "quizQuestion", question);
            Set(level, "quizOptions", options);
            Set(level, "correctOption", correct);
            Set(level, "quizExplanation", explanation);
            return level;
        }

        static LevelRoot BuildLevel1(Prefabs p, Transform parent)
        {
            var level = NewLevel(parent, "Level1_FirstLight", "First light",
                "Drag the mirror into the beam, then rotate it.", 4,
                "A mirror bounces light back at the same angle it arrives.\n\nAngle in (i) = angle out (r)",
                "A ray hits a mirror 30° from the normal. What is the angle of reflection?",
                new[] { "15°", "30°", "60°" }, 1,
                "The angle of reflection always equals the angle of incidence.");
            var t = level.transform;

            Place(p.Laser, t, -5f, 0.5f, -2f, 90f);
            var targetPad = Place(p.Slot, t, 2f, 0.1f, -2f);
            Place(p.Slot, t, -1.5f, 0.1f, 1f);
            var mirror = Place(p.Mirror, t, -1.5f, 0.6f, 1f);
            Set(level, "hintFrom", mirror.transform);
            Set(level, "hintTo", targetPad.transform);
            Desk(p, t, 2f, 2.3f, "Future engineer");
            return level;
        }

        static LevelRoot BuildLevel2(Prefabs p, Transform parent)
        {
            var level = NewLevel(parent, "Level2_AroundTheBarrier", "Around the barrier",
                "Bounce the light around the wall.", 8,
                "Some learners are cut off by barriers.\n\nUse two mirrors to guide the light around the wall. Every bounce follows i = r.",
                "Angles of incidence and reflection are measured from the normal. The normal is a line...",
                new[] { "along the mirror's surface", "at 90° to the mirror's surface", "at 45° to the mirror's surface" }, 1,
                "The normal is perpendicular (90°) to the mirror's surface.");
            var t = level.transform;

            Place(p.Laser, t, -5f, 0.5f, -2.5f, 90f);
            var wall = Place(p.Wall, t, 0f, 0.6f, -1f);
            wall.transform.localScale = new Vector3(0.4f, 1f, 5f);
            var firstPad = Place(p.Slot, t, -3f, 0.1f, -2.5f);
            Place(p.Slot, t, -3f, 0.1f, 2.5f);
            Place(p.Slot, t, -1.5f, 0.1f, 0f);
            Place(p.Slot, t, 2f, 0.1f, -1.8f);
            var firstMirror = Place(p.Mirror, t, -1.5f, 0.6f, 0f);
            Set(level, "hintFrom", firstMirror.transform);
            Set(level, "hintTo", firstPad.transform);
            Place(p.Mirror, t, 2f, 0.6f, -1.8f);
            Desk(p, t, 4f, 2.5f, "Future doctor");
            return level;
        }

        static LevelRoot BuildLevel3(Prefabs p, Transform parent)
        {
            var level = NewLevel(parent, "Level3_PassItOn", "Pass it on",
                "Light the teacher. Lit students shine where their arrows point.", 8,
                "Glass slows light, so the beam bends as it goes through (Snell's law).\n\n" +
                "Students with gold arrows pass the light on when they are lit. Guide each one's light to the next student.",
                "When light passes from air into glass, it bends...",
                new[] { "away from the normal", "towards the normal", "it does not bend" }, 1,
                "Light slows down in glass, so it bends towards the normal.");
            var t = level.transform;

            Place(p.Laser, t, -5f, 0.5f, -2.5f, 90f);
            var glass = Place(p.Glass, t, -2f, 0.5f, -2.5f, 30f);
            glass.transform.localScale = new Vector3(1.2f, 0.8f, 1.6f);
            var padA = Place(p.Slot, t, 2f, 0.1f, -2.5f);
            Place(p.Slot, t, 4.5f, 0.1f, 0.8f);
            Place(p.Slot, t, -0.5f, 0.1f, 0.4f);
            Place(p.Slot, t, 0.6f, 0.1f, -0.8f);
            Place(p.Mirror, t, -0.5f, 0.6f, 0.4f);
            var nearMirror = Place(p.Mirror, t, 0.6f, 0.6f, -0.8f);
            Set(level, "hintFrom", nearMirror.transform);
            Set(level, "hintTo", padA.transform);

            var teacher = Desk(p, t, 2f, 0.8f, "Future teacher");
            AddRelay(p, teacher, new Vector3(0.6f, 0.05f, 0f), 90f);
            var nurse = Desk(p, t, 4.5f, 2.7f, "Future nurse");
            AddRelay(p, nurse, new Vector3(-0.6f, 0.05f, 0f), -90f);
            Desk(p, t, -1.5f, 2.7f, "Future leader");
            return level;
        }

        static GameObject Place(GameObject prefab, Transform parent, float x, float y, float z, float yaw = 0f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        static int nextCharacter;

        static StudentDesk Desk(Prefabs p, Transform parent, float x, float z, string dream)
        {
            var desk = Place(p.Desk, parent, x, 0.45f, z).GetComponent<StudentDesk>();
            desk.name = "Desk_" + dream.Replace("Future ", "");
            Set(desk, "studentDream", dream);

            string character = Characters[nextCharacter++ % Characters.Length];
            var seat = desk.transform.Find("StudentSeat");
            var student = FitHeight($"{CharactersDir}/{character}.fbx", seat, Vector3.zero, 1.25f, StudentYaw);
            student.name = "Student";
            Set(desk, "student", student.transform);
            return desk;
        }

        static void AddPostProcessing(Camera cam)
        {
            string path = Root + "/Settings/PostProcessing.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(1.3f);
            bloom.scatter.Override(0.65f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.5f);
            var grading = profile.Add<ColorAdjustments>(true);
            grading.saturation.Override(12f);
            grading.contrast.Override(8f);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);

            var volume = new GameObject("PostProcessing").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }

        static ParticleSystem Confetti(Material material)
        {
            var go = new GameObject("Confetti");
            go.transform.SetPositionAndRotation(new Vector3(0f, 5f, 0f), Quaternion.Euler(90f, 0f, 0f));
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = 2.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Gold, 0f), new GradientColorKey(new Color(0.3f, 0.8f, 1f), 0.33f),
                        new GradientColorKey(new Color(1f, 0.4f, 0.5f), 0.66f), new GradientColorKey(new Color(0.5f, 1f, 0.5f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
            main.gravityModifier = 1.2f;
            main.maxParticles = 250;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 200) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 3f;
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            return ps;
        }

        static AudioClip MusicLoop()
        {
            const int sampleRate = 22050;
            const float beat = 0.6f;
            float[][] chords =
            {
                new[] { 261.6f, 329.6f, 392.0f, 523.3f },
                new[] { 220.0f, 261.6f, 329.6f, 440.0f },
                new[] { 174.6f, 220.0f, 261.6f, 349.2f },
                new[] { 196.0f, 246.9f, 293.7f, 392.0f },
            };
            int[] pattern = { 0, 1, 2, 3, 2, 1, 2, 3 };
            float noteLength = beat / 2f;
            int perNote = Mathf.RoundToInt(noteLength * sampleRate);
            int perChord = perNote * pattern.Length;
            var data = new float[perChord * chords.Length * 2];

            for (int bar = 0; bar < chords.Length * 2; bar++)
            {
                var chord = chords[bar % chords.Length];
                int barStart = bar * perChord;
                for (int i = 0; i < perChord; i++)
                {
                    float t = (float)i / sampleRate;
                    float pad = 0f;
                    for (int k = 0; k < 3; k++) pad += Mathf.Sin(2f * Mathf.PI * chord[k] * 0.5f * t);
                    float fade = Mathf.Min(1f, i / 2000f) * Mathf.Min(1f, (perChord - i) / 2000f);
                    data[barStart + i] += 0.06f * pad * fade;
                }
                for (int n = 0; n < pattern.Length; n++)
                {
                    float f = chord[pattern[n]];
                    int start = barStart + n * perNote;
                    for (int i = 0; i < perNote; i++)
                    {
                        float t = (float)i / sampleRate;
                        float env = Mathf.Exp(-4f * i / perNote) * Mathf.Min(1f, i / 60f);
                        data[start + i] += 0.18f * env * (Mathf.Sin(2f * Mathf.PI * f * t) + 0.2f * Mathf.Sin(6f * Mathf.PI * f * t));
                    }
                }
            }
            return SaveWav("music_loop", data, sampleRate);
        }

        static AudioClip SaveWav(string name, float[] data, int sampleRate)
        {
            string path = $"{Root}/Audio/{name}.wav";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                int dataBytes = data.Length * 2;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataBytes);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataBytes);
                foreach (float v in data) writer.Write((short)(Mathf.Clamp(v, -1f, 1f) * short.MaxValue));
            }
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static AudioClip Tone(string name, float noteLength, params float[] frequencies)
        {
            const int sampleRate = 22050;
            int perNote = Mathf.RoundToInt(noteLength * sampleRate);
            var data = new float[perNote * frequencies.Length];

            for (int n = 0; n < frequencies.Length; n++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / sampleRate;
                    float envelope = Mathf.Exp(-5f * i / perNote) * Mathf.Min(1f, i / 80f);
                    float wave = Mathf.Sin(2f * Mathf.PI * frequencies[n] * t)
                               + 0.3f * Mathf.Sin(4f * Mathf.PI * frequencies[n] * t);
                    data[n * perNote + i] = 0.5f * envelope * wave;
                }
            }
            return SaveWav(name, data, sampleRate);
        }

        const string FurnitureDir = Root + "/Art/Kenney/Furniture";
        const string CharactersDir = Root + "/Art/Kenney/Characters";
        const float StudentYaw = 180f;

        static readonly string[] Characters =
        {
            "character-female-a", "character-male-b", "character-female-c",
            "character-male-d", "character-female-e", "character-male-f",
        };

        static void AddDecor(Transform parent)
        {
            for (int i = 0; i < 3; i++)
                FitHeight(FurnitureDir + "/bookcaseOpen.fbx", parent, new Vector3(-4f + i * 4f, -0.9f, 4.6f), 2.2f, 180f);
            FitHeight(FurnitureDir + "/pottedPlant.fbx", parent, new Vector3(-6.6f, -0.9f, 3.6f), 1.5f, 0f);
            FitHeight(FurnitureDir + "/pottedPlant.fbx", parent, new Vector3(6.6f, -0.9f, 3.6f), 1.5f, 0f);
            FitHeight(FurnitureDir + "/books.fbx", parent, new Vector3(5.2f, 0.1f, 3.3f), 0.25f, 20f);
            FitHeight(FurnitureDir + "/lampRoundTable.fbx", parent, new Vector3(-5.3f, 0.1f, 3.3f), 0.6f, 0f);
        }

        static GameObject Model(string path, Transform parent, float yaw)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset) throw new FileNotFoundException(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return go;
        }

        static GameObject FitHeight(string path, Transform parent, Vector3 localBottom, float height, float yaw)
        {
            var go = Model(path, parent, yaw);
            go.transform.localScale = Vector3.one * (height / WorldBounds(go).size.y);
            AlignBottom(go, parent.TransformPoint(localBottom));
            return go;
        }

        static GameObject FitBox(string path, Transform parent, Vector3 localCentre, Vector3 size)
        {
            var go = Model(path, parent, 0f);
            Vector3 b = WorldBounds(go).size;
            go.transform.localScale = new Vector3(size.x / b.x, size.y / b.y, size.z / b.z);
            AlignBottom(go, parent.TransformPoint(localCentre - new Vector3(0f, size.y / 2f, 0f)));
            return go;
        }

        static void AlignBottom(GameObject go, Vector3 worldBottom)
        {
            var b = WorldBounds(go);
            go.transform.position += worldBottom - new Vector3(b.center.x, b.min.y, b.center.z);
        }

        static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        static void AddRelay(Prefabs p, StudentDesk desk, Vector3 localPosition, float yaw)
        {
            var relay = (GameObject)PrefabUtility.InstantiatePrefab(p.RelayBeam, desk.transform);
            relay.transform.localPosition = localPosition;
            relay.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Set(desk, "relayBeam", relay.GetComponent<LaserEmitter>());

            var hint = new GameObject("RelayHint");
            hint.transform.SetParent(desk.transform, false);
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            for (int i = 0; i < 3; i++)
            {
                var arrowGo = new GameObject($"Arrow{i + 1}", typeof(RectTransform), typeof(Canvas));
                arrowGo.transform.SetParent(hint.transform, false);
                var rect = (RectTransform)arrowGo.transform;
                rect.sizeDelta = new Vector2(100f, 100f);
                rect.localScale = Vector3.one * 0.0065f;
                Vector3 pos = localPosition + forward * (0.45f + i * 0.55f);
                rect.localPosition = new Vector3(pos.x, -0.33f, pos.z);
                rect.localRotation = Quaternion.Euler(90f, yaw + 180f, 0f);
                arrowGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                IconImage("Icon", rect, "arrowDown", new Color(1f, 0.8f, 0.3f, 0.9f - i * 0.25f), Half, Vector2.zero, 100);
            }
            Set(desk, "relayHint", hint);
        }

        const string IconsDir = Root + "/Art/Kenney/Icons";
        static Sprite circleSprite;

        static UIManager BuildUI()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var c = canvasGo.transform;
            var ui = canvasGo.AddComponent<UIManager>();
            Vector2 left = new(0f, 0.5f), right = new(1f, 0.5f), bottomLeft = Vector2.zero, bottomRight = new(1f, 0f);

            var hud = Stretch("HUD", c).gameObject;
            var top = Panel("TopBar", hud.transform, PanelColor, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -90), Vector2.zero, rounded: false);
            Panel("GoldLine", top.transform, Gold, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -3), Vector2.zero, rounded: false).raycastTarget = false;
            var home = IconButton("HomeButton", top.transform, "home", ButtonColor, left, new Vector2(60, 0), 66);
            var dots = new Image[3];
            for (int i = 0; i < 3; i++)
                dots[i] = Dot($"LevelDot{i + 1}", top.transform, left, new Vector2(130 + i * 34, 0), 18);
            var levelTitle = Display(Label("LevelTitle", top.transform, "", 30, TextColor, TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(0.4f, 1), new Vector2(230, 0), Vector2.zero));
            var studentIcons = new Image[3];
            for (int i = 0; i < 3; i++)
                studentIcons[i] = IconImage($"StudentIcon{i + 1}", top.transform, "singleplayer", Gold, Half, new Vector2((i - 1) * 74, 0), 58);
            Display(Label("MovesCaption", top.transform, "MOVES", 18, new Color(1, 1, 1, 0.55f), TextAnchor.MiddleCenter,
                right, right, new Vector2(-370, 6), new Vector2(-250, 36)));
            var moves = Label("Moves", top.transform, "0", 36, TextColor, TextAnchor.MiddleCenter,
                right, right, new Vector2(-370, -36), new Vector2(-250, 8));
            moves.fontStyle = FontStyle.Bold;
            for (int i = 0; i < 3; i++)
                IconImage($"TargetStar{i + 1}", top.transform, "star", Gold, right, new Vector2(-520 + i * 26, 14), 24);
            var starTarget = Label("StarTarget", top.transform, "", 22, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleCenter,
                right, right, new Vector2(-600, -38), new Vector2(-390, -2));
            var hudMusic = IconButton("MusicButton", top.transform, "musicOn", ButtonColor, right, new Vector2(-175, 0), 66);
            var pause = IconButton("PauseButton", top.transform, "pause", ButtonColor, right, new Vector2(-75, 0), 66);

            var tipPill = Panel("TipPill", hud.transform, PanelColor, bottomLeft, bottomLeft, new Vector2(20, 20), new Vector2(960, 120));
            var help = IconButton("HelpButton", tipPill.transform, "question", new Color(0.75f, 0.55f, 0.12f), left, new Vector2(55, 0), 76);
            var tip = Label("Tip", tipPill.transform, "", 32, TextColor, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(110, 0), new Vector2(-20, 0));

            var anglePanel = Panel("AnglePanel", hud.transform, PanelColor, bottomRight, bottomRight, new Vector2(-370, 200), new Vector2(-30, 270));
            var angle = Label("AngleText", anglePanel.transform, "", 34, Gold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var rotateLeft = IconButton("RotateLeftButton", hud.transform, "return", ButtonColor, bottomRight, new Vector2(-270, 95), 140);
            var rotateRight = IconButton("RotateRightButton", hud.transform, "return", ButtonColor, bottomRight, new Vector2(-110, 95), 140);
            rotateRight.transform.Find("Icon").localScale = new Vector3(-1f, 1f, 1f);
            Label("KeyQ", rotateLeft.transform, "Q", 24, new Color(1, 1, 1, 0.6f), TextAnchor.LowerCenter, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -8));
            Label("KeyE", rotateRight.transform, "E", 24, new Color(1, 1, 1, 0.6f), TextAnchor.LowerCenter, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -8));

            var hint = IconImage("HintArrow", hud.transform, "arrowDown", Gold, Half, Vector2.zero, 90).rectTransform;

            var lesson = Overlay("LessonPanel", c);
            var lessonCard = Card("Card", lesson.transform, new Vector2(1060, 620));
            IconImage("InfoIcon", lessonCard.transform, "information", Gold, new Vector2(0.5f, 1f), new Vector2(0, -75), 80);
            var lessonTitle = Display(Label("Title", lessonCard.transform, "", 46, Gold, TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, new Vector2(40, 0), new Vector2(-40, -130)));
            var lessonGoal = Display(Label("Goal", lessonCard.transform, "", 26, TextColor, TextAnchor.LowerCenter,
                Vector2.zero, Vector2.one, new Vector2(40, 190), new Vector2(-40, 0)));
            var lessonBody = Label("Body", lessonCard.transform, "", 34, TextColor, TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, new Vector2(70, 0), new Vector2(-70, -200));
            Label("Controls", lessonCard.transform, "Drag with mouse or finger   ·   Rotate: Q / E or the round buttons", 24,
                new Color(1, 1, 1, 0.55f), TextAnchor.LowerCenter, Vector2.zero, Vector2.one, new Vector2(40, 150), new Vector2(-40, 0));
            var lessonClose = IconButton("GotItButton", lessonCard.transform, "checkmark", new Color(0.75f, 0.55f, 0.12f),
                new Vector2(0.5f, 0f), new Vector2(0, 80), 110);

            var quiz = Overlay("QuizPanel", c);
            var quizCard = Card("Card", quiz.transform, new Vector2(1120, 720));
            IconImage("QuestionIcon", quizCard.transform, "question", Gold, new Vector2(0.5f, 1f), new Vector2(0, -70), 76);
            var question = Label("Question", quizCard.transform, "", 36, TextColor, TextAnchor.MiddleCenter,
                new Vector2(0, 0.66f), new Vector2(1, 0.86f), new Vector2(60, 0), new Vector2(-60, 0));
            var options = new Button[3];
            var marks = new Image[3];
            string[] letters = { "A", "B", "C" };
            for (int i = 0; i < 3; i++)
            {
                float y = 95f - i * 108f;
                options[i] = MakeButton($"Option{letters[i]}", quizCard.transform, "", 32, ButtonColor,
                    Half, Half, new Vector2(-450, y - 45), new Vector2(450, y + 45));
                var badge = Dot("Badge", options[i].transform, left, new Vector2(48, 0), 58);
                badge.color = new Color(1, 1, 1, 0.15f);
                Display(Label("Letter", badge.transform, letters[i], 30, Gold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero));
                marks[i] = IconImage("Mark", options[i].transform, "checkmark", Color.white, right, new Vector2(-50, 0), 52);
            }
            var feedback = Label("Feedback", quizCard.transform, "", 30, TextColor, TextAnchor.MiddleCenter,
                new Vector2(0, 0), new Vector2(1, 0.14f), new Vector2(40, 10), new Vector2(-40, 0));

            var complete = Overlay("CompletePanel", c);
            var completeCard = Card("Card", complete.transform, new Vector2(920, 620));
            Display(Label("Title", completeCard.transform, "CLASS LIT!", 62, Gold, TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -45)));
            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                bool middle = i == 1;
                stars[i] = IconImage($"Star{i + 1}", completeCard.transform, "star", Gold, Half,
                    new Vector2((i - 1) * 170, middle ? 85 : 55), middle ? 170 : 130);
            }
            var stats = Label("Stats", completeCard.transform, "", 34, TextColor, TextAnchor.MiddleCenter,
                Half, Half, new Vector2(-400, -95), new Vector2(400, -35));
            var replay = IconButton("ReplayButton", completeCard.transform, "return", ButtonColor, Half, new Vector2(-170, -190), 110);
            var completeHome = IconButton("HomeButton", completeCard.transform, "home", ButtonColor, Half, new Vector2(0, -190), 110);
            var next = IconButton("NextButton", completeCard.transform, "right", new Color(0.75f, 0.55f, 0.12f), Half, new Vector2(170, -190), 110);

            var pausePanel = Overlay("PausePanel", c);
            var pauseCard = Card("Card", pausePanel.transform, new Vector2(760, 440));
            Display(Label("Title", pauseCard.transform, "PAUSED", 54, Gold, TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -45)));
            var resume = IconButton("ResumeButton", pauseCard.transform, "right", new Color(0.75f, 0.55f, 0.12f), Half, new Vector2(-200, -20), 130, "Resume");
            var restart = IconButton("RestartButton", pauseCard.transform, "return", ButtonColor, Half, new Vector2(0, -20), 130, "Restart");
            var toMenu = IconButton("MenuButton", pauseCard.transform, "home", ButtonColor, Half, new Vector2(200, -20), 130, "Menu");

            var menu = Panel("MenuPanel", c, new Color(Navy.r, Navy.g, Navy.b, 0.88f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, rounded: false);
            Display(Label("Title", menu.transform, "PASS THE LIGHT", 104, Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 0.79f), new Vector2(1, 0.95f), Vector2.zero, Vector2.zero));
            Panel("TitleBeam", menu.transform, new Color(1f, 0.35f, 0.25f), new Vector2(0.5f, 0.785f), new Vector2(0.5f, 0.785f),
                new Vector2(-380, -3), new Vector2(380, 3), rounded: false).raycastTarget = false;
            Label("Subtitle", menu.transform, "Every student you reach lights the way for the next.", 38, TextColor,
                TextAnchor.MiddleCenter, new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.78f), Vector2.zero, Vector2.zero);
            var levelButtons = new Button[3];
            var levelStars = new Image[9];
            var locks = new GameObject[3];
            string[] titles = { "First light", "Around the barrier", "Pass it on" };
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 540f;
                var card = MakeButton($"Level{i + 1}Card", menu.transform, "", 34, ButtonColor,
                    Half, Half, new Vector2(x - 240, -140), new Vector2(x + 240, 170));
                var label = card.GetComponentInChildren<Text>();
                label.text = titles[i];
                label.alignment = TextAnchor.MiddleCenter;
                Display(Label("Number", card.transform, (i + 1).ToString(), 96, Gold, TextAnchor.UpperCenter,
                    Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -18)));
                for (int s = 0; s < 3; s++)
                    levelStars[i * 3 + s] = IconImage($"Star{s + 1}", card.transform, "star", Gold,
                        new Vector2(0.5f, 0f), new Vector2((s - 1) * 70, 55), 58);
                var lockPanel = Panel("Lock", card.transform, new Color(0.04f, 0.05f, 0.1f, 0.55f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                lockPanel.raycastTarget = false;
                IconImage("LockIcon", lockPanel.transform, "locked", new Color(1, 1, 1, 0.85f), new Vector2(0.5f, 0f), new Vector2(0, 55), 70);
                levelButtons[i] = card;
                locks[i] = lockPanel.gameObject;
            }
            string[] stepIcons = { "arrowDown", "return", "singleplayer" };
            string[] stepText = { "Drop a mirror on a pad", "Rotate it", "Light every student" };
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 480f;
                var step = Dot($"Step{i + 1}", menu.transform, new Vector2(0.5f, 0.17f), new Vector2(x - 150, 0), 90);
                step.color = new Color(1, 1, 1, 0.1f);
                IconImage("Icon", step.transform, stepIcons[i], Gold, Half, Vector2.zero, 52);
                Label("Text", menu.transform, stepText[i], 30, TextColor, TextAnchor.MiddleLeft,
                    new Vector2(0.5f, 0.17f), new Vector2(0.5f, 0.17f), new Vector2(x - 90, -30), new Vector2(x + 230, 30));
            }
            var menuMusic = IconButton("MusicButton", menu.transform, "musicOn", ButtonColor, bottomLeft, new Vector2(80, 80), 80);
            var reset = MakeButton("ResetProgressButton", menu.transform, "Reset progress", 24, new Color(0.3f, 0.18f, 0.2f),
                bottomRight, bottomRight, new Vector2(-290, 40), new Vector2(-40, 105));

            var final = Panel("FinalPanel", c, new Color(Navy.r, Navy.g, Navy.b, 0.96f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, rounded: false);
            final.gameObject.AddComponent<CanvasGroup>();
            final.gameObject.AddComponent<PopIn>();
            IconImage("Trophy", final.transform, "trophy", Gold, new Vector2(0.5f, 0.78f), Vector2.zero, 190);
            Display(Label("Title", final.transform, "THE WHOLE CLASS IS LIT!", 72, Gold, TextAnchor.MiddleCenter,
                new Vector2(0, 0.55f), new Vector2(1, 0.68f), Vector2.zero, Vector2.zero));
            Label("Body", final.transform,
                "You reached one student, and they passed the light on.\nEmpowered people empower those who come after them.",
                36, TextColor, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.54f), Vector2.zero, Vector2.zero);
            var finalMenu = IconButton("FinalMenuButton", final.transform, "home", new Color(0.75f, 0.55f, 0.12f),
                new Vector2(0.5f, 0.2f), Vector2.zero, 130, "Menu");

            Set(ui, "menuPanel", menu.gameObject);
            Set(ui, "levelButtons", levelButtons);
            Set(ui, "levelStars", levelStars);
            Set(ui, "levelLocks", locks);
            Set(ui, "resetProgressButton", reset);
            Set(ui, "hudPanel", hud);
            Set(ui, "levelTitleText", levelTitle);
            Set(ui, "levelDots", dots);
            Set(ui, "studentIcons", studentIcons);
            Set(ui, "movesText", moves);
            Set(ui, "starTargetText", starTarget);
            Set(ui, "homeButton", home);
            Set(ui, "pauseButton", pause);
            Set(ui, "helpButton", help);
            Set(ui, "tipText", tip);
            Set(ui, "anglePanel", anglePanel.gameObject);
            Set(ui, "angleText", angle);
            Set(ui, "rotateLeftButton", rotateLeft);
            Set(ui, "rotateRightButton", rotateRight);
            Set(ui, "hintArrow", hint);
            Set(ui, "musicButtons", new Object[] { hudMusic, menuMusic });
            Set(ui, "musicIcons", new Object[] { hudMusic.transform.Find("Icon").GetComponent<Image>(), menuMusic.transform.Find("Icon").GetComponent<Image>() });
            Set(ui, "musicOnSprite", Icon("musicOn"));
            Set(ui, "musicOffSprite", Icon("musicOff"));
            Set(ui, "lessonPanel", lesson);
            Set(ui, "lessonTitleText", lessonTitle);
            Set(ui, "lessonBodyText", lessonBody);
            Set(ui, "lessonGoalText", lessonGoal);
            Set(ui, "lessonCloseButton", lessonClose);
            Set(ui, "quizPanel", quiz);
            Set(ui, "quizCard", (RectTransform)quizCard.transform);
            Set(ui, "quizQuestionText", question);
            Set(ui, "quizFeedbackText", feedback);
            Set(ui, "quizOptionButtons", options);
            Set(ui, "quizOptionMarks", marks);
            Set(ui, "checkSprite", Icon("checkmark"));
            Set(ui, "crossSprite", Icon("cross"));
            Set(ui, "completePanel", complete);
            Set(ui, "completeStatsText", stats);
            Set(ui, "completeStars", stars);
            Set(ui, "nextButton", next);
            Set(ui, "replayButton", replay);
            Set(ui, "completeHomeButton", completeHome);
            Set(ui, "pausePanel", pausePanel);
            Set(ui, "resumeButton", resume);
            Set(ui, "restartButton", restart);
            Set(ui, "menuButton", toMenu);
            Set(ui, "finalPanel", final.gameObject);
            Set(ui, "finalMenuButton", finalMenu);

            hud.SetActive(false);
            lesson.SetActive(false);
            quiz.SetActive(false);
            complete.SetActive(false);
            pausePanel.SetActive(false);
            final.gameObject.SetActive(false);
            return ui;
        }

        static readonly Vector2 Half = new(0.5f, 0.5f);

        static Sprite Icon(string name)
        {
            string path = $"{IconsDir}/{name}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite CircleSprite()
        {
            string path = Root + "/Art/UI/circle.png";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                const int size = 256;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float r = size / 2f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d)));
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static RectTransform Point(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = Stretch(name, parent);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        static GameObject Overlay(string name, Transform parent) =>
            Panel(name, parent, new Color(0f, 0f, 0f, 0.6f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, rounded: false).gameObject;

        static Image Card(string name, Transform parent, Vector2 size)
        {
            var card = Panel(name, parent, PanelColor, Half, Half, -size / 2f, size / 2f);
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>();
            return card;
        }

        static Image Panel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax,
                           Vector2 offsetMin, Vector2 offsetMax, bool rounded = true)
        {
            var rect = Stretch(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            if (rounded)
            {
                image.sprite = roundedSprite;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        static Image Dot(string name, Transform parent, Vector2 anchor, Vector2 position, float size)
        {
            var image = Point(name, parent, anchor, position, new Vector2(size, size)).gameObject.AddComponent<Image>();
            image.sprite = circleSprite;
            image.color = Gold;
            image.raycastTarget = false;
            return image;
        }

        static Image IconImage(string name, Transform parent, string icon, Color color, Vector2 anchor, Vector2 position, float size)
        {
            var image = Point(name, parent, anchor, position, new Vector2(size, size)).gameObject.AddComponent<Image>();
            image.sprite = Icon(icon);
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static Button IconButton(string name, Transform parent, string icon, Color color, Vector2 anchor, Vector2 position,
                                 float size, string caption = null)
        {
            var rect = Point(name, parent, anchor, position, new Vector2(size, size));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = circleSprite;
            var button = rect.gameObject.AddComponent<Button>();
            button.colors = Colors(color);
            IconImage("Icon", rect, icon, TextColor, Half, Vector2.zero, size * 0.5f);
            if (caption != null)
                Display(Label("Caption", rect, caption.ToUpperInvariant(), 20, TextColor, TextAnchor.UpperCenter,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-100, -45), new Vector2(100, -8)));
            rect.gameObject.AddComponent<ButtonFeedback>();
            return button;
        }

        static ColorBlock Colors(Color color)
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.selectedColor = color;
            colors.disabledColor = new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, 0.8f);
            return colors;
        }

        static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor alignment,
                          Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = Stretch(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = true;
            label.raycastTarget = false;
            return label;
        }

        static Text Display(Text label)
        {
            label.font = displayFont;
            label.fontStyle = FontStyle.Normal;
            return label;
        }

        static Button MakeButton(string name, Transform parent, string text, int size, Color color,
                                 Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var image = Panel(name, parent, Color.white, anchorMin, anchorMax, offsetMin, offsetMax);
            var button = image.gameObject.AddComponent<Button>();
            button.colors = Colors(color);
            Label("Label", image.transform, text, size, TextColor, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(12, 6), new Vector2(-12, -6));
            image.gameObject.AddComponent<ButtonFeedback>();
            return button;
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Gisele Mwizera";
            PlayerSettings.productName = "Pass the Light";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.giselemwizera.passthelight");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.template = "PROJECT:PassTheLight";
        }

        public static void BuildWebGL() => Build(BuildTarget.WebGL, "docs");

        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Android/PassTheLight.apk");
        }

        static void Build(BuildTarget target, string location)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(location)) ?? ".");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = location,
                target = target,
                options = BuildOptions.None,
            });
            var summary = report.summary;
            Debug.Log($"BUILD {target}: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, errors {summary.totalErrors}");
            if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static void CaptureScreens()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var cam = Camera.main;
            var levels = Object.FindObjectsByType<LevelRoot>(FindObjectsInactive.Include);
            Directory.CreateDirectory("Builds/Screens");
            var rt = new RenderTexture(1600, 900, 24);
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            foreach (var level in levels)
            {
                foreach (var other in levels) other.gameObject.SetActive(other == level);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                tex.Apply();
                File.WriteAllBytes($"Builds/Screens/{level.name}.png", tex.EncodeToPNG());
            }
            cam.targetTexture = null;
            RenderTexture.active = null;
            EditorSceneManager.CloseScene(scene, false);
        }

        static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) throw new System.ArgumentException($"{target.GetType().Name}.{field} not found");

            switch (value)
            {
                case null: prop.objectReferenceValue = null; break;
                case Object[] objects:
                    prop.arraySize = objects.Length;
                    for (int i = 0; i < objects.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
                    break;
                case string[] strings:
                    prop.arraySize = strings.Length;
                    for (int i = 0; i < strings.Length; i++) prop.GetArrayElementAtIndex(i).stringValue = strings[i];
                    break;
                case Object obj: prop.objectReferenceValue = obj; break;
                case string s: prop.stringValue = s; break;
                case int n: prop.intValue = n; break;
                case float f: prop.floatValue = f; break;
                case bool b: prop.boolValue = b; break;
                case Color color: prop.colorValue = color; break;
                default: throw new System.ArgumentException($"Unsupported type {value.GetType()}");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
