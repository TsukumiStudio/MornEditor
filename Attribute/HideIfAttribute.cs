using UnityEngine;

namespace MornEditor
{
    public sealed class HideIfAttribute : PropertyAttribute
    {
        public string PropertyName { get; }

        public HideIfAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}