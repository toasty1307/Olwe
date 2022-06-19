using Remora.Commands.Conditions;

namespace Olwe.Remora.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class EnsureEnabledAttribute : ConditionAttribute
{
    public string Name { get; }

    public EnsureEnabledAttribute(string name)
    {
        Name = name;
    }
}