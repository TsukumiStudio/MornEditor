using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("MornEditor.Editor")]
namespace MornEditor
{
    internal static class MornEditorLogger
    {
        private static string ModuleName => nameof(MornEditor);
        private static string Prefix => $"[<color=#{ColorUtility.ToHtmlStringRGB(Color.green)}>{ModuleName}</color>] ";

        public static void Log(string message)
        {
            Debug.Log($"{Prefix} {message}");
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"{Prefix} {message}");
        }
    }
}