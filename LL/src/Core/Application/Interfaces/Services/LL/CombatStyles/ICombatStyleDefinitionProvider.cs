using Domain.Models.CombatStyles;

namespace Application.Interfaces.Services.LL.CombatStyles;

public interface ICombatStyleDefinitionProvider
{
    IReadOnlyCollection<CombatStyleDefinition> GetAll();
    CombatStyleDefinition? GetById(string styleId);
    CombatStyleFocusDefinition? GetFocus(string styleId, string focusId);
}
