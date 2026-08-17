using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.MuteChat;

[NonTransactional]
public sealed record MuteChatCommand(
    Guid OperationId,
    Guid CharacterId,
    AdministrationActor Actor,
    string Reason,
    DateTimeOffset? ExpiresAt) : ICommand<Response<ChatModerationResultDto>>;

public sealed class MuteChatCommandHandler(IChatModerationGateway gateway)
    : IRequestHandler<MuteChatCommand, Response<ChatModerationResultDto>>
{
    public async Task<Response<ChatModerationResultDto>> Handle(
        MuteChatCommand request,
        CancellationToken cancellationToken)
    {
        var result = await gateway.MuteAsync(
            new ChatMuteGatewayRequest(
                request.OperationId,
                request.CharacterId,
                request.Actor.Subject,
                request.Actor.DisplayName,
                request.Reason,
                request.ExpiresAt),
            cancellationToken);
        return result.IsSuccess && result.RestrictionId.HasValue
            ? Response<ChatModerationResultDto>.Success(
                new ChatModerationResultDto(
                    result.RestrictionId.Value,
                    result.WasAlreadyProcessed))
            : Response<ChatModerationResultDto>.Fail(result.ErrorMessage);
    }
}
