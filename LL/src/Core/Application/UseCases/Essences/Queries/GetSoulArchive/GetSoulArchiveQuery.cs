using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Essences.Queries.GetSoulArchive;

public record GetSoulArchiveQuery(Guid CharacterId) : IQuery<SoulArchiveDto>;

public class GetSoulArchiveQueryHandler : IRequestHandler<GetSoulArchiveQuery, SoulArchiveDto>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;

    public GetSoulArchiveQueryHandler(IMapper mapper, IEssenceService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<SoulArchiveDto> Handle(GetSoulArchiveQuery request, CancellationToken cancellationToken) =>
        _mapper.Map<SoulArchiveDto>(await _service.GetSoulArchiveAsync(request.CharacterId, cancellationToken));
}
