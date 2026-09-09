namespace Domain.Models.CombatStyles;

public sealed class CombatStyleConfigurationException(string message) : InvalidOperationException(message);
