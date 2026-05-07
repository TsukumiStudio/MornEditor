using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace MornLib
{
    /// <summary>Drawer共通処理のユーティリティクラス</summary>
    public static class MornEditorDrawerUtil
    {
#if UNITY_EDITOR
        private static readonly Dictionary<string, ReorderableList> _reorderableLists = new();
        private const float ElementVerticalPadding = 4f;
        private const float ElementTopOffset = 2f;
        private const float PingButtonWidth = 60f;

        /// <summary>無効化された状態でプロパティを描画</summary>
        public static void DrawDisabledProperty(Rect position, SerializedProperty property, GUIContent label,
            bool includeChildren = true)
        {
            if (IsDrawableArray(property))
            {
                GetOrCreateDisabledList(property, label).DoList(position);
                return;
            }

            var cachedEnabled = GUI.enabled;
            try
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label, includeChildren);
            }
            finally
            {
                GUI.enabled = cachedEnabled;
            }
        }

        /// <summary>無効化された状態でのプロパティの高さを取得</summary>
        public static float GetDisabledPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (IsDrawableArray(property))
            {
                return GetOrCreateDisabledList(property, label).GetHeight();
            }

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <summary>無効化された状態でプロパティを描画（レイアウト版）</summary>
        public static void DrawDisabledPropertyLayout(SerializedProperty property, GUIContent label = null,
            bool includeChildren = true)
        {
            label ??= new GUIContent(property.displayName);
            var height = GetDisabledPropertyHeight(property, label);
            var rect = EditorGUILayout.GetControlRect(true, height);
            DrawDisabledProperty(rect, property, label, includeChildren);
        }

        private static bool IsDrawableArray(SerializedProperty property)
        {
            return property.isArray && property.propertyType != SerializedPropertyType.String;
        }

        /// <summary>無効化された配列描画用の ReorderableList を取得 or 生成。
        /// callback は property.Copy() をクロージャで保持し、外部 iterator の進行に影響されない。</summary>
        private static ReorderableList GetOrCreateDisabledList(SerializedProperty property, GUIContent label)
        {
            var key = $"disabled_{property.propertyPath}";
            if (_reorderableLists.TryGetValue(key, out var cached) && IsAlive(cached))
            {
                return cached;
            }

            _reorderableLists.Remove(key);

            var propCopy = property.Copy();
            var list = new ReorderableList(property.serializedObject, propCopy, false, true, false, false)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, label),
                drawElementCallback = (rect, index, _, _) =>
                {
                    if (index < 0 || index >= propCopy.arraySize)
                    {
                        return;
                    }

                    var element = propCopy.GetArrayElementAtIndex(index);
                    rect.y += ElementTopOffset;
                    var prev = GUI.enabled;
                    try
                    {
                        GUI.enabled = false;
                        EditorGUI.PropertyField(rect, element, GUIContent.none, true);
                    }
                    finally
                    {
                        GUI.enabled = prev;
                    }
                },
                elementHeightCallback = index =>
                {
                    if (index < 0 || index >= propCopy.arraySize)
                    {
                        return EditorGUIUtility.singleLineHeight + ElementVerticalPadding;
                    }

                    var element = propCopy.GetArrayElementAtIndex(index);
                    return EditorGUI.GetPropertyHeight(element, GUIContent.none, true) + ElementVerticalPadding;
                }
            };
            _reorderableLists[key] = list;
            return list;
        }

        private static bool IsAlive(ReorderableList list)
        {
            try
            {
                _ = list.serializedProperty.serializedObject.targetObject;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>現在MornEditorUtilで処理中のAttributeを保持（二重処理防止用）</summary>
        private static readonly HashSet<object> _processingAttributes = new HashSet<object>();

        /// <summary>MornEditorUtilで処理中のAttributeかチェック</summary>
        public static bool IsProcessingAttribute(object attribute)
        {
            return _processingAttributes.Contains(attribute);
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
                PropertyDrawer.DrawProperty(target, property, _processingAttributes);
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
                try
                {
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(property);
                }
                finally
                {
                    GUI.enabled = cachedEnabled;
                }

                if (GUILayout.Button("Ping", GUILayout.Width(PingButtonWidth)))
                {
                    EditorGUIUtility.PingObject(target);
                }
            }
        }

        /// <summary>Editor GUI 中に呼ぶ Reflection invoke。
        /// 呼び出し先の例外で Inspector 全体が停止しないよう Console に流して握り潰す。</summary>
        private static void InvokeSafe(MethodInfo method, object target)
        {
            try
            {
                method.Invoke(target, null);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex.InnerException ?? ex);
            }
        }

        /// <summary>UnityEngine.Object ではない任意のインスタンスに対して [Button]/[OnInspectorGUI] を IMGUI 内で描画する。
        /// undoTarget は Undo/SetDirty 対象のシリアライズ owner。null の場合は Undo/Dirty なしで実行のみ。</summary>
        public static void HandleCustomAttributesForObject(object target, Object undoTarget)
        {
            if (target == null) return;
            var needsRepaint = false;
            var methods = target.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            foreach (var method in methods)
            {
                if (method.GetParameters().Length != 0) continue;
                var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
                if (buttonAttribute != null)
                {
                    var buttonName = string.IsNullOrEmpty(buttonAttribute.Name) ? method.Name : buttonAttribute.Name;
                    if (GUILayout.Button(buttonName))
                    {
                        if (undoTarget != null) Undo.RegisterCompleteObjectUndo(undoTarget, $"Button: {buttonName}");
                        InvokeSafe(method, target);
                        if (undoTarget != null) EditorUtility.SetDirty(undoTarget);
                        needsRepaint = true;
                    }
                }
                var onInspectorGUIAttribute = method.GetCustomAttribute<OnInspectorGUIAttribute>();
                if (onInspectorGUIAttribute != null)
                {
                    InvokeSafe(method, target);
                }
            }
            if (needsRepaint)
            {
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        private static void HandleCustomAttributes(Object target, SerializedObject serializedObject)
        {
            var needsRepaint = false;
            // Editor GUI 拡張のため Reflection で [Button] / [OnInspectorGUI] 属性付きメソッドを抽出。
            // 呼び出し範囲は target インスタンスの直接メソッドのみで AOT 制約外。
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
                        foreach (var t in serializedObject.targetObjects)
                        {
                            InvokeSafe(method, t);
                            EditorUtility.SetDirty(t);
                        }

                        needsRepaint = true;
                        serializedObject.Update();
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                var onInspectorGUIAttribute = method.GetCustomAttribute<OnInspectorGUIAttribute>();
                if (onInspectorGUIAttribute != null)
                {
                    InvokeSafe(method, target);
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
                // SerializedProperty 名から FieldInfo を Reflection 解決して属性 ([ShowIf] 等) を取得する。
                // ネスト構造体内のフィールドは name が衝突するためトップレベルの簡易解決のみ対応。
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

                // ShowIf/HideIf/EnableIf/DisableIf の条件評価のため、属性で指定された名前を
                // Reflection で解決する。Editor 専用のため AOT 制約は問題にならない。
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

                    // Array.data[N] の組を IList の N 番目要素として解決する
                    if (part == "Array" && i + 1 < pathParts.Length && pathParts[i + 1].StartsWith("data["))
                    {
                        var dataPart = pathParts[i + 1];
                        var lb = dataPart.IndexOf('[');
                        var rb = dataPart.IndexOf(']');
                        if (lb < 0 || rb <= lb) return null;
                        if (!int.TryParse(dataPart.Substring(lb + 1, rb - lb - 1), out var idx)) return null;
                        if (currentObject is System.Collections.IList list && idx < list.Count)
                        {
                            currentObject = list[idx];
                            if (currentObject == null) return null;
                            i++; // data[N] をスキップ
                            continue;
                        }
                        return null;
                    }

                    // 現在のオブジェクトから次のフィールドを継承チェーンも含めて取得
                    var field = FindFieldRecursive(currentObject.GetType(), part);
                    if (field == null) return null;

                    currentObject = field.GetValue(currentObject);
                    if (currentObject == null) return null;
                }

                return currentObject;
            }

            private static FieldInfo FindFieldRecursive(System.Type type, string name)
            {
                while (type != null)
                {
                    var f = type.GetField(name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f != null) return f;
                    type = type.BaseType;
                }
                return null;
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