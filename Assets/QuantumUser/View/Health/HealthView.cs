// ────────────────────────────────────────────────────────────────────────────
// HealthView.cs  —  Quantum.Unity  (View assembly)
//
// Entity view component for the Quantum health system.
//
// Responsibilities:
//   • Polls VerifiedFrame each view tick to keep health/shield bars up to date
//     (continuous display — mirrors PlayerHealthLabel.cs logic).
//   • Subscribes to synced events (HealthDied, HealthRevived) for confirmed
//     reactions such as death animation triggers.
//
// For inspector-wirable UnityEvent bindings (including normalized values),
// add HealthUIEventEmitter to the same GameObject instead of or alongside
// this component.
// ────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Quantum
{
    public class HealthView : QuantumEntityViewComponent
    {
        // ── Inspector references ──────────────────────────────────────────────

        [Header("Health")]
        [SerializeField] private TMP_Text  _healthText;
        [SerializeField] private Image     _healthBarFill;

        [Header("Shield")]
        [SerializeField] private TMP_Text  _shieldText;
        [SerializeField] private Image     _shieldBarFill;

        [Header("Status")]
        [Tooltip("Activated when the entity is dead, deactivated when alive.")]
        [SerializeField] private GameObject _deadIndicator;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnActivate(Frame frame)
        {
            // Subscribe to confirmed (synced) events only.
            // Speculative events are handled by HealthUIEventEmitter.
            QuantumEvent.Subscribe<EventHealthDied>   (this, OnHealthDied);
            QuantumEvent.Subscribe<EventHealthRevived>(this, OnHealthRevived);
        }

        public override void OnDeactivate()
        {
            QuantumEvent.UnsubscribeListener(this);
        }

        // ── Continuous polling (replaces Update-driven label refresh) ─────────

        public override void OnUpdateView()
        {
            var frame = VerifiedFrame;
            if (!frame.TryGet<HealthComponent>(EntityRef, out var h)) return;

            var config   = frame.FindAsset(h.Config);
            FP maxHealth  = config.MaxHealth + h.MaxHealthBonus;

            float healthNorm = maxHealth.AsFloat > 0f
                ? h.CurrentHealth.AsFloat / maxHealth.AsFloat
                : 0f;

            float shieldNorm = config.MaxShield.AsFloat > 0f
                ? h.CurrentShield.AsFloat / config.MaxShield.AsFloat
                : 0f;

            if (_healthText    != null) _healthText.text       = Mathf.RoundToInt(h.CurrentHealth.AsFloat).ToString();
            if (_healthBarFill != null) _healthBarFill.fillAmount = healthNorm;
            if (_shieldText    != null) _shieldText.text       = Mathf.RoundToInt(h.CurrentShield.AsFloat).ToString();
            if (_shieldBarFill != null) _shieldBarFill.fillAmount = shieldNorm;
            if (_deadIndicator != null) _deadIndicator.SetActive(h.IsDead);
        }

        // ── Confirmed event handlers ──────────────────────────────────────────

        /// <summary>
        /// Called on a confirmed death frame.  Override or extend to trigger
        /// death VFX, animation state, audio, etc.
        /// </summary>
        protected virtual void OnHealthDied(EventHealthDied e)
        {
            if (e.Entity != EntityRef) return;
            // Override in a subclass to add death reactions.
        }

        /// <summary>
        /// Called on a confirmed revival frame.  Override or extend to trigger
        /// revive VFX, animation state, audio, etc.
        /// </summary>
        protected virtual void OnHealthRevived(EventHealthRevived e)
        {
            if (e.Entity != EntityRef) return;
            // Override in a subclass to add revival reactions.
        }
    }
}
