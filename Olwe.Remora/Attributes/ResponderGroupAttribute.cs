using Remora.Discord.Gateway.Responders;

namespace Olwe.Remora.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ResponderGroupAttribute : Attribute
{
    public ResponderGroupAttribute(ResponderGroup group)
    {
        Group = group;
    }
    
    public ResponderGroup Group { get; init; }
}