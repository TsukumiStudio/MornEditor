using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public static void DrawDisabledProperty(Rect position, SerializedProperty property, GUIContent label,
            bool includeChildren = true)
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
        private static void DrawDisabledArrayProperty(Rect position, SerializedProperty property, GUIContent label,
            bool cachedEnabled)
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
        public static void DrawDisabledPropertyLayout(SerializedProperty property, GUIContent label = null,
            bool includeChildren = true)
        {
            label = label ?? new GUIContent(property.displayName);
            var height = GetDisabledPropertyHeight(property, label);
            var rect = EditorGUILayout.GetControlRect(true, height);
            DrawDisabledProperty(rect, property, label, includeChildren);
        }
#endif
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
            return ReflectionHelper.TryGetBoolValue(propertyName, property, out value);
        }

        public static void OnInspectorGUI(Object target, SerializedObject serializedObject)
        {
            // 外部からの変更（コピー/ペースト等）を確実に反映
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            // 1行目のm_scriptと、そのpingボタン
            DrawScriptFieldWithPingButton(property, target);

            // プロパティの描画
            while (property.NextVisible(false))
            {
                PropertyDrawer.DrawProperty(target, property, ProcessingAttributes);
            }

            // GUI経由の変更を適用
            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }

            // ButtonAttributeとOnInspectorGUIAttributeの処理
            HandleCustomAttributes(target, serializedObject);
        }

        private static void DrawScriptFieldWithPingButton(SerializedProperty property, Object target)
        {
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
        }

        private static void HandleCustomAttributes(Object target, SerializedObject serializedObject)
        {
            var needsRepaint = false;
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
                        needsRepaint = true;
                        // ボタン実行後は即座に変更を反映
                        EditorUtility.SetDirty(target);
                        serializedObject.Update();
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                var onInspectorGUIAttribute = method.GetCustomAttribute<OnInspectorGUIAttribute>();
                if (onInspectorGUIAttribute != null)
                {
                    method.Invoke(target, null);
                }
            }

            // 再描画が必要な場合は強制的に再描画
            if (needsRepaint)
            {
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        #region Public Attribute Evaluation Methods

        /// <summary>ShowIfAttributeの条件を評価</summary>
        public static bool EvaluateShowIfCondition(ShowIfAttribute attribute, SerializedProperty property)
        {
            return AttributeProcessor.EvaluateConditions(attribute.PropertyNames, property, true);
        }

        /// <summary>HideIfAttributeの条件を評価</summary>
        public static bool EvaluateHideIfCondition(HideIfAttribute attribute, SerializedProperty property)
        {
            return AttributeProcessor.EvaluateConditions(attribute.PropertyNames, property, false);
        }

        /// <summary>EnableIfAttributeの条件を評価</summary>
        public static bool EvaluateEnableIfCondition(EnableIfAttribute attribute, SerializedProperty property)
        {
            return AttributeProcessor.EvaluateConditions(attribute.PropertyNames, property, true);
        }

        /// <summary>DisableIfAttributeの条件を評価</summary>
        public static bool EvaluateDisableIfCondition(DisableIfAttribute attribute, SerializedProperty property)
        {
            return AttributeProcessor.EvaluateConditions(attribute.PropertyNames, property, false);
        }

        #endregion

        /// <summary>プロパティ描画を担当する内部クラス</summary>
        internal static class PropertyDrawer
        {
            internal static void DrawProperty(Object target, SerializedProperty property,
                HashSet<object> processingAttributes)
            {
                var targetType = target.GetType();
                var fieldInfo = targetType.GetField(
                    property.name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fieldInfo == null)
                {
                    // 通常のPropertyFieldを使用
                    EditorGUILayout.PropertyField(property, true);
                    return;
                }

                // 各Attributeを取得
                var showIfAttribute = fieldInfo.GetCustomAttribute<ShowIfAttribute>();
                var hideIfAttribute = fieldInfo.GetCustomAttribute<HideIfAttribute>();
                var enableIfAttribute = fieldInfo.GetCustomAttribute<EnableIfAttribute>();
                var disableIfAttribute = fieldInfo.GetCustomAttribute<DisableIfAttribute>();
                var readOnlyAttribute = fieldInfo.GetCustomAttribute<ReadOnlyAttribute>();

                // 処理中のAttributeとして登録
                RegisterAttributes(
                    processingAttributes,
                    showIfAttribute,
                    hideIfAttribute,
                    enableIfAttribute,
                    disableIfAttribute,
                    readOnlyAttribute);
                try
                {
                    // 表示/非表示の判定
                    if (!AttributeProcessor.CheckVisibility(fieldInfo, property))
                    {
                        return;
                    }

                    // ReadOnlyAttributeまたは無効状態の判定
                    var hasReadOnly = readOnlyAttribute != null;
                    var isEnabled = !hasReadOnly && AttributeProcessor.CheckEnabled(fieldInfo, property);
                    if (!isEnabled)
                    {
                        // 無効化された描画（配列の+-ボタン非表示を含む）
                        MornEditorDrawerUtil.DrawDisabledPropertyLayout(property);
                    }
                    else
                    {
                        DrawPropertyWithDragAndDrop(property);
                    }
                }
                finally
                {
                    // 処理が終わったらAttributeを削除
                    UnregisterAttributes(
                        processingAttributes,
                        showIfAttribute,
                        hideIfAttribute,
                        enableIfAttribute,
                        disableIfAttribute,
                        readOnlyAttribute);
                }
            }

            private static void RegisterAttributes(HashSet<object> processingAttributes, params object[] attributes)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute != null)
                    {
                        processingAttributes.Add(attribute);
                    }
                }
            }

            private static void UnregisterAttributes(HashSet<object> processingAttributes, params object[] attributes)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute != null)
                    {
                        processingAttributes.Remove(attribute);
                    }
                }
            }

            private static void DrawPropertyWithDragAndDrop(SerializedProperty property)
            {
                // 配列プロパティの場合、要素の型をチェック
                if (property.isArray && property.propertyType != SerializedPropertyType.String)
                {
                    var elementType = ReflectionHelper.GetArrayElementType(property);

                    // UnityEngine.Objectを継承する型の配列のみドラッグ&ドロップ対応
                    if (elementType != null && typeof(UnityEngine.Object).IsAssignableFrom(elementType))
                    {
                        DragAndDropHandler.DrawArrayPropertyWithDragAndDrop(property);
                    }
                    else
                    {
                        // 構造体などの配列は通常のPropertyFieldで描画
                        EditorGUILayout.PropertyField(property, true);
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }
        }

        /// <summary>属性の評価を担当する内部クラス</summary>
        internal static class AttributeProcessor
        {
            /// <summary>ShowIf/HideIfAttributeに基づいてプロパティの表示可否を判定</summary>
            internal static bool CheckVisibility(FieldInfo fieldInfo, SerializedProperty property)
            {
                // ShowIfAttributeの処理
                var showIfAttribute = fieldInfo.GetCustomAttribute<ShowIfAttribute>();
                if (showIfAttribute != null && !EvaluateConditions(showIfAttribute.PropertyNames, property, true))
                {
                    return false;
                }

                // HideIfAttributeの処理
                var hideIfAttribute = fieldInfo.GetCustomAttribute<HideIfAttribute>();
                if (hideIfAttribute != null && EvaluateConditions(hideIfAttribute.PropertyNames, property, false))
                {
                    return false;
                }

                return true;
            }

            /// <summary>EnableIf/DisableIfAttributeに基づいてプロパティの有効状態を判定</summary>
            internal static bool CheckEnabled(FieldInfo fieldInfo, SerializedProperty property)
            {
                // EnableIfAttributeの処理
                var enableIfAttribute = fieldInfo.GetCustomAttribute<EnableIfAttribute>();
                if (enableIfAttribute != null && !EvaluateConditions(enableIfAttribute.PropertyNames, property, true))
                {
                    return false;
                }

                // DisableIfAttributeの処理
                var disableIfAttribute = fieldInfo.GetCustomAttribute<DisableIfAttribute>();
                if (disableIfAttribute != null && EvaluateConditions(disableIfAttribute.PropertyNames, property, false))
                {
                    return false;
                }

                return true;
            }

            /// <summary>条件を評価（AND条件）</summary>
            /// <param name="propertyNames">評価するプロパティ名の配列</param>
            /// <param name="property">現在のプロパティ</param>
            /// <param name="requireAll">すべてtrueである必要があるか（true）、いずれかtrueで良いか（false）</param>
            internal static bool EvaluateConditions(string[] propertyNames, SerializedProperty property,
                bool requireAll)
            {
                foreach (var propertyName in propertyNames)
                {
                    if (!ReflectionHelper.TryGetBoolValue(propertyName, property, out var value))
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
        }

        /// <summary>ドラッグ&ドロップを担当する内部クラス</summary>
        internal static class DragAndDropHandler
        {
            internal static void DrawArrayPropertyWithDragAndDrop(SerializedProperty property)
            {
                var evt = Event.current;

                // 配列プロパティフィールドの描画前の位置を記録
                var lastRect = GUILayoutUtility.GetLastRect();
                var expectedHeaderRect = new Rect(
                    lastRect.x,
                    lastRect.y + lastRect.height + EditorGUIUtility.standardVerticalSpacing,
                    lastRect.width,
                    EditorGUIUtility.singleLineHeight);

                // ドラッグ&ドロップイベントの処理
                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                {
                    // ヘッダー領域内でのみドラッグ&ドロップを処理
                    if (expectedHeaderRect.Contains(evt.mousePosition))
                    {
                        var isValid = IsValidDraggedObjects(property);
                        if (isValid)
                        {
                            if (evt.type == EventType.DragUpdated)
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                                evt.Use();
                            }
                            else if (evt.type == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();
                                HandleArrayDrop(property);
                                evt.Use();
                            }
                        }
                    }
                }

                // 通常のPropertyFieldを描画（配列要素内のフィールドへのドラッグも可能）
                EditorGUILayout.PropertyField(property, true);
            }

            private static void HandleArrayDrop(SerializedProperty arrayProperty)
            {
                // 配列要素の型を取得
                var elementType = ReflectionHelper.GetArrayElementType(arrayProperty);

                // ドラッグされたオブジェクトを配列に追加
                foreach (var draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject != null)
                    {
                        var objectToAdd = draggedObject;

                        // Sprite配列にTexture2Dをドラッグする特殊ケース
                        if (elementType == typeof(Sprite) && draggedObject is Texture2D texture)
                        {
                            var path = AssetDatabase.GetAssetPath(texture);
                            // Texture2DからSpriteを取得
                            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                            if (sprites.Length > 0)
                            {
                                // 複数のSpriteがある場合（マルチスプライト）、すべて追加
                                foreach (var sprite in sprites)
                                {
                                    var index = arrayProperty.arraySize;
                                    arrayProperty.InsertArrayElementAtIndex(index);
                                    var element = arrayProperty.GetArrayElementAtIndex(index);
                                    element.objectReferenceValue = sprite;
                                }

                                continue;
                            }
                        }
                        // GameObject がドラッグされていて、Component型の配列の場合
                        else if (elementType != null
                                 && typeof(Component).IsAssignableFrom(elementType)
                                 && draggedObject is GameObject gameObject)
                        {
                            objectToAdd = gameObject.GetComponent(elementType);
                            if (objectToAdd == null)
                            {
                                continue;
                            }
                        }

                        // 通常のケース
                        if (elementType == null || elementType.IsAssignableFrom(objectToAdd.GetType()))
                        {
                            var index = arrayProperty.arraySize;
                            arrayProperty.InsertArrayElementAtIndex(index);
                            var element = arrayProperty.GetArrayElementAtIndex(index);
                            element.objectReferenceValue = objectToAdd;
                        }
                    }
                }

                // 変更を確実に適用
                arrayProperty.serializedObject.ApplyModifiedProperties();

                // オブジェクトを変更済みとしてマーク（確実に保存されるように）
                EditorUtility.SetDirty(arrayProperty.serializedObject.targetObject);

                // Inspectorを強制的に再描画
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }

            private static bool IsValidDraggedObjects(SerializedProperty arrayProperty)
            {
                if (DragAndDrop.objectReferences.Length == 0)
                {
                    return false;
                }

                // 配列要素の型を取得
                var elementType = ReflectionHelper.GetArrayElementType(arrayProperty);
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

                // 構造体やクラスの場合（UnityEngine.Objectを継承していない場合）
                if (!typeof(UnityEngine.Object).IsAssignableFrom(elementType))
                {
                    return false;
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
                        // Sprite配列にTexture2Dをドラッグする特殊ケース
                        if (elementType == typeof(Sprite) && obj is Texture2D texture)
                        {
                            // TextureImporterでSpriteモードかチェック
                            var path = AssetDatabase.GetAssetPath(texture);
                            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                            if (importer != null && importer.textureType == TextureImporterType.Sprite)
                            {
                                continue;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        // Componentの派生型の場合、GameObjectからGetComponentで取得可能かチェック
                        else if (typeof(Component).IsAssignableFrom(elementType) && obj is GameObject gameObject)
                        {
                            var component = gameObject.GetComponent(elementType);
                            if (component == null)
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
        }

        /// <summary>リフレクション関連のヘルパークラス</summary>
        internal static class ReflectionHelper
        {
            internal static bool TryGetBoolValue(string propertyName, SerializedProperty property, out bool value)
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
                    var field = type.GetField(
                        part,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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

            internal static System.Type GetArrayElementType(SerializedProperty arrayProperty)
            {
                // SerializedPropertyから直接親オブジェクトを取得
                var targetObject = arrayProperty.serializedObject.targetObject;
                var targetType = targetObject.GetType();

                // プロパティ名から配列フィールドを検索
                var fieldName = arrayProperty.name;

                // privateフィールドも含めて検索
                var fieldInfo = targetType.GetField(
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
        }
#endif
    }
}