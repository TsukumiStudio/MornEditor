namespace MornEditor
{
    public sealed class ShowIfAttribute : GivenBoolNameAttributeBase
    {
        public ShowIfAttribute(string propertyName) : base(propertyName)
        {
        }
    }
}
