using UnityEngine;
using UnityEngine.UI;

public class TurretReticle : MonoBehaviour
{
    [SerializeField] private TurretSettings settings;
    [SerializeField] private TurretShooter shooter;
    [SerializeField] private Image progress;
    [SerializeField] private Image center;

    private void Update()
    {
        float ready = shooter.ReloadProgress;
        progress.fillAmount = ready;
        center.color = Color.Lerp(settings.ReloadColor, settings.ReadyColor, ready);
    }
}
