using Application.Interfaces.Services.LL.Nobility;
using Application.MediatR.Markers;
using Application.UseCases.Nobility.Dtos;
using Application.UseCases.Nobility.Mappings;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Nobility;
using MediatR;

namespace Application.UseCases.Nobility.Commands.RedeemSignets;

public sealed record RedeemSignetsCommand(Guid AccountId, Guid CharacterId, Guid OperationId,
    Guid MembershipVersion, Guid[] UnitIds, DateOnly ExpectedExpiryDate) : ICommand<Response<SignetRedemptionDto>>;

public sealed class RedeemSignetsCommandHandler(INobilityService service, IMapper mapper)
    : IRequestHandler<RedeemSignetsCommand, Response<SignetRedemptionDto>>
{
    public async Task<Response<SignetRedemptionDto>> Handle(RedeemSignetsCommand request, CancellationToken ct) =>
        NobilityMappingProfile.MapResult<SignetRedemption, SignetRedemptionDto>(
            await service.RedeemAsync(request.AccountId, request.CharacterId, request.OperationId,
                request.MembershipVersion, request.UnitIds, request.ExpectedExpiryDate, ct), mapper);
}
