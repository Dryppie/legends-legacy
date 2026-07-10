using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Essences.Queries.GetCreatureArchive;

public record GetCreatureArchiveQuery(Guid CharacterId) : IRequest<CreatureArchiveDto>;

public sealed class GetCreatureArchiveQueryHandler : IRequestHandler<GetCreatureArchiveQuery, CreatureArchiveDto>
{
    private readonly IMapper _mapper;
    private readonly ICreatureArchiveService _service;

    public GetCreatureArchiveQueryHandler(IMapper mapper, ICreatureArchiveService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<CreatureArchiveDto> Handle(GetCreatureArchiveQuery request, CancellationToken cancellationToken) =>
        _mapper.Map<CreatureArchiveDto>(await _service.GetCreatureArchiveAsync(request.CharacterId, cancellationToken));
}
