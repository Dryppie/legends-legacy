using Application.Interfaces.Services.LL.Prophecies;
using Application.MediatR.Markers;
using Application.UseCases.Prophecies.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Prophecies.Commands.AssembleProphecySigil;

public sealed record AssembleProphecySigilCommand(
    Guid PlayerId,
    Guid CharacterId,
    string SigilItemId) : ICommand<Response<ProphecySigilForgeResponseDto>>;

public sealed class AssembleProphecySigilCommandHandler
    : IRequestHandler<AssembleProphecySigilCommand, Response<ProphecySigilForgeResponseDto>>
{
    private readonly IProphecyService _prophecyService;
    private readonly IMapper _mapper;

    public AssembleProphecySigilCommandHandler(IProphecyService prophecyService, IMapper mapper)
    {
        _prophecyService = prophecyService;
        _mapper = mapper;
    }

    public async Task<Response<ProphecySigilForgeResponseDto>> Handle(
        AssembleProphecySigilCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _prophecyService.AssembleSigilAsync(
            request.PlayerId,
            request.CharacterId,
            request.SigilItemId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return result.Succeeded && result.Value is not null
            ? Response<ProphecySigilForgeResponseDto>.Success(_mapper.Map<ProphecySigilForgeResponseDto>(result.Value))
            : Response<ProphecySigilForgeResponseDto>.Fail(result.Error ?? "Could not assemble the dungeon sigil.");
    }
}
