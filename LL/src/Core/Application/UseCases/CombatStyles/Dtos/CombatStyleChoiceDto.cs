using Domain.Models.CombatStyles;
using Domain.Models.Essences;

namespace Application.UseCases.CombatStyles.Dtos;

public sealed record CombatStyleChoiceDto(string Id, string Name, string Description, CombatStyleTuning? Tuning,
    string MasteryDescription);
