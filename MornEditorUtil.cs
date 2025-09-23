using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    public static class MornEditorUtil
    {
        public static void SetDirty(Object target)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(target);
#endif
        }
    }
}