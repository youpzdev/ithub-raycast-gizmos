using System;
using System.Collections.Generic;
using UnityEngine;

public class Cannonball : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurretSettings settings;
    [SerializeField] private Rigidbody body;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private ParticleSystem smokePrefab;
    [SerializeField] private ParticleSystem impactPrefab;

    private readonly Collider[] hits = new Collider[32];
    private readonly HashSet<Rigidbody> pushedBodies = new();

    private Action cancelLifetime;

    private Vector3 flightDirection;
    private bool flying;
    private float launchTime;

    private ParticleSystem smoke;

    public event Action<Vector3> Impacted;

    public void PrewarmSmoke() => Pooling.Prewarm(smokePrefab.gameObject, 16);

    public void Launch(Vector3 direction)
    {
        if (direction.sqrMagnitude == 0f)
        {
            Debug.LogError($"Снаряд {name}: Launch получил direction={direction}", this);
            ReturnToPool();

            return;
        }

        cancelLifetime?.Invoke();
        flightDirection = direction.normalized;
        body.useGravity = false;
        body.linearDamping = settings.ProjectileDrag;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.angularVelocity = Vector3.zero;
        body.linearVelocity = flightDirection * settings.ProjectileSpeed;
        body.WakeUp();
        trail.Clear();
        smoke = Pooling.Instantiate(smokePrefab.gameObject, transform.position, Quaternion.identity).GetComponent<ParticleSystem>();
        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        smoke.Play(true);
        launchTime = Time.fixedTime;
        flying = true;
        cancelLifetime = Timer.After(settings.ProjectileLifetime, ReturnToPool, this);
    }

    public void ReturnToPool() {FinishFlight(false);}

    private void FinishFlight(bool lingerSmoke)
    {
        flying = false;
        cancelLifetime?.Invoke();
        cancelLifetime = null;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        trail.Clear();
        if (smoke != null)
        {
            var effect = smoke;
            smoke = null;
            effect.Stop(true, lingerSmoke ? ParticleSystemStopBehavior.StopEmitting : ParticleSystemStopBehavior.StopEmittingAndClear);
            if (lingerSmoke) Timer.After(effect.main.startLifetime.constantMax, () => Pooling.Destroy(effect.gameObject), effect);
            else Pooling.Destroy(effect.gameObject);
        }
        Pooling.Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (!flying) return;

        body.AddForce(CannonFlight.Acceleration(Time.fixedTime - launchTime, settings), ForceMode.Acceleration);
        if (body.linearVelocity.sqrMagnitude > .001f) flightDirection = body.linearVelocity.normalized;
    }

    private void LateUpdate()
    {
        if (flying) smoke.transform.position = transform.position;
    }

    private void OnDisable()
    {
        flying = false;
        cancelLifetime?.Invoke();
        cancelLifetime = null;
        if (smoke != null)
        {
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Pooling.Destroy(smoke.gameObject);
            smoke = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!flying) return;

        flying = false;
        Vector3 point = collision.GetContact(0).point;
        PushBlocks(point);
        Audio.Play(settings.ImpactClip, point, settings.ImpactVolume, UnityEngine.Random.Range(settings.MinSoundPitch, settings.MaxSoundPitch));
        PlayImpact(point, collision.GetContact(0).normal);
        Impacted?.Invoke(point);
        FinishFlight(true);
    }

    private void PushBlocks(Vector3 point)
    {
        pushedBodies.Clear();
        int count = Physics.OverlapSphereNonAlloc(point, settings.ImpactRadius, hits, 1 << 8, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) Debug.LogWarning($"Снаряд {name}: ImpactRadius={settings.ImpactRadius} заполнил буфер из {hits.Length} коллайдеров", this);

        for (int i = 0; i < count; i++)
        {
            Rigidbody target = hits[i].attachedRigidbody;
            if (target == null || !target.TryGetComponent<WallBlock>(out _) || !pushedBodies.Add(target)) continue;

            float distance = Vector3.Distance(point, hits[i].ClosestPoint(point));
            float force = settings.ImpactForce * Mathf.Clamp01(1f - distance / settings.ImpactRadius);
            target.AddForceAtPosition(flightDirection * force, point, ForceMode.Impulse);
        }
    }

    private void PlayImpact(Vector3 point, Vector3 normal)
    {
        if (impactPrefab == null) return;

        var effect = Pooling.Instantiate(impactPrefab.gameObject, point, Quaternion.LookRotation(normal)).GetComponent<ParticleSystem>();
        effect.Clear(true);
        effect.Play(true);
        var main = effect.main;
        Timer.After(main.duration + main.startLifetime.constantMax, () => Pooling.Destroy(effect.gameObject), effect);
    }
}
