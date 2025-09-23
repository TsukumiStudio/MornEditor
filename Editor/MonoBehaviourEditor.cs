using UnityEditor;
using UnityEngine;

namespace MornEditor
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