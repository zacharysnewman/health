namespace Healthy.Networking
{
    /// <summary>
    /// Optional abstraction for networking adapters that replicate Health state.
    ///
    /// Implement this interface on your networking adapter to provide a unified API
    /// for setting authoritative health values from the network layer without
    /// touching core Health logic.
    ///
    /// This interface is intentionally minimal. Adapters own the replication strategy;
    /// the interface just standardizes how replicated values are written back to the
    /// local Health component.
    /// </summary>
    public interface IHealthReplicator
    {
        void SetHealth(float value);
        void SetShield(float value);
    }
}
