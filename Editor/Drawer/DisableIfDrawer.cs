#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    /// <summary>DisableIfAttributeのPropertyDrawer（MonoBehaviour/ScriptableObject以外で使用）</summary>
    [CustomPropertyDrawer(typeof(DisableIfAttribute))]
    public class DisableIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var disableIfAttribute = (DisableIfAttribute)attribute;
            
            // MornEditorUtilで処理中の場合はPropertyDrawerでは処理しない
            if (MornEditorUtil.IsProcessingAttribute(disableIfAttribute))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
            var shouldDisable = MornEditorUtil.EvaluateDisableIfCondition(disableIfAttribute, property);
            
            using (new EditorGUI.DisabledScope(shouldDisable))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif