using TMPro;
using UnityEditor;
using UnityEngine;

namespace MornEditor
{
    [InitializeOnLoad]
    public static class MornEditorDynamicFontCleaner
    {
        static MornEditorDynamicFontCleaner()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                var tmpFontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var tmpFontAsset in tmpFontAssets)
                {
                    if (tmpFontAsset != null && tmpFontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                    {
                        tmpFontAsset.ClearFontAssetData();
                        MornEditorLogger.Log($"ClearFontAssetData\"{tmpFontAsset.name}\"");
                    }
                }
            }
        }
    }
}