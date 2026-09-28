using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Mappings;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;

namespace Application.UseCases.Administration.Commands.AddSupportCaseNote;
public sealed record AddSupportCaseNoteCommand(SupportCaseChange Change, AdministrationActor Actor) : ICommand<Response<SupportCaseDetailsDto>>;
public sealed class AddSupportCaseNoteCommandHandler(ISupportCaseService service, IMapper mapper)
    : IRequestHandler<AddSupportCaseNoteCommand, Response<SupportCaseDetailsDto>>
{
    public async Task<Response<SupportCaseDetailsDto>> Handle(AddSupportCaseNoteCommand request, CancellationToken ct) =>
        SupportCaseMappingProfile.Map(await service.ApplyAsync(request.Change with { Kind = SupportCaseEntryKind.Note }, request.Actor, ct), mapper);
}
