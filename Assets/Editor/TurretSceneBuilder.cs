using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class TurretSceneBuilder
{
    private const string root = "Assets/_Features/Turret";

    private static Material red, dark, gold, blue, ground, grass, rock, outline;

    private static Mesh rounded;

    private static TurretSettings settings;

    [MenuItem("Tools/Turret/Build Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Останови Play Mode перед сборкой сцены");
        for (int i = 0; i < SceneManagerCount(); i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Перед сборкой сохрани открытые сцены");

        Directory.CreateDirectory(root + "/Art");
        Directory.CreateDirectory(root + "/Data");
        Directory.CreateDirectory(root + "/Prefabs");
        Directory.CreateDirectory(root + "/Scenes");
        AssetDatabase.Refresh();
        settings = AssetDatabase.LoadAssetAtPath<TurretSettings>(root + "/Data/TurretSettings.asset");
        if (settings == null) { settings = ScriptableObject.CreateInstance<TurretSettings>(); AssetDatabase.CreateAsset(settings, root + "/Data/TurretSettings.asset"); }
        rounded = RoundedMesh();
        red = Material("CannonRed", new Color(.83f, .075f, .055f));
        dark = Material("DarkMetal", new Color(.12f, .17f, .2f));
        gold = Material("ShellYellow", new Color(1f, .74f, .025f));
        blue = Material("BlockBlue", new Color(.12f, .65f, .84f));
        ground = Material("Concrete", new Color(.65f, .69f, .63f));
        grass = Material("Grass", new Color(.32f, .66f, .24f));
        rock = Material("Rock", new Color(.44f, .55f, .59f));
        outline = Material("Outline", new Color(.055f, .065f, .09f), "Turret/Outline");
        outline.SetFloat("_OutlineWidth", .018f);
        outline.SetColor("_OutlineColor", new Color(.045f, .055f, .075f));

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetLayer(8, "Target");
        SetLayer(9, "Cannonball");
        SetLayer(10, "Turret");
        Physics.IgnoreLayerCollision(9, 10, true);
        Physics.IgnoreLayerCollision(9, 9, true);
        Physics.defaultSolverIterations = 12;
        Physics.defaultSolverVelocityIterations = 4;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.62f, .7f, .8f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.61f, .83f, .92f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 34;
        RenderSettings.fogEndDistance = 85;
        RenderSettings.skybox = null;
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.2f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(43, -35, 0);

        Box("GrassField", null, new Vector3(0, -.5f, 5), new Vector3(95, .8f, 95), grass, false, true);
        Box("FiringPlatform", null, new Vector3(0, -.06f, -7), new Vector3(7, .4f, 7), ground, true, true);
        Box("TargetPlatform", null, new Vector3(0, -.06f, 8), new Vector3(9, .4f, 5), ground, true, true);
        for (int i = 0; i < 4; i++) Box("LaneMark" + i, null, new Vector3(0, -.08f, i * 2.2f - 2), new Vector3(.16f, .04f, .8f), gold, false, false);

        var wallRoot = new GameObject("TargetWall");
        var wall = wallRoot.AddComponent<TargetWall>();
        WallBlock[] blocks = new WallBlock[25];
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
            {
                var block = Box("Block_" + y + "_" + x, wallRoot.transform, new Vector3(x - 2 + (y % 2) * .45f - .225f, .64f + y, 8), Vector3.one, blue, true, true);
                block.layer = 8;
                var body = block.AddComponent<Rigidbody>();
                body.mass = settings.BlockMass; body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
                var script = block.AddComponent<WallBlock>();
                References(script, ("settings", settings), ("body", body));
                blocks[y * 5 + x] = script;
            }
        var wallData = new SerializedObject(wall);
        var blockArray = wallData.FindProperty("blocks"); blockArray.arraySize = blocks.Length;
        for (int i = 0; i < blocks.Length; i++) blockArray.GetArrayElementAtIndex(i).objectReferenceValue = blocks[i];
        wallData.ApplyModifiedPropertiesWithoutUndo();

        var turret = new GameObject("Turret"); turret.transform.position = new Vector3(0, .16f, -7);
        var pedestal = Primitive("Pedestal", turret.transform, new Vector3(0, .35f, 0), new Vector3(2.1f, .35f, 2.1f), PrimitiveType.Cylinder, dark);
        var head = new GameObject("Head").transform;
        head.SetParent(turret.transform, false);
        head.localPosition = new Vector3(0, 1.28f, 0);
        Primitive("TurretBody", head, Vector3.zero, new Vector3(1.9f, 1.25f, 1.7f), PrimitiveType.Sphere, red);
        Box("SidePlateL", head, new Vector3(-.87f, 0, -.05f), new Vector3(.22f, .74f, 1.0f), dark, true, false);
        Box("SidePlateR", head, new Vector3(.87f, 0, -.05f), new Vector3(.22f, .74f, 1.0f), dark, true, false);
        var barrel = new GameObject("Barrel").transform; barrel.SetParent(head, false);
        var tube = Primitive("BarrelTube", barrel, new Vector3(0, 0, 1.1f), new Vector3(.78f, .92f, .78f), PrimitiveType.Cylinder, red);
        tube.transform.localRotation = Quaternion.Euler(90, 0, 0);
        var collar = Primitive("MuzzleCollar", barrel, new Vector3(0, 0, 1.95f), new Vector3(.98f, .19f, .98f), PrimitiveType.Cylinder, gold);
        collar.transform.localRotation = Quaternion.Euler(90, 0, 0);
        var bore = Primitive("Bore", barrel, new Vector3(0, 0, 2.15f), new Vector3(.66f, .025f, .66f), PrimitiveType.Cylinder, dark);
        bore.transform.localRotation = Quaternion.Euler(90, 0, 0);
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(head, false);
        muzzle.localPosition = new Vector3(0, 0, 2.28f);
        foreach (Transform part in turret.GetComponentsInChildren<Transform>()) part.gameObject.layer = 10;

        var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.fieldOfView = 63;
        camera.nearClipPlane = .08f;
        camera.farClipPlane = 110;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = RenderSettings.fogColor;
        camera.gameObject.AddComponent<AudioListener>();
        camera.transform.SetPositionAndRotation(turret.transform.position + new Vector3(.9f, 2.15f, -3.8f), Quaternion.Euler(-5, 0, 0));

        var line = new GameObject("AimBeam").AddComponent<LineRenderer>();
        ConfigureBeam(line);
        line.SetPosition(0, muzzle.position); line.SetPosition(1, new Vector3(0, 2.5f, 8));
        var aim = turret.AddComponent<TurretAim>();
        References(aim, ("settings", settings), ("viewCamera", camera), ("head", head), ("muzzle", muzzle), ("beam", line), ("resetWall", wall));

        var projectile = MakeProjectile();
        var shooter = turret.AddComponent<TurretShooter>();
        References(shooter, ("settings", settings), ("aim", aim), ("projectilePrefab", projectile), ("wall", wall));
        MakeReticle(shooter);

        for (int i = 0; i < 13; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            var boulder = Primitive("DistantRock" + i, null, new Vector3(side * (12 + i % 3 * 3), .5f, -5 + i * 3.4f), new Vector3(3 + i % 3, 2 + i % 4, 3), PrimitiveType.Sphere, rock);
            boulder.transform.rotation = Quaternion.Euler(i * 13, i * 29, 0);
        }

        EditorUtility.SetDirty(settings);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), root + "/Scenes/TurretRange.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(root + "/Scenes/TurretRange.unity", true) };
        AssetDatabase.SaveAssets();
        TurretPolishBuilder.Apply();
        CannonballPolishBuilder.Apply();
        TurretRockRepair.Apply();
        Selection.activeGameObject = turret;
        SceneView.lastActiveSceneView?.LookAt(new Vector3(0, 1.8f, 2), Quaternion.Euler(22, -30, 0), 22);
        Debug.Log("Сцена TurretRange сохранена: 25 кубов, пушка и камера");
    }

    private static int SceneManagerCount() => UnityEngine.SceneManagement.SceneManager.sceneCount;

    private static Cannonball MakeProjectile()
    {
        var ball = Primitive("Cannonball", null, Vector3.zero, Vector3.one * .55f, PrimitiveType.Sphere, gold);
        ball.layer = 9; ball.AddComponent<SphereCollider>();
        var body = ball.AddComponent<Rigidbody>();
        body.mass = 2;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.interpolation = RigidbodyInterpolation.Interpolate;
        var trail = ball.AddComponent<TrailRenderer>(); trail.sharedMaterial = ProjectileTrailMaterial();
        trail.time = .12f;
        trail.startWidth = .22f;
        trail.endWidth = 0;
        trail.minVertexDistance = .06f;
        trail.startColor = new Color(1, .85f, .1f, .8f); trail.endColor = new Color(1, .4f, 0, 0);
        trail.shadowCastingMode = ShadowCastingMode.Off;
        var script = ball.AddComponent<Cannonball>();
        References(script, ("settings", settings), ("body", body), ("trail", trail));
        var previous = AssetDatabase.LoadAssetAtPath<Cannonball>(root + "/Prefabs/Cannonball.prefab");
        if (previous != null)
        {
            var data = new SerializedObject(previous);
            References(script, ("smokePrefab", data.FindProperty("smokePrefab").objectReferenceValue), ("impactPrefab", data.FindProperty("impactPrefab").objectReferenceValue));
        }
        var prefab = PrefabUtility.SaveAsPrefabAsset(ball, root + "/Prefabs/Cannonball.prefab");
        Object.DestroyImmediate(ball);

        return prefab.GetComponent<Cannonball>();
    }

    private static void MakeReticle(TurretShooter shooter)
    {
        var canvas = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900); scaler.matchWidthOrHeight = .5f;
        var reticle = new GameObject("Reticle", typeof(RectTransform)).transform; reticle.SetParent(canvas.transform, false);
        var rect = (RectTransform)reticle;
        rect.sizeDelta = new Vector2(32, 32);
        rect.anchoredPosition = Vector2.zero;
        var dot = Image("Center", reticle, new Vector2(4, 4), Vector2.zero, settings.ReadyColor);
        for (int i = 0; i < 4; i++)
        {
            var arm = Image("Arm" + i, reticle, new Vector2(2, 7), new Vector2(i < 2 ? 0 : (i == 2 ? -11 : 11), i < 2 ? (i == 0 ? -11 : 11) : 0), Color.white);
            if (i > 1) arm.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
        }
        var progress = Image("Reload", reticle, new Vector2(36, 36), Vector2.zero, new Color(1, .8f, .15f, .8f));
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float r = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32));
                texture.SetPixel(x, y, new Color(1, 1, 1, r > 27 && r < 30 ? 1 : 0));
            }
        texture.Apply();
        string path = root + "/Art/ReticleRing.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        progress.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        progress.type = UnityEngine.UI.Image.Type.Filled;
        progress.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
        var script = canvas.AddComponent<TurretReticle>();
        References(script, ("settings", settings), ("shooter", shooter), ("progress", progress), ("center", dot));
    }

    private static Image Image(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = size; image.rectTransform.anchoredPosition = position;

        return image;
    }

    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool edged, bool collider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = size;
        go.AddComponent<MeshFilter>().sharedMesh = rounded;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (collider) go.AddComponent<BoxCollider>();
        if (edged) Outline(go);

        return go;
    }

    private static GameObject Primitive(string name, Transform parent, Vector3 position, Vector3 scale, PrimitiveType type, Material material)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material; Outline(go);

        return go;
    }

    private static void Outline(GameObject source)
    {
        var go = new GameObject("Outline"); go.transform.SetParent(source.transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = outline;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
    }

    public static void ConfigureBeam(LineRenderer line)
    {
        var material = Material("AimBeam", new Color(1f, .14f, .02f), "Universal Render Pipeline/Unlit");
        material.SetFloat("_Surface", 0);
        material.SetInt("_SrcBlend", (int)BlendMode.One);
        material.SetInt("_DstBlend", (int)BlendMode.Zero);
        material.SetFloat("_ZWrite", 1);
        material.renderQueue = 2000;
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        line.sharedMaterial = material;
        line.positionCount = 2;
        line.startWidth = .045f;
        line.endWidth = .035f;
        line.startColor = line.endColor = Color.white;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
    }

    public static Material ProjectileTrailMaterial()
    {
        var material = Material("ProjectileTrail", Color.white, "Universal Render Pipeline/Particles/Unlit");
        material.SetFloat("_Surface", 1);
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0);
        material.renderQueue = 3000;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return material;
    }

    private static Material Material(string name, Color color, string shaderName = "Turret/Toon")
    {
        string path = root + "/Art/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find(shaderName);
        if (shader == null) throw new InvalidOperationException("Не найден шейдер " + shaderName);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.shader = shader; material.SetColor("_BaseColor", color);
        if (material.HasProperty("_ShadowColor")) material.SetColor("_ShadowColor", new Color(.46f, .5f, .62f));
        EditorUtility.SetDirty(material);

        return material;
    }

    private static void References(Object target, params (string name, Object value)[] fields)
    {
        var data = new SerializedObject(target);
        foreach (var field in fields) data.FindProperty(field.name).objectReferenceValue = field.value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayer(int index, string name)
    {
        var data = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layer = data.FindProperty("layers").GetArrayElementAtIndex(index);
        if (layer.stringValue.Length != 0 && layer.stringValue != name) throw new InvalidOperationException("Слой " + index + " уже занят: " + layer.stringValue);
        layer.stringValue = name; data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Mesh RoundedMesh()
    {
        string path = root + "/Art/RoundedBox.asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved != null) return saved;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var normals = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();
        float[] points = { -.5f, -.48f, -.44f, -.4f, .4f, .44f, .48f, .5f };
        Vector3[] axes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var normal in axes)
        {
            Vector3 u = Mathf.Abs(normal.y) > .5f ? Vector3.right : Vector3.up;
            Vector3 v = Vector3.Cross(normal, u);
            int start = vertices.Count;
            for (int y = 0; y < points.Length; y++)
                for (int x = 0; x < points.Length; x++)
                {
                    Vector3 point = normal * .5f + u * points[x] + v * points[y];
                    Vector3 inner = new(Mathf.Clamp(point.x, -.4f, .4f), Mathf.Clamp(point.y, -.4f, .4f), Mathf.Clamp(point.z, -.4f, .4f));
                    Vector3 n = (point - inner).normalized;
                    vertices.Add(inner + n * .1f); normals.Add(n);
                }
            for (int y = 0; y < points.Length - 1; y++)
                for (int x = 0; x < points.Length - 1; x++)
                {
                    int a = start + y * points.Length + x, b = a + 1, c = a + points.Length, d = c + 1;
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(d);
                    triangles.Add(c);
                }
        }
        var mesh = new Mesh { name = "RoundedBox" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);

        return mesh;
    }
}
