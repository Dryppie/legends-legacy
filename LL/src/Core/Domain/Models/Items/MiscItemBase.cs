namespace Domain.Models.Items;

/// <summary>Catalog items without Equipment or Essence behavior, such as Signets.</summary>
public sealed class MiscItemBase : ItemBase
{
    public MiscItemBase() => ItemType = ItemType.Misc;
}
