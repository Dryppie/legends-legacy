using Application.Interfaces.Services.LL.RegionBosses;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using Application.UseCases.RegionBosses.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.RegionBosses;

public sealed record GetRegionBossStatusQuery(Guid CharacterId, int? RegionId) : IQuery<IReadOnlyList<RegionBossStatusDto>>;
public sealed record GetRegionBossEventQuery(Guid CharacterId, Guid EventId) : IQuery<RegionBossStatusDto?>;
public sealed record GetRegionBossPlaybackQuery(Guid CharacterId, Guid RunId) : IQuery<RegionBossPlaybackDto?>;
public sealed record GetRegionBossPlaybackBundleQuery(Guid CharacterId, Guid RunId) : IQuery<RegionBossPlaybackBundleContentDto?>;
[NonTransactional]
public sealed record SignupRegionBossCommand(Guid CharacterId, Guid EventId) : ICommand<Response<RegionBossStatusDto>>;
[NonTransactional]
public sealed record WithdrawRegionBossCommand(Guid CharacterId, Guid EventId) : ICommand<Response<RegionBossStatusDto>>;
public sealed record ClaimRegionBossRewardCommand(Guid CharacterId, Guid GrantId) : ICommand<Response<RegionBossClaimResultDto>>;
[NonTransactional]
public sealed record SpawnDevelopmentRegionBossCommand(
    Guid CharacterId,
    int RegionId,
    int AdditionalSignupCount) : ICommand<Response<RegionBossStatusDto>>;

public sealed class GetRegionBossStatusQueryHandler(IRegionBossService service) : IRequestHandler<GetRegionBossStatusQuery, IReadOnlyList<RegionBossStatusDto>>
{
    public Task<IReadOnlyList<RegionBossStatusDto>> Handle(GetRegionBossStatusQuery request, CancellationToken cancellationToken) =>
        service.GetStatusAsync(request.CharacterId, request.RegionId, cancellationToken);
}

public sealed class GetRegionBossEventQueryHandler(IRegionBossService service) : IRequestHandler<GetRegionBossEventQuery, RegionBossStatusDto?>
{
    public Task<RegionBossStatusDto?> Handle(GetRegionBossEventQuery request, CancellationToken cancellationToken) =>
        service.GetEventAsync(request.CharacterId, request.EventId, cancellationToken);
}

public sealed class GetRegionBossPlaybackQueryHandler(IRegionBossService service) : IRequestHandler<GetRegionBossPlaybackQuery, RegionBossPlaybackDto?>
{
    public Task<RegionBossPlaybackDto?> Handle(GetRegionBossPlaybackQuery request, CancellationToken cancellationToken) =>
        service.GetPlaybackAsync(request.CharacterId, request.RunId, cancellationToken);
}

public sealed class GetRegionBossPlaybackBundleQueryHandler(IRegionBossService service) : IRequestHandler<GetRegionBossPlaybackBundleQuery, RegionBossPlaybackBundleContentDto?>
{
    public Task<RegionBossPlaybackBundleContentDto?> Handle(GetRegionBossPlaybackBundleQuery request, CancellationToken cancellationToken) =>
        service.GetPlaybackBundleAsync(request.CharacterId, request.RunId, cancellationToken);
}

public static class RegionBossRequestResponses
{
    public static Response<T> ToResponse<T>(RegionBossOperationResult<T> result) =>
        result.Succeeded ? Response<T>.Success(result.Value!) : Response<T>.Fail(result.Error!);
}

public sealed class SignupRegionBossCommandHandler(IRegionBossService service) : IRequestHandler<SignupRegionBossCommand, Response<RegionBossStatusDto>>
{
    public async Task<Response<RegionBossStatusDto>> Handle(SignupRegionBossCommand request, CancellationToken cancellationToken) =>
        RegionBossRequestResponses.ToResponse(await service.SignupAsync(request.CharacterId, request.EventId, cancellationToken));
}

public sealed class WithdrawRegionBossCommandHandler(IRegionBossService service) : IRequestHandler<WithdrawRegionBossCommand, Response<RegionBossStatusDto>>
{
    public async Task<Response<RegionBossStatusDto>> Handle(WithdrawRegionBossCommand request, CancellationToken cancellationToken) =>
        RegionBossRequestResponses.ToResponse(await service.WithdrawAsync(request.CharacterId, request.EventId, cancellationToken));
}

public sealed class ClaimRegionBossRewardCommandHandler(IRegionBossService service) : IRequestHandler<ClaimRegionBossRewardCommand, Response<RegionBossClaimResultDto>>
{
    public async Task<Response<RegionBossClaimResultDto>> Handle(ClaimRegionBossRewardCommand request, CancellationToken cancellationToken) =>
        RegionBossRequestResponses.ToResponse(await service.ClaimAsync(request.CharacterId, request.GrantId, cancellationToken));
}

public sealed class SpawnDevelopmentRegionBossCommandHandler(IRegionBossService service)
    : IRequestHandler<SpawnDevelopmentRegionBossCommand, Response<RegionBossStatusDto>>
{
    public async Task<Response<RegionBossStatusDto>> Handle(
        SpawnDevelopmentRegionBossCommand request,
        CancellationToken cancellationToken) =>
        RegionBossRequestResponses.ToResponse(await service.SpawnDevelopmentEventAsync(
            request.CharacterId,
            request.RegionId,
            request.AdditionalSignupCount,
            cancellationToken));
}
