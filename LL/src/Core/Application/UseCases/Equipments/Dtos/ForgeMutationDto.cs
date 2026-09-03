using Application.Common.Mappings;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Equipments.Dtos;

public sealed class ForgeMutationDto : IMapFrom<ForgeResult>
{
    public ForgeOutcomeDto? Outcome { get; set; }
    public ForgeQuoteDto? FreshQuote { get; set; }
    public void Mapping(Profile profile) => profile.CreateMap<ForgeResult, ForgeMutationDto>();
    public static Response<ForgeMutationDto> From(ForgeResult result, IMapper mapper) => new()
    {
        IsSuccess = result.Outcome != null, Data = mapper.Map<ForgeMutationDto>(result),
        ErrorMessage = result.Error ?? string.Empty, IsConflict = result.FreshQuote != null,
        ErrorCode = result.FreshQuote != null ? "forge_quote_changed_or_unavailable" : Response<ForgeMutationDto>.DefaultErrorCode
    };
}
