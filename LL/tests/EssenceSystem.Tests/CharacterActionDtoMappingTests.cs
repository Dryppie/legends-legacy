using Application.Common.Mappings;
using Application.UseCases.CharacterActions.Dtos.Responses;
using AutoMapper;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
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

    [Fact]
    public void Combat_action_maps_its_paused_tempering_queue_in_position_order()
    {
        var first = QueueItem(position: 0, "First");
        var second = QueueItem(position: 1, "Second");
        var action = new CharacterAction
        {
            CharacterId = Guid.NewGuid(),
            ActionDetails = new CombatActionDetails(),
            UpdatedAt = DateTimeOffset.Parse("2026-08-18T12:00:00Z"),
            PausedTemperingQueueItems = [second, first]
        };

        var dto = _mapper.Map<CharacterActionDto>(action);

        Assert.Equal([first.Id, second.Id], dto.TemperingQueueItems.Select(item => item.Id));
        Assert.Null(dto.CraftingActionDetails);
    }

    private static CraftingQueueItem QueueItem(int position, string name)
    {
        var equipmentBase = new EquipmentBase
        {
            Id = $"test-{position}",
            Name = name,
            EquipmentType = EquipmentType.OneHanded
        };
        var equipment = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = equipmentBase.Id,
            ItemBase = equipmentBase
        };
        return new CraftingQueueItem
        {
            Id = Guid.NewGuid(),
            Position = position,
            EquipmentInstanceId = equipment.Id,
            EquipmentInstance = equipment
        };
    }
}
