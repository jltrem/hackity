using System;

namespace RoslynCodeGen;

[AttributeUsage(AttributeTargets.Class)]
public class WrappedValidationAttribute : Attribute
{
    public Type TargetType { get; }

    public WrappedValidationAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}