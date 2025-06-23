using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    public static class MornEditorInternalExtension
    {
        // コピーされたコンポーネントの情報を保持
        private static Component _sCopiedComponent;
        private static SerializedObject _sCopiedSerializedObject;

        [EditorHeaderItem(typeof(Component))]
        private static bool ComponentCopyPasteHeaderItem(Rect pos, Object[] targets)
        {
            if (targets.Length == 0 || !(targets[0] is Component component))
            {
                return false;
            }

            const float offsetX = 5f;
            const float buttonSize = 17f;
            const float padding = 3f;
            const string iconButtonStyle = "IconButton";
            const string copyIcon = "TreeEditor.Duplicate";
            const string pasteIcon = "Clipboard";
            pos.x += offsetX;
            var pasteRect = new Rect(pos.x, pos.y + (pos.height - buttonSize) * 0.5f, buttonSize, buttonSize);
            var copyRect = new Rect(pasteRect.x - buttonSize - padding, pasteRect.y, buttonSize, buttonSize);
            var copyContent = new GUIContent(EditorGUIUtility.IconContent(copyIcon))
            {
                tooltip = "コンポーネントの値をコピー",
            };
            if (GUI.Button(copyRect, copyContent, iconButtonStyle))
            {
                CopyComponent(component);
            }

            // ペーストボタン（アイコンスタイル）
            var canPaste = CanPaste(component);
            using (new EditorGUI.DisabledScope(!canPaste))
            {
                var pasteContent = new GUIContent(EditorGUIUtility.IconContent(pasteIcon));
                pasteContent.tooltip = canPaste ? $"{GetCopiedComponentTypeName()} からペースト" : "互換性のあるコンポーネントがコピーされていません";
                if (GUI.Button(pasteRect, pasteContent, iconButtonStyle))
                {
                    PasteComponentValues(component);
                }
            }

            return true;
        }

        private static void CopyComponent(Component component)
        {
            if (component == null)
            {
                return;
            }

            _sCopiedComponent = component;
            _sCopiedSerializedObject = new SerializedObject(component);
        }

        private static bool CanPaste(Component targetComponent)
        {
            if (_sCopiedComponent == null || targetComponent == null)
            {
                return false;
            }

            // 同じ型または互換性のある型かチェック
            var copiedType = _sCopiedComponent.GetType();
            var targetType = targetComponent.GetType();
            return copiedType == targetType || targetType.IsAssignableFrom(copiedType);
        }

        private static void PasteComponentValues(Component targetComponent)
        {
            if (!CanPaste(targetComponent))
            {
                return;
            }

            Undo.RecordObject(targetComponent, "Paste Component Values");
            var targetSerializedObject = new SerializedObject(targetComponent);

            // すべてのプロパティをコピー
            var sourceIterator = _sCopiedSerializedObject.GetIterator();

            // 最初のプロパティ（m_Script）をスキップしてイテレーション開始
            if (sourceIterator.NextVisible(true))
            {
                while (sourceIterator.NextVisible(false))
                {
                    var propertyPath = sourceIterator.propertyPath;
                    var targetProperty = targetSerializedObject.FindProperty(propertyPath);
                    if (targetProperty != null && targetProperty.editable)
                    {
                        targetSerializedObject.CopyFromSerializedProperty(sourceIterator);
                    }
                }
            }

            targetSerializedObject.ApplyModifiedProperties();
        }

        private static string GetCopiedComponentTypeName()
        {
            return _sCopiedComponent != null ? _sCopiedComponent.GetType().Name : string.Empty;
        }
    }
}