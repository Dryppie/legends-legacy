using Application.Interfaces.Services.LL;
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

namespace Application.UseCases.Guilds.Commands.WithdrawGuildVaultItem;

public record WithdrawGuildVaultItemCommand(Guid CharacterId, Guid VaultItemId) : ICommand<Response<bool>>;

public class WithdrawGuildVaultItemCommandHandler : IRequestHandler<WithdrawGuildVaultItemCommand, Response<bool>>
{
    private readonly IGuildVaultService _vault;
    private readonly IGuildService _guild;
    private readonly IGameEventPublisher _events;
    private readonly IGameEventOutbox _outbox;
    private readonly IMapper _mapper;

    public WithdrawGuildVaultItemCommandHandler(
        IGuildVaultService vault,
        IGuildService guild,
        IGameEventPublisher events,
        IGameEventOutbox outbox,
        IMapper mapper)
    {
        _vault = vault;
        _guild = guild;
        _events = events;
        _outbox = outbox;
        _mapper = mapper;
    }

    public async Task<Response<bool>> Handle(WithdrawGuildVaultItemCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guild.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        var result = await _vault.WithdrawAsync(request.CharacterId, request.VaultItemId, cancellationToken);
        if (!result.Succeeded) return Response<bool>.Fail(result.Error ?? "Failed to withdraw equipment.");

        var mutation = result.Value!;
        await _outbox.EnqueueAsync(
            GameEventTypes.GuildVaultChatMessage,
            new GuildVaultChatMessagePayload(
                mutation.GuildId,
                mutation.CharacterId,
                mutation.CharacterName,
                "withdrew",
                _mapper.Map<EquipmentInstanceDto>(mutation.Equipment),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow),
            request.CharacterId,
            null,
            cancellationToken);

        if (guild is not null)
            await _events.PublishAsync(new Audience.Guild(guild.Id), new GuildStateChangedMsg(guild.Id));
        return Response<bool>.Success(true);
    }
}
