using Application.Common.Mappings;
using Application.UseCases.Equipments.Queries.GetLinkedEquipment;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class LinkedEquipmentTests
{
    [Fact]
    public async Task Link_reads_persisted_equipment_stats_without_owner_preferences_and_expires_on_deletion()
    {
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var item = ProgressionTestEquipment.Create(rarity: Rarity.Epic,
            stats: new Dictionary<AttributeType, float> { [AttributeType.Power] = 42 });
        item.IsFavorite = true;
        db.ItemInstances.Add(item);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        var service = new EquipmentSlotService(new EquipmentSlotRepository(db), null!);
        var handler = new GetLinkedEquipmentQueryHandler(service, mapper);
        var dto = await handler.Handle(new(item.Id), default);

        Assert.NotNull(dto);
        Assert.Equal(item.Id, dto.Id);
        Assert.Equal(item.DisplayName, dto.DisplayName);
        Assert.Equal(Rarity.Epic, dto.Rarity);
        Assert.NotNull(dto.Progression);
        Assert.Contains(dto.AttributeModifiers, modifier => modifier.AttributeType == AttributeType.Power && modifier.Amount == 42);
        Assert.False(dto.IsFavorite);
        Assert.Null(dto.GuildVaultItemId);
        Assert.Empty(db.ChangeTracker.Entries());

        var persisted = await db.ItemInstances.SingleAsync(x => x.Id == item.Id);
        db.ItemInstances.Remove(persisted);
        await db.SaveChangesAsync();
        Assert.Null(await handler.Handle(new(item.Id), default));
        Assert.Null(await handler.Handle(new(Guid.Empty), default));
    }
}
