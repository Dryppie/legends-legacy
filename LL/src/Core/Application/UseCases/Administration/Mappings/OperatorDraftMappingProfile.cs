using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;

namespace Application.UseCases.Administration.Mappings;

public sealed class OperatorDraftMappingProfile : Profile
{
    public OperatorDraftMappingProfile() => CreateMap<OperatorDraft, OperatorDraftDto>();
    public static Response<OperatorDraftDto> Map(Response<OperatorDraft> response, IMapper mapper) =>
        response.IsSuccess ? Response<OperatorDraftDto>.Success(mapper.Map<OperatorDraftDto>(response.Data)) :
        response.IsConflict ? Response<OperatorDraftDto>.Conflict(response.ErrorMessage, response.ErrorCode) :
        Response<OperatorDraftDto>.Fail(response.ErrorMessage, response.ErrorCode);
}
