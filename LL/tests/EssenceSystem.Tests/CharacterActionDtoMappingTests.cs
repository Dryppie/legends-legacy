using Application.Common.Mappings;
using Application.UseCases.CharacterActions.Dtos.Responses;
using AutoMapper;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class CharacterActionDtoMappingTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<MappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Due_combat_action_reports_pending_resolution()
    {
        var action = new CharacterAction
        {
            CharacterId = Guid.NewGuid(),
            ActionDetails = new CombatActionDetails(),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        var dto = _mapper.Map<CharacterActionDto>(action);

        Assert.True(dto.HasPendingCombatResolution);
    }

    [Fact]
    public void Future_combat_action_does_not_report_pending_resolution()
    {
        var action = new CharacterAction
        {
            CharacterId = Guid.NewGuid(),
            ActionDetails = new CombatActionDetails(),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(1)
        };

        var dto = _mapper.Map<CharacterActionDto>(action);

        Assert.False(dto.HasPendingCombatResolution);
    }
}
