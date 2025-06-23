using UnityEngine;

namespace MornEditor
{
    public sealed class EnableIfAttribute : PropertyAttribute
    {
        public string[] PropertyNames { get; }

        public EnableIfAttribute(string propertyName)
        {
            PropertyNames = new[] { propertyName };
        }
        
        public EnableIfAttribute(string propertyName1, string propertyName2)
        {
            PropertyNames = new[] { propertyName1, propertyName2 };
        }
        
        public EnableIfAttribute(params string[] propertyNames)
        {
            PropertyNames = propertyNames;
        }
    }
}