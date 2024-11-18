using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MornEditor
{
    public static class MornEditorUtil
    {
#if DISABLE_MORN_EDITOR_LOG
        private const bool ShowLOG = false;
#else
        private const bool ShowLOG = true;
#endif
        private const string Prefix = "[<color=green>MornEditor</color>] ";

        internal static void Log(string message)
        {
            if (ShowLOG)
            {
                Debug.Log(Prefix + message);
            }
        }

        internal static void LogError(string message)
        {
            if (ShowLOG)
            {
                Debug.LogError(Prefix + message);
            }
        }

        internal static void LogWarning(string message)
        {
            if (ShowLOG)
            {
                Debug.LogWarning(Prefix + message);
            }
        }

        public static void SetDirty(Object obj)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(obj);
#endif
        }

        public static T InstantiatePrefab<T>(T prefab, Transform parent) where T : Object
        {
#if UNITY_EDITOR
            return (T)PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
            LogWarning("PrefabUtility.InstantiatePrefab is only available in Editor.");
            return Object.Instantiate(prefab, parent);
        }
    }
}