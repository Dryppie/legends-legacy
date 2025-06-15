using Application.Interfaces.Services.LL;
using Common.Primitives;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Soulstones.Commands.ResetSoulstoneUpgrades;
public record ResetSoulstoneUpgradesCommand(Guid CharacterId) : IRequest<Response<bool>>;
public class ResetSoulstoneUpgradesCommandHandler : IRequestHandler<ResetSoulstoneUpgradesCommand, Response<bool>>
{
    private readonly ISoulstoneUpgradeService soulstoneUpgradeService;

    public ResetSoulstoneUpgradesCommandHandler(ISoulstoneUpgradeService soulstoneUpgradeService)
    {
        this.soulstoneUpgradeService = soulstoneUpgradeService;
    }

    public async Task<Response<bool>> Handle(ResetSoulstoneUpgradesCommand request, CancellationToken cancellationToken)
    {
        return await soulstoneUpgradeService.ResetSoulstoneUpgradesAsync(request.CharacterId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to reset soulstone upgrades.");
    }
}
