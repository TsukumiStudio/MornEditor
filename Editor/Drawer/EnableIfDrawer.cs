#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    /// <summary>EnableIfAttributeのPropertyDrawer（MonoBehaviour/ScriptableObject以外で使用）</summary>
    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    public class EnableIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enableIfAttribute = (EnableIfAttribute)attribute;
            
            // MornEditorUtilで処理中の場合はPropertyDrawerでは処理しない
            if (MornEditorUtil.IsProcessingAttribute(enableIfAttribute))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
            var isEnabled = MornEditorUtil.EvaluateEnableIfCondition(enableIfAttribute, property);
            
            using (new EditorGUI.DisabledScope(!isEnabled))
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