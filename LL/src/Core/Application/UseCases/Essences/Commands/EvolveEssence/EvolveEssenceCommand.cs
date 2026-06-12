using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.EvolveEssence;

public record EvolveEssenceCommand(Guid CharacterId, Guid PlayerEssenceId) : ICommand<Response<ResponseMessageDto>>;

public class EvolveEssenceCommandHandler : IRequestHandler<EvolveEssenceCommand, Response<ResponseMessageDto>>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;

    public EvolveEssenceCommandHandler(IMapper mapper, IEssenceService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<Response<ResponseMessageDto>> Handle(EvolveEssenceCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.EvolveEssenceAsync(request.CharacterId, request.PlayerEssenceId, cancellationToken);
        var dto = _mapper.Map<ResponseMessageDto>(result);
        return result.Succeeded ? Response<ResponseMessageDto>.Success(dto) : Response<ResponseMessageDto>.Fail(result.Message);
    }
}
