#if USE_MORNEDITOR_INSPECTOR
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    [CustomEditor(typeof(ScriptableObject), true)]
    [CanEditMultipleObjects]
    internal sealed class ScriptableObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            MornEditorDrawerUtil.OnInspectorGUI(target, serializedObject);
        }
    }
}
#endif
