using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.SpendEssenceDust;

public record SpendEssenceDustCommand(Guid CharacterId, Guid PlayerEssenceId, int DustAmount) : ICommand<Response<SpendEssenceDustResultDto>>;

public class SpendEssenceDustCommandHandler : IRequestHandler<SpendEssenceDustCommand, Response<SpendEssenceDustResultDto>>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;

    public SpendEssenceDustCommandHandler(IMapper mapper, IEssenceService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<Response<SpendEssenceDustResultDto>> Handle(SpendEssenceDustCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.SpendEssenceDustAsync(request.CharacterId, request.PlayerEssenceId, request.DustAmount, cancellationToken);
        var dto = _mapper.Map<SpendEssenceDustResultDto>(result);
        return result.Succeeded ? Response<SpendEssenceDustResultDto>.Success(dto) : Response<SpendEssenceDustResultDto>.Fail(result.Message);
    }
}
