using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    [CustomPropertyDrawer(typeof(DisableIfAttribute))]
    internal sealed class DisableIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var disableIfAttribute = (DisableIfAttribute)attribute;
            
            if (ShouldDisable(disableIfAttribute, property))
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
        
        private bool ShouldDisable(DisableIfAttribute disableIfAttribute, SerializedProperty property)
        {
            foreach (var propertyName in disableIfAttribute.PropertyNames)
            {
                if (MornEditorUtil.TryGetBool(propertyName, property, out var show) && show)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}