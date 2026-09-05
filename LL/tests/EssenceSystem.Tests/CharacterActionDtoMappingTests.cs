using System.Text.Json;
using Application.Common.Mappings;
using Application.UseCases.CharacterActions.Dtos.Responses;
using AutoMapper;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Items.Equipments;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class CharacterActionDtoMappingTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<MappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Explicit_due_state_maps_to_the_current_contract()
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

        Assert.True(dto.HasMoreDueWork);
        Assert.Equal(action.NextResolutionAtUtc, dto.NextResolutionAtUtc);
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

        Assert.False(dto.HasMoreDueWork);
    }

    [Fact]
    public void Json_contract_exposes_only_explicit_schedule_fields()
    {
        var dto = new CharacterActionDto
        {
            UpdatedAt = DateTimeOffset.Parse("2026-08-24T12:00:00Z"),
            NextResolutionAtUtc = DateTimeOffset.Parse("2026-08-24T12:00:10Z"),
            HasMoreDueWork = true
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(
            dto,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var contract = document.RootElement;

        Assert.True(contract.TryGetProperty("nextResolutionAtUtc", out _));
        Assert.True(contract.TryGetProperty("hasMoreDueWork", out _));
        Assert.False(contract.TryGetProperty("returnToCombatAreaId", out _));
        Assert.False(contract.TryGetProperty("nextResolutionAt", out _));
        Assert.False(contract.TryGetProperty("hasPendingCombatResolution", out _));
    }

}
