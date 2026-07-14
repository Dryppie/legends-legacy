using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Prophecies;
using AutoMapper;

namespace Application.UseCases.Prophecies.Dtos;

public sealed class ProphecySigilForgeResponseDto : IMapFrom<ProphecySigilForgeResult>
{
    public string SigilItemId { get; set; } = string.Empty;
    public int InventoryQuantity { get; set; }
    public long SigilFragmentsRemaining { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ProphecySigilForgeResult, ProphecySigilForgeResponseDto>();
    }
}
