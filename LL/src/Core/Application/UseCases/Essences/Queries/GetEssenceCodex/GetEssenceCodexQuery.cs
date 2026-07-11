using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Essences.Queries.GetEssenceCodex;

public record GetEssenceCodexQuery(Guid CharacterId) : IRequest<EssenceCodexDto>;

public sealed class GetEssenceCodexQueryHandler : IRequestHandler<GetEssenceCodexQuery, EssenceCodexDto>
{
    private readonly IMapper _mapper;
    private readonly ICreatureArchiveService _service;

    public GetEssenceCodexQueryHandler(IMapper mapper, ICreatureArchiveService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<EssenceCodexDto> Handle(GetEssenceCodexQuery request, CancellationToken cancellationToken) =>
        _mapper.Map<EssenceCodexDto>(await _service.GetEssenceCodexAsync(request.CharacterId, cancellationToken));
}
