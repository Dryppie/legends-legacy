using Application.Common.Mappings;
using Application.UseCases.Guilds.Dtos.Responses;
using AutoMapper;
using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Domain.Models.Items.Equipments;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class GuildDtoMappingTests
{
    [Fact]
    public void GuildMapsRolePermissionsAndVaultItems()
    {
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var guildId = Guid.NewGuid();
        var donor = new Character { Id = Guid.NewGuid(), Name = "Donor" };
        var equipment = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = "vault_helm",
            ItemBase = new EquipmentBase
            {
                Id = "vault_helm",
                Name = "Vault Helm",
                EquipmentType = EquipmentType.Head
            }
        };
        var guild = new Guild
        {
            Id = guildId,
            Name = "Mapping Guild",
            OwnerId = donor.Id,
            Owner = donor
        };
        guild.RolePermissions.Add(GuildRolePermission.CreateDefault(guildId, GuildRole.Member));
        guild.VaultItems.Add(new GuildVaultItem
        {
            GuildId = guildId,
            Guild = guild,
            EquipmentInstanceId = equipment.Id,
            EquipmentInstance = equipment,
            DonatedByCharacterId = donor.Id,
            DonatedByCharacter = donor
        });

        var dto = mapper.Map<GuildDto>(guild);

        Assert.Single(dto.RolePermissions);
        Assert.True(dto.RolePermissions[0].CanBorrowVault);
        Assert.Single(dto.VaultItems);
        Assert.Equal("Donor", dto.VaultItems[0].DonatedByName);
        Assert.Equal(equipment.Id, dto.VaultItems[0].Equipment.Id);
    }
}
