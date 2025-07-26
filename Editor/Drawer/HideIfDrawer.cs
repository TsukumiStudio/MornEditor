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
            
            // MornEditorUtilで処理中の場合はPropertyDrawerでは処理しない
            if (MornEditorUtil.IsProcessingAttribute(hideIfAttribute))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
            if (!MornEditorUtil.EvaluateHideIfCondition(hideIfAttribute, property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var hideIfAttribute = (HideIfAttribute)attribute;
            
            // MornEditorUtilで処理中の場合はデフォルトの高さを返す
            if (MornEditorUtil.IsProcessingAttribute(hideIfAttribute))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            
            if (!MornEditorUtil.EvaluateHideIfCondition(hideIfAttribute, property))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            
            return -EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
#endif