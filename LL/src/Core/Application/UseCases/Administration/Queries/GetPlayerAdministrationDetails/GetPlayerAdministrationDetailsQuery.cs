using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Administration.Queries.GetPlayerAdministrationDetails;

public sealed record GetPlayerAdministrationDetailsQuery(
    Guid CharacterId,
    int HistoryLimit = 50)
    : IQuery<Response<PlayerAdministrationDetailsDto>>;

public sealed class GetPlayerAdministrationDetailsQueryHandler(
    ILiveOpsService liveOps,
    IChatModerationGateway chat,
    IMapper mapper)
    : IRequestHandler<GetPlayerAdministrationDetailsQuery, Response<PlayerAdministrationDetailsDto>>
{
    public async Task<Response<PlayerAdministrationDetailsDto>> Handle(
        GetPlayerAdministrationDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.CharacterId == Guid.Empty)
        {
            return Response<PlayerAdministrationDetailsDto>.Fail(
                "A character ID is required.");
        }

        var player = await liveOps.GetPlayerAsync(
            request.CharacterId,
            cancellationToken);
        if (player is null)
        {
            return Response<PlayerAdministrationDetailsDto>.Fail(
                "The target player was not found.");
        }

        var historyLimit = Math.Clamp(request.HistoryLimit, 1, 100);
        var administrationHistory = await liveOps.GetHistoryAsync(
            player.AccountId,
            player.CharacterId,
            historyLimit,
            cancellationToken);
        var chatState = await chat.GetStateAsync(
            player.CharacterId,
            historyLimit,
            cancellationToken);

        var activeMute = chatState.ActiveMute is null
            ? null
            : new ChatRestrictionDto(
                chatState.ActiveMute.Id,
                chatState.ActiveMute.CharacterId,
                chatState.ActiveMute.Reason,
                chatState.ActiveMute.CreatedBySubject,
                chatState.ActiveMute.CreatedAt,
                chatState.ActiveMute.ExpiresAt,
                chatState.ActiveMute.RevokedBySubject,
                chatState.ActiveMute.RevokedAt,
                chatState.ActiveMute.RevocationReason);
        var chatHistory = chatState.History
            .Select(x => new ChatModerationHistoryDto(
                x.OperationId,
                x.ActionType,
                x.CharacterId,
                x.RestrictionId,
                x.ActorSubject,
                x.ActorDisplayName,
                x.Reason,
                x.OccurredAt))
            .ToList();

        return Response<PlayerAdministrationDetailsDto>.Success(
            new PlayerAdministrationDetailsDto(
                mapper.Map<PlayerAdministrationDto>(player),
                activeMute,
                chatState.IsSuccess,
                chatState.IsSuccess ? null : chatState.ErrorMessage,
                mapper.Map<IReadOnlyList<AdministrationHistoryDto>>(administrationHistory),
                chatHistory));
    }
}
