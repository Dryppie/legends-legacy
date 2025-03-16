using Application.Common.Responses;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Essences.Queries.GetEquippedEssencesAndInventoryEssences;
public record GetEquippedEssencesAndInventoryEssencesQuery(Guid CharacterId) : IRequest<Response<EquippedEssencesAndInventoryEssencesDto>>;

public class GetEquippedEssencesAndInventoryEssencesQueryHandler : IRequestHandler<GetEquippedEssencesAndInventoryEssencesQuery, Response<EquippedEssencesAndInventoryEssencesDto>>
{
    private readonly IEssenceService _essenceService;
    private readonly IMapper _mapper;


    public GetEquippedEssencesAndInventoryEssencesQueryHandler(IEssenceService essenceService, IMapper mapper)
    {
        _essenceService = essenceService;
        _mapper = mapper;
    }

    public async Task<Response<EquippedEssencesAndInventoryEssencesDto>> Handle(GetEquippedEssencesAndInventoryEssencesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var equippedEssencesAndInventoryEssences = await _essenceService.GetEquippedEssencesAndInventoryEssences(request.CharacterId, cancellationToken);
            var equippedEssencesAndInventoryEssencesDto = _mapper.Map<EquippedEssencesAndInventoryEssencesDto>(equippedEssencesAndInventoryEssences);

            return Response<EquippedEssencesAndInventoryEssencesDto>.Success(equippedEssencesAndInventoryEssencesDto);
        }
        catch (Exception)
        {
            return Response<EquippedEssencesAndInventoryEssencesDto>.Fail("Error getting equipped essences and inventory essences");
        }
        
    }
}