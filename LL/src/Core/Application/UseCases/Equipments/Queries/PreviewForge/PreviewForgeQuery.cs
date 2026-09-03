using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using Domain.Models.Items.Equipments.Progression;
using MediatR;

namespace Application.UseCases.Equipments.Queries.PreviewForge;

public sealed record PreviewForgeQuery(Guid CharacterId, ForgeRequest Operation) : IQuery<ForgeQuoteDto>;
public sealed class PreviewForgeQueryHandler(IForgeService service, IMapper mapper)
    : IRequestHandler<PreviewForgeQuery, ForgeQuoteDto>
{
    public async Task<ForgeQuoteDto> Handle(PreviewForgeQuery request, CancellationToken ct) =>
        mapper.Map<ForgeQuoteDto>(await service.PreviewAsync(request.CharacterId, request.Operation, ct));
}
