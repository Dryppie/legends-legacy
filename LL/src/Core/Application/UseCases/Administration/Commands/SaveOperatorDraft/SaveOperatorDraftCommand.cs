using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Mappings;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Administration.Commands.SaveOperatorDraft;

public sealed record SaveOperatorDraftCommand(string Actor, string Key, Guid ExpectedVersion, string Content) : ICommand<Response<OperatorDraftDto>>;
public sealed class SaveOperatorDraftCommandHandler(IOperatorDraftService service, IMapper mapper)
    : IRequestHandler<SaveOperatorDraftCommand, Response<OperatorDraftDto>>
{
    public async Task<Response<OperatorDraftDto>> Handle(SaveOperatorDraftCommand request, CancellationToken ct) =>
        OperatorDraftMappingProfile.Map(await service.SaveAsync(request.Actor, request.Key, request.ExpectedVersion, request.Content, ct), mapper);
}
