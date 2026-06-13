using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.AbsorbUnboundEssence;

public record AbsorbUnboundEssenceCommand(Guid CharacterId, Guid InventoryItemId) : ICommand<Response<ResponseMessageDto>>;

public class AbsorbUnboundEssenceCommandHandler : IRequestHandler<AbsorbUnboundEssenceCommand, Response<ResponseMessageDto>>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;

    public AbsorbUnboundEssenceCommandHandler(IMapper mapper, IEssenceService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<Response<ResponseMessageDto>> Handle(AbsorbUnboundEssenceCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.AbsorbUnboundEssenceAsync(request.CharacterId, request.InventoryItemId, cancellationToken);
        var dto = _mapper.Map<ResponseMessageDto>(result);
        return result.Succeeded ? Response<ResponseMessageDto>.Success(dto) : Response<ResponseMessageDto>.Fail(result.Message);
    }
}
