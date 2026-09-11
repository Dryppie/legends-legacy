using Application.Interfaces.Services.LL.Nobility;
using Application.UseCases.Nobility.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Nobility;

namespace Application.UseCases.Nobility.Mappings;

public sealed class NobilityMappingProfile : Profile
{
    public NobilityMappingProfile()
    {
        CreateMap<NobilityStatus, NobilityStatusDto>();
        CreateMap<SignetPreview, SignetPreviewDto>();
        CreateMap<SignetRedemption, SignetRedemptionDto>();
        CreateMap<SignetIssuance, SignetGrantDto>();
    }

    internal static Response<TDto> MapResult<TSource, TDto>(Response<TSource> result, IMapper mapper) => new()
    {
        IsSuccess = result.IsSuccess,
        Data = result.IsSuccess ? mapper.Map<TDto>(result.Data) : default,
        ErrorMessage = result.ErrorMessage, ErrorCode = result.ErrorCode, IsConflict = result.IsConflict
    };
}
