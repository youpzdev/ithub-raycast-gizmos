using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Turret/Settings")]
public class TurretSettings : ScriptableObject
{
    [Header("Aim")]
    [SerializeField] [FormerlySerializedAs("MouseSensitivity")] private float mouseSensitivity = .12f;
    [SerializeField] [FormerlySerializedAs("TurnSpeed")] private float turnSpeed = 230f;
    [SerializeField] [FormerlySerializedAs("AimSmoothTime")] private float aimSmoothTime = .075f;
    [SerializeField] [FormerlySerializedAs("BarrelFollow")] private float barrelFollow = 10f;
    [SerializeField] [FormerlySerializedAs("MinPitch")] private float minPitch = -18f;
    [SerializeField] [FormerlySerializedAs("MaxPitch")] private float maxPitch = 48f;
    [SerializeField] [FormerlySerializedAs("AimDistance")] private float aimDistance = 90f;
    [SerializeField] [FormerlySerializedAs("CameraOffset")] private Vector3 cameraOffset = new(.9f, .7f, -3.8f);

    [Header("Shot")]
    [SerializeField] [FormerlySerializedAs("ReloadTime")] private float reloadTime = .5f;
    [SerializeField] [FormerlySerializedAs("ProjectileSpeed")] private float projectileSpeed = 50f;
    [SerializeField] [FormerlySerializedAs("ProjectileLifetime")] private float projectileLifetime = 6.5f;
    [SerializeField] [FormerlySerializedAs("FlightStraightTime")] private float flightStraightTime = .32f;
    [SerializeField] [FormerlySerializedAs("ProjectileDrag")] private float projectileDrag = .32f;
    [SerializeField] [FormerlySerializedAs("ProjectileGravity")] private float projectileGravity = 1.1f;
    [SerializeField] [FormerlySerializedAs("ImpactForce")] private float impactForce = 18f;
    [SerializeField] [FormerlySerializedAs("ImpactRadius")] private float impactRadius = 3.2f;
    [SerializeField] [FormerlySerializedAs("BlockMass")] private float blockMass = 1f;

    [Header("Look")]
    [SerializeField] [FormerlySerializedAs("ReadyColor")] private Color readyColor = new(1f, .94f, .6f);
    [SerializeField] [FormerlySerializedAs("ReloadColor")] private Color reloadColor = new(.47f, .56f, .6f);
    [SerializeField] [FormerlySerializedAs("RecoilDistance")] private float recoilDistance = .44f;
    [SerializeField] [FormerlySerializedAs("ShakeStrength")] private float shakeStrength = .045f;

    [Header("Sound")]
    [SerializeField] [FormerlySerializedAs("ShotClip")] private AudioClip shotClip;
    [SerializeField] [FormerlySerializedAs("ImpactClip")] private AudioClip impactClip;
    [SerializeField] [FormerlySerializedAs("BlockClips")] private AudioClip[] blockClips = System.Array.Empty<AudioClip>();
    [SerializeField] [FormerlySerializedAs("ShotVolume")] private float shotVolume = .6f;
    [SerializeField] [FormerlySerializedAs("ImpactVolume")] private float impactVolume = .65f;
    [SerializeField] [FormerlySerializedAs("BlockSoundThreshold")] private float blockSoundThreshold = 1.5f;
    [SerializeField] [FormerlySerializedAs("BlockSoundCooldown")] private float blockSoundCooldown = .12f;
    [SerializeField] [FormerlySerializedAs("BlockSoundVolume")] private float blockSoundVolume = .35f;
    [SerializeField] [FormerlySerializedAs("MinSoundPitch")] private float minSoundPitch = .9f;
    [SerializeField] [FormerlySerializedAs("MaxSoundPitch")] private float maxSoundPitch = 1.1f;

    public float MouseSensitivity {get => mouseSensitivity; set => mouseSensitivity = value;}
    public float TurnSpeed {get => turnSpeed; set => turnSpeed = value;}
    public float AimSmoothTime {get => aimSmoothTime; set => aimSmoothTime = value;}
    public float BarrelFollow {get => barrelFollow; set => barrelFollow = value;}
    public float MinPitch {get => minPitch; set => minPitch = value;}
    public float MaxPitch {get => maxPitch; set => maxPitch = value;}
    public float AimDistance {get => aimDistance; set => aimDistance = value;}
    public Vector3 CameraOffset {get => cameraOffset; set => cameraOffset = value;}

    public float ReloadTime {get => reloadTime; set => reloadTime = value;}
    public float ProjectileSpeed {get => projectileSpeed; set => projectileSpeed = value;}
    public float ProjectileLifetime {get => projectileLifetime; set => projectileLifetime = value;}
    public float FlightStraightTime {get => flightStraightTime; set => flightStraightTime = value;}
    public float ProjectileDrag {get => projectileDrag; set => projectileDrag = value;}
    public float ProjectileGravity {get => projectileGravity; set => projectileGravity = value;}
    public float ImpactForce {get => impactForce; set => impactForce = value;}
    public float ImpactRadius {get => impactRadius; set => impactRadius = value;}
    public float BlockMass {get => blockMass; set => blockMass = value;}

    public Color ReadyColor {get => readyColor; set => readyColor = value;}
    public Color ReloadColor {get => reloadColor; set => reloadColor = value;}
    public float RecoilDistance {get => recoilDistance; set => recoilDistance = value;}
    public float ShakeStrength {get => shakeStrength; set => shakeStrength = value;}

    public AudioClip ShotClip {get => shotClip; set => shotClip = value;}
    public AudioClip ImpactClip {get => impactClip; set => impactClip = value;}
    public AudioClip[] BlockClips {get => blockClips; set => blockClips = value;}
    public float ShotVolume {get => shotVolume; set => shotVolume = value;}
    public float ImpactVolume {get => impactVolume; set => impactVolume = value;}
    public float BlockSoundThreshold {get => blockSoundThreshold; set => blockSoundThreshold = value;}
    public float BlockSoundCooldown {get => blockSoundCooldown; set => blockSoundCooldown = value;}
    public float BlockSoundVolume {get => blockSoundVolume; set => blockSoundVolume = value;}
    public float MinSoundPitch {get => minSoundPitch; set => minSoundPitch = value;}
    public float MaxSoundPitch {get => maxSoundPitch; set => maxSoundPitch = value;}
}
