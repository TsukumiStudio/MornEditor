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
            
            if (MornEditorUtil.TryGetBool(showIfAttribute.PropertyName, property, out var show) && show)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var showIfAttribute = (ShowIfAttribute)attribute;
            
            if (MornEditorUtil.TryGetBool(showIfAttribute.PropertyName, property, out var show) && show)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            
            return 0f;
        }
    }
}
#endif