#if USE_MORNEDITOR_INSPECTOR
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    [CustomEditor(typeof(MonoBehaviour), true)] // すべてのMonoBehaviourに適用
    internal sealed class MonoBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            MornEditorDrawerUtil.OnInspectorGUI(target, serializedObject);
        }
    }
}
#endif