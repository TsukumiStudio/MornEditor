using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    internal sealed class HideIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var propertyName = ((DisableIfAttribute)attribute).PropertyName;
            if (MornEditorUtil.TryGetBool(propertyName, property, out var boolValue) && boolValue)
            {
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }
}