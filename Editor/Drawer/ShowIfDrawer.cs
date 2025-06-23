#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var showIfAttribute = (ShowIfAttribute)attribute;
            
            if (ShouldShow(showIfAttribute, property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var showIfAttribute = (ShowIfAttribute)attribute;
            
            if (ShouldShow(showIfAttribute, property))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            
            return 0f;
        }
        
        private bool ShouldShow(ShowIfAttribute showIfAttribute, SerializedProperty property)
        {
            foreach (var propertyName in showIfAttribute.PropertyNames)
            {
                if (!MornEditorUtil.TryGetBool(propertyName, property, out var show) || !show)
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}
#endif