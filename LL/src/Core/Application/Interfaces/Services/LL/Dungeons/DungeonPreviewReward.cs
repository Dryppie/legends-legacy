using Domain.Models.Items;

namespace Application.Interfaces.Services.LL.Dungeons;

public sealed record DungeonPreviewReward(
    ItemBase ItemBase,
    string Category,
    string Source,
    int MinQuantity = 1,
    int MaxQuantity = 1,
    double? DropChancePercent = null,
    bool CanDropNothing = false,
    double? NoDropChancePercent = null);
