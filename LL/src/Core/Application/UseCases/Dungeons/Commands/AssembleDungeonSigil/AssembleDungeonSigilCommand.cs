using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using Application.UseCases.Dungeons.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.AssembleDungeonSigil;

public sealed record AssembleDungeonSigilCommand(
    Guid CharacterId,
    string DungeonId) : ICommand<Response<DungeonSigilAssemblyResponseDto>>;

public sealed class AssembleDungeonSigilCommandHandler(
    IDungeonSigilAssemblyService assemblyService,
    IMapper mapper)
    : IRequestHandler<AssembleDungeonSigilCommand, Response<DungeonSigilAssemblyResponseDto>>
{
    public async Task<Response<DungeonSigilAssemblyResponseDto>> Handle(
        AssembleDungeonSigilCommand request,
        CancellationToken cancellationToken)
    {
        var result = await assemblyService.AssembleAsync(
            request.CharacterId,
            request.DungeonId,
            cancellationToken);

        return result.Succeeded && result.Value is not null
            ? Response<DungeonSigilAssemblyResponseDto>.Success(
                mapper.Map<DungeonSigilAssemblyResponseDto>(result.Value))
            : Response<DungeonSigilAssemblyResponseDto>.Fail(
                result.Error ?? "Could not assemble the dungeon sigil.");
    }
}
