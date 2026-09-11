using Application.Interfaces.Services.LL.Nobility;
using Application.MediatR.Markers;
using Application.UseCases.Nobility.Dtos;
using Application.UseCases.Nobility.Mappings;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Nobility.Queries.PreviewSignetRedemption;

public sealed record PreviewSignetRedemptionQuery(Guid AccountId, Guid CharacterId, int Quantity) : IQuery<Response<SignetPreviewDto>>;

public sealed class PreviewSignetRedemptionQueryHandler(INobilityService service, IMapper mapper)
    : IRequestHandler<PreviewSignetRedemptionQuery, Response<SignetPreviewDto>>
{
    public async Task<Response<SignetPreviewDto>> Handle(PreviewSignetRedemptionQuery request, CancellationToken ct) =>
        NobilityMappingProfile.MapResult<SignetPreview, SignetPreviewDto>(
            await service.PreviewAsync(request.AccountId, request.CharacterId, request.Quantity, ct), mapper);
}
