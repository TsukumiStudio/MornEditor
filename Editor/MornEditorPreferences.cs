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

            private void CacheDefines()
            {
#if UNITY_2023_1_OR_NEWER
                var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(_buildTarget);
                _oldDefines = _defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
                _oldDefines = _defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(_buildTarget);
#endif
            }

            private void ApplyDefines()
            {
#if UNITY_2023_1_OR_NEWER
                var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(_buildTarget);
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, _defines);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(_buildTarget, _defines);
#endif
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