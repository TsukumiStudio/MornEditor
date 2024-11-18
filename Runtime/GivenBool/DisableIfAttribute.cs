namespace MornEditor
{
    public sealed class DisableIfAttribute : GivenBoolNameAttributeBase
    {
        public DisableIfAttribute(string propertyName) : base(propertyName)
        {
        }
    }
}
