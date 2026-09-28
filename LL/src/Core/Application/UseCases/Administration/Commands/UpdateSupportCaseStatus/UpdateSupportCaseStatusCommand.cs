using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Mappings;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.UpdateSupportCaseStatus;
public sealed record UpdateSupportCaseStatusCommand(SupportCaseChange Change, AdministrationActor Actor) : ICommand<Response<SupportCaseDetailsDto>>;
public sealed class UpdateSupportCaseStatusCommandHandler(ISupportCaseService service, IMapper mapper)
    : IRequestHandler<UpdateSupportCaseStatusCommand, Response<SupportCaseDetailsDto>>
{
    public async Task<Response<SupportCaseDetailsDto>> Handle(UpdateSupportCaseStatusCommand request, CancellationToken ct) =>
        SupportCaseMappingProfile.Map(await service.ApplyAsync(request.Change with { Kind = SupportCaseEntryKind.StatusChanged }, request.Actor, ct), mapper);
}
