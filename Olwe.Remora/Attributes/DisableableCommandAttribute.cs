using Remora.Commands.Conditions;

namespace Olwe.Remora.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisableableCommandAttribute : ConditionAttribute
{
    
}