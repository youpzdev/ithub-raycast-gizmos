using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class TurretPolishBuilder
{
    private const string root = "Assets/_Features/Turret";
    private const string audioSource = "E:/Assets/Sfx Packs/half-life2-sounds";

    private static Mesh sphere, star;

    private static Material sparks, dust, steam, metal, yellow;

    [MenuItem("Tools/Turret/Apply Polish")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Останови Play Mode перед полишингом");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.path.EndsWith("/TurretRange.unity", StringComparison.Ordinal)) throw new InvalidOperationException("Открой сцену TurretRange перед полишингом");
        var turret = GameObject.Find("Turret");
        if (turret == null) throw new InvalidOperationException("В TurretRange не найдена Turret");
        Transform head = RequireChild(turret.transform, "Head");
        Transform barrel = RequireChild(head, "Barrel");
        Transform muzzle = RequireChild(head, "Muzzle");
        var aim = turret.GetComponent<TurretAim>();
        var shooter = turret.GetComponent<TurretShooter>();
        var settings = AssetDatabase.LoadAssetAtPath<TurretSettings>(root + "/Data/TurretSettings.asset");
        if (aim == null || shooter == null || settings == null) throw new InvalidOperationException("Не найдены TurretAim, TurretShooter или TurretSettings");
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null) throw new InvalidOperationException("Активный render pipeline не URP");

        Directory.CreateDirectory(root + "/Audio");
        Directory.CreateDirectory(root + "/Art/Polish");
        AssetDatabase.Refresh();
        sphere = SphereMesh();
        star = StarMesh();
        sparks = ParticleMaterial("Spark", Color.white);
        dust = ParticleMaterial("Dust", Color.white);
        steam = ParticleMaterial("Steam", Color.white);
        metal = ToonMaterial("VentMetal", new Color(.13f, .19f, .23f));
        yellow = ToonMaterial("VentYellow", new Color(1f, .74f, .025f));

        if (settings.ShotClip == null) settings.ShotClip = ImportSound("weapons/rpg/rocketfire1.wav", "CannonShot.wav");
        if (settings.ImpactClip == null) settings.ImpactClip = ImportSound("weapons/mortar/mortar_explode1.wav", "CannonImpact.wav");
        if (settings.BlockClips == null || settings.BlockClips.Length == 0)
        {
            settings.BlockClips = new AudioClip[4];
            for (int i = 0; i < settings.BlockClips.Length; i++)
                settings.BlockClips[i] = ImportSound("physics/concrete/rock_impact_hard" + (i + 1) + ".wav", "BlockImpact" + (i + 1) + ".wav");
        }
        settings.CameraOffset = new Vector3(1.5f, 1f, -4.2f);
        EditorUtility.SetDirty(settings);

        ImpactPrefab();

        RemoveOwnChild(muzzle, "CartoonMuzzleFlash");
        var flash = MakeParticles("CartoonMuzzleFlash", muzzle, star, sparks, 10, new Color(1f, .83f, .12f), .08f, .18f, .15f, .32f, 5f, 9f, false);
        var flashShape = flash.shape;
        flashShape.shapeType = ParticleSystemShapeType.Cone;
        flashShape.angle = 24f;
        flashShape.radius = .08f;
        var smoke = MakeParticles("MuzzleSmoke", flash.transform, sphere, dust, 5, new Color(.79f, .84f, .87f, .6f), .2f, .38f, .17f, .36f, .4f, 1.1f, false);
        var smokeShape = smoke.shape;
        smokeShape.shapeType = ParticleSystemShapeType.Cone;
        smokeShape.angle = 18;
        smokeShape.radius = .1f;

        RemoveOwnChild(head, "ReloadVents");
        var ventsRoot = new GameObject("ReloadVents").transform; ventsRoot.SetParent(head, false);
        Transform[] vents = new Transform[2];
        for (int i = 0; i < vents.Length; i++)
        {
            float side = i == 0 ? -1f : 1f;
            var pivot = new GameObject("VentHinge" + i).transform; pivot.SetParent(ventsRoot, false);
            pivot.localPosition = new Vector3(side * .42f, .52f, -.2f);
            MeshObject("VentCover", pivot, sphere, metal, new Vector3(side * .16f, 0, 0), new Vector3(.5f, .1f, .6f));
            MeshObject("VentStripe", pivot, sphere, yellow, new Vector3(side * .16f, .035f, 0), new Vector3(.32f, .055f, .4f));
            vents[i] = pivot;
        }

        RemoveOwnChild(head, "ReloadSteam");
        var reloadSteam = MakeParticles("ReloadSteam", head, sphere, steam, 5, new Color(.87f, .94f, 1f, .5f), .18f, .36f, .12f, .24f, .7f, 1.3f, false);
        reloadSteam.transform.localPosition = new Vector3(-.58f, .56f, -.2f);
        reloadSteam.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        var steamShape = reloadSteam.shape;
        steamShape.shapeType = ParticleSystemShapeType.Cone;
        steamShape.angle = 15;
        steamShape.radius = .08f;
        var secondSteam = MakeParticles("RightVentSteam", reloadSteam.transform, sphere, steam, 5, new Color(.87f, .94f, 1f, .5f), .18f, .36f, .12f, .24f, .7f, 1.3f, false);
        secondSteam.transform.localPosition = new Vector3(1.16f, 0, 0);
        var secondShape = secondSteam.shape;
        secondShape.shapeType = ParticleSystemShapeType.Cone;
        secondShape.angle = 15;
        secondShape.radius = .08f;

        var feedback = turret.GetComponent<TurretFeedback>();
        if (feedback == null) feedback = turret.AddComponent<TurretFeedback>();
        References(feedback, ("settings", settings), ("shooter", shooter), ("aim", aim), ("barrel", barrel), ("flash", flash),
            ("bodyVisual", RequireChild(head, "TurretBody")), ("collar", RequireChild(barrel, "MuzzleCollar")), ("reloadSteam", reloadSteam));
        var feedbackData = new SerializedObject(feedback);
        var ventArray = feedbackData.FindProperty("reloadVents");
        if (ventArray == null) throw new InvalidOperationException("В TurretFeedback отсутствует reloadVents");
        ventArray.arraySize = vents.Length;
        for (int i = 0; i < vents.Length; i++) ventArray.GetArrayElementAtIndex(i).objectReferenceValue = vents[i];
        feedbackData.ApplyModifiedPropertiesWithoutUndo();

        var previous = GameObject.Find("RangePolish");
        if (previous != null) Object.DestroyImmediate(previous);
        var environment = new GameObject("RangePolish").transform;
        var randomState = Random.state;
        try
        {
            Random.InitState(1847);
            MakeGrass(environment);
            MakeClouds(environment);
            MakeDetails(environment);
        }
        finally { Random.state = randomState; }

        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) { camera.allowMSAA = true; camera.fieldOfView = 60f; }
        QualitySettings.antiAliasing = 4;
        pipeline.msaaSampleCount = 4;
        pipeline.shadowDistance = 65f;
        pipeline.renderScale = 1f;
        pipeline.mainLightShadowmapResolution = 4096;
        pipeline.shadowDepthBias = 1f;
        pipeline.shadowNormalBias = .8f;
        EditorUtility.SetDirty(pipeline);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Полишинг TurretRange сохранен: трава, частицы, звуки и перезарядка");
    }

    private static ParticleSystem ImpactPrefab()
    {
        string path = root + "/Prefabs/CannonImpact.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing.GetComponent<ParticleSystem>();
        var impact = MakeParticles("CannonImpact", null, star, sparks, 18, new Color(1f, .76f, .08f), .35f, .7f, .12f, .34f, 2.5f, 6.5f, true);
        var shape = impact.shape; shape.radius = .12f;
        var debris = MakeParticles("BlueChips", impact.transform, sphere, sparks, 12, new Color(.13f, .63f, .85f), .25f, .55f, .08f, .19f, 2f, 5f, true);
        var debrisMain = debris.main; debrisMain.gravityModifier = 1f;
        var puff = MakeParticles("ImpactDust", impact.transform, sphere, dust, 10, new Color(.84f, .78f, .6f, .6f), .35f, .65f, .3f, .65f, .5f, 2f, true);
        var puffMain = puff.main; puffMain.gravityModifier = -.08f;
        var prefab = PrefabUtility.SaveAsPrefabAsset(impact.gameObject, path);
        Object.DestroyImmediate(impact.gameObject);

        return prefab.GetComponent<ParticleSystem>();
    }

    private static ParticleSystem MakeParticles(string name, Transform parent, Mesh mesh, Material material, short count, Color color,
        float lifeMin, float lifeMax, float sizeMin, float sizeMax, float speedMin, float speedMax, bool worldSpace)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = .12f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = color; main.maxParticles = 64;
        main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2f);
        var emission = system.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0, count) });
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .05f;
        var size = system.sizeOverLifetime; size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, .45f), new Keyframe(.22f, 1f), new Keyframe(1, 0)));
        var fade = system.colorOverLifetime; fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .6f), new GradientAlphaKey(0, 1) });
        fade.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;

        return system;
    }

    private static void MakeGrass(Transform parent)
    {
        var groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(root + "/Art/Grass.mat");
        if (groundMaterial == null) throw new InvalidOperationException("Не найден материал подложки Grass.mat");
        var shader = Shader.Find("Turret/Grass");
        if (shader == null) throw new InvalidOperationException("Не найден Turret/Grass");
        Color baseColor = groundMaterial.GetColor("_BaseColor");
        for (int shade = 0; shade < 3; shade++)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            for (int tuft = 0; tuft < 900; tuft++)
            {
                Vector3 origin = new(Random.Range(-27f, 27f), -.105f, Random.Range(-26f, 35f));
                bool firingPlatform = Mathf.Abs(origin.x) < 3.8f && origin.z > -11f && origin.z < -3f;
                bool targetPlatform = Mathf.Abs(origin.x) < 4.8f && origin.z > 5f && origin.z < 11f;
                bool firingLane = Mathf.Abs(origin.x) < 1f && origin.z >= -3f && origin.z <= 5f;
                if (firingPlatform || targetPlatform || firingLane) continue;
                for (int blade = 0; blade < 5; blade++)
                {
                    float angle = Random.Range(0, Mathf.PI * 2f);
                    Vector3 side = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    Vector3 forward = Vector3.Cross(Vector3.up, side);
                    float height = Random.Range(.21f, .55f), width = Random.Range(.055f, .11f);
                    Vector3 foot = origin + side * Random.Range(-.13f, .13f);
                    Vector3 tip = foot + Vector3.up * height + forward * height * .3f;
                    Vector3 middle = foot + Vector3.up * height * .48f + forward * height * .06f;
                    int start = vertices.Count;
                    vertices.Add(foot - side * width);
                    vertices.Add(foot + side * width);
                    vertices.Add(foot + forward * width * .4f);
                    vertices.Add(middle - side * width * .6f);
                    vertices.Add(middle + side * width * .6f);
                    vertices.Add(middle + forward * width * .28f);
                    vertices.Add(tip);
                    for (int i = 0; i < 7; i++)
                    {
                        float relativeHeight = i < 3 ? 0 : (i < 6 ? .48f : 1f);
                        normals.Add((Vector3.up + forward * relativeHeight * .18f).normalized);
                        uv.Add(new Vector2(i % 3 * .5f, relativeHeight));
                    }
                    int[] bladeTriangles = { 0, 3, 1, 1, 3, 4, 0, 2, 3, 2, 5, 3, 1, 4, 2, 2, 4, 5, 3, 6, 4, 3, 5, 6, 4, 6, 5 };
                    foreach (int index in bladeTriangles) triangles.Add(start + index);
                }
            }
            string path = root + "/Art/Polish/GrassPatch" + shade + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh { name = "GrassPatch" + shade }; AssetDatabase.CreateAsset(mesh, path); }
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.RecalculateBounds();
            Bounds bounds = mesh.bounds;
            bounds.Expand(.32f);
            mesh.bounds = bounds;
            EditorUtility.SetDirty(mesh);
            string materialPath = root + "/Art/Polish/GrassShade" + shade + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            material.shader = shader;
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_TipColor", Color.Lerp(baseColor, Color.white, .04f + shade * .02f));
            material.SetColor("_ShadowColor", groundMaterial.GetColor("_ShadowColor"));
            material.SetFloat("_Steps", groundMaterial.GetFloat("_Steps"));
            material.SetFloat("_AmbientStrength", groundMaterial.GetFloat("_AmbientStrength"));
            material.SetFloat("_WindStrength", .075f);
            material.SetFloat("_WindSpeed", 1.6f);
            EditorUtility.SetDirty(material);
            MeshObject("Grass" + shade, parent, mesh, material, Vector3.zero, Vector3.one);
        }
    }

    private static void MakeClouds(Transform parent)
    {
        var material = ToonMaterial("CloudWhite", new Color(.94f, .97f, 1f));
        Vector3[] centers = { new(-18, 12, 27), new(16, 14, 31), new(2, 16, 43), new(-29, 13, -12) };
        for (int i = 0; i < centers.Length; i++)
        {
            var cloud = new GameObject("Cloud" + i).transform;
            cloud.SetParent(parent, false);
            cloud.localPosition = centers[i];
            for (int puff = 0; puff < 3; puff++)
            {
                var mesh = MeshObject("Puff" + puff, cloud, sphere, material, new Vector3((puff - 1) * 2.2f, puff == 1 ? .4f : 0, 0), new Vector3(4f, puff == 1 ? 2.8f : 2f, 2.2f));
                mesh.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }

    private static void MakeDetails(Transform parent)
    {
        var stones = new List<CombineInstance>();
        Mesh borderMesh = AssetDatabase.LoadAssetAtPath<Mesh>(root + "/Art/RoundedBox.asset");
        if (borderMesh == null) throw new InvalidOperationException("Не найден RoundedBox.asset для окантовки");
        Vector3[] platforms = { new(0, .13f, -7), new(0, .13f, 8) };
        Vector2[] sizes = { new(7, 7), new(9, 5) };
        for (int platform = 0; platform < platforms.Length; platform++)
        {
            Vector3 center = platforms[platform];
            Vector2 size = sizes[platform];
            for (int side = -1; side <= 1; side += 2)
                for (float z = -.5f * size.y + .3f; z < .5f * size.y; z += .65f)
                {
                    Vector3 position = center + new Vector3(side * (size.x * .5f - .08f), 0, z);
                    stones.Add(new CombineInstance { mesh = borderMesh, transform = Matrix4x4.TRS(position, Quaternion.Euler(0, Random.Range(-5f, 5f), 0), new Vector3(.25f, .18f, .56f)) });
                }
            for (float x = -.5f * size.x + .35f; x < .5f * size.x; x += .65f)
                stones.Add(new CombineInstance { mesh = borderMesh, transform = Matrix4x4.TRS(center + new Vector3(x, 0, size.y * .5f - .08f), Quaternion.Euler(0, Random.Range(-4f, 4f), 0), new Vector3(.56f, .18f, .25f)) });
        }

        Mesh angular = AngularStoneMesh();
        Vector3[] clusters = { new(-7, -.1f, -10), new(6, -.1f, -4), new(-8, -.1f, 12), new(9, -.1f, 15), new(-12, -.1f, 24), new(14, -.1f, 27) };
        foreach (Vector3 center in clusters)
            for (int rock = 0; rock < 3; rock++)
            {
                Vector3 position = center + new Vector3(Random.Range(-.65f, .65f), 0, Random.Range(-.6f, .6f));
                Vector3 scale = new(Random.Range(.45f, 1.15f), Random.Range(.25f, .65f), Random.Range(.4f, .9f));
                position.y -= .26f * scale.y;
                stones.Add(new CombineInstance { mesh = angular, transform = Matrix4x4.TRS(position, Quaternion.Euler(0, Random.Range(0, 360f), 0), scale) });
            }
        CombinedObject("RangeStones", parent, stones, ToonMaterial("BorderStone", new Color(.54f, .58f, .52f)));

        MeshObject("TargetPostLeft", parent, borderMesh, metal, new Vector3(-5f, .48f, 7f), new Vector3(.18f, 1.16f, .18f));
        MeshObject("TargetPostRight", parent, borderMesh, metal, new Vector3(5f, .48f, 7f), new Vector3(.18f, 1.16f, .18f));
        MeshObject("TargetPostCapLeft", parent, borderMesh, yellow, new Vector3(-5f, 1.03f, 7f), new Vector3(.25f, .12f, .25f));
        MeshObject("TargetPostCapRight", parent, borderMesh, yellow, new Vector3(5f, 1.03f, 7f), new Vector3(.25f, .12f, .25f));

        var petals = new List<CombineInstance>();
        var centers = new List<CombineInstance>();
        for (int cluster = 0; cluster < 10; cluster++)
        {
            float side = cluster % 2 == 0 ? -1 : 1;
            Vector3 center = new(side * Random.Range(4.1f, 7.5f), .1f, Random.Range(-3f, 5f));
            for (int flower = 0; flower < 3; flower++)
            {
                Vector3 position = center + new Vector3(Random.Range(-.35f, .35f), Random.Range(-.02f, .07f), Random.Range(-.35f, .35f));
                centers.Add(new CombineInstance { mesh = sphere, transform = Matrix4x4.TRS(position, Quaternion.identity, new Vector3(.075f, .035f, .075f)) });
                for (int petal = 0; petal < 5; petal++)
                {
                    float angle = petal * Mathf.PI * .4f;
                    Vector3 offset = new(Mathf.Cos(angle) * .055f, -.012f, Mathf.Sin(angle) * .055f);
                    petals.Add(new CombineInstance { mesh = sphere, transform = Matrix4x4.TRS(position + offset, Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0), new Vector3(.085f, .025f, .045f)) });
                }
            }
        }
        CombinedObject("FieldFlowerPetals", parent, petals, ToonMaterial("FlowerCream", new Color(.96f, .93f, .66f)));
        CombinedObject("FieldFlowerCenters", parent, centers, yellow);
    }

    private static void CombinedObject(string name, Transform parent, List<CombineInstance> pieces, Material material)
    {
        string path = root + "/Art/Polish/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null) { mesh = new Mesh { name = name }; AssetDatabase.CreateAsset(mesh, path); }
        mesh.Clear();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.CombineMeshes(pieces.ToArray(), true, true);
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        GameObject combined = MeshObject(name, parent, mesh, material, Vector3.zero, Vector3.one);
        if (name == "RangeStones") combined.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    private static Mesh AngularStoneMesh()
    {
        string path = root + "/Art/Polish/AngularStone.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        Vector3[] corners = { new(-.14f, .9f, .04f), new(0, 0, 0), new(-.5f, .22f, 0), new(.55f, .25f, .08f), new(.05f, .28f, .5f), new(-.03f, .36f, -.46f) };
        int[] faces = { 0, 4, 3, 0, 2, 4, 0, 5, 2, 0, 3, 5, 1, 3, 4, 1, 4, 2, 1, 2, 5, 1, 5, 3 };
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        foreach (int index in faces) { triangles.Add(vertices.Count); vertices.Add(corners[index]); }
        var mesh = new Mesh { name = "AngularStone" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);

        return mesh;
    }

    private static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, Vector3 position, Vector3 scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (name.StartsWith("TargetPost", StringComparison.Ordinal)) go.AddComponent<MeshCollider>().sharedMesh = mesh;

        return go;
    }

    private static Mesh SphereMesh()
    {
        var temporary = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = temporary.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(temporary);

        return mesh;
    }

    private static Mesh StarMesh()
    {
        string path = root + "/Art/Polish/ImpactStar.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        var vertices = new List<Vector3> { new(0, 0, -.05f), new(0, 0, .05f) };
        var triangles = new List<int>();
        for (int side = 0; side < 2; side++)
            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI / 5f, radius = i % 2 == 0 ? .5f : .22f;
                vertices.Add(new Vector3(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius, side == 0 ? -.05f : .05f));
            }
        for (int i = 0; i < 10; i++)
        {
            int a = 2 + i, b = 2 + (i + 1) % 10, c = a + 10, d = b + 10;
            triangles.AddRange(new[] { 0, a, b, 1, d, c, a, c, b, b, c, d });
        }
        var mesh = new Mesh { name = "ImpactStar" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);

        return mesh;
    }

    private static Material ParticleMaterial(string name, Color color)
    {
        string path = root + "/Art/Polish/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) throw new InvalidOperationException("Не найден URP Particles/Unlit");
        var material = new Material(shader); material.SetColor("_BaseColor", color);
        material.SetFloat("_Surface", 1);
        material.SetFloat("_Blend", 0);
        material.SetInt("_Cull", (int)CullMode.Off);
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        AssetDatabase.CreateAsset(material, path);

        return material;
    }

    private static Material ToonMaterial(string name, Color color)
    {
        string path = root + "/Art/Polish/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var shader = Shader.Find("Turret/Toon");
        if (shader == null) throw new InvalidOperationException("Не найден Turret/Toon");
        var material = new Material(shader); material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);

        return material;
    }

    private static AudioClip ImportSound(string relativePath, string name)
    {
        string source = Path.Combine(audioSource, relativePath), target = root + "/Audio/" + name;
        if (!File.Exists(target))
        {
            if (!File.Exists(source)) throw new FileNotFoundException("Не найден звук " + source);
            File.Copy(source, target);
        }
        AssetDatabase.ImportAsset(target);
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(target);
        if (clip == null) throw new InvalidOperationException("Не загрузился AudioClip " + target);

        return clip;
    }

    private static Transform RequireChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null) throw new InvalidOperationException("Не найден " + parent.name + "/" + name);

        return child;
    }

    private static void RemoveOwnChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) Object.DestroyImmediate(child.gameObject);
    }

    private static void References(Object target, params (string name, Object value)[] fields)
    {
        if (target == null) throw new InvalidOperationException("Не найден компонент для назначения ссылок");
        var data = new SerializedObject(target);
        foreach (var field in fields)
        {
            var property = data.FindProperty(field.name);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + ": не найдено поле " + field.name);
            property.objectReferenceValue = field.value;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
