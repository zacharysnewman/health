using TMPro;
using UnityEngine;
using Healthy;

[RequireComponent(typeof(TMP_Text))]
public class PlayerHealthLabel : MonoBehaviour
{
    [SerializeField] private Health health;

    private TMP_Text label;
    private float currentHealth;
    private float currentShield;
    private string status = "";

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        health.events.OnHealthChangeEvent.AddListener(v => { currentHealth = v; UpdateLabel(); });
        health.events.OnShieldChangeEvent.AddListener(v => { currentShield = v; UpdateLabel(); });
        health.events.OnDieEvent.AddListener(_ => { status = "DEAD"; UpdateLabel(); });
        health.events.OnReviveEvent.AddListener(() => { status = ""; UpdateLabel(); });
        health.events.OnOverhealEvent.AddListener(_ => { status = "OVERHEALED"; UpdateLabel(); });
        health.events.OnDamageEvent.AddListener(_ => { status = ""; UpdateLabel(); });
        health.events.OnRegenHealthStartEvent.AddListener(() => { status = "REGENERATING"; UpdateLabel(); });
        health.events.OnRegenShieldStartEvent.AddListener(() => { status = "REGENERATING"; UpdateLabel(); });
        health.events.OnMaxHealthEvent.AddListener(() => { if (status == "REGENERATING") status = ""; UpdateLabel(); });
        health.events.OnMaxShieldEvent.AddListener(() => { if (status == "REGENERATING") status = ""; UpdateLabel(); });

        currentHealth = health.CurrentHealth;
        currentShield = health.CurrentShield;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        string statusPrefix = status.Length > 0 ? $"[{status}]\n" : "";
        label.text = $"{statusPrefix}HP: {currentHealth:F0} | SH: {currentShield:F0}";
    }
}
