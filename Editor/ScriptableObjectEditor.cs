#if USE_MORNEDITOR_INSPECTOR
using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    [CustomEditor(typeof(ScriptableObject), true)] // すべてのMonoBehaviourに適用
    internal sealed class ScriptableObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            MornEditorDrawerUtil.OnInspectorGUI(target, serializedObject);
        }
    }
}
#endif