using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Essences.Commands.SetEssenceFocus;

public record SetEssenceFocusCommand(Guid CharacterId, string? CreatureId) : ICommand<CreatureArchiveDto>;

public sealed class SetEssenceFocusCommandHandler : IRequestHandler<SetEssenceFocusCommand, CreatureArchiveDto>
{
    private readonly IMapper _mapper;
    private readonly ICreatureArchiveService _service;

    public SetEssenceFocusCommandHandler(IMapper mapper, ICreatureArchiveService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<CreatureArchiveDto> Handle(SetEssenceFocusCommand request, CancellationToken cancellationToken) =>
        _mapper.Map<CreatureArchiveDto>(
            await _service.SetEssenceFocusAsync(request.CharacterId, request.CreatureId, cancellationToken));
}
