using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TurretRockRepair
{
    [MenuItem("Tools/Turret/Repair Rock Collisions")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Останови Play Mode перед исправлением камней");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.path.EndsWith("/TurretRange.unity", StringComparison.Ordinal)) throw new InvalidOperationException("Открой сцену TurretRange перед исправлением камней");
        GameObject grass = null;
        GameObject polish = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "GrassField") grass = root;
            if (root.name == "RangePolish") polish = root;
        }
        if (grass == null || !grass.TryGetComponent<Collider>(out var ground)) throw new InvalidOperationException("Не найден Collider GrassField");
        float groundY = ground.bounds.max.y;
        int rocks = 0, lowered = 0, details = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (!root.name.StartsWith("DistantRock", StringComparison.Ordinal) || !int.TryParse(root.name.Substring("DistantRock".Length), out _)) continue;
            Mesh mesh = SetCollider(root);
            float bottom = float.PositiveInfinity;
            foreach (Vector3 vertex in mesh.vertices) bottom = Mathf.Min(bottom, root.transform.TransformPoint(vertex).y);
            if (bottom > groundY + .001f)
            {
                Vector3 position = root.transform.position;
                position.y -= bottom - groundY + .03f;
                root.transform.position = position;
                lowered++;
            }
            rocks++;
        }
        if (polish != null)
            foreach (Transform child in polish.transform)
                if (child.name == "RangeStones" || child.name.StartsWith("TargetPost", StringComparison.Ordinal)) { SetCollider(child.gameObject); details++; }

        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Камни исправлены: крупных {rocks}, опущено {lowered}, объектов окантовки и столбиков {details}");
    }

    private static Mesh SetCollider(GameObject target)
    {
        var filter = target.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) throw new InvalidOperationException(target.name + ": не найден MeshFilter.sharedMesh");
        var collider = target.GetComponent<MeshCollider>();
        if (collider == null) collider = target.AddComponent<MeshCollider>();
        foreach (Collider previous in target.GetComponents<Collider>())
            if (previous != collider) previous.enabled = false;
        collider.convex = false;
        collider.isTrigger = false;
        collider.enabled = true;
        collider.sharedMesh = filter.sharedMesh;

        return filter.sharedMesh;
    }
}
