using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.FavoriteEssence;

public record FavoriteEssenceCommand(Guid CharacterId, Guid PlayerEssenceId, bool IsFavorite) : ICommand<Response<ResponseMessageDto>>;

public class FavoriteEssenceCommandHandler : IRequestHandler<FavoriteEssenceCommand, Response<ResponseMessageDto>>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;

    public FavoriteEssenceCommandHandler(IMapper mapper, IEssenceService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<Response<ResponseMessageDto>> Handle(FavoriteEssenceCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.SetFavoriteAsync(request.CharacterId, request.PlayerEssenceId, request.IsFavorite, cancellationToken);
        var dto = _mapper.Map<ResponseMessageDto>(result);
        return result.Succeeded ? Response<ResponseMessageDto>.Success(dto) : Response<ResponseMessageDto>.Fail(result.Message);
    }
}
