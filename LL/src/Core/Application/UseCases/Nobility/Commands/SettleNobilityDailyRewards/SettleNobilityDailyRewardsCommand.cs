using Application.Interfaces.Services.LL.Nobility;
using Application.MediatR.Markers;
using MediatR;

namespace Application.UseCases.Nobility.Commands.SettleNobilityDailyRewards;

public sealed record SettleNobilityDailyRewardsCommand(Guid AccountId, Guid CharacterId) : ICommand<int>;

public sealed class SettleNobilityDailyRewardsCommandHandler(INobilityService service)
    : IRequestHandler<SettleNobilityDailyRewardsCommand, int>
{
    public Task<int> Handle(SettleNobilityDailyRewardsCommand request, CancellationToken ct) =>
        service.ApplyDailyRewardsAsync(request.AccountId, request.CharacterId, ct);
}
