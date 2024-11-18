using UnityEngine;

namespace MornEditor
{
    public abstract class GivenBoolNameAttributeBase : PropertyAttribute
    {
        public readonly string PropertyName;

        protected GivenBoolNameAttributeBase(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}
