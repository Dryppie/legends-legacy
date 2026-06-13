using AutoMapper;
using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceOperationResultMappingProfile : Profile
{
    public EssenceOperationResultMappingProfile()
    {
        CreateMap<EssenceOperationResult, ResponseMessageDto>();
        CreateMap<DismantleEssenceResult, DismantleEssenceResultDto>();
        CreateMap<SpendEssenceDustResult, SpendEssenceDustResultDto>();
    }
}
