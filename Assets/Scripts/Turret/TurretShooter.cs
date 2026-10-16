using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TurretShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurretSettings settings;
    [SerializeField] private TurretAim aim;
    [SerializeField] private Cannonball projectilePrefab;
    [SerializeField] private TargetWall wall;

    private readonly List<Cannonball> projectiles = new();

    private float readyAt;

    public event Action Fired;
    public event Action ResetPerformed;
    public float ReloadProgress => settings.ReloadTime <= 0f ? 1f : Mathf.Clamp01(1f - (readyAt - Time.time) / settings.ReloadTime);

    private void Start()
    {
        Pooling.Prewarm(projectilePrefab.gameObject, 12);
        projectilePrefab.PrewarmSmoke();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ResetRange();
        if (!aim.Captured || aim.CapturedFrame == Time.frameCount) return;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) TryFire();
    }

    public bool TryFire()
    {
        if (Time.time < readyAt) return false;

        readyAt = Time.time + settings.ReloadTime;
        Transform muzzle = aim.Muzzle;
        var projectile = Pooling.Instantiate(projectilePrefab.gameObject, muzzle.position, muzzle.rotation).GetComponent<Cannonball>();
        if (!projectiles.Contains(projectile)) projectiles.Add(projectile);
        projectile.Launch(muzzle.forward);
        Fired?.Invoke();

        return true;
    }

    public void ResetRange()
    {
        foreach (var projectile in projectiles)
            if (projectile != null && projectile.gameObject.activeSelf) projectile.ReturnToPool();
        wall.Restore();
        aim.ResetAim();
        readyAt = Time.time;
        ResetPerformed?.Invoke();
    }
}
