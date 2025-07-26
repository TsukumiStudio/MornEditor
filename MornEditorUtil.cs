using System.Collections.Generic;
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
        /// <summary>現在MornEditorUtilで処理中のAttributeを保持（二重処理防止用）</summary>
        private static readonly HashSet<object> ProcessingAttributes = new HashSet<object>();
        
        /// <summary>MornEditorUtilで処理中のAttributeかチェック</summary>
        public static bool IsProcessingAttribute(object attribute)
        {
            return ProcessingAttributes.Contains(attribute);
        }
        
        /// <summary>プロパティを描画（配列の場合も含めて適切に処理）</summary>
        public static void DrawPropertyField(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
        
        /// <summary>プロパティの高さを取得（配列の場合も含めて適切に処理）</summary>
        public static float GetPropertyFieldHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        
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
            
            if (fieldInfo == null)
            {
                EditorGUILayout.PropertyField(property, true);
                return;
            }
            
            // 各Attributeを取得
            var showIfAttribute = fieldInfo.GetCustomAttribute<ShowIfAttribute>();
            var hideIfAttribute = fieldInfo.GetCustomAttribute<HideIfAttribute>();
            var enableIfAttribute = fieldInfo.GetCustomAttribute<EnableIfAttribute>();
            var disableIfAttribute = fieldInfo.GetCustomAttribute<DisableIfAttribute>();
            
            // 処理中のAttributeとして登録
            if (showIfAttribute != null) ProcessingAttributes.Add(showIfAttribute);
            if (hideIfAttribute != null) ProcessingAttributes.Add(hideIfAttribute);
            if (enableIfAttribute != null) ProcessingAttributes.Add(enableIfAttribute);
            if (disableIfAttribute != null) ProcessingAttributes.Add(disableIfAttribute);
            
            try
            {
                // 表示/非表示の判定
                if (!CheckVisibility(fieldInfo, property))
                {
                    return;
                }
                
                // 有効/無効の判定と描画
                var isEnabled = CheckEnabled(fieldInfo, property);
                using (new EditorGUI.DisabledScope(!isEnabled))
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }
            finally
            {
                // 処理が終わったらAttributeを削除
                if (showIfAttribute != null) ProcessingAttributes.Remove(showIfAttribute);
                if (hideIfAttribute != null) ProcessingAttributes.Remove(hideIfAttribute);
                if (enableIfAttribute != null) ProcessingAttributes.Remove(enableIfAttribute);
                if (disableIfAttribute != null) ProcessingAttributes.Remove(disableIfAttribute);
            }
        }
        
        
        /// <summary>ShowIf/HideIfAttributeに基づいてプロパティの表示可否を判定</summary>
        private static bool CheckVisibility(FieldInfo fieldInfo, SerializedProperty property)
        {
            // ShowIfAttributeの処理
            var showIfAttribute = fieldInfo.GetCustomAttribute<ShowIfAttribute>();
            if (showIfAttribute != null && !EvaluateShowIfCondition(showIfAttribute, property))
            {
                return false;
            }
            
            // HideIfAttributeの処理
            var hideIfAttribute = fieldInfo.GetCustomAttribute<HideIfAttribute>();
            if (hideIfAttribute != null && EvaluateHideIfCondition(hideIfAttribute, property))
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>EnableIf/DisableIfAttributeに基づいてプロパティの有効状態を判定</summary>
        private static bool CheckEnabled(FieldInfo fieldInfo, SerializedProperty property)
        {
            // EnableIfAttributeの処理
            var enableIfAttribute = fieldInfo.GetCustomAttribute<EnableIfAttribute>();
            if (enableIfAttribute != null && !EvaluateEnableIfCondition(enableIfAttribute, property))
            {
                return false;
            }
            
            // DisableIfAttributeの処理
            var disableIfAttribute = fieldInfo.GetCustomAttribute<DisableIfAttribute>();
            if (disableIfAttribute != null && EvaluateDisableIfCondition(disableIfAttribute, property))
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>ShowIfAttributeの条件を評価</summary>
        public static bool EvaluateShowIfCondition(ShowIfAttribute attribute, SerializedProperty property)
        {
            return EvaluateConditions(attribute.PropertyNames, property, true);
        }
        
        /// <summary>HideIfAttributeの条件を評価</summary>
        public static bool EvaluateHideIfCondition(HideIfAttribute attribute, SerializedProperty property)
        {
            return EvaluateConditions(attribute.PropertyNames, property, false);
        }
        
        /// <summary>EnableIfAttributeの条件を評価</summary>
        public static bool EvaluateEnableIfCondition(EnableIfAttribute attribute, SerializedProperty property)
        {
            return EvaluateConditions(attribute.PropertyNames, property, true);
        }
        
        /// <summary>DisableIfAttributeの条件を評価</summary>
        public static bool EvaluateDisableIfCondition(DisableIfAttribute attribute, SerializedProperty property)
        {
            return EvaluateConditions(attribute.PropertyNames, property, false);
        }
        
        /// <summary>条件を評価（AND条件）</summary>
        /// <param name="propertyNames">評価するプロパティ名の配列</param>
        /// <param name="property">現在のプロパティ</param>
        /// <param name="requireAll">すべてtrueである必要があるか（true）、いずれかtrueで良いか（false）</param>
        private static bool EvaluateConditions(string[] propertyNames, SerializedProperty property, bool requireAll)
        {
            foreach (var propertyName in propertyNames)
            {
                if (!TryGetBool(propertyName, property, out var value))
                {
                    // プロパティが見つからない場合はfalseとして扱う
                    value = false;
                }
                
                if (requireAll && !value)
                {
                    // AND条件で一つでもfalseならfalse
                    return false;
                }
                else if (!requireAll && value)
                {
                    // OR条件で一つでもtrueならtrue
                    return true;
                }
            }
            
            // AND条件の場合はすべてtrue、OR条件の場合はすべてfalse
            return requireAll;
        }

        private static void DrawArrayPropertyWithDragAndDrop(SerializedProperty property)
        {
            // まず通常のPropertyFieldを描画（EditorGUILayoutを使用）
            EditorGUILayout.PropertyField(property, true);
            
            // ドラッグ&ドロップのための領域を取得
            var lastRect = GUILayoutUtility.GetLastRect();
            var headerRect = new Rect(lastRect.x, lastRect.y, lastRect.width, EditorGUIUtility.singleLineHeight);
            
            // ドラッグ&ドロップの処理
            if (property.arraySize >= 0 || property.arraySize == -1) // 空の配列も受け付ける
            {
                HandleArrayDragAndDrop(property, headerRect);
            }
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