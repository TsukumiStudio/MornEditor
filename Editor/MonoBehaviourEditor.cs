using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    [CustomEditor(typeof(MonoBehaviour), true)] // すべてのMonoBehaviourに適用
    public sealed class MonoBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var methods = target.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            foreach (var method in methods)
            {
                var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
                if (buttonAttribute != null)
                {
                    if (GUILayout.Button(method.Name))
                    {
                        method.Invoke(target, null);
                    }
                }

                var onInspectorGUIAttribute = method.GetCustomAttribute<OnInspectorGUIAttribute>();
                if (onInspectorGUIAttribute != null)
                {
                    method.Invoke(target, null);
                }
            }
        }
    }
}