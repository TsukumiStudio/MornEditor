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
            
            if (!MornEditorUtil.TryGetBool(hideIfAttribute.PropertyName, property, out var hide) || !hide)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var hideIfAttribute = (HideIfAttribute)attribute;
            
            if (!MornEditorUtil.TryGetBool(hideIfAttribute.PropertyName, property, out var hide) || !hide)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            
            return 0f;
        }
    }
}
#endif