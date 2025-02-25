using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;
public class EssenceDetailsDto : IMapFrom<Essence>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AbilityDescriptionDto Active { get; set; } = null!;
    public AbilityDescriptionDto Passive { get; set; } = null!;
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Essence, EssenceDetailsDto>();
    }
}
