namespace Application.MediatR.Attributes;
[AttributeUsage(AttributeTargets.Class)]
public sealed class NonTransactionalAttribute : Attribute { }
