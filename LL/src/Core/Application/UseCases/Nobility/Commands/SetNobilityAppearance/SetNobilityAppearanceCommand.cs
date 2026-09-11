using Application.Interfaces.Services.LL.Nobility;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Nobility.Commands.SetNobilityAppearance;

public sealed record SetNobilityAppearanceCommand(Guid AccountId, Guid CharacterId, bool ShowBadge) : ICommand<Response<bool>>;

public sealed class SetNobilityAppearanceCommandHandler(INobilityService service)
    : IRequestHandler<SetNobilityAppearanceCommand, Response<bool>>
{
    public Task<Response<bool>> Handle(SetNobilityAppearanceCommand request, CancellationToken ct) =>
        service.SetAppearanceAsync(request.AccountId, request.CharacterId, request.ShowBadge, ct);
}
