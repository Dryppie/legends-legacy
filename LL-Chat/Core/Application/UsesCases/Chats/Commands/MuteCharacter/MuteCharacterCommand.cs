using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Dtos;
using MediatR;

namespace Application.UsesCases.Chats.Commands.MuteCharacter;

public sealed record MuteCharacterCommand(
    Guid OperationId,
    Guid CharacterId,
    string ActorSubject,
    string ActorDisplayName,
    string Reason,
    DateTimeOffset? ExpiresAt) : IRequest<ChatModerationDto?>;

public sealed class MuteCharacterCommandHandler(IChatModerationService moderation)
    : IRequestHandler<MuteCharacterCommand, ChatModerationDto?>
{
    public async Task<ChatModerationDto?> Handle(
        MuteCharacterCommand request,
        CancellationToken cancellationToken)
    {
        var result = await moderation.MuteAsync(
            request.OperationId,
            request.CharacterId,
            request.ActorSubject,
            request.ActorDisplayName,
            request.Reason,
            request.ExpiresAt,
            cancellationToken);
        return result.IsSuccess && result.Restriction is not null
            ? new ChatModerationDto(
                result.Restriction.Id,
                result.WasAlreadyProcessed)
            : null;
    }
}
