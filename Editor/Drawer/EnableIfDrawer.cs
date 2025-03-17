using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    internal sealed class EnableIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var propertyName = ((DisableIfAttribute)attribute).PropertyName;
            if (MornEditorUtil.TryGetBool(propertyName, property, out var boolValue) && !boolValue)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(position, property, label, true);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }
}