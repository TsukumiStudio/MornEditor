using UnityEngine;

namespace MornEditor
{
    public sealed class ShowIfAttribute : PropertyAttribute
    {
        public string PropertyName { get; }

        public ShowIfAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}