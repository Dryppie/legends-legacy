using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.DeleteEquippedEssence;
public record DeleteEquippedEssenceCommand(Guid CharacterId, string EssenceId) : ICommand<Response<bool>>;

public class DeleteEquippedEssenceCommandHandler : IRequestHandler<DeleteEquippedEssenceCommand, Response<bool>>
{
    private readonly IEssenceService _essenceService;
    public DeleteEquippedEssenceCommandHandler(IEssenceService essenceService)
    {
        _essenceService = essenceService;
    }

    public async Task<Response<bool>> Handle(DeleteEquippedEssenceCommand request, CancellationToken cancellationToken)
    {
        return await _essenceService.DeleteEquippedEssence(request.CharacterId, Guid.Parse(request.EssenceId), cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to absorb essence.");
    }
}
