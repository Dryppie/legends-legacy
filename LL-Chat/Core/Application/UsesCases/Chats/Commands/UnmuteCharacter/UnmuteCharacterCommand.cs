using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Dtos;
using MediatR;

namespace Application.UsesCases.Chats.Commands.UnmuteCharacter;

public sealed record UnmuteCharacterCommand(
    Guid OperationId,
    Guid RestrictionId,
    string ActorSubject,
    string ActorDisplayName,
    string Reason) : IRequest<ChatModerationDto?>;

public sealed class UnmuteCharacterCommandHandler(IChatModerationService moderation)
    : IRequestHandler<UnmuteCharacterCommand, ChatModerationDto?>
{
    public async Task<ChatModerationDto?> Handle(
        UnmuteCharacterCommand request,
        CancellationToken cancellationToken)
    {
        var result = await moderation.UnmuteAsync(
            request.OperationId,
            request.RestrictionId,
            request.ActorSubject,
            request.ActorDisplayName,
            request.Reason,
            cancellationToken);
        return result.IsSuccess && result.Restriction is not null
            ? new ChatModerationDto(
                result.Restriction.Id,
                result.WasAlreadyProcessed)
            : null;
    }
}
