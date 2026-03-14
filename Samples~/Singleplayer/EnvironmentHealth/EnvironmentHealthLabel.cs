using TMPro;
using UnityEngine;
using Healthy;

[RequireComponent(typeof(TMP_Text))]
public class EnvironmentHealthLabel : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private string entityName = "Crate";

    private TMP_Text label;
    private float currentHealth;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        health.events.OnHealthChangeEvent.AddListener(v => { currentHealth = v; UpdateLabel(); });
        health.events.OnDieEvent.AddListener(_ => { label.text = $"[DESTROYED] {entityName}"; });
        health.events.OnReviveEvent.AddListener(() => { currentHealth = health.CurrentHealth; UpdateLabel(); });

        currentHealth = health.CurrentHealth;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        label.text = $"{entityName} [{currentHealth:F0}/{health.healthData.Traits.MaxHealth:F0}]";
    }
}
