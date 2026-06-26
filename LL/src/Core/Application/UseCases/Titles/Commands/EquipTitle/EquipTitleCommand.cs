using Application.Interfaces.Services.LL.Achievements;
using Application.MediatR.Markers;
using Application.UseCases.Achievements.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Titles.Commands.EquipTitle;

public sealed record EquipTitleRequest(string TitleKey);

public record EquipTitleCommand(Guid AccountId, Guid CharacterId, string TitleKey)
    : ICommand<Response<EquippedTitleDto>>;

public sealed class EquipTitleCommandHandler : IRequestHandler<EquipTitleCommand, Response<EquippedTitleDto>>
{
    private readonly IAchievementService _achievementService;

    public EquipTitleCommandHandler(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    public async Task<Response<EquippedTitleDto>> Handle(
        EquipTitleCommand request,
        CancellationToken cancellationToken)
    {
        var title = await _achievementService.EquipTitleAsync(
            request.AccountId,
            request.CharacterId,
            request.TitleKey,
            cancellationToken);

        return title is null
            ? Response<EquippedTitleDto>.Fail("Title is locked or does not exist.")
            : Response<EquippedTitleDto>.Success(title);
    }
}
