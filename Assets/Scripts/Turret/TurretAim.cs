using UnityEngine;
using UnityEngine.InputSystem;

public class TurretAim : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurretSettings settings;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform head;
    [SerializeField] private Transform muzzle;
    [SerializeField] private LineRenderer beam;
    [SerializeField] private TargetWall resetWall;

    private float yaw;
    private float pitch;
    private float desiredYaw;
    private float desiredPitch;
    private float yawVelocity;
    private float pitchVelocity;

    private Quaternion initialHeadRotation;
    private Vector3 rayEnd;

    private float shake;

    private readonly Vector3[] trajectory = new Vector3[400];
    private int trajectoryCount;

    public bool Captured { get; private set; }
    public int CapturedFrame { get; private set; }
    public Transform Muzzle => muzzle;
    public Vector3 RayEnd => rayEnd;

    private void Awake()
    {
        initialHeadRotation = head.rotation;
        ResetAim();
        Capture();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) {Release(); return;}
        if (mouse == null) return;
        if (!Captured)
        {
            if (mouse.leftButton.wasPressedThisFrame) Capture();
            return;
        }

        if (CapturedFrame == Time.frameCount) return;

        Vector2 delta = mouse.delta.ReadValue() * settings.MouseSensitivity;
        desiredYaw = Mathf.Repeat(desiredYaw + delta.x, 360f);
        desiredPitch = Mathf.Clamp(desiredPitch - delta.y, -settings.MaxPitch, -settings.MinPitch);
        yaw = Mathf.SmoothDampAngle(yaw, desiredYaw, ref yawVelocity, settings.AimSmoothTime);
        pitch = Mathf.SmoothDampAngle(pitch, desiredPitch, ref pitchVelocity, settings.AimSmoothTime);
    }

    private void LateUpdate()
    {
        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = transform.position + Vector3.up * 1.45f;
        Vector3 desiredPosition = pivot + Quaternion.Euler(0, yaw, 0) * settings.CameraOffset;
        Vector3 cameraPath = desiredPosition - pivot;
        if (Physics.SphereCast(pivot, .22f, cameraPath.normalized, out RaycastHit obstacle, cameraPath.magnitude, (1 << 0) | (1 << 8), QueryTriggerInteraction.Ignore))
            desiredPosition = pivot + cameraPath.normalized * Mathf.Max(.3f, obstacle.distance - .08f);
        viewCamera.transform.SetPositionAndRotation(desiredPosition, orbit);
        if (shake > 0f)
        {
            viewCamera.transform.position += Random.insideUnitSphere * shake;
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * .35f);
        }

        Ray cameraRay = viewCamera.ViewportPointToRay(new Vector3(.5f, .5f));
        Vector3 point = cameraRay.GetPoint(settings.AimDistance);
        if (Physics.Raycast(cameraRay, out RaycastHit cameraHit, settings.AimDistance, (1 << 0) | (1 << 8), QueryTriggerInteraction.Ignore)) point = cameraHit.point;
        Vector3 direction = point - head.position;
        if (direction.sqrMagnitude > .001f)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            Quaternion eased = Quaternion.Slerp(head.rotation, rotation, 1f - Mathf.Exp(-settings.BarrelFollow * Time.deltaTime));
            head.rotation = Quaternion.RotateTowards(head.rotation, eased, settings.TurnSpeed * Time.deltaTime);
        }
        UpdateRay();
    }

    public void UpdateRay()
    {
        Ray ray = new(muzzle.position, muzzle.forward);
        rayEnd = ray.GetPoint(settings.AimDistance);
        if (Physics.Raycast(ray, out RaycastHit hit, settings.AimDistance, (1 << 0) | (1 << 8), QueryTriggerInteraction.Ignore))
            rayEnd = hit.point;

        beam.SetPosition(0, muzzle.position);
        beam.SetPosition(1, rayEnd);
    }

    public void ResetAim()
    {
        Vector3 target = Vector3.zero;
        foreach (var block in resetWall.Blocks) target += block.transform.position;
        target /= resetWall.Blocks.Count;
        Vector3 offset = target - (transform.position + Vector3.up * 1.45f);
        float horizontal = new Vector2(offset.x, offset.z).magnitude;
        yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg - Mathf.Asin(Mathf.Clamp(settings.CameraOffset.x / horizontal, -1f, 1f)) * Mathf.Rad2Deg;
        float forwardDistance = Mathf.Sqrt(horizontal * horizontal - settings.CameraOffset.x * settings.CameraOffset.x) - settings.CameraOffset.z;
        pitch = -Mathf.Atan2(offset.y - settings.CameraOffset.y, forwardDistance) * Mathf.Rad2Deg;
        desiredYaw = yaw;
        desiredPitch = pitch;
        yawVelocity = pitchVelocity = 0f;
        shake = 0f;
        head.rotation = initialHeadRotation;
    }

    public void AddShake(float strength) => shake = Mathf.Max(shake, strength);

    public void Capture()
    {
        Captured = true;
        CapturedFrame = Time.frameCount;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Release()
    {
        Captured = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) Release();
    }

    private void OnDisable()
    {
        Release();
    }

    private void OnDrawGizmos()
    {
        if (muzzle == null || settings == null) return;

        Gizmos.color = new Color(.15f, .85f, 1f, .75f);
        Vector3 straightEnd = muzzle.position + muzzle.forward * settings.AimDistance;
        if (Physics.Raycast(muzzle.position, muzzle.forward, out RaycastHit straightHit, settings.AimDistance, (1 << 0) | (1 << 8), QueryTriggerInteraction.Ignore)) straightEnd = straightHit.point;
        Gizmos.DrawLine(muzzle.position, straightEnd);
        CalculateTrajectory();
        Gizmos.color = new Color(1f, .7f, .1f);
        for (int i = 1; i < trajectoryCount; i++)
        {
            Gizmos.DrawLine(trajectory[i - 1], trajectory[i]);
            if (i % 10 == 0) Gizmos.DrawWireSphere(trajectory[i], .055f);
        }
        Gizmos.DrawWireSphere(trajectory[trajectoryCount - 1], .275f);
    }

    public int CalculateTrajectory()
    {
        Vector3 position = muzzle.position;
        Vector3 velocity = muzzle.forward * settings.ProjectileSpeed;
        float step = Time.fixedDeltaTime;
        trajectoryCount = 1;
        trajectory[0] = position;
        if (Physics.CheckSphere(position, .275f, (1 << 0) | (1 << 8), QueryTriggerInteraction.Ignore)) return trajectoryCount;

        int steps = Mathf.Min(trajectory.Length - 1, Mathf.CeilToInt(settings.ProjectileLifetime / step));
        for (int i = 1; i <= steps; i++)
        {
            velocity = CannonFlight.NextVelocity(velocity, i * step, step, settings);
            Vector3 next = position + velocity * step;
            Vector3 delta = next - position;
            if (Physics.SphereCast(position, .275f, delta.normalized, out RaycastHit hit, delta.magnitude, (1 << 0) | (1 << 8), QueryTriggerInteraction.Ignore))
            {
                trajectory[trajectoryCount++] = position + delta.normalized * hit.distance;
                break;
            }
            trajectory[trajectoryCount++] = next;
            position = next;
        }

        return trajectoryCount;
    }
}
