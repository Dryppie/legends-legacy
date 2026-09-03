using Domain.Models.Items.Equipments.Progression;

namespace Application.Interfaces.Services.LL.Items;

public interface IForgeService
{
    Task<ForgeQuote> PreviewAsync(Guid characterId, ForgeRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ForgeStyleOption>> GetStylesAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken);
    Task<ForgeResult> ExecuteAsync(Guid characterId, Guid operationId, ForgeRequest request,
        string expectedQuote, CancellationToken cancellationToken);
}
