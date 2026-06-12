using AutoMapper;
using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;

public sealed class SaveEssenceLoadoutMappingProfile : Profile
{
    public SaveEssenceLoadoutMappingProfile()
    {
        CreateMap<SaveEssenceLoadoutDto, SaveEssenceLoadoutRequest>();
        CreateMap<SaveEssenceLoadoutSlotDto, SaveEssenceLoadoutSlotRequest>();
    }
}
