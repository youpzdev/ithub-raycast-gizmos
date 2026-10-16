using UnityEngine;

public class WallBlock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurretSettings settings;
    [SerializeField] private Rigidbody body;

    private Vector3 startingPosition;
    private Quaternion startingRotation;

    private float nextSoundTime;

    public Rigidbody Body => body;

    private void Awake()
    {
        startingPosition = body.position;
        startingRotation = body.rotation;
        body.mass = settings.BlockMass;
        body.Sleep();
        nextSoundTime = Time.time + settings.BlockSoundCooldown;
    }

    public void Restore()
    {
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = startingPosition;
        body.rotation = startingRotation;
        transform.SetPositionAndRotation(startingPosition, startingRotation);
        body.Sleep();
        nextSoundTime = Time.time + settings.BlockSoundCooldown;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time < nextSoundTime || collision.relativeVelocity.magnitude < settings.BlockSoundThreshold) return;
        if (settings.BlockClips.Length == 0) return;

        nextSoundTime = Time.time + settings.BlockSoundCooldown;
        AudioClip clip = settings.BlockClips[Random.Range(0, settings.BlockClips.Length)];
        Audio.Play(clip, collision.GetContact(0).point, settings.BlockSoundVolume, Random.Range(settings.MinSoundPitch, settings.MaxSoundPitch));
    }
}
