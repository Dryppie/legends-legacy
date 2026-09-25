using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Analytics;
using MediatR;

namespace Application.UseCases.Analytics.Commands.RecordActivityDay;

public sealed record RecordActivityDayCommand(Guid AccountId) : ICommand<Response<bool>>;

public sealed class RecordActivityDayCommandHandler(ITelemetryRepository telemetry)
    : IRequestHandler<RecordActivityDayCommand, Response<bool>>
{
    public async Task<Response<bool>> Handle(RecordActivityDayCommand request, CancellationToken ct)
    {
        await telemetry.RecordActivityAsync(request.AccountId, DateTimeOffset.UtcNow, ct);
        return Response<bool>.Success(true);
    }
}
