using Application.Common.Mappings;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Domain.Models.Guilds;

namespace Application.UseCases.Guilds.Dtos.Responses;

public class GuildVaultItemDto : IMapFrom<GuildVaultItem>
{
    public Guid Id { get; set; }
    public EquipmentInstanceDto Equipment { get; set; } = null!;
    public Guid DonatedByCharacterId { get; set; }
    public string DonatedByName { get; set; } = string.Empty;
    public DateTimeOffset DonatedAt { get; set; }
    public Guid? BorrowedByCharacterId { get; set; }
    public string? BorrowedByName { get; set; }
    public DateTimeOffset? BorrowedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<GuildVaultItem, GuildVaultItemDto>()
            .ForMember(x => x.Equipment, options => options.MapFrom(x => x.EquipmentInstance))
            .ForMember(x => x.DonatedByName, options => options.MapFrom(x => x.DonatedByCharacter.Name))
            .ForMember(x => x.BorrowedByName, options => options.MapFrom(x => x.BorrowedByCharacter == null ? null : x.BorrowedByCharacter.Name));
    }
}
