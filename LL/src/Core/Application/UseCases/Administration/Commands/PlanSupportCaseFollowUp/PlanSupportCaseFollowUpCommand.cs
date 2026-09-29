using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Mappings;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.PlanSupportCaseFollowUp;
public sealed record PlanSupportCaseFollowUpCommand(SupportCaseChange Change, AdministrationActor Actor) : ICommand<Response<SupportCaseDetailsDto>>;
public sealed class PlanSupportCaseFollowUpCommandHandler(ISupportCaseService service, IMapper mapper)
    : IRequestHandler<PlanSupportCaseFollowUpCommand, Response<SupportCaseDetailsDto>>
{
    public async Task<Response<SupportCaseDetailsDto>> Handle(PlanSupportCaseFollowUpCommand request, CancellationToken ct) =>
        SupportCaseMappingProfile.Map(await service.ApplyAsync(request.Change with { Kind = SupportCaseEntryKind.FollowUpChanged }, request.Actor, ct), mapper);
}
