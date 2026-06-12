using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Essences.Queries.GetEssenceCatalog;

public record GetEssenceCatalogQuery : IQuery<EssenceCatalogDto>;

public class GetEssenceCatalogQueryHandler : IRequestHandler<GetEssenceCatalogQuery, EssenceCatalogDto>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;

    public GetEssenceCatalogQueryHandler(IMapper mapper, IEssenceService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<EssenceCatalogDto> Handle(GetEssenceCatalogQuery request, CancellationToken cancellationToken) =>
        _mapper.Map<EssenceCatalogDto>(await _service.GetCatalogAsync(cancellationToken));
}
