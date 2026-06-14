using Domain.Models.Items;

namespace Application.Interfaces.Services.LL.Dungeons;

public sealed record DungeonPreviewReward(
    ItemBase ItemBase,
    string Category,
    string Source);
