using System;

namespace MornEditor
{
    public sealed class ButtonAttribute : Attribute
    {
        public string Name { get; }
        
        public ButtonAttribute()
        {
        }
        
        public ButtonAttribute(string name)
        {
            Name = name;
        }
    }
}
