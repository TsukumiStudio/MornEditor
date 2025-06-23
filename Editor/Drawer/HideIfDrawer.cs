#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    public class HideIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var hideIfAttribute = (HideIfAttribute)attribute;
            
            if (!ShouldHide(hideIfAttribute, property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var hideIfAttribute = (HideIfAttribute)attribute;
            
            if (!ShouldHide(hideIfAttribute, property))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            
            return 0f;
        }
        
        private bool ShouldHide(HideIfAttribute hideIfAttribute, SerializedProperty property)
        {
            foreach (var propertyName in hideIfAttribute.PropertyNames)
            {
                if (MornEditorUtil.TryGetBool(propertyName, property, out var hide) && hide)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
#endif