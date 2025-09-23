using Arbor;
using UnityEditor;

namespace MornEditor
{
    [CustomEditor(typeof(StateBehaviour), true)] // すべてのStateBehaviourに適用
    internal sealed class StateBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            MornEditorDrawerUtil.OnInspectorGUI(target, serializedObject);
        }
    }
}