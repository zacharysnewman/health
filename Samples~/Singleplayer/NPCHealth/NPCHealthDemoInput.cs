using UnityEngine;
using Healthy;

public class NPCHealthDemoInput : MonoBehaviour
{
    [SerializeField] private Health health;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) health.TakeDamage(new DamageInfo { Amount = 25 });
        if (Input.GetKeyDown(KeyCode.C)) health.ChargeShield(200);
        if (Input.GetKeyDown(KeyCode.R)) health.Revive();
    }
}
