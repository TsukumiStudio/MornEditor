using System;
using System.Reflection;
using UnityEngine;
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
            var propertyInfo = targetObject.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propertyInfo != null && propertyInfo.GetValue(targetObject) is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            value = false;
            return false;
        }
#endif
    }
}