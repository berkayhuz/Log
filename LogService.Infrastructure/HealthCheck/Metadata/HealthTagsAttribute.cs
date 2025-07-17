namespace LogService.Infrastructure.HealthCheck.Metadata;
using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class HealthTagsAttribute(params string[] tags) : Attribute
{
    public string[] Tags { get; } = tags;
}
