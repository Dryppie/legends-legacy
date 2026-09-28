using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Analytics;
using MediatR;

namespace Application.UseCases.Analytics.Queries.GetItemizationTelemetry;

public sealed record GetItemizationTelemetryQuery(int Days = 30) : IQuery<Response<IReadOnlyList<ItemizationCohortReport>>>;

public sealed class GetItemizationTelemetryQueryHandler(IItemizationTelemetryRepository repository)
    : IRequestHandler<GetItemizationTelemetryQuery, Response<IReadOnlyList<ItemizationCohortReport>>>
{
    public async Task<Response<IReadOnlyList<ItemizationCohortReport>>> Handle(GetItemizationTelemetryQuery request, CancellationToken ct) =>
        Response<IReadOnlyList<ItemizationCohortReport>>.Success(await repository.GetReportsAsync(Math.Clamp(request.Days, 1, 90), ct));
}
