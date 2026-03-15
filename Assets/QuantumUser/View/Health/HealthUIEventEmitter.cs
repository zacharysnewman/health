// ────────────────────────────────────────────────────────────────────────────
// HealthUIEventEmitter.cs  —  Quantum.Unity  (View assembly)
//
// Bridges all Quantum health simulation events to UnityEvent<T> fields that
// can be wired directly in the Unity inspector — no code required for basic
// UI hookup.
//
// Usage
// ─────
// 1. Add this component to the same GameObject as your QuantumEntityView
//    (or any child).
// 2. In the inspector, wire the events you care about:
//      • OnHealthChangedNormalized → Image.fillAmount  (health bar)
//      • OnShieldChangedNormalized → Image.fillAmount  (shield bar)
//      • OnHealthChangedText       → TMP_Text.SetText  (health label)
//      • OnDied                    → Animator.SetTrigger / GameObject.SetActive
//      …etc.
//
// Normalized values (0–1) are computed from the CurrentHealth/MaxHealth payload
// carried by the simulation event, so no additional simulation data is needed.
//
// This component is the Quantum equivalent of the original SetImageFill.cs /
// SetText.cs sample helpers, generalised to cover the full event surface.
// ────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.Events;

namespace Quantum
{
    public class HealthUIEventEmitter : QuantumEntityViewComponent
    {
        // ── Health value events ───────────────────────────────────────────────

        [Header("Health — Value")]
        [Tooltip("Fires with absolute CurrentHealth whenever health changes.")]
        public UnityEvent<float> OnHealthChanged = new UnityEvent<float>();

        [Tooltip("Fires with normalized health (0–1) whenever health changes. Wire to Image.fillAmount.")]
        public UnityEvent<float> OnHealthChangedNormalized = new UnityEvent<float>();

        [Tooltip("Fires with health as a rounded integer string. Wire to TMP_Text.SetText.")]
        public UnityEvent<string> OnHealthChangedText = new UnityEvent<string>();

        // ── Shield value events ───────────────────────────────────────────────

        [Header("Shield — Value")]
        [Tooltip("Fires with absolute CurrentShield whenever shield changes.")]
        public UnityEvent<float> OnShieldChanged = new UnityEvent<float>();

        [Tooltip("Fires with normalized shield (0–1) whenever shield changes. Wire to Image.fillAmount.")]
        public UnityEvent<float> OnShieldChangedNormalized = new UnityEvent<float>();

        [Tooltip("Fires with shield as a rounded integer string. Wire to TMP_Text.SetText.")]
        public UnityEvent<string> OnShieldChangedText = new UnityEvent<string>();

        // ── Health status events ──────────────────────────────────────────────

        [Header("Health — Status")]
        [Tooltip("Fires (confirmed) when the entity dies. Wire to death VFX / animator.")]
        public UnityEvent OnDied = new UnityEvent();

        [Tooltip("Fires (confirmed) when the entity is revived.")]
        public UnityEvent OnRevived = new UnityEvent();

        [Tooltip("Fires with damage amount when non-lethal damage is dealt.")]
        public UnityEvent<float> OnDamaged = new UnityEvent<float>();

        [Tooltip("Fires with damage amount when damage is dealt to an already-dead entity.")]
        public UnityEvent<float> OnOverkill = new UnityEvent<float>();

        [Tooltip("Fires with heal amount when health is healed.")]
        public UnityEvent<float> OnHealed = new UnityEvent<float>();

        [Tooltip("Fires when health reaches MaxHealth (from heal or regen completion).")]
        public UnityEvent OnMaxHealthReached = new UnityEvent();

        [Tooltip("Fires with the excess amount when a heal would exceed MaxHealth.")]
        public UnityEvent<float> OnOverhealed = new UnityEvent<float>();

        [Tooltip("Fires when health regeneration begins (after any delay).")]
        public UnityEvent OnHealthRegenStarted = new UnityEvent();

        // ── Shield status events ──────────────────────────────────────────────

        [Header("Shield — Status")]
        [Tooltip("Fires with charge amount when shield is charged.")]
        public UnityEvent<float> OnShieldCharged = new UnityEvent<float>();

        [Tooltip("Fires when shield reaches MaxShield (from charge or regen completion).")]
        public UnityEvent OnMaxShieldReached = new UnityEvent();

        [Tooltip("Fires with the excess amount when a charge would exceed MaxShield.")]
        public UnityEvent<float> OnShieldOvercharged = new UnityEvent<float>();

        [Tooltip("Fires when shield regeneration begins (after any delay).")]
        public UnityEvent OnShieldRegenStarted = new UnityEvent();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnActivate(Frame frame)
        {
            QuantumEvent.Subscribe<EventHealthChanged>     (this, HandleHealthChanged);
            QuantumEvent.Subscribe<EventShieldChanged>     (this, HandleShieldChanged);
            QuantumEvent.Subscribe<EventHealthDied>        (this, HandleDied);
            QuantumEvent.Subscribe<EventHealthRevived>     (this, HandleRevived);
            QuantumEvent.Subscribe<EventHealthDamaged>     (this, HandleDamaged);
            QuantumEvent.Subscribe<EventHealthOverkill>    (this, HandleOverkill);
            QuantumEvent.Subscribe<EventHealthHealed>      (this, HandleHealed);
            QuantumEvent.Subscribe<EventHealthMaxReached>  (this, HandleMaxHealthReached);
            QuantumEvent.Subscribe<EventHealthOverhealed>  (this, HandleOverhealed);
            QuantumEvent.Subscribe<EventHealthRegenStarted>(this, HandleHealthRegenStarted);
            QuantumEvent.Subscribe<EventShieldCharged>     (this, HandleShieldCharged);
            QuantumEvent.Subscribe<EventShieldMaxReached>  (this, HandleMaxShieldReached);
            QuantumEvent.Subscribe<EventShieldOvercharged> (this, HandleShieldOvercharged);
            QuantumEvent.Subscribe<EventShieldRegenStarted>(this, HandleShieldRegenStarted);
        }

        public override void OnDeactivate()
        {
            QuantumEvent.UnsubscribeListener(this);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandleHealthChanged(EventHealthChanged e)
        {
            if (e.Entity != EntityRef) return;
            float abs  = e.CurrentHealth.AsFloat;
            float norm = e.MaxHealth.AsFloat > 0f ? abs / e.MaxHealth.AsFloat : 0f;
            OnHealthChanged.Invoke(abs);
            OnHealthChangedNormalized.Invoke(norm);
            OnHealthChangedText.Invoke(Mathf.RoundToInt(abs).ToString());
        }

        private void HandleShieldChanged(EventShieldChanged e)
        {
            if (e.Entity != EntityRef) return;
            float abs  = e.CurrentShield.AsFloat;
            float norm = e.MaxShield.AsFloat > 0f ? abs / e.MaxShield.AsFloat : 0f;
            OnShieldChanged.Invoke(abs);
            OnShieldChangedNormalized.Invoke(norm);
            OnShieldChangedText.Invoke(Mathf.RoundToInt(abs).ToString());
        }

        private void HandleDied(EventHealthDied e)
        {
            if (e.Entity != EntityRef) return;
            OnDied.Invoke();
        }

        private void HandleRevived(EventHealthRevived e)
        {
            if (e.Entity != EntityRef) return;
            OnRevived.Invoke();
        }

        private void HandleDamaged(EventHealthDamaged e)
        {
            if (e.Entity != EntityRef) return;
            OnDamaged.Invoke(e.Amount.AsFloat);
        }

        private void HandleOverkill(EventHealthOverkill e)
        {
            if (e.Entity != EntityRef) return;
            OnOverkill.Invoke(e.Amount.AsFloat);
        }

        private void HandleHealed(EventHealthHealed e)
        {
            if (e.Entity != EntityRef) return;
            OnHealed.Invoke(e.Amount.AsFloat);
        }

        private void HandleMaxHealthReached(EventHealthMaxReached e)
        {
            if (e.Entity != EntityRef) return;
            OnMaxHealthReached.Invoke();
        }

        private void HandleOverhealed(EventHealthOverhealed e)
        {
            if (e.Entity != EntityRef) return;
            OnOverhealed.Invoke(e.Overage.AsFloat);
        }

        private void HandleHealthRegenStarted(EventHealthRegenStarted e)
        {
            if (e.Entity != EntityRef) return;
            OnHealthRegenStarted.Invoke();
        }

        private void HandleShieldCharged(EventShieldCharged e)
        {
            if (e.Entity != EntityRef) return;
            OnShieldCharged.Invoke(e.Amount.AsFloat);
        }

        private void HandleMaxShieldReached(EventShieldMaxReached e)
        {
            if (e.Entity != EntityRef) return;
            OnMaxShieldReached.Invoke();
        }

        private void HandleShieldOvercharged(EventShieldOvercharged e)
        {
            if (e.Entity != EntityRef) return;
            OnShieldOvercharged.Invoke(e.Overage.AsFloat);
        }

        private void HandleShieldRegenStarted(EventShieldRegenStarted e)
        {
            if (e.Entity != EntityRef) return;
            OnShieldRegenStarted.Invoke();
        }
    }
}
