using UnityEngine;

namespace MornEditor
{
    public sealed class DisableIfAttribute : PropertyAttribute
    {
        public string PropertyName { get; }

        public DisableIfAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}