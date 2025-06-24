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

            // 配列/リストプロパティの場合、ドラッグ&ドロップを処理
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                DrawArrayPropertyWithDragAndDrop(property);
            }
            else
            {
                EditorGUILayout.PropertyField(property, true);
            }
            
            GUI.enabled = previousEnabled;
        }

        private static void DrawArrayPropertyWithDragAndDrop(SerializedProperty property)
        {
            // BeginPropertyを使用してPropertyFieldの描画位置を正確に取得
            var rect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(property, true));
            
            // ヘッダー部分のRectを計算（プロパティの最初の行）
            var headerRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            
            // ドラッグ&ドロップの処理を先に行う（イベントが消費される前に）
            if (property.arraySize >= 0 || property.arraySize == -1) // 空の配列も受け付ける
            {
                HandleArrayDragAndDrop(property, headerRect);
            }
            
            // 通常のPropertyFieldを描画
            EditorGUI.PropertyField(rect, property, true);
        }

        private static void HandleArrayDragAndDrop(SerializedProperty arrayProperty, Rect dropArea)
        {
            var evt = Event.current;
            
            // ドラッグオーバー時の処理
            if (dropArea.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.DragUpdated)
                {
                    // ドラッグされているオブジェクトが適切な型かチェック
                    if (IsValidDraggedObjects(arrayProperty))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        evt.Use();
                    }
                }
                else if (evt.type == EventType.DragPerform)
                {
                    // ドロップ時の処理
                    if (IsValidDraggedObjects(arrayProperty))
                    {
                        DragAndDrop.AcceptDrag();
                        
                        // 配列要素の型を取得
                        var elementType = GetArrayElementType(arrayProperty);
                        
                        // ドラッグされたオブジェクトを配列に追加
                        foreach (var draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject != null)
                            {
                                var objectToAdd = draggedObject;
                                
                                // GameObject がドラッグされていて、Component型の配列の場合
                                if (elementType != null && 
                                    typeof(Component).IsAssignableFrom(elementType) && 
                                    draggedObject is GameObject gameObject)
                                {
                                    objectToAdd = gameObject.GetComponent(elementType);
                                    if (objectToAdd == null)
                                    {
                                        continue;
                                    }
                                }
                                
                                var index = arrayProperty.arraySize;
                                arrayProperty.InsertArrayElementAtIndex(index);
                                var element = arrayProperty.GetArrayElementAtIndex(index);
                                element.objectReferenceValue = objectToAdd;
                            }
                        }
                        
                        arrayProperty.serializedObject.ApplyModifiedProperties();
                        evt.Use();
                    }
                }
            }
        }

        private static bool IsValidDraggedObjects(SerializedProperty arrayProperty)
        {
            if (DragAndDrop.objectReferences.Length == 0)
            {
                return false;
            }
            
            // 配列要素の型を取得
            var elementType = GetArrayElementType(arrayProperty);
            if (elementType == null)
            {
                // 型情報が取得できない場合は、ObjectReference型のプロパティであればOKとする
                if (arrayProperty.arraySize > 0)
                {
                    var firstElement = arrayProperty.GetArrayElementAtIndex(0);
                    return firstElement.propertyType == SerializedPropertyType.ObjectReference;
                }
                // 空の配列の場合は、とりあえず受け入れる
                return true;
            }
            
            // ドラッグされたすべてのオブジェクトが適切な型かチェック
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj == null)
                {
                    continue;
                }
                
                // 型の互換性をチェック
                if (!elementType.IsAssignableFrom(obj.GetType()))
                {
                    // Componentの派生型の場合、GameObjectからGetComponentで取得可能かチェック
                    if (typeof(Component).IsAssignableFrom(elementType) && obj is GameObject gameObject)
                    {
                        if (gameObject.GetComponent(elementType) == null)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        private static System.Type GetArrayElementType(SerializedProperty arrayProperty)
        {
            // プロパティパスから親オブジェクトとフィールド情報を取得
            var parentObject = GetActualObjectForProperty(arrayProperty);
            if (parentObject == null)
            {
                return null;
            }
            
            // フィールド名を取得（Array.data[n]形式を考慮）
            var fieldName = arrayProperty.name;
            if (arrayProperty.propertyPath.Contains(".Array.data["))
            {
                var lastDotIndex = arrayProperty.propertyPath.LastIndexOf(".Array.data[");
                if (lastDotIndex > 0)
                {
                    var parentPath = arrayProperty.propertyPath.Substring(0, lastDotIndex);
                    var parts = parentPath.Split('.');
                    fieldName = parts[parts.Length - 1];
                }
            }
            
            var fieldInfo = parentObject.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (fieldInfo != null)
            {
                var fieldType = fieldInfo.FieldType;
                
                // 配列型の場合
                if (fieldType.IsArray)
                {
                    return fieldType.GetElementType();
                }
                
                // List<T>型の場合
                if (fieldType.IsGenericType)
                {
                    var genericTypeDef = fieldType.GetGenericTypeDefinition();
                    if (genericTypeDef == typeof(System.Collections.Generic.List<>))
                    {
                        return fieldType.GetGenericArguments()[0];
                    }
                }
            }
            
            return null;
        }
#endif
    }
}