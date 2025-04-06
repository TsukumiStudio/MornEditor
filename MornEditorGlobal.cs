using System.Runtime.CompilerServices;
using MornGlobal;

[assembly: InternalsVisibleTo("MornEditor.Editor")]
namespace MornEditor
{
    internal sealed class MornEditorGlobal : MornGlobalPureBase<MornEditorGlobal>
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