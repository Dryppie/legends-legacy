using Domain.Models.Items;

namespace EssenceSystem.Tests;

public sealed class ItemTypeCompatibilityTests
{
    [Fact]
    public void PersistedItemTypeValuesRemainStable()
    {
        Assert.Equal(0, (int)ItemType.Equipment);
        Assert.Equal(2, (int)ItemType.Resource);
        Assert.Equal(3, (int)ItemType.Essence);
        Assert.Equal(4, (int)ItemType.Misc);
    }
}
