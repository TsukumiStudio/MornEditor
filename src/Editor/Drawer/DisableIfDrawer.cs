#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    /// <summary>DisableIfAttributeのPropertyDrawer（MonoBehaviour/ScriptableObject以外で使用）</summary>
    [CustomPropertyDrawer(typeof(DisableIfAttribute))]
    public sealed class DisableIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var disableIfAttribute = (DisableIfAttribute)attribute;
            
            // MornEditorUtilで処理中の場合はPropertyDrawerでは処理しない
            if (MornEditorDrawerUtil.IsProcessingAttribute(disableIfAttribute))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
            var shouldDisable = MornEditorDrawerUtil.EvaluateDisableIfCondition(disableIfAttribute, property);
            
            if (shouldDisable)
            {
                MornEditorDrawerUtil.DrawDisabledProperty(position, property, label);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var disableIfAttribute = (DisableIfAttribute)attribute;
            
            // MornEditorUtilで処理中の場合は通常の高さを返す
            if (MornEditorDrawerUtil.IsProcessingAttribute(disableIfAttribute))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
            
            var shouldDisable = MornEditorDrawerUtil.EvaluateDisableIfCondition(disableIfAttribute, property);
            
            if (shouldDisable)
            {
                return MornEditorDrawerUtil.GetDisabledPropertyHeight(property, label);
            }
            
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif