using UnityEngine;

namespace MornEditor
{
    public sealed class HideIfAttribute : PropertyAttribute
    {
        public string[] PropertyNames { get; }

        public HideIfAttribute(string propertyName)
        {
            PropertyNames = new[] { propertyName };
        }
        
        public HideIfAttribute(string propertyName1, string propertyName2)
        {
            PropertyNames = new[] { propertyName1, propertyName2 };
        }
        
        public HideIfAttribute(params string[] propertyNames)
        {
            PropertyNames = propertyNames;
        }
    }
}