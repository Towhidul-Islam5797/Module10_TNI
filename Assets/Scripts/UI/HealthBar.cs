using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Image fillImage;

    private void Update()
    {
        fillImage.fillAmount = (float)health.CurrentHealth / health.MaxHealth;
    }
}
