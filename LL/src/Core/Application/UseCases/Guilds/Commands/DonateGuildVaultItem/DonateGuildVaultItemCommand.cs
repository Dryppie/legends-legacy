using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Services.LL;
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
    private readonly IGuildService _guild;
    private readonly IGameEventOutbox _outbox;
    private readonly IMapper _mapper;

    public DonateGuildVaultItemCommandHandler(
        IGuildVaultService vault,
        IGameEventPublisher events,
        IGuildService guild,
        IGameEventOutbox outbox,
        IMapper mapper)
    {
        _vault = vault;
        _events = events;
        _guild = guild;
        _outbox = outbox;
        _mapper = mapper;
    }

    public async Task<Response<bool>> Handle(DonateGuildVaultItemCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guild.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        var result = await _vault.DonateAsync(request.CharacterId, request.EquipmentInstanceId, cancellationToken);
        if (!result.Succeeded) return Response<bool>.Fail(result.Error ?? "Failed to donate equipment.");

        var mutation = result.Value!;
        await _outbox.EnqueueAsync(
            GameEventTypes.GuildVaultChatMessage,
            new GuildVaultChatMessagePayload(
                mutation.GuildId,
                mutation.CharacterId,
                mutation.CharacterName,
                "donated",
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
