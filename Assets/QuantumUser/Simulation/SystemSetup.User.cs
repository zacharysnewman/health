#if HEALTH_QUANTUM
// ────────────────────────────────────────────────────────────────────────────
// SystemSetup.User.cs  —  Quantum.Simulation
//
// Partial class hook that registers user systems with the Quantum runner.
// MUST remain at Assets/QuantumUser/Simulation/ (root — no subfolder).
//
// To add more systems from other features, append additional systems.Add(...)
// calls inside AddSystemsUser.  Order matters: init systems must precede their
// corresponding main systems.
// ────────────────────────────────────────────────────────────────────────────

namespace Quantum
{
    using System.Collections.Generic;

    public static partial class DeterministicSystemSetup
    {
        static partial void AddSystemsUser(
            ICollection<SystemBase> systems,
            RuntimeConfig            gameConfig,
            SimulationConfig         simulationConfig,
            SystemsConfig            systemsConfig)
        {
            // ── Health ────────────────────────────────────────────────────────
            // InitSystem must run before the main System so that component fields
            // are set before the first Update tick processes them.
            systems.Add(new HealthInitSystem());
            systems.Add(new HealthSystem());
        }
    }
}

#endif
