using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.DismantleUnboundEssence;

public record DismantleUnboundEssenceCommand(Guid CharacterId, Guid InventoryItemId) : ICommand<Response<DismantleEssenceResultDto>>;

public class DismantleUnboundEssenceCommandHandler : IRequestHandler<DismantleUnboundEssenceCommand, Response<DismantleEssenceResultDto>>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;

    public DismantleUnboundEssenceCommandHandler(IMapper mapper, IEssenceService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<Response<DismantleEssenceResultDto>> Handle(DismantleUnboundEssenceCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.DismantleUnboundEssenceAsync(request.CharacterId, request.InventoryItemId, cancellationToken);
        var dto = _mapper.Map<DismantleEssenceResultDto>(result);
        return result.Succeeded ? Response<DismantleEssenceResultDto>.Success(dto) : Response<DismantleEssenceResultDto>.Fail(result.Message);
    }
}
