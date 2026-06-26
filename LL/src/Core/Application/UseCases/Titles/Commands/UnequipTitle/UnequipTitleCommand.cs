using Application.Interfaces.Services.LL.Achievements;
using Application.MediatR.Markers;
using Application.UseCases.Achievements.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Titles.Commands.UnequipTitle;

public record UnequipTitleCommand(Guid AccountId, Guid CharacterId)
    : ICommand<Response<EquippedTitleDto?>>;

public sealed class UnequipTitleCommandHandler : IRequestHandler<UnequipTitleCommand, Response<EquippedTitleDto?>>
{
    private readonly IAchievementService _achievementService;

    public UnequipTitleCommandHandler(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    public async Task<Response<EquippedTitleDto?>> Handle(
        UnequipTitleCommand request,
        CancellationToken cancellationToken)
    {
        await _achievementService.UnequipTitleAsync(
            request.AccountId,
            request.CharacterId,
            cancellationToken);

        return Response<EquippedTitleDto?>.Success(null);
    }
}
