#if USE_ARBOR
using UnityEngine;
using UnityEditor;
using ArborEditor;
using Arbor;

namespace MornEditor
{
    [InitializeOnLoad]
    internal static class ArborEditorWindowExtension
    {
        static ArborEditorWindowExtension()
        {
            ArborEditorWindow.toolbarGUI += OnToolbarGUI;
        }

        private static void OnToolbarGUI(NodeGraph nodeGraph)
        {
            if (nodeGraph == null)
            {
                return;
            }

            GUILayout.Space(10);
            var reloadContent = new GUIContent("再読み込み", "グラフビューをリロードしてStateLinkの表示を更新します");
            if (GUILayout.Button(reloadContent, EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ReloadArborEditor();
            }
        }

        private static void ReloadArborEditor()
        {
            // アクティブなArborEditorWindowを取得
            var arborWindow = ArborEditorWindow.activeWindow;
            if (arborWindow == null)
            {
                Debug.LogWarning("アクティブなArborEditorWindowが見つかりません。");
                return;
            }

            // 現在開いているグラフを取得
            NodeGraph currentGraph = null;
            if (arborWindow.graphEditor != null)
            {
                currentGraph = arborWindow.graphEditor.nodeGraph;
            }

            if (currentGraph != null)
            {
                // 一度nullを設定してから、元のグラフを再設定
                ArborEditorWindow.activeWindow.SelectRootGraph(null);
                ArborEditorWindow.activeWindow.SelectRootGraph(currentGraph);
            }
        }
    }
}
#endif