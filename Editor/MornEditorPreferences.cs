using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    /// <summary>
    /// MornEditor ProjectSettings設定
    /// </summary>
    internal static class MornEditorPreferences
    {
        private const string DEFINE_USE_MORNEDITOR_INSPECTOR = "USE_MORNEDITOR_INSPECTOR";

        [SettingsProvider]
        public static SettingsProvider CreateMornEditorSettingsProvider()
        {
            return new MornEditorSettingsProvider("Project/Morn Editor", SettingsScope.Project);
        }

        private class MornEditorSettingsProvider : SettingsProvider
        {
            private string _defines;
            private string _oldDefines;
            private bool _unappliedChanges;
            private BuildTargetGroup _buildTarget;

            public MornEditorSettingsProvider(string path, SettingsScope scope) : base(path, scope)
            {
                keywords = new HashSet<string>(
                    new[]
                    {
                        "Morn",
                        "Editor",
                        "Inspector",
                        DEFINE_USE_MORNEDITOR_INSPECTOR
                    });
            }

            public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
            {
                _buildTarget = BuildTargetGroup.Unknown;
            }

            private string GetDefines(BuildTargetGroup group)
            {
#if UNITY_2023_1_OR_NEWER
                var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                return PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
                return PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif
            }

            private void SetDefines(BuildTargetGroup group, string defines)
            {
#if UNITY_2023_1_OR_NEWER
                var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
#endif
            }

            private void CacheDefines()
            {
                _oldDefines = _defines = GetDefines(_buildTarget);
            }

            private void ApplyDefines()
            {
                SetDefines(_buildTarget, _defines);
                CacheDefines();
            }

            private bool HasDefine(string define)
            {
                return _defines.IndexOf(define) >= 0;
            }

            private void AddDefine(string define)
            {
                _defines = (_defines + ";" + define + ";").Replace(";;", ";");
            }

            private void RemoveDefine(string define)
            {
                _defines = _defines.Replace(define, "").Replace(";;", ";");
            }

            private bool HasDefineChanged(string define)
            {
                bool current = HasDefine(define);
                bool old = _oldDefines.IndexOf(define) >= 0;
                return current != old;
            }

            private static string AddDefineToString(string defines, string define)
            {
                if (defines.IndexOf(define) >= 0) return defines;
                return (defines + ";" + define + ";").Replace(";;", ";");
            }

            private static string RemoveDefineFromString(string defines, string define)
            {
                return defines.Replace(define, "").Replace(";;", ";");
            }

            private void ApplyToAllPlatforms(string define, bool enable)
            {
                foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
                {
                    if (group == BuildTargetGroup.Unknown) continue;
                    if (IsObsolete(group)) continue;

                    try
                    {
                        var current = GetDefines(group);
                        var updated = enable
                            ? AddDefineToString(current, define)
                            : RemoveDefineFromString(current, define);
                        if (current != updated)
                        {
                            SetDefines(group, updated);
                        }
                    }
                    catch
                    {
                        // サポートされていないプラットフォームはスキップ
                    }
                }

                CacheDefines();
            }

            private static bool IsObsolete(BuildTargetGroup group)
            {
                var field = typeof(BuildTargetGroup).GetField(group.ToString());
                if (field == null) return true;
                return Attribute.IsDefined(field, typeof(ObsoleteAttribute));
            }

            public override void OnTitleBarGUI()
            {
                if (_unappliedChanges)
                {
                    GUI.color = Color.green;
                    if (GUILayout.Button("Apply Changes"))
                    {
                        ApplyDefines();
                        _unappliedChanges = false;
                    }

                    GUI.color = Color.white;
                }
            }

            public override void OnGUI(string searchContext)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Scripting Define Symbols", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    $"{DEFINE_USE_MORNEDITOR_INSPECTOR} を有効にすると、MornEditorの強化されたインスペクター機能が有効になります。\n"
                    + "変更を適用するには、「Apply Changes」ボタンをクリックしてください。",
                    MessageType.Info);
                EditorGUILayout.Space();

                // 全プラットフォーム一括ボタン
                EditorGUILayout.LabelField("全プラットフォーム一括", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("全プラットフォームに有効化"))
                    {
                        ApplyToAllPlatforms(DEFINE_USE_MORNEDITOR_INSPECTOR, true);
                    }
                    if (GUILayout.Button("全プラットフォームから無効化"))
                    {
                        ApplyToAllPlatforms(DEFINE_USE_MORNEDITOR_INSPECTOR, false);
                    }
                }
                EditorGUILayout.Space();

                var activeBuildTarget = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                EditorGUILayout.LabelField($"現在のアクティブなプラットフォーム: {activeBuildTarget}", EditorStyles.helpBox);
                EditorGUILayout.Space();
                bool isActivePlatform = activeBuildTarget == _buildTarget;
                if (isActivePlatform)
                {
                    GUI.color = new Color(1.1f, 1.1f, 1.1f, 1f);
                }

                BuildTargetGroup group = EditorGUILayout.BeginBuildTargetSelectionGrouping();
                if (_buildTarget != group)
                {
                    _buildTarget = group;
                    CacheDefines();
                }

                _unappliedChanges = ShowDefineToggle(DEFINE_USE_MORNEDITOR_INSPECTOR, DEFINE_USE_MORNEDITOR_INSPECTOR);
                EditorGUILayout.EndBuildTargetSelectionGrouping();
                GUI.color = Color.white;
                EditorGUILayout.Space();
            }

            private bool ShowDefineToggle(string label, string define)
            {
                bool enabled = HasDefine(define);
                bool changed = HasDefineChanged(define);
                var displayLabel = label;
                if (changed)
                {
                    displayLabel += " *";
                }

                bool newState = GUILayout.Toggle(enabled, displayLabel);
                if (newState != enabled)
                {
                    if (newState)
                    {
                        AddDefine(define);
                    }
                    else
                    {
                        RemoveDefine(define);
                    }
                }

                return HasDefineChanged(define);
            }
        }
    }
}
