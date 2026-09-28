using AutoMapper;
using Domain.Models.Administration;
using Application.UseCases.Administration.Dtos;

namespace Application.UseCases.Administration.Mappings;
public sealed class SupportCaseMappingProfile : Profile
{
    public SupportCaseMappingProfile()
    {
        CreateMap<SupportCase, SupportCaseDto>(); CreateMap<SupportCaseEntry, SupportCaseEntryDto>();
        CreateMap<SupportCasePage, SupportCasePageDto>(); CreateMap<SupportCaseDetails, SupportCaseDetailsDto>();
    }
    public static global::Common.Primitives.Response<SupportCaseDetailsDto> Map(global::Common.Primitives.Response<SupportCaseDetails> result, IMapper mapper) =>
        result.IsSuccess ? global::Common.Primitives.Response<SupportCaseDetailsDto>.Success(mapper.Map<SupportCaseDetailsDto>(result.Data)) :
        result.IsConflict ? global::Common.Primitives.Response<SupportCaseDetailsDto>.Conflict(result.ErrorMessage, result.ErrorCode) :
        global::Common.Primitives.Response<SupportCaseDetailsDto>.Fail(result.ErrorMessage, result.ErrorCode);
}
