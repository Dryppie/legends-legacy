using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Essences.Queries.GetEquippedEssences;
public record GetEquippedEssencesQuery(Guid CharacterId) : IQuery<List<EssenceSlotDto>>;

public class GetEquippedEssencesQueryHandler : IRequestHandler<GetEquippedEssencesQuery, List<EssenceSlotDto>>
{
    private readonly IEssenceService _essenceService;
    private readonly IMapper _mapper;


    public GetEquippedEssencesQueryHandler(IEssenceService essenceService, IMapper mapper)
    {
        _essenceService = essenceService;
        _mapper = mapper;
    }

    public async Task<List<EssenceSlotDto>> Handle(GetEquippedEssencesQuery request, CancellationToken cancellationToken)
    {
        var equippedEssences = await _essenceService.GetEquippedEssences(request.CharacterId, cancellationToken);

        return _mapper.Map<List<EssenceSlotDto>>(equippedEssences);
    }
}