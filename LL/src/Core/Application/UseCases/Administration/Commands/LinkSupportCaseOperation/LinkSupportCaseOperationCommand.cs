using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Mappings;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.LinkSupportCaseOperation;
public sealed record LinkSupportCaseOperationCommand(SupportCaseChange Change, AdministrationActor Actor) : ICommand<Response<SupportCaseDetailsDto>>;
public sealed class LinkSupportCaseOperationCommandHandler(ISupportCaseService service, IMapper mapper)
    : IRequestHandler<LinkSupportCaseOperationCommand, Response<SupportCaseDetailsDto>>
{
    public async Task<Response<SupportCaseDetailsDto>> Handle(LinkSupportCaseOperationCommand request, CancellationToken ct) =>
        SupportCaseMappingProfile.Map(await service.ApplyAsync(request.Change with { Kind = SupportCaseEntryKind.OperationLinked }, request.Actor, ct), mapper);
}
