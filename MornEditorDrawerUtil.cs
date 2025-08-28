using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace MornEditor
{
    /// <summary>Drawer共通処理のユーティリティクラス</summary>
    public static class MornEditorDrawerUtil
    {
#if UNITY_EDITOR
        private static readonly Dictionary<string, ReorderableList> s_reorderableLists = new();

        /// <summary>無効化された状態でプロパティを描画</summary>
        public static void DrawDisabledProperty(Rect position, SerializedProperty property, GUIContent label, bool includeChildren = true)
        {
            // 現在のGUI.enabledの状態を保存
            var cachedEnabled = GUI.enabled;
            
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                DrawDisabledArrayProperty(position, property, label, cachedEnabled);
            }
            else
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label, includeChildren);
                GUI.enabled = cachedEnabled;
            }
        }

        /// <summary>無効化された配列プロパティを描画（+-ボタンなし）</summary>
        private static void DrawDisabledArrayProperty(Rect position, SerializedProperty property, GUIContent label, bool cachedEnabled)
        {
            var key = $"disabled_{property.propertyPath}";
            
            if (s_reorderableLists.TryGetValue(key, out var list))
            {
                // SerializedObjectがDisposedされていないかチェック
                try
                {
                    // serializedObjectにアクセスして、Disposedされていないか確認
                    var _ = list.serializedProperty.serializedObject.targetObject;
                }
                catch
                {
                    // Disposedされていた場合はキャッシュから削除して再作成
                    s_reorderableLists.Remove(key);
                    list = null;
                }
            }
            
            if (list == null)
            {
                list = new ReorderableList(property.serializedObject, property, false, true, false, false)
                {
                    drawHeaderCallback = rect => EditorGUI.LabelField(rect, label),
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var element = property.GetArrayElementAtIndex(index);
                        rect.y += 2;
                        GUI.enabled = false;
                        EditorGUI.PropertyField(rect, element, GUIContent.none, true);
                        GUI.enabled = cachedEnabled;
                    },
                    elementHeightCallback = index =>
                    {
                        var element = property.GetArrayElementAtIndex(index);
                        return EditorGUI.GetPropertyHeight(element, GUIContent.none, true) + 4;
                    }
                };
                s_reorderableLists[key] = list;
            }
            
            list.DoList(position);
        }

        /// <summary>無効化された状態でのプロパティの高さを取得</summary>
        public static float GetDisabledPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                var key = $"disabled_{property.propertyPath}";
                
                if (s_reorderableLists.TryGetValue(key, out var list))
                {
                    // SerializedObjectがDisposedされていないかチェック
                    try
                    {
                        // serializedObjectにアクセスして、Disposedされていないか確認
                        var _ = list.serializedProperty.serializedObject.targetObject;
                        return list.GetHeight();
                    }
                    catch
                    {
                        // Disposedされていた場合はキャッシュから削除
                        s_reorderableLists.Remove(key);
                    }
                }
            }
            
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        
        /// <summary>無効化された状態でプロパティを描画（レイアウト版）</summary>
        public static void DrawDisabledPropertyLayout(SerializedProperty property, GUIContent label = null, bool includeChildren = true)
        {
            label = label ?? new GUIContent(property.displayName);
            var height = GetDisabledPropertyHeight(property, label);
            var rect = EditorGUILayout.GetControlRect(true, height);
            DrawDisabledProperty(rect, property, label, includeChildren);
        }
#endif
    }
}