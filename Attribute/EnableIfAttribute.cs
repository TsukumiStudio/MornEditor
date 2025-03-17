using UnityEngine;

namespace MornEditor
{
    public sealed class EnableIfAttribute : PropertyAttribute
    {
        public string PropertyName { get; }

        public EnableIfAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}