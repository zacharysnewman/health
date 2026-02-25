using UnityEditor;
using UnityEngine;

namespace Healthy
{
    [CustomEditor(typeof(HealthData))]
    public class HealthDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty traits = serializedObject.FindProperty("traits");

            SerializedProperty maxHealth = traits.FindPropertyRelative("maxHealth");
            SerializedProperty shouldHealthRegen = traits.FindPropertyRelative("shouldHealthRegen");
            SerializedProperty healthRegenRate = traits.FindPropertyRelative("healthRegenRate");
            SerializedProperty healthRegenDelay = traits.FindPropertyRelative("healthRegenDelay");

            SerializedProperty hasShields = traits.FindPropertyRelative("hasShields");
            SerializedProperty maxShield = traits.FindPropertyRelative("maxShield");
            SerializedProperty shieldBleedThrough = traits.FindPropertyRelative("shieldBleedThrough");
            SerializedProperty shouldShieldRegen = traits.FindPropertyRelative("shouldShieldRegen");
            SerializedProperty shieldRegenRate = traits.FindPropertyRelative("shieldRegenRate");
            SerializedProperty shieldRegenDelay = traits.FindPropertyRelative("shieldRegenDelay");

            SerializedProperty regenTrigger = traits.FindPropertyRelative("regenTrigger");

            // Health
            EditorGUILayout.LabelField("Health", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(maxHealth);
            EditorGUILayout.PropertyField(shouldHealthRegen);
            EditorGUI.BeginDisabledGroup(!shouldHealthRegen.boolValue);
            EditorGUILayout.PropertyField(healthRegenRate);
            EditorGUILayout.PropertyField(healthRegenDelay);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            // Shield
            EditorGUILayout.LabelField("Shield", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(hasShields);
            EditorGUI.BeginDisabledGroup(!hasShields.boolValue);
            EditorGUILayout.PropertyField(maxShield);
            EditorGUILayout.PropertyField(shieldBleedThrough);
            EditorGUILayout.PropertyField(shouldShieldRegen);
            EditorGUI.BeginDisabledGroup(!shouldShieldRegen.boolValue);
            EditorGUILayout.PropertyField(shieldRegenRate);
            EditorGUILayout.PropertyField(shieldRegenDelay);
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            // Regen order — only relevant when both regen systems are active
            EditorGUILayout.LabelField("Regen Order", EditorStyles.boldLabel);
            bool bothRegen = shouldHealthRegen.boolValue && hasShields.boolValue && shouldShieldRegen.boolValue;
            EditorGUI.BeginDisabledGroup(!bothRegen);
            EditorGUILayout.PropertyField(regenTrigger);
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
