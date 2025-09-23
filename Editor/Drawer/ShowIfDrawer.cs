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
            
            // MornEditorUtilで処理中の場合はPropertyDrawerでは処理しない
            if (MornEditorDrawerUtil.IsProcessingAttribute(showIfAttribute))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
            if (MornEditorDrawerUtil.EvaluateShowIfCondition(showIfAttribute, property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var showIfAttribute = (ShowIfAttribute)attribute;
            
            // MornEditorUtilで処理中の場合はデフォルトの高さを返す
            if (MornEditorDrawerUtil.IsProcessingAttribute(showIfAttribute))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            
            if (MornEditorDrawerUtil.EvaluateShowIfCondition(showIfAttribute, property))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            
            return -EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
#endif