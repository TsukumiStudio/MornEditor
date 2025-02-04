using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
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

#if UNITY_EDITOR
        public static Texture2D RenderStaticPreview(Sprite sprite, Editor editor, string assetPath, Object[] subAssets, int width, int height)
        {
            if (sprite != null)
            {
                var t = GetType("UnityEditor.SpriteUtility");
                if (t != null)
                {
                    var method = t.GetMethod("RenderStaticPreview", new[] { typeof(Sprite), typeof(Color), typeof(int), typeof(int) });
                    if (method != null)
                    {
                        var ret = method.Invoke("RenderStaticPreview", new object[] { sprite, Color.white, width, height });
                        if (ret is Texture2D)
                        {
                            return ret as Texture2D;
                        }
                    }
                }
            }
            return editor.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        private static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            var currentAssembly = Assembly.GetExecutingAssembly();
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach (var assemblyName in referencedAssemblies)
            {
                var assembly = Assembly.Load(assemblyName);
                if (assembly != null)
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                        return type;
                }
            }
            return null;
        }
#endif
    }
}