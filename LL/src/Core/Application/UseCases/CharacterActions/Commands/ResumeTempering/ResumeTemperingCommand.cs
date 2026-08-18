using Application.Interfaces.Services.LL.CharacterActions;
using Application.MediatR.Markers;
using Application.UseCases.CharacterActions.Dtos.Responses;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.CharacterActions.Commands.ResumeTempering;

public record ResumeTemperingCommand(Guid CharacterId)
    : ICommand<Response<CharacterActionDto>>;

public sealed class ResumeTemperingCommandHandler
    : IRequestHandler<ResumeTemperingCommand, Response<CharacterActionDto>>
{
    private readonly ICharacterActionService _characterActionService;
    private readonly IMapper _mapper;

    public ResumeTemperingCommandHandler(
        ICharacterActionService characterActionService,
        IMapper mapper)
    {
        _characterActionService = characterActionService;
        _mapper = mapper;
    }

    public async Task<Response<CharacterActionDto>> Handle(
        ResumeTemperingCommand request,
        CancellationToken cancellationToken)
    {
        var action = await _characterActionService.ResumeTemperingAsync(
            request.CharacterId,
            cancellationToken);

        return action == null
            ? Response<CharacterActionDto>.Fail("No paused Tempering queue is available.")
            : Response<CharacterActionDto>.Success(_mapper.Map<CharacterActionDto>(action));
    }
}
