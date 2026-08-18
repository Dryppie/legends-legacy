using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Mappings;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Queries.GetAccountRiskPage;

public sealed record GetAccountRiskPageQuery(
    string? Search,
    AccountRiskSeverity? MinimumSeverity,
    AccountRiskSignalType? SignalType,
    AccountInvestigationStatus? Status,
    int? MinimumScore,
    int? MaximumAccountAgeDays,
    DateTimeOffset? LastTriggeredAfter,
    string Sort,
    int Page,
    int PageSize) : IQuery<Response<AccountRiskPageDto>>;

public sealed class GetAccountRiskPageQueryHandler(ILiveOpsAccountRiskService service)
    : IRequestHandler<GetAccountRiskPageQuery, Response<AccountRiskPageDto>>
{
    public async Task<Response<AccountRiskPageDto>> Handle(GetAccountRiskPageQuery request, CancellationToken cancellationToken)
    {
        if (request.MinimumScore is < 0 or > 100)
            return Response<AccountRiskPageDto>.Fail("Minimum score must be between 0 and 100.");
        var page = await service.SearchAsync(new AccountRiskSearch(
            request.Search,
            request.MinimumSeverity,
            request.SignalType,
            request.Status,
            request.MinimumScore,
            request.MaximumAccountAgeDays,
            request.LastTriggeredAfter,
            request.Sort ?? "risk",
            Math.Max(1, request.Page),
            Math.Clamp(request.PageSize, 1, 100)), cancellationToken);
        return Response<AccountRiskPageDto>.Success(AccountRiskDtoMapper.ToDto(
            page,
            Math.Max(1, request.Page),
            Math.Clamp(request.PageSize, 1, 100)));
    }
}
