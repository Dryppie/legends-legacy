using AutoMapper;
using Domain.Models.Essences.Definitions;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceEvolutionMappingProfile : Profile
{
    public EssenceEvolutionMappingProfile()
    {
        CreateMap<EssenceEvolutionDefinition, EssenceEvolutionDto>();
    }
}
