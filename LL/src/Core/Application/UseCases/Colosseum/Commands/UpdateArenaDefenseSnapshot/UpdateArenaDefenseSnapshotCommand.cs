using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using Application.UseCases.Colosseum.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Colosseum.Commands.UpdateArenaDefenseSnapshot;

public record UpdateArenaDefenseSnapshotCommand(Guid CharacterId) : ICommand<Response<ArenaDefenseStatusDto>>;

public sealed class UpdateArenaDefenseSnapshotCommandHandler
    : IRequestHandler<UpdateArenaDefenseSnapshotCommand, Response<ArenaDefenseStatusDto>>
{
    private readonly IColosseumService _colosseumService;
    private readonly IMapper _mapper;

    public UpdateArenaDefenseSnapshotCommandHandler(IColosseumService colosseumService, IMapper mapper)
    {
        _colosseumService = colosseumService;
        _mapper = mapper;
    }

    public async Task<Response<ArenaDefenseStatusDto>> Handle(UpdateArenaDefenseSnapshotCommand request, CancellationToken cancellationToken)
    {
        var snapshot = await _colosseumService.UpdateDefenseSnapshotAsync(request.CharacterId, cancellationToken);
        if (snapshot is null)
        {
            return Response<ArenaDefenseStatusDto>.Fail("Failed to update arena defense.");
        }

        return Response<ArenaDefenseStatusDto>.Success(_mapper.Map<ArenaDefenseStatusDto>(snapshot));
    }
}
