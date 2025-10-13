using System;
using UnityEngine;

namespace MornEditor
{
    public static class MornGUILayout
    {
        public readonly struct EnableScope : IDisposable
        {
            private readonly bool _wasEnabled;

            public EnableScope(bool enabled)
            {
                _wasEnabled = GUI.enabled;
                GUI.enabled = enabled;
            }

            public void Dispose()
            {
                GUI.enabled = _wasEnabled;
            }
        }
    }
}