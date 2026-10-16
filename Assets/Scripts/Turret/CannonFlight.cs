using UnityEngine;

public static class CannonFlight
{
    public static Vector3 Acceleration(float elapsed, TurretSettings settings) => elapsed >= settings.FlightStraightTime ? Physics.gravity * settings.ProjectileGravity : Vector3.zero;

    public static Vector3 NextVelocity(Vector3 velocity, float elapsed, float step, TurretSettings settings) => (velocity + Acceleration(elapsed, settings) * step) * Mathf.Max(0f, 1f - settings.ProjectileDrag * step);
}
