using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    internal sealed class EnableIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enableIfAttribute = (EnableIfAttribute)attribute;
            
            if (ShouldDisable(enableIfAttribute, property))
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
        
        private bool ShouldDisable(EnableIfAttribute enableIfAttribute, SerializedProperty property)
        {
            foreach (var propertyName in enableIfAttribute.PropertyNames)
            {
                if (!MornEditorUtil.TryGetBool(propertyName, property, out var show) || !show)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}