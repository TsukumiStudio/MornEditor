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
#if UNITY_EDITOR
        internal static bool TryGetBool(string propertyName, SerializedProperty property, out bool value)
        {
            // ネストされたプロパティの実際の値を取得するために、プロパティパスを辿る
            var actualObject = GetActualObjectForProperty(property);
            if (actualObject == null)
            {
                value = false;
                return false;
            }
            
            var targetType = actualObject.GetType();
            
            // まずプロパティを探す
            var propertyInfo = targetType.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propertyInfo != null && propertyInfo.GetValue(actualObject) is bool boolPropertyValue)
            {
                value = boolPropertyValue;
                return true;
            }
            
            // プロパティが見つからなければフィールドを探す
            var fieldInfo = targetType.GetField(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null && fieldInfo.GetValue(actualObject) is bool boolFieldValue)
            {
                value = boolFieldValue;
                return true;
            }

            value = false;
            return false;
        }
        
        private static object GetActualObjectForProperty(SerializedProperty property)
        {
            var path = property.propertyPath;
            var targetObject = property.serializedObject.targetObject;
            
            // パスを'.'で分割して、各階層を辿る
            var pathParts = path.Split('.');
            object currentObject = targetObject;
            
            // 最後の要素は現在のプロパティ自体なので、その一つ前まで辿る
            for (var i = 0; i < pathParts.Length - 1; i++)
            {
                var part = pathParts[i];
                
                // Array要素の場合の処理
                if (part == "Array" && i + 1 < pathParts.Length && pathParts[i + 1].StartsWith("data["))
                {
                    // Arrayの場合は次のdata[n]も含めて処理
                    i++; // data[n]をスキップ
                    continue;
                }
                
                // 現在のオブジェクトから次のフィールドを取得
                var type = currentObject.GetType();
                var field = type.GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (field == null)
                {
                    return null;
                }
                
                currentObject = field.GetValue(currentObject);
                if (currentObject == null)
                {
                    return null;
                }
            }
            
            return currentObject;
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
                    var buttonName = string.IsNullOrEmpty(buttonAttribute.Name) ? method.Name : buttonAttribute.Name;
                    if (GUILayout.Button(buttonName))
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
                if (showIfAttribute != null)
                {
                    var shouldShow = true;
                    foreach (var propertyName in showIfAttribute.PropertyNames)
                    {
                        if (!TryGetBool(propertyName, property, out var show) || !show)
                        {
                            shouldShow = false;
                            break;
                        }
                    }
                    if (!shouldShow)
                    {
                        return;
                    }
                }

                var hideIf = fieldInfo.GetCustomAttribute<HideIfAttribute>();
                if (hideIf != null)
                {
                    foreach (var propertyName in hideIf.PropertyNames)
                    {
                        if (TryGetBool(propertyName, property, out var hide) && hide)
                        {
                            return;
                        }
                    }
                }
            }

            var previousEnabled = GUI.enabled;
            if (fieldInfo != null)
            {
                var enableIfAttribute = fieldInfo.GetCustomAttribute<EnableIfAttribute>();
                if (enableIfAttribute != null)
                {
                    var shouldEnable = true;
                    foreach (var propertyName in enableIfAttribute.PropertyNames)
                    {
                        if (!TryGetBool(propertyName, property, out var enable) || !enable)
                        {
                            shouldEnable = false;
                            break;
                        }
                    }
                    GUI.enabled = shouldEnable;
                }

                var disableIfAttribute = fieldInfo.GetCustomAttribute<DisableIfAttribute>();
                if (disableIfAttribute != null)
                {
                    foreach (var propertyName in disableIfAttribute.PropertyNames)
                    {
                        if (TryGetBool(propertyName, property, out var disable) && disable)
                        {
                            GUI.enabled = false;
                            break;
                        }
                    }
                }
            }

            EditorGUILayout.PropertyField(property, true);
            
            GUI.enabled = previousEnabled;
        }
#endif
    }
}