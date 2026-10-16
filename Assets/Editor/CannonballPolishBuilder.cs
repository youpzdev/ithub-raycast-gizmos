using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class CannonballPolishBuilder
{
    private const string root = "Assets/_Features/Turret";

    [MenuItem("Tools/Turret/Apply Cannonball Polish")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Останови Play Mode перед настройкой Cannonball");
        ParticleSystem smoke = MakeSmoke();
        string path = root + "/Prefabs/Cannonball.prefab";
        var ball = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var data = new SerializedObject(ball.GetComponent<Cannonball>());
            data.FindProperty("smokePrefab").objectReferenceValue = smoke;
            if (data.FindProperty("impactPrefab").objectReferenceValue == null)
                data.FindProperty("impactPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ParticleSystem>(root + "/Prefabs/CannonImpact.prefab");
            data.ApplyModifiedPropertiesWithoutUndo();
            var trail = ball.GetComponentInChildren<TrailRenderer>(true);
            if (trail == null) throw new InvalidOperationException("В Cannonball.prefab не найден TrailRenderer");
            trail.time = .22f;
            trail.widthMultiplier = .32f;
            trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            PrefabUtility.SaveAsPrefabAsset(ball, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(ball); }

        AssetDatabase.SaveAssets();
        Debug.Log("Cannonball сохранен: объемный дымный шлейф");
    }

    private static ParticleSystem MakeSmoke()
    {
        string path = root + "/Prefabs/CannonSmoke.prefab";
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) throw new InvalidOperationException("Не найден шейдер URP Particles/Unlit для CannonSmoke");
        string materialPath = root + "/Art/Polish/CannonSmoke.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        bool createMaterial = material == null;
        if (createMaterial) material = new Material(shader);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1);
        material.SetFloat("_Blend", 0);
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        if (createMaterial) AssetDatabase.CreateAsset(material, materialPath);
        else EditorUtility.SetDirty(material);

        var temporary = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh sphere = temporary.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(temporary);
        var go = new GameObject("CannonSmoke");
        try
        {
            var smoke = go.AddComponent<ParticleSystem>();
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = smoke.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.65f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(.16f, .3f);
            main.startSpeed = .12f;
            main.startColor = new Color(.85f, .88f, .91f, .55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 360;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = smoke.emission;
            emission.rateOverTime = 12f;
            emission.rateOverDistance = 13f;
            emission.SetBursts(Array.Empty<ParticleSystem.Burst>());
            var shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .08f;
            var velocity = smoke.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = .2f;
            velocity.y = .6f;
            velocity.z = .1f;
            var size = smoke.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, .65f, 1, 2.7f));
            var color = smoke.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(new Color(1f, .91f, .65f), 0), new GradientColorKey(Color.white, .2f), new GradientColorKey(new Color(.8f, .84f, .87f), 1) },
                new[] { new GradientAlphaKey(.75f, 0), new GradientAlphaKey(.6f, .4f), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            var noise = smoke.noise;
            noise.enabled = true;
            noise.strength = .18f;
            noise.frequency = .7f;
            noise.scrollSpeed = .5f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = sphere;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);

            return prefab.GetComponent<ParticleSystem>();
        }
        finally { Object.DestroyImmediate(go); }
    }
}
