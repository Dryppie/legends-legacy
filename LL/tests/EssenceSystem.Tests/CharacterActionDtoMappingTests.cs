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
    public void Explicit_due_state_maps_to_new_and_compatibility_contracts()
    {
        var action = new CharacterAction
        {
            CharacterId = Guid.NewGuid(),
            ActionDetails = new CombatActionDetails(),
            UpdatedAt = DateTimeOffset.Parse("2026-08-17T12:00:00Z"),
            NextResolutionAtUtc = DateTimeOffset.Parse("2026-08-17T12:00:10Z"),
            HasMoreDueWork = true,
            ProcessedCount = 100,
            ResolutionIntervalMs = 10_000
        };

        var dto = _mapper.Map<CharacterActionDto>(action);

        Assert.True(dto.HasPendingCombatResolution);
        Assert.True(dto.HasMoreDueWork);
        Assert.Equal(action.NextResolutionAtUtc, dto.NextResolutionAtUtc);
        Assert.Equal(action.NextResolutionAtUtc, dto.NextResolutionAt);
        Assert.Equal(100, dto.ProcessedCount);
        Assert.Equal(10_000, dto.ResolutionIntervalMs);
    }

    [Fact]
    public void Explicit_not_due_state_does_not_report_pending_resolution()
    {
        var action = new CharacterAction
        {
            CharacterId = Guid.NewGuid(),
            ActionDetails = new CombatActionDetails(),
            UpdatedAt = DateTimeOffset.Parse("2026-08-17T12:00:00Z"),
            NextResolutionAtUtc = DateTimeOffset.Parse("2026-08-17T12:00:10Z"),
            HasMoreDueWork = false
        };

        var dto = _mapper.Map<CharacterActionDto>(action);

        Assert.False(dto.HasPendingCombatResolution);
    }
}
