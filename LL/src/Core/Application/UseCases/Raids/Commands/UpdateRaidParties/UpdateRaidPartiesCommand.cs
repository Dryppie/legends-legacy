using Application.Interfaces.Services.LL.Raids;
using Application.MediatR.Markers;
using Application.UseCases.Raids.Dtos;
using Common.Primitives;
using Domain.Models.Raids;
using MediatR;

namespace Application.UseCases.Raids.Commands.UpdateRaidParties;

public sealed record UpdateRaidPartiesCommand(
    Guid CharacterId,
    Guid RaidRunId,
    IReadOnlyList<RaidPartyAssignment> Assignments)
    : ICommand<Response<RaidRunDto>>;

public sealed class UpdateRaidPartiesCommandHandler(IRaidService raids)
    : IRequestHandler<UpdateRaidPartiesCommand, Response<RaidRunDto>>
{
    public async Task<Response<RaidRunDto>> Handle(
        UpdateRaidPartiesCommand request,
        CancellationToken cancellationToken)
    {
        var result = await raids.UpdatePartiesAsync(
            request.CharacterId,
            request.RaidRunId,
            request.Assignments,
            cancellationToken);
        return result.Succeeded
            ? Response<RaidRunDto>.Success(result.Value!)
            : Response<RaidRunDto>.Fail(result.Error!);
    }
}
