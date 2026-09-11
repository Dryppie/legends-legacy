using Application.Interfaces.Services.LL.Nobility;
using Application.MediatR.Markers;
using Application.UseCases.Nobility.Dtos;
using Application.UseCases.Nobility.Mappings;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Nobility;
using MediatR;

namespace Application.UseCases.Nobility.Commands.GrantAlphaSignets;

public sealed record GrantAlphaSignetsCommand(string ActorSubject, Guid CharacterId, Guid OperationId,
    int Quantity, string Reason) : ICommand<Response<SignetGrantDto>>;

public sealed class GrantAlphaSignetsCommandHandler(INobilityService service, IMapper mapper)
    : IRequestHandler<GrantAlphaSignetsCommand, Response<SignetGrantDto>>
{
    public async Task<Response<SignetGrantDto>> Handle(GrantAlphaSignetsCommand request, CancellationToken ct) =>
        NobilityMappingProfile.MapResult<SignetIssuance, SignetGrantDto>(
            await service.GrantAlphaAsync(request.ActorSubject, request.CharacterId, request.OperationId,
                request.Quantity, request.Reason, ct), mapper);
}
