using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Essences.Commands.DeleteEssenceLoadout;

public record DeleteEssenceLoadoutCommand(Guid CharacterId, Guid LoadoutId) : ICommand<Response<ResponseMessageDto>>;

public class DeleteEssenceLoadoutCommandHandler : IRequestHandler<DeleteEssenceLoadoutCommand, Response<ResponseMessageDto>>
{
    private readonly IMapper _mapper;
    private readonly IEssenceService _service;

    public DeleteEssenceLoadoutCommandHandler(IMapper mapper, IEssenceService service)
    {
        _mapper = mapper;
        _service = service;
    }

    public async Task<Response<ResponseMessageDto>> Handle(DeleteEssenceLoadoutCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteLoadoutAsync(request.CharacterId, request.LoadoutId, cancellationToken);
        var dto = _mapper.Map<ResponseMessageDto>(result);
        return result.Succeeded ? Response<ResponseMessageDto>.Success(dto) : Response<ResponseMessageDto>.Fail(result.Message);
    }
}
