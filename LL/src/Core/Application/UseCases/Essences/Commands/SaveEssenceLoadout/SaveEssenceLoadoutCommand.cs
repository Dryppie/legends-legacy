using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Commands;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Essences;
using MediatR;

namespace Application.UseCases.Essences.Commands.SaveEssenceLoadout;

public record SaveEssenceLoadoutCommand(Guid CharacterId, SaveEssenceLoadoutDto Request) : ICommand<Response<EssenceStateResponseDto>>;

public class SaveEssenceLoadoutCommandHandler : IRequestHandler<SaveEssenceLoadoutCommand, Response<EssenceStateResponseDto>>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;
    private readonly EssenceMutationResponseFactory _responses;

    public SaveEssenceLoadoutCommandHandler(
        IMapper mapper,
        IEssenceService service,
        EssenceMutationResponseFactory responses)
    {
        _mapper = mapper;
        _service = service;
        _responses = responses;
    }

    public async Task<Response<EssenceStateResponseDto>> Handle(SaveEssenceLoadoutCommand request, CancellationToken cancellationToken)
    {
        var saveRequest = _mapper.Map<SaveEssenceLoadoutRequest>(request.Request);
        var result = await _service.SaveLoadoutAsync(request.CharacterId, saveRequest, cancellationToken);
        return Response<EssenceStateResponseDto>.Success(await _responses.CreateStateAsync(
            request.CharacterId,
            true,
            "Essence loadout saved.",
            cancellationToken,
            result));
    }
}
