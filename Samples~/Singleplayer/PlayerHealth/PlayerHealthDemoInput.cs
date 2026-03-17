using UnityEngine;
using Healthy;

public class PlayerHealthDemoInput : MonoBehaviour
{
    [SerializeField] private Health health;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) health.TakeDamage(new DamageInfo { Amount = 25 });
        if (Input.GetKeyDown(KeyCode.H)) health.HealHealth(25);
        if (Input.GetKeyDown(KeyCode.S)) health.ChargeShield(25);
        if (Input.GetKeyDown(KeyCode.O)) health.HealHealth(200);
        if (Input.GetKeyDown(KeyCode.R)) health.Revive();
    }
}
