namespace LogService.Infrastructure.HealthCheck.Metadata;
using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class NameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
