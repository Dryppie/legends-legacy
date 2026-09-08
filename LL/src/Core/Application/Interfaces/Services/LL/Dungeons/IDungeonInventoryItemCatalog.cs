namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonInventoryItemCatalog
{
    bool AffectsDungeonAvailability(string itemBaseId);
}
