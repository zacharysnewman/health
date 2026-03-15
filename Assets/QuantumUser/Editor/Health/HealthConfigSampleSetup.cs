// ────────────────────────────────────────────────────────────────────────────
// HealthConfigSampleSetup.cs  —  Quantum.Unity.Editor  (Editor only)
//
// SAMPLE / OPTIONAL — safe to delete once you have your own HealthConfig asset.
//
// Creates a pre-configured HealthConfig asset the first time Unity compiles
// after importing this package, then self-deletes so it does not run again.
//
// The created asset is placed at:
//   Assets/QuantumUser/Health/Sample/SampleHealthConfig.asset
//
// Default values match the original Healthy.HealthTraits() constructor:
//   MaxHealth         = 100
//   MaxShield         = 100
//   HealthRegenRate   = 10
//   ShieldRegenRate   = 20
//   HealthRegenDelay  = 1
//   ShieldRegenDelay  = 2
//   ShieldBleedThrough = true
//   HasShields        = true
//   ShouldHealthRegen = true
//   ShouldShieldRegen = true
//   RegenTrigger      = HealthThenShield
// ────────────────────────────────────────────────────────────────────────────

#if HEALTH_QUANTUM && UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Quantum.Editor
{
    [InitializeOnLoad]
    public static class HealthConfigSampleSetup
    {
        private const string AssetPath    = "Assets/QuantumUser/Health/Sample/SampleHealthConfig.asset";
        private const string ScriptPath   = "Assets/QuantumUser/Editor/Health/HealthConfigSampleSetup.cs";
        private const string SampleFolder = "Assets/QuantumUser/Health/Sample";

        static HealthConfigSampleSetup()
        {
            // Guard: only run once (when the asset does not yet exist).
            if (AssetDatabase.LoadAssetAtPath<HealthConfig>(AssetPath) != null)
                return;

            // Ensure the sample directory exists.
            if (!AssetDatabase.IsValidFolder(SampleFolder))
            {
                AssetDatabase.CreateFolder("Assets/QuantumUser/Health", "Sample");
            }

            // Create the asset with default field values.
            // HealthConfig fields default to the correct values in the class
            // declaration, so CreateInstance alone is sufficient.
            var asset = ScriptableObject.CreateInstance<HealthConfig>();

            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[HealthConfigSampleSetup] Created SampleHealthConfig.asset at " + AssetPath
                    + "\nAfter running Quantum asset baking (Quantum > Bake Assets), assign this"
                    + " asset to the Config field on your entity's HealthComponent prototype.");

            // Self-delete this script so it does not run on future recompiles.
            AssetDatabase.DeleteAsset(ScriptPath);
        }
    }
}
#endif
