using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MornEditor
{
    public static class MornEditorUtil
    {
        public struct MornEditorOption
        {
            public bool IsEnabled;
            public bool IsIndent;
            public bool IsBox;
            public string Header;
            public Color? Color;
            public Color? BackgroundColor;
            public bool ChangeColorIfEnabled;
            public bool ChangeBackgroundColorIfEnabled;
        }

        public static void Draw(MornEditorOption option, Action action)
        {
            var cachedEnabled = GUI.enabled;
            var cachedColor = GUI.color;
            var cachedBackgroundColor = GUI.backgroundColor;
            GUI.enabled = option.IsEnabled;
            GUI.color = !option.ChangeColorIfEnabled || option.IsEnabled ? option.Color ?? cachedColor : cachedColor;
            GUI.backgroundColor = !option.ChangeBackgroundColorIfEnabled || option.IsEnabled
                ? option.BackgroundColor ?? cachedBackgroundColor : cachedBackgroundColor;
            if (option.IsBox)
            {
                GUILayout.BeginVertical(GUI.skin.box);
            }

            if (!string.IsNullOrEmpty(option.Header))
            {
                // bold
                var cachedFontStyle = GUI.skin.label.fontStyle;
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUILayout.Label(option.Header);
                GUI.skin.label.fontStyle = cachedFontStyle;
            }

            if (option.IsIndent)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.BeginVertical();
            }

            action?.Invoke();
            if (option.IsIndent)
            {
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

            if (option.IsBox)
            {
                GUILayout.EndVertical();
            }

            GUI.enabled = cachedEnabled;
            GUI.color = cachedColor;
            GUI.backgroundColor = cachedBackgroundColor;
        }

#if UNITY_EDITOR
        internal static bool TryGetBool(string propertyName, SerializedProperty property, out bool value)
        {
            var targetObject = property.serializedObject.targetObject;
            var targetType = targetObject.GetType();
            
            // まずプロパティを探す
            var propertyInfo = targetType.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propertyInfo != null && propertyInfo.GetValue(targetObject) is bool boolPropertyValue)
            {
                value = boolPropertyValue;
                return true;
            }
            
            // プロパティが見つからなければフィールドを探す
            var fieldInfo = targetType.GetField(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null && fieldInfo.GetValue(targetObject) is bool boolFieldValue)
            {
                value = boolFieldValue;
                return true;
            }

            value = false;
            return false;
        }

        public static void OnInspectorGUI(Object target, SerializedObject serializedObject)
        {
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                // 1行目のm_scriptと、そのpingボタン
                using (new GUILayout.HorizontalScope())
                {
                    var cachedEnabled = GUI.enabled;
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(property);
                    GUI.enabled = cachedEnabled;
                    if (GUILayout.Button("Ping", GUILayout.Width(60)))
                    {
                        EditorGUIUtility.PingObject(target);
                    }
                }

                while (property.NextVisible(false))
                {
                    DrawProperty(target, property);
                }

                if (changeCheck.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            var methods = target.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            foreach (var method in methods)
            {
                var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
                if (buttonAttribute != null)
                {
                    if (GUILayout.Button(method.Name))
                    {
                        method.Invoke(target, null);
                    }
                }

                var onInspectorGUIAttribute = method.GetCustomAttribute<OnInspectorGUIAttribute>();
                if (onInspectorGUIAttribute != null)
                {
                    method.Invoke(target, null);
                }
            }
        }

        private static void DrawProperty(Object target, SerializedProperty property)
        {
            var targetType = target.GetType();
            var fieldInfo = targetType.GetField(
                property.name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                var showIfAttribute = fieldInfo.GetCustomAttribute<ShowIfAttribute>();
                var propertyName = showIfAttribute?.PropertyName;
                if (showIfAttribute != null && TryGetBool(propertyName, property, out var show) && !show)
                {
                    return;
                }

                var hideIf = fieldInfo.GetCustomAttribute<HideIfAttribute>();
                propertyName = hideIf?.PropertyName;
                if (hideIf != null && TryGetBool(propertyName, property, out var hide) && hide)
                {
                    return;
                }
            }

            EditorGUILayout.PropertyField(property, true);
        }
#endif
    }
}