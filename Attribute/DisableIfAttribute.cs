using UnityEngine;

namespace MornEditor
{
    public sealed class DisableIfAttribute : PropertyAttribute
    {
        public string[] PropertyNames { get; }

        public DisableIfAttribute(string propertyName)
        {
            PropertyNames = new[] { propertyName };
        }
        
        public DisableIfAttribute(string propertyName1, string propertyName2)
        {
            PropertyNames = new[] { propertyName1, propertyName2 };
        }
        
        public DisableIfAttribute(params string[] propertyNames)
        {
            PropertyNames = propertyNames;
        }
    }
}