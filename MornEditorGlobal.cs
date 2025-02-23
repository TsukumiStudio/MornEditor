using System.Runtime.CompilerServices;
using MornGlobal;
using UnityEngine;

[assembly: InternalsVisibleTo("MornEditor.Editor")]
namespace MornEditor
{
    [CreateAssetMenu(fileName = nameof(MornEditorGlobal), menuName = "Morn/" + nameof(MornEditorGlobal))]
    internal sealed class MornEditorGlobal : MornGlobalBase<MornEditorGlobal>
    {
        protected override string ModuleName => nameof(MornEditor);

        public static void Log(string message)
        {
            I.LogInternal(message);
        }

        public static void LogWarning(string message)
        {
            I.LogWarningInternal(message);
        }

        public static void LogError(string message)
        {
            I.LogErrorInternal(message);
        }
    }
}