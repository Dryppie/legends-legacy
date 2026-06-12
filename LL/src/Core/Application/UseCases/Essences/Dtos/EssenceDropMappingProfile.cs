using AutoMapper;
using Domain.Models.Essences.Definitions;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceDropMappingProfile : Profile
{
    public EssenceDropMappingProfile()
    {
        CreateMap<EssenceDropDefinition, EssenceDropDto>();
    }
}
