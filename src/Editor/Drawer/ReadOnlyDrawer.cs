using UnityEditor;
using UnityEngine;

namespace MornLib
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    internal sealed class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
            {
                DrawArray(position, property, label);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
            {
                return GetArrayHeight(property, label);
            }

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private static void DrawArray(Rect position, SerializedProperty property, GUIContent label)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            var foldoutRect = new Rect(position.x, position.y, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            if (!property.isExpanded)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    var yOffset = lineHeight + spacing;
                    var sizeRect = new Rect(position.x, position.y + yOffset, position.width, lineHeight);
                    EditorGUI.IntField(sizeRect, "Size", property.arraySize);
                    yOffset += lineHeight + spacing;
                    for (var i = 0; i < property.arraySize; i++)
                    {
                        var element = property.GetArrayElementAtIndex(i);
                        var elementHeight = EditorGUI.GetPropertyHeight(element, true);
                        var elementRect = new Rect(position.x, position.y + yOffset, position.width, elementHeight);
                        EditorGUI.PropertyField(elementRect, element, true);
                        yOffset += elementHeight + spacing;
                    }
                }
            }
        }

        private static float GetArrayHeight(SerializedProperty property, GUIContent label)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            if (!property.isExpanded)
            {
                return lineHeight;
            }

            var total = lineHeight + spacing; // foldout 行
            total += lineHeight + spacing;    // Size 行
            for (var i = 0; i < property.arraySize; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                total += EditorGUI.GetPropertyHeight(element, true) + spacing;
            }

            return total;
        }
    }
}