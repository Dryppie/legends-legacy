using Application.Interfaces.Services.LL.WorldTower;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using Application.UseCases.WorldTower.Dtos;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Common.Primitives;
using Domain.Models.WorldTower;
using MediatR;

namespace Application.UseCases.WorldTower;

public sealed record GetWorldTowerOverviewQuery(Guid CharacterId) : IQuery<TowerOverviewDto>;
public sealed record GetTowerFloorQuery(Guid CharacterId, int FloorNumber) : IQuery<TowerFloorDetailDto?>;
public sealed record GetTowerRallyQuery(Guid CharacterId, Guid RallyId) : IQuery<TowerRallyDto?>;
public sealed record GetTowerAttemptReportQuery(Guid CharacterId, Guid AttemptId) : IQuery<TowerBattleReportDto?>;
public sealed record GetTowerAttemptCombatResultQuery(Guid CharacterId, Guid AttemptId) : IQuery<CombatResultDto?>;
public sealed record GetTowerAttemptPlaybackQuery(Guid CharacterId, Guid AttemptId) : IQuery<TowerCombatPlaybackDto?>;
public sealed record GetTowerAttemptPlaybackFramesQuery(Guid CharacterId, Guid AttemptId, int AfterSequence) : IQuery<TowerCombatFrameBatchDto?>;
public sealed record GetTowerHallOfFameQuery : IQuery<IReadOnlyList<TowerHallOfFameEntryDto>>;
public sealed record GetPersonalTowerExpeditionsQuery(Guid CharacterId) : IQuery<IReadOnlyList<TowerPersonalExpeditionDto>>;
public sealed record CreateTowerRallyCommand(Guid CharacterId, int FloorNumber, TowerRallyMode Mode) : ICommand<Response<TowerRallyDto>>;
public sealed record ApplyToTowerRallyCommand(Guid CharacterId, Guid RallyId) : ICommand<Response<TowerRallyDto>>;
public sealed record AcceptTowerRallyApplicationCommand(Guid CharacterId, Guid RallyId, Guid ApplicationId) : ICommand<Response<TowerRallyDto>>;
public sealed record DeclineTowerRallyApplicationCommand(Guid CharacterId, Guid RallyId, Guid ApplicationId) : ICommand<Response<TowerRallyDto>>;
public sealed record LeaveTowerRallyCommand(Guid CharacterId, Guid RallyId) : ICommand<Response<TowerRallyDto>>;
public sealed record FillTowerRallyWithDevelopmentCharactersCommand(Guid CharacterId, Guid RallyId) : ICommand<Response<TowerRallyDto>>;
[NonTransactional]
public sealed record StartTowerRallyCommand(Guid CharacterId, Guid RallyId) : ICommand<Response<TowerAttemptResultDto>>;
public sealed record ContributeToTowerCommand(Guid CharacterId, int FloorNumber, TowerContributionKind Kind, int Amount) : ICommand<Response<TowerFloorDetailDto>>;

public sealed class GetWorldTowerOverviewQueryHandler(IWorldTowerService tower)
    : IRequestHandler<GetWorldTowerOverviewQuery, TowerOverviewDto>
{
    public Task<TowerOverviewDto> Handle(GetWorldTowerOverviewQuery request, CancellationToken cancellationToken) =>
        tower.GetOverviewAsync(request.CharacterId, cancellationToken);
}

public sealed class GetTowerFloorQueryHandler(IWorldTowerService tower)
    : IRequestHandler<GetTowerFloorQuery, TowerFloorDetailDto?>
{
    public Task<TowerFloorDetailDto?> Handle(GetTowerFloorQuery request, CancellationToken cancellationToken) =>
        tower.GetFloorAsync(request.CharacterId, request.FloorNumber, cancellationToken);
}

public sealed class GetTowerRallyQueryHandler(IWorldTowerService tower)
    : IRequestHandler<GetTowerRallyQuery, TowerRallyDto?>
{
    public Task<TowerRallyDto?> Handle(GetTowerRallyQuery request, CancellationToken cancellationToken) =>
        tower.GetRallyAsync(request.CharacterId, request.RallyId, cancellationToken);
}

public sealed class GetTowerAttemptReportQueryHandler(IWorldTowerService tower)
    : IRequestHandler<GetTowerAttemptReportQuery, TowerBattleReportDto?>
{
    public Task<TowerBattleReportDto?> Handle(GetTowerAttemptReportQuery request, CancellationToken cancellationToken) =>
        tower.GetAttemptReportAsync(request.CharacterId, request.AttemptId, cancellationToken);
}

public sealed class GetTowerAttemptCombatResultQueryHandler(IWorldTowerService tower)
    : IRequestHandler<GetTowerAttemptCombatResultQuery, CombatResultDto?>
{
    public Task<CombatResultDto?> Handle(
        GetTowerAttemptCombatResultQuery request,
        CancellationToken cancellationToken) =>
        tower.GetAttemptCombatResultAsync(request.CharacterId, request.AttemptId, cancellationToken);
}

public sealed class GetTowerAttemptPlaybackQueryHandler(IWorldTowerService tower)
    : IRequestHandler<GetTowerAttemptPlaybackQuery, TowerCombatPlaybackDto?>
{
    public Task<TowerCombatPlaybackDto?> Handle(
        GetTowerAttemptPlaybackQuery request,
        CancellationToken cancellationToken) =>
        tower.GetAttemptPlaybackAsync(request.CharacterId, request.AttemptId, cancellationToken);
}

public sealed class GetTowerAttemptPlaybackFramesQueryHandler(IWorldTowerService tower)
    : IRequestHandler<GetTowerAttemptPlaybackFramesQuery, TowerCombatFrameBatchDto?>
{
    public Task<TowerCombatFrameBatchDto?> Handle(
        GetTowerAttemptPlaybackFramesQuery request,
        CancellationToken cancellationToken) =>
        tower.GetAttemptPlaybackFramesAsync(
            request.CharacterId,
            request.AttemptId,
            request.AfterSequence,
            cancellationToken);
}

public sealed class GetTowerHallOfFameQueryHandler(IWorldTowerService tower)
    : IRequestHandler<GetTowerHallOfFameQuery, IReadOnlyList<TowerHallOfFameEntryDto>>
{
    public Task<IReadOnlyList<TowerHallOfFameEntryDto>> Handle(GetTowerHallOfFameQuery request, CancellationToken cancellationToken) =>
        tower.GetHallOfFameAsync(cancellationToken);
}

public sealed class GetPersonalTowerExpeditionsQueryHandler(IWorldTowerService tower)
    : IRequestHandler<GetPersonalTowerExpeditionsQuery, IReadOnlyList<TowerPersonalExpeditionDto>>
{
    public Task<IReadOnlyList<TowerPersonalExpeditionDto>> Handle(
        GetPersonalTowerExpeditionsQuery request,
        CancellationToken cancellationToken) =>
        tower.GetPersonalExpeditionsAsync(request.CharacterId, cancellationToken);
}

public sealed class CreateTowerRallyCommandHandler(IWorldTowerService tower)
    : IRequestHandler<CreateTowerRallyCommand, Response<TowerRallyDto>>
{
    public async Task<Response<TowerRallyDto>> Handle(CreateTowerRallyCommand request, CancellationToken cancellationToken) =>
        ToResponse(await tower.CreateRallyAsync(request.CharacterId, request.FloorNumber, request.Mode, cancellationToken));

    private static Response<TowerRallyDto> ToResponse(TowerOperationResult<TowerRallyDto> result) =>
        result.Succeeded ? Response<TowerRallyDto>.Success(result.Value!) : Response<TowerRallyDto>.Fail(result.Error!);
}

public sealed class ApplyToTowerRallyCommandHandler(IWorldTowerService tower)
    : IRequestHandler<ApplyToTowerRallyCommand, Response<TowerRallyDto>>
{
    public async Task<Response<TowerRallyDto>> Handle(ApplyToTowerRallyCommand request, CancellationToken cancellationToken)
    {
        var result = await tower.ApplyToRallyAsync(request.CharacterId, request.RallyId, cancellationToken);
        return result.Succeeded ? Response<TowerRallyDto>.Success(result.Value!) : Response<TowerRallyDto>.Fail(result.Error!);
    }
}

public sealed class AcceptTowerRallyApplicationCommandHandler(IWorldTowerService tower)
    : IRequestHandler<AcceptTowerRallyApplicationCommand, Response<TowerRallyDto>>
{
    public async Task<Response<TowerRallyDto>> Handle(AcceptTowerRallyApplicationCommand request, CancellationToken cancellationToken)
    {
        var result = await tower.AcceptRallyApplicationAsync(request.CharacterId, request.RallyId, request.ApplicationId, cancellationToken);
        return result.Succeeded ? Response<TowerRallyDto>.Success(result.Value!) : Response<TowerRallyDto>.Fail(result.Error!);
    }
}

public sealed class DeclineTowerRallyApplicationCommandHandler(IWorldTowerService tower)
    : IRequestHandler<DeclineTowerRallyApplicationCommand, Response<TowerRallyDto>>
{
    public async Task<Response<TowerRallyDto>> Handle(DeclineTowerRallyApplicationCommand request, CancellationToken cancellationToken)
    {
        var result = await tower.DeclineRallyApplicationAsync(request.CharacterId, request.RallyId, request.ApplicationId, cancellationToken);
        return result.Succeeded ? Response<TowerRallyDto>.Success(result.Value!) : Response<TowerRallyDto>.Fail(result.Error!);
    }
}

public sealed class LeaveTowerRallyCommandHandler(IWorldTowerService tower)
    : IRequestHandler<LeaveTowerRallyCommand, Response<TowerRallyDto>>
{
    public async Task<Response<TowerRallyDto>> Handle(LeaveTowerRallyCommand request, CancellationToken cancellationToken)
    {
        var result = await tower.LeaveRallyAsync(request.CharacterId, request.RallyId, cancellationToken);
        return result.Succeeded ? Response<TowerRallyDto>.Success(result.Value!) : Response<TowerRallyDto>.Fail(result.Error!);
    }
}

public sealed class FillTowerRallyWithDevelopmentCharactersCommandHandler(IWorldTowerService tower)
    : IRequestHandler<FillTowerRallyWithDevelopmentCharactersCommand, Response<TowerRallyDto>>
{
    public async Task<Response<TowerRallyDto>> Handle(
        FillTowerRallyWithDevelopmentCharactersCommand request,
        CancellationToken cancellationToken)
    {
        var result = await tower.FillRallyWithDevelopmentCharactersAsync(
            request.CharacterId,
            request.RallyId,
            cancellationToken);
        return result.Succeeded
            ? Response<TowerRallyDto>.Success(result.Value!)
            : Response<TowerRallyDto>.Fail(result.Error!);
    }
}

public sealed class StartTowerRallyCommandHandler(IWorldTowerService tower)
    : IRequestHandler<StartTowerRallyCommand, Response<TowerAttemptResultDto>>
{
    public async Task<Response<TowerAttemptResultDto>> Handle(StartTowerRallyCommand request, CancellationToken cancellationToken)
    {
        var result = await tower.StartRallyAsync(request.CharacterId, request.RallyId, cancellationToken);
        return result.Succeeded ? Response<TowerAttemptResultDto>.Success(result.Value!) : Response<TowerAttemptResultDto>.Fail(result.Error!);
    }
}

public sealed class ContributeToTowerCommandHandler(IWorldTowerService tower)
    : IRequestHandler<ContributeToTowerCommand, Response<TowerFloorDetailDto>>
{
    public async Task<Response<TowerFloorDetailDto>> Handle(ContributeToTowerCommand request, CancellationToken cancellationToken)
    {
        var result = await tower.ContributeAsync(request.CharacterId, request.FloorNumber, request.Kind, request.Amount, cancellationToken);
        return result.Succeeded ? Response<TowerFloorDetailDto>.Success(result.Value!) : Response<TowerFloorDetailDto>.Fail(result.Error!);
    }
}
