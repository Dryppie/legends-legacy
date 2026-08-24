using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.BorrowGuildVaultItem;

public record BorrowGuildVaultItemCommand(Guid CharacterId, Guid VaultItemId) : ICommand<Response<bool>>;

public class BorrowGuildVaultItemCommandHandler : IRequestHandler<BorrowGuildVaultItemCommand, Response<bool>>
{
    private readonly IGuildVaultService _vault;
    public BorrowGuildVaultItemCommandHandler(IGuildVaultService vault)
    {
        _vault = vault;
    }

    public async Task<Response<bool>> Handle(BorrowGuildVaultItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _vault.BorrowAsync(request.CharacterId, request.VaultItemId, cancellationToken);
        if (!result.Succeeded) return Response<bool>.Fail(result.Error ?? "Failed to borrow equipment.");
        return Response<bool>.Success(true);
    }
}
