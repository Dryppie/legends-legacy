using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Commands;
using Application.UseCases.Essences.Dtos;
using Common.Primitives;
using Domain.Models.Essences;
using MediatR;

namespace Application.UseCases.Essences.Commands.SetEssenceLoadoutAutoUseActivities;

public sealed record SetEssenceLoadoutAutoUseActivitiesCommand(
    Guid CharacterId,
    Guid LoadoutId,
    IReadOnlyCollection<EssenceCombatActivity> Activities)
    : ICommand<Response<EssenceStateResponseDto>>;

public sealed class SetEssenceLoadoutAutoUseActivitiesCommandHandler(
    IEssenceService service,
    EssenceMutationResponseFactory responses)
    : IRequestHandler<SetEssenceLoadoutAutoUseActivitiesCommand, Response<EssenceStateResponseDto>>
{
    public async Task<Response<EssenceStateResponseDto>> Handle(
        SetEssenceLoadoutAutoUseActivitiesCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.SetAutoUseActivitiesAsync(
            request.CharacterId,
            request.LoadoutId,
            request.Activities,
            cancellationToken);
        if (!result.Succeeded)
            return Response<EssenceStateResponseDto>.Fail(result.Message);

        return Response<EssenceStateResponseDto>.Success(await responses.CreateStateAsync(
            request.CharacterId,
            true,
            result.Message,
            cancellationToken));
    }
}
