using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Essences.Queries.GetActiveEssenceLoadout;

public record GetActiveEssenceLoadoutQuery(Guid CharacterId) : IQuery<EssenceLoadoutDto?>;

public class GetActiveEssenceLoadoutQueryHandler : IRequestHandler<GetActiveEssenceLoadoutQuery, EssenceLoadoutDto?>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;

    public GetActiveEssenceLoadoutQueryHandler(IMapper mapper, IEssenceService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<EssenceLoadoutDto?> Handle(GetActiveEssenceLoadoutQuery request, CancellationToken cancellationToken) =>
        _mapper.Map<EssenceLoadoutDto?>(await _service.GetActiveLoadoutAsync(request.CharacterId, cancellationToken));
}
