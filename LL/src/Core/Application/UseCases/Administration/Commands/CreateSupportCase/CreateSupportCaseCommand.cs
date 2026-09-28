using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Mappings;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.CreateSupportCase;
public sealed record CreateSupportCaseCommand(SupportCaseChange Change, AdministrationActor Actor) : ICommand<Response<SupportCaseDetailsDto>>;
public sealed class CreateSupportCaseCommandHandler(ISupportCaseService service, IMapper mapper)
    : IRequestHandler<CreateSupportCaseCommand, Response<SupportCaseDetailsDto>>
{
    public async Task<Response<SupportCaseDetailsDto>> Handle(CreateSupportCaseCommand request, CancellationToken ct) =>
        SupportCaseMappingProfile.Map(await service.ApplyAsync(request.Change with { Kind = SupportCaseEntryKind.Created }, request.Actor, ct), mapper);
}
