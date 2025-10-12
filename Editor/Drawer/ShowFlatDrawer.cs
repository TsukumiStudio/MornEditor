using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    [CustomPropertyDrawer(typeof(ShowFlatAttribute))]
    internal sealed class ShowFlatDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var yPosition = position.y;
            var childProperty = property.Copy();
            var enterChildren = true;
            var depth = childProperty.depth;

            while (childProperty.NextVisible(enterChildren) && childProperty.depth > depth)
            {
                enterChildren = false;
                var height = EditorGUI.GetPropertyHeight(childProperty, true);
                var rect = new Rect(position.x, yPosition, position.width, height);
                EditorGUI.PropertyField(rect, childProperty, true);
                yPosition += height + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var totalHeight = 0f;
            var childProperty = property.Copy();
            var enterChildren = true;
            var depth = childProperty.depth;

            while (childProperty.NextVisible(enterChildren) && childProperty.depth > depth)
            {
                enterChildren = false;
                totalHeight += EditorGUI.GetPropertyHeight(childProperty, true);
                totalHeight += EditorGUIUtility.standardVerticalSpacing;
            }

            if (totalHeight > 0)
                totalHeight -= EditorGUIUtility.standardVerticalSpacing;

            return totalHeight;
        }
    }
}
