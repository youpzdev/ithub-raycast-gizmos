using DG.Tweening;
using UnityEngine;

public class TurretFeedback : MonoBehaviour
{
    [SerializeField] private TurretSettings settings;
    [SerializeField] private TurretShooter shooter;
    [SerializeField] private TurretAim aim;
    [SerializeField] private Transform barrel;
    [SerializeField] private ParticleSystem flash;
    [SerializeField] private Transform bodyVisual;
    [SerializeField] private Transform collar;
    [SerializeField] private Transform[] reloadVents;
    [SerializeField] private ParticleSystem reloadSteam;

    private Vector3 restPosition;
    private Vector3 barrelScale;
    private Vector3 bodyScale;
    private Vector3 collarScale;

    private Quaternion[] ventRotations;

    private Sequence recoil;

    private void Awake()
    {
        restPosition = barrel.localPosition;
        barrelScale = barrel.localScale;
        bodyScale = bodyVisual.localScale;
        collarScale = collar.localScale;
        ventRotations = new Quaternion[reloadVents.Length];
        for (int i = 0; i < reloadVents.Length; i++) ventRotations[i] = reloadVents[i].localRotation;
    }

    private void OnEnable()
    {
        shooter.Fired += OnFired;
        shooter.ResetPerformed += ResetVisuals;
    }

    private void OnFired()
    {
        ResetVisuals();
        float duration = settings.ReloadTime;
        recoil = DOTween.Sequence();
        recoil.Append(barrel.DOLocalMove(restPosition - Vector3.forward * settings.RecoilDistance, .065f).SetEase(Ease.OutQuad));
        recoil.Append(barrel.DOLocalMove(restPosition + Vector3.forward * .025f, duration * .62f).SetEase(Ease.OutCubic));
        recoil.Append(barrel.DOLocalMove(restPosition, duration * .15f).SetEase(Ease.InOutSine));
        recoil.Insert(0, bodyVisual.DOScale(Vector3.Scale(bodyScale, new Vector3(1.08f, .84f, 1.07f)), .07f).SetEase(Ease.OutQuad));
        recoil.Insert(.07f, bodyVisual.DOScale(bodyScale, duration * .7f).SetEase(Ease.OutBack, 1.25f));
        recoil.Insert(0, barrel.DOScale(Vector3.Scale(barrelScale, new Vector3(1.08f, 1.08f, .88f)), .07f).SetEase(Ease.OutQuad));
        recoil.Insert(.07f, barrel.DOScale(barrelScale, duration * .75f).SetEase(Ease.OutBack, 1.2f));
        recoil.Insert(.04f, collar.DOScale(collarScale * 1.2f, .12f).SetEase(Ease.OutQuad));
        recoil.Insert(.16f, collar.DOScale(collarScale, duration * .6f).SetEase(Ease.InOutSine));
        for (int i = 0; i < reloadVents.Length; i++)
        {
            Transform vent = reloadVents[i];
            Quaternion closed = ventRotations[i];
            float angle = i % 2 == 0 ? -55f : 55f;
            recoil.Insert(.045f, vent.DOLocalRotateQuaternion(closed * Quaternion.Euler(0, 0, angle), duration * .25f).SetEase(Ease.OutCubic));
            recoil.Insert(duration * .6f, vent.DOLocalRotateQuaternion(closed, duration * .35f).SetEase(Ease.InOutSine));
        }

        recoil.InsertCallback(.09f, () => reloadSteam.Play(true));
        flash.Play(true);
        aim.AddShake(settings.ShakeStrength);
        Audio.Play(settings.ShotClip, aim.Muzzle.position, settings.ShotVolume, Random.Range(settings.MinSoundPitch, settings.MaxSoundPitch));
    }

    private void OnDisable()
    {
        shooter.Fired -= OnFired;
        shooter.ResetPerformed -= ResetVisuals;
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        recoil?.Kill();
        barrel.localPosition = restPosition;
        barrel.localScale = barrelScale;
        bodyVisual.localScale = bodyScale;
        collar.localScale = collarScale;
        for (int i = 0; i < reloadVents.Length; i++) reloadVents[i].localRotation = ventRotations[i];
        flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        reloadSteam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
