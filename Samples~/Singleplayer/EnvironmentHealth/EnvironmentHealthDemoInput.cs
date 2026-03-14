using UnityEngine;
using Healthy;

public class EnvironmentHealthDemoInput : MonoBehaviour
{
    [SerializeField] private Health health;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) health.Damage(25);
        if (Input.GetKeyDown(KeyCode.R)) health.Revive();
    }
}
