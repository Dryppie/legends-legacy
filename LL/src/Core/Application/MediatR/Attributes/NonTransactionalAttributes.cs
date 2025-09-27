namespace Application.MediatR.Attributes;
[AttributeUsage(AttributeTargets.Class)]
public sealed class NonTransactionalAttribute : Attribute { }

// Add [NonTransactional] to a command to opt out of the transaction behavior