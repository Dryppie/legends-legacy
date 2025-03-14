using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Essences.Queries.GetEquippedEssencesAndInventoryEssences;
public record GetEquippedEssencesAndInventoryEssencesQuery(Guid CharacterId) : IRequest<EquippedEssencesAndInventoryEssencesDto>;

public class GetEquippedEssencesAndInventoryEssencesQueryHandler : IRequestHandler<GetEquippedEssencesAndInventoryEssencesQuery, EquippedEssencesAndInventoryEssencesDto>
{
    private readonly IEssenceService _essenceService;
    private readonly IMapper _mapper;


    public GetEquippedEssencesAndInventoryEssencesQueryHandler(IEssenceService essenceService, IMapper mapper)
    {
        _essenceService = essenceService;
        _mapper = mapper;
    }

    public async Task<EquippedEssencesAndInventoryEssencesDto> Handle(GetEquippedEssencesAndInventoryEssencesQuery request, CancellationToken cancellationToken)
    {
        var equippedEssencesAndInventoryEssences = await _essenceService.GetEquippedEssencesAndInventoryEssences(request.CharacterId, cancellationToken);

        return _mapper.Map<EquippedEssencesAndInventoryEssencesDto>(equippedEssencesAndInventoryEssences);
    }
}