using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.DonateGuildVaultItem;

public record DonateGuildVaultItemCommand(Guid CharacterId, Guid EquipmentInstanceId) : ICommand<Response<bool>>;

public class DonateGuildVaultItemCommandHandler : IRequestHandler<DonateGuildVaultItemCommand, Response<bool>>
{
    private readonly IGuildVaultService _vault;
    private readonly IGameEventPublisher _events;
    private readonly IGameEventOutbox _outbox;
    private readonly IMapper _mapper;

    public DonateGuildVaultItemCommandHandler(
        IGuildVaultService vault,
        IGameEventPublisher events,
        IGameEventOutbox outbox,
        IMapper mapper)
    {
        _vault = vault;
        _events = events;
        _outbox = outbox;
        _mapper = mapper;
    }

    public async Task<Response<bool>> Handle(DonateGuildVaultItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _vault.DonateAsync(request.CharacterId, request.EquipmentInstanceId, cancellationToken);
        if (!result.Succeeded) return Response<bool>.Fail(result.Error ?? "Failed to donate equipment.");

        var mutation = result.Value!;
        var equipment = _mapper.Map<EquipmentInstanceDto>(mutation.Equipment);
        var messageId = Guid.NewGuid();
        var sentAt = DateTimeOffset.UtcNow;
        await _outbox.EnqueueAsync(
            GameEventTypes.GuildVaultChatMessage,
            new GuildVaultChatMessagePayload(
                mutation.GuildId,
                mutation.CharacterId,
                mutation.CharacterName,
                "donated",
                equipment,
                messageId,
                sentAt),
            request.CharacterId,
            null,
            cancellationToken);

        await _events.PublishAsync(
            new Audience.Guild(mutation.GuildId),
            new GuildVaultChatMessageMsg(
                mutation.GuildId,
                messageId,
                mutation.CharacterId,
                mutation.CharacterName,
                "donated",
                equipment,
                sentAt));

        await _events.PublishAsync(
            new Audience.Guild(mutation.GuildId),
            new GuildStateChangedMsg(mutation.GuildId));
        return Response<bool>.Success(true);
    }
}
