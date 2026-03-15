// ────────────────────────────────────────────────────────────────────────────
// HealthConfig.cs  —  Quantum.Simulation  (no UnityEngine references)
//
// Replaces Healthy.HealthData (ScriptableObject) + Healthy.HealthTraits
// (serializable class) merged into one AssetObject.
//
// Default values exactly match HealthTraits() default constructor.
// ────────────────────────────────────────────────────────────────────────────

namespace Quantum
{
    using Photon.Deterministic;

    public partial class HealthConfig : AssetObject
    {
        // ── Health ────────────────────────────────────────────────────────────

        [Header("Health")]

        /// <summary>Maximum health points.  Mirrors HealthTraits.MaxHealth (default 100).</summary>
        public FP MaxHealth = 100;

        /// <summary>Whether health regenerates over time.  Mirrors HealthTraits.ShouldHealthRegen.</summary>
        public bool ShouldHealthRegen = true;

        /// <summary>Health restored per second during regeneration.  Mirrors HealthTraits.HealthRegenRate (default 10).</summary>
        public FP HealthRegenRate = 10;

        /// <summary>Seconds after taking damage before health regen begins.  Mirrors HealthTraits.HealthRegenDelay (default 1).</summary>
        public FP HealthRegenDelay = FP._1;

        // ── Shield ────────────────────────────────────────────────────────────

        [Header("Shield")]

        /// <summary>Whether this entity has a shield pool at all.  Mirrors HealthTraits.HasShields (default true).</summary>
        public bool HasShields = true;

        /// <summary>Maximum shield points.  Mirrors HealthTraits.MaxShield (default 100).</summary>
        public FP MaxShield = 100;

        /// <summary>Whether shield regenerates over time.  Mirrors HealthTraits.ShouldShieldRegen.</summary>
        public bool ShouldShieldRegen = true;

        /// <summary>Shield restored per second during regeneration.  Mirrors HealthTraits.ShieldRegenRate (default 20).</summary>
        public FP ShieldRegenRate = 20;

        /// <summary>Seconds after taking damage before shield regen begins.  Mirrors HealthTraits.ShieldRegenDelay (default 2).</summary>
        public FP ShieldRegenDelay = FP._2;

        /// <summary>
        /// When true, excess damage that depletes the shield carries over to health.
        /// Mirrors HealthTraits.ShieldBleedThrough (default true).
        /// </summary>
        public bool ShieldBleedThrough = true;

        // ── Regeneration order ────────────────────────────────────────────────

        [Header("Regeneration Order")]

        /// <summary>
        /// Controls whether health or shield regenerates first, or both together.
        /// Mirrors HealthTraits.RegenTrigger (default HealthThenShield).
        /// </summary>
        public RegenTrigger RegenTrigger = RegenTrigger.HealthThenShield;
    }
}
