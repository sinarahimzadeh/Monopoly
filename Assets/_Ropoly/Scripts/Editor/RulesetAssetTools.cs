using Ropoly.Infrastructure.Content;
using UnityEditor;
using UnityEngine;

namespace Ropoly.Editor
{
    public static class RulesetAssetTools
    {
        public const string DefaultRulesetPath =
            "Assets/_Ropoly/Content/Rulesets/ClassicRopoly.asset";

        [MenuItem("Ropoly/Content/Create Missing Classic Ruleset")]
        public static void CreateDefaultRuleset()
        {
            GameRulesetDefinition existing =
                AssetDatabase.LoadAssetAtPath<GameRulesetDefinition>(DefaultRulesetPath);
            if (existing != null)
            {
                Debug.Log($"Classic Ropoly ruleset already exists at '{DefaultRulesetPath}'.", existing);
                return;
            }

            GameRulesetDefinition ruleset = ScriptableObject.CreateInstance<GameRulesetDefinition>();
            if (!ruleset.Validate().IsValid)
            {
                Object.DestroyImmediate(ruleset);
                throw new System.InvalidOperationException("Default Classic Ropoly ruleset is invalid.");
            }

            AssetDatabase.CreateAsset(ruleset, DefaultRulesetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!UnityEngine.Application.isBatchMode)
            {
                Selection.activeObject = ruleset;
            }

            Debug.Log($"Created Classic Ropoly ruleset at '{DefaultRulesetPath}'.", ruleset);
        }
    }
}
