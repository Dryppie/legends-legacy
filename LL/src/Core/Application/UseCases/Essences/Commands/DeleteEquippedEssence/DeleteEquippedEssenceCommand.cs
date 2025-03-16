using Application.Common.Responses;
using Application.Interfaces.Services.LL.Essences;
using MediatR;

namespace Application.UseCases.Essences.Commands.DeleteEquippedEssence;
public record DeleteEquippedEssenceCommand(Guid CharacterId, string EssenceId) : IRequest<Response<bool>>;

public class DeleteEquippedEssenceCommandHandler : IRequestHandler<DeleteEquippedEssenceCommand, Response<bool>>
{
    private readonly IEssenceService _essenceService;
    public DeleteEquippedEssenceCommandHandler(IEssenceService essenceService)
    {
        _essenceService = essenceService;
    }

    public async Task<Response<bool>> Handle(DeleteEquippedEssenceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            bool deleteEquippedEssence = await _essenceService.DeleteEquippedEssence(request.CharacterId, Guid.Parse(request.EssenceId), cancellationToken);
            return Response<bool>.Success(deleteEquippedEssence);
        }
        catch (Exception)
        {
            return Response<bool>.Fail("Error deleting essence: " + request.EssenceId + "For character: " + request.CharacterId);
        }
        
    }
}
