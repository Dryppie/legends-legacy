using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Essences.Queries.GetEssenceLoadouts;

public record GetEssenceLoadoutsQuery(Guid CharacterId) : IQuery<EssenceLoadoutsDto>;

public class GetEssenceLoadoutsQueryHandler : IRequestHandler<GetEssenceLoadoutsQuery, EssenceLoadoutsDto>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;

    public GetEssenceLoadoutsQueryHandler(IMapper mapper, IEssenceService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<EssenceLoadoutsDto> Handle(GetEssenceLoadoutsQuery request, CancellationToken cancellationToken) =>
        _mapper.Map<EssenceLoadoutsDto>(await _service.GetLoadoutsAsync(request.CharacterId, cancellationToken));
}
