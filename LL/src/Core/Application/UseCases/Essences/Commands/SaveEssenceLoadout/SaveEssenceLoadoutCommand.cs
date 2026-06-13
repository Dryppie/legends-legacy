using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Essences;
using MediatR;

namespace Application.UseCases.Essences.Commands.SaveEssenceLoadout;

public record SaveEssenceLoadoutCommand(Guid CharacterId, SaveEssenceLoadoutDto Request) : ICommand<Response<EssenceLoadoutDto>>;

public class SaveEssenceLoadoutCommandHandler : IRequestHandler<SaveEssenceLoadoutCommand, Response<EssenceLoadoutDto>>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;

    public SaveEssenceLoadoutCommandHandler(IMapper mapper, IEssenceService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<Response<EssenceLoadoutDto>> Handle(SaveEssenceLoadoutCommand request, CancellationToken cancellationToken)
    {
        var saveRequest = _mapper.Map<SaveEssenceLoadoutRequest>(request.Request);
        var result = await _service.SaveLoadoutAsync(request.CharacterId, saveRequest, cancellationToken);
        return Response<EssenceLoadoutDto>.Success(_mapper.Map<EssenceLoadoutDto>(result));
    }
}
