using Application.Interfaces.Services.LL.Raids;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using Application.UseCases.Raids.Dtos;
using Common.Primitives;
using Domain.Models.Raids;
using MediatR;

namespace Application.UseCases.Raids;

public sealed record GetRaidBossesQuery(Guid CharacterId, int? Region) : IQuery<IReadOnlyList<RaidBossSummaryDto>>;
public sealed record GetOpenRaidsQuery(Guid CharacterId, string RaidBossId) : IQuery<IReadOnlyList<RaidRunSummaryDto>>;
public sealed record GetRaidHistoryQuery(Guid CharacterId, string? RaidBossId, int Take) : IQuery<IReadOnlyList<RaidHistoryEntryDto>>;
public sealed record GetRaidQuery(Guid CharacterId, Guid RaidRunId) : IQuery<RaidRunDto?>;
public sealed record GetActiveRaidQuery(Guid CharacterId) : IQuery<RaidRunDto?>;
public sealed record GetRaidPlaybackQuery(Guid CharacterId, Guid RaidRunId, RaidLane Lane) : IQuery<RaidPlaybackDto?>;
public sealed record GetRaidPlaybackBundleQuery(Guid CharacterId, Guid RaidRunId, RaidLane Lane) : IQuery<RaidPlaybackBundleContentDto?>;
public sealed record GetRaidTrophyVendorQuery(Guid CharacterId, string RaidBossId) : IQuery<RaidTrophyVendorDto?>;
public sealed record CreateRaidCommand(Guid CharacterId, string RaidBossId, int Tier) : ICommand<Response<RaidRunDto>>;
public sealed record CreateDevelopmentRaidCommand(Guid CharacterId, string RaidBossId, int Tier) : ICommand<Response<RaidRunDto>>;
public sealed record JoinRaidCommand(Guid CharacterId, Guid RaidRunId) : ICommand<Response<RaidRunDto>>;
public sealed record LeaveRaidCommand(Guid CharacterId, Guid RaidRunId) : ICommand<Response<RaidRunDto>>;
public sealed record CancelRaidCommand(Guid CharacterId, Guid RaidRunId) : ICommand<Response<RaidRunDto>>;
public sealed record TransferRaidLeadershipCommand(Guid CharacterId, Guid RaidRunId, Guid TargetCharacterId) : ICommand<Response<RaidRunDto>>;
public sealed record RefreshRaidSnapshotCommand(Guid CharacterId, Guid RaidRunId) : ICommand<Response<RaidRunDto>>;
public sealed record AssignRaidWingCommand(Guid CharacterId, Guid RaidRunId, Guid TargetCharacterId, RaidLane Lane, int SlotIndex) : ICommand<Response<RaidRunDto>>;
public sealed record FillRaidWithDevelopmentCharactersCommand(Guid CharacterId, Guid RaidRunId) : ICommand<Response<RaidRunDto>>;
[NonTransactional]
public sealed record PreviewRaidBattlePlanQuery(Guid CharacterId, Guid RaidRunId) : IQuery<Response<RaidBattlePlanPreviewDto>>;
[NonTransactional]
public sealed record CommenceRaidCommand(Guid CharacterId, Guid RaidRunId) : ICommand<Response<RaidRunDto>>;
public sealed record ClaimRaidRewardsCommand(Guid CharacterId, Guid RaidRunId) : ICommand<Response<RaidRewardDto>>;
public sealed record PurchaseRaidTrophyVendorItemCommand(Guid CharacterId, string RaidBossId, string ItemId, int Quantity) : ICommand<Response<RaidTrophyPurchaseDto>>;

public sealed class GetRaidBossesQueryHandler(IRaidService raids) : IRequestHandler<GetRaidBossesQuery, IReadOnlyList<RaidBossSummaryDto>>
{
    public Task<IReadOnlyList<RaidBossSummaryDto>> Handle(GetRaidBossesQuery request, CancellationToken cancellationToken) =>
        raids.GetRaidBossesAsync(request.CharacterId, request.Region, cancellationToken);
}

public sealed class GetOpenRaidsQueryHandler(IRaidService raids) : IRequestHandler<GetOpenRaidsQuery, IReadOnlyList<RaidRunSummaryDto>>
{
    public Task<IReadOnlyList<RaidRunSummaryDto>> Handle(GetOpenRaidsQuery request, CancellationToken cancellationToken) =>
        raids.GetOpenRaidsAsync(request.CharacterId, request.RaidBossId, cancellationToken);
}

public sealed class GetRaidHistoryQueryHandler(IRaidService raids) : IRequestHandler<GetRaidHistoryQuery, IReadOnlyList<RaidHistoryEntryDto>>
{
    public Task<IReadOnlyList<RaidHistoryEntryDto>> Handle(GetRaidHistoryQuery request, CancellationToken cancellationToken) =>
        raids.GetHistoryAsync(request.CharacterId, request.RaidBossId, request.Take, cancellationToken);
}

public sealed class GetRaidQueryHandler(IRaidService raids) : IRequestHandler<GetRaidQuery, RaidRunDto?>
{
    public Task<RaidRunDto?> Handle(GetRaidQuery request, CancellationToken cancellationToken) =>
        raids.GetRaidAsync(request.CharacterId, request.RaidRunId, cancellationToken);
}

public sealed class GetActiveRaidQueryHandler(IRaidService raids) : IRequestHandler<GetActiveRaidQuery, RaidRunDto?>
{
    public Task<RaidRunDto?> Handle(GetActiveRaidQuery request, CancellationToken cancellationToken) =>
        raids.GetActiveRaidAsync(request.CharacterId, cancellationToken);
}

public sealed class GetRaidPlaybackQueryHandler(IRaidService raids) : IRequestHandler<GetRaidPlaybackQuery, RaidPlaybackDto?>
{
    public Task<RaidPlaybackDto?> Handle(GetRaidPlaybackQuery request, CancellationToken cancellationToken) =>
        raids.GetPlaybackAsync(request.CharacterId, request.RaidRunId, request.Lane, cancellationToken);
}

public sealed class GetRaidPlaybackBundleQueryHandler(IRaidService raids) : IRequestHandler<GetRaidPlaybackBundleQuery, RaidPlaybackBundleContentDto?>
{
    public Task<RaidPlaybackBundleContentDto?> Handle(GetRaidPlaybackBundleQuery request, CancellationToken cancellationToken) =>
        raids.GetPlaybackBundleAsync(request.CharacterId, request.RaidRunId, request.Lane, cancellationToken);
}

public sealed class GetRaidTrophyVendorQueryHandler(IRaidService raids) : IRequestHandler<GetRaidTrophyVendorQuery, RaidTrophyVendorDto?>
{
    public Task<RaidTrophyVendorDto?> Handle(GetRaidTrophyVendorQuery request, CancellationToken cancellationToken) =>
        raids.GetTrophyVendorAsync(request.CharacterId, request.RaidBossId, cancellationToken);
}

public abstract class RaidCommandHandler<TRequest, TResponse>(IRaidService raids)
    where TRequest : IRequest<Response<TResponse>>
{
    protected IRaidService Raids { get; } = raids;
    protected static Response<TResponse> ToResponse(RaidOperationResult<TResponse> result) =>
        result.Succeeded ? Response<TResponse>.Success(result.Value!) : Response<TResponse>.Fail(result.Error!);
}

public sealed class CreateRaidCommandHandler(IRaidService raids) : RaidCommandHandler<CreateRaidCommand, RaidRunDto>(raids), IRequestHandler<CreateRaidCommand, Response<RaidRunDto>>
{
    public async Task<Response<RaidRunDto>> Handle(CreateRaidCommand request, CancellationToken cancellationToken) =>
        ToResponse(await Raids.CreateAsync(request.CharacterId, request.RaidBossId, request.Tier, cancellationToken));
}

public sealed class CreateDevelopmentRaidCommandHandler(IRaidService raids)
    : RaidCommandHandler<CreateDevelopmentRaidCommand, RaidRunDto>(raids),
        IRequestHandler<CreateDevelopmentRaidCommand, Response<RaidRunDto>>
{
    public async Task<Response<RaidRunDto>> Handle(
        CreateDevelopmentRaidCommand request,
        CancellationToken cancellationToken) =>
        ToResponse(await Raids.CreateDevelopmentAsync(
            request.CharacterId,
            request.RaidBossId,
            request.Tier,
            cancellationToken));
}

public sealed class JoinRaidCommandHandler(IRaidService raids) : RaidCommandHandler<JoinRaidCommand, RaidRunDto>(raids), IRequestHandler<JoinRaidCommand, Response<RaidRunDto>>
{
    public async Task<Response<RaidRunDto>> Handle(JoinRaidCommand request, CancellationToken cancellationToken) =>
        ToResponse(await Raids.JoinAsync(request.CharacterId, request.RaidRunId, cancellationToken));
}

public sealed class LeaveRaidCommandHandler(IRaidService raids) : RaidCommandHandler<LeaveRaidCommand, RaidRunDto>(raids), IRequestHandler<LeaveRaidCommand, Response<RaidRunDto>>
{
    public async Task<Response<RaidRunDto>> Handle(LeaveRaidCommand request, CancellationToken cancellationToken) =>
        ToResponse(await Raids.LeaveAsync(request.CharacterId, request.RaidRunId, cancellationToken));
}

public sealed class CancelRaidCommandHandler(IRaidService raids) : RaidCommandHandler<CancelRaidCommand, RaidRunDto>(raids), IRequestHandler<CancelRaidCommand, Response<RaidRunDto>>
{
    public async Task<Response<RaidRunDto>> Handle(CancelRaidCommand request, CancellationToken cancellationToken) =>
        ToResponse(await Raids.CancelAsync(request.CharacterId, request.RaidRunId, cancellationToken));
}

public sealed class TransferRaidLeadershipCommandHandler(IRaidService raids) : RaidCommandHandler<TransferRaidLeadershipCommand, RaidRunDto>(raids), IRequestHandler<TransferRaidLeadershipCommand, Response<RaidRunDto>>
{
    public async Task<Response<RaidRunDto>> Handle(TransferRaidLeadershipCommand request, CancellationToken cancellationToken) =>
        ToResponse(await Raids.TransferLeadershipAsync(
            request.CharacterId,
            request.RaidRunId,
            request.TargetCharacterId,
            cancellationToken));
}

public sealed class RefreshRaidSnapshotCommandHandler(IRaidService raids) : RaidCommandHandler<RefreshRaidSnapshotCommand, RaidRunDto>(raids), IRequestHandler<RefreshRaidSnapshotCommand, Response<RaidRunDto>>
{
    public async Task<Response<RaidRunDto>> Handle(RefreshRaidSnapshotCommand request, CancellationToken cancellationToken) =>
        ToResponse(await Raids.RefreshSnapshotAsync(request.CharacterId, request.RaidRunId, cancellationToken));
}

public sealed class AssignRaidWingCommandHandler(IRaidService raids) : RaidCommandHandler<AssignRaidWingCommand, RaidRunDto>(raids), IRequestHandler<AssignRaidWingCommand, Response<RaidRunDto>>
{
    public async Task<Response<RaidRunDto>> Handle(AssignRaidWingCommand request, CancellationToken cancellationToken) =>
        ToResponse(await Raids.AssignAsync(request.CharacterId, request.RaidRunId, request.TargetCharacterId, request.Lane, request.SlotIndex, cancellationToken));
}

public sealed class FillRaidWithDevelopmentCharactersCommandHandler(IRaidService raids)
    : RaidCommandHandler<FillRaidWithDevelopmentCharactersCommand, RaidRunDto>(raids),
        IRequestHandler<FillRaidWithDevelopmentCharactersCommand, Response<RaidRunDto>>
{
    public async Task<Response<RaidRunDto>> Handle(
        FillRaidWithDevelopmentCharactersCommand request,
        CancellationToken cancellationToken) =>
        ToResponse(await Raids.FillWithDevelopmentCharactersAsync(
            request.CharacterId,
            request.RaidRunId,
            cancellationToken));
}

public sealed class PreviewRaidBattlePlanQueryHandler(IRaidService raids) : IRequestHandler<PreviewRaidBattlePlanQuery, Response<RaidBattlePlanPreviewDto>>
{
    public async Task<Response<RaidBattlePlanPreviewDto>> Handle(PreviewRaidBattlePlanQuery request, CancellationToken cancellationToken)
    {
        var result = await raids.PreviewBattlePlanAsync(request.CharacterId, request.RaidRunId, cancellationToken);
        return result.Succeeded
            ? Response<RaidBattlePlanPreviewDto>.Success(result.Value!)
            : Response<RaidBattlePlanPreviewDto>.Fail(result.Error!);
    }
}

public sealed class CommenceRaidCommandHandler(IRaidService raids) : RaidCommandHandler<CommenceRaidCommand, RaidRunDto>(raids), IRequestHandler<CommenceRaidCommand, Response<RaidRunDto>>
{
    public async Task<Response<RaidRunDto>> Handle(CommenceRaidCommand request, CancellationToken cancellationToken) =>
        ToResponse(await Raids.CommenceAsync(request.CharacterId, request.RaidRunId, cancellationToken));
}

public sealed class ClaimRaidRewardsCommandHandler(IRaidService raids) : RaidCommandHandler<ClaimRaidRewardsCommand, RaidRewardDto>(raids), IRequestHandler<ClaimRaidRewardsCommand, Response<RaidRewardDto>>
{
    public async Task<Response<RaidRewardDto>> Handle(ClaimRaidRewardsCommand request, CancellationToken cancellationToken) =>
        ToResponse(await Raids.ClaimAsync(request.CharacterId, request.RaidRunId, cancellationToken));
}

public sealed class PurchaseRaidTrophyVendorItemCommandHandler(IRaidService raids) : RaidCommandHandler<PurchaseRaidTrophyVendorItemCommand, RaidTrophyPurchaseDto>(raids), IRequestHandler<PurchaseRaidTrophyVendorItemCommand, Response<RaidTrophyPurchaseDto>>
{
    public async Task<Response<RaidTrophyPurchaseDto>> Handle(PurchaseRaidTrophyVendorItemCommand request, CancellationToken cancellationToken) =>
        ToResponse(await Raids.PurchaseTrophyVendorItemAsync(
            request.CharacterId,
            request.RaidBossId,
            request.ItemId,
            request.Quantity,
            cancellationToken));
}
