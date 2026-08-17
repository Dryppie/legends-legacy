using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.UnmuteChat;

[NonTransactional]
public sealed record UnmuteChatCommand(
    Guid OperationId,
    Guid RestrictionId,
    AdministrationActor Actor,
    string Reason) : ICommand<Response<ChatModerationResultDto>>;

public sealed class UnmuteChatCommandHandler(IChatModerationGateway gateway)
    : IRequestHandler<UnmuteChatCommand, Response<ChatModerationResultDto>>
{
    public async Task<Response<ChatModerationResultDto>> Handle(
        UnmuteChatCommand request,
        CancellationToken cancellationToken)
    {
        var result = await gateway.UnmuteAsync(
            new ChatUnmuteGatewayRequest(
                request.OperationId,
                request.RestrictionId,
                request.Actor.Subject,
                request.Actor.DisplayName,
                request.Reason),
            cancellationToken);
        return result.IsSuccess && result.RestrictionId.HasValue
            ? Response<ChatModerationResultDto>.Success(
                new ChatModerationResultDto(
                    result.RestrictionId.Value,
                    result.WasAlreadyProcessed))
            : Response<ChatModerationResultDto>.Fail(result.ErrorMessage);
    }
}
