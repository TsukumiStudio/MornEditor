using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    [CustomEditor(typeof(ScriptableObject), true)] // すべてのMonoBehaviourに適用
    public sealed class ScriptableObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            MornEditorUtil.OnInspectorGUI(target, serializedObject);
        }
    }
}