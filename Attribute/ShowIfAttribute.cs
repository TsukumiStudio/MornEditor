using UnityEngine;

namespace MornEditor
{
    public sealed class ShowIfAttribute : PropertyAttribute
    {
        public string[] PropertyNames { get; }

        public ShowIfAttribute(string propertyName)
        {
            PropertyNames = new[] { propertyName };
        }
        
        public ShowIfAttribute(string propertyName1, string propertyName2)
        {
            PropertyNames = new[] { propertyName1, propertyName2 };
        }
        
        public ShowIfAttribute(params string[] propertyNames)
        {
            PropertyNames = propertyNames;
        }
    }
}