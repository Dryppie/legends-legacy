using Application.Interfaces.Services.LL.CombatStyles;
using Application.MediatR.Markers;
using Application.UseCases.CombatStyles.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.CombatStyles.Commands.ActivateCombatStyle;

public sealed record ActivateCombatStyleCommand(Guid CharacterId, string StyleId)
    : ICommand<Response<ActivateCombatStyleResponseDto>>;

public sealed class ActivateCombatStyleCommandHandler
    : IRequestHandler<ActivateCombatStyleCommand, Response<ActivateCombatStyleResponseDto>>
{
    private readonly ICombatStyleService _service;
    private readonly IMapper _mapper;

    public ActivateCombatStyleCommandHandler(ICombatStyleService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    public async Task<Response<ActivateCombatStyleResponseDto>> Handle(
        ActivateCombatStyleCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ActivateStyleAsync(request.CharacterId, request.StyleId, cancellationToken);
        var dto = _mapper.Map<ActivateCombatStyleResponseDto>(result);

        return result.Succeeded
            ? Response<ActivateCombatStyleResponseDto>.Success(dto)
            : Response<ActivateCombatStyleResponseDto>.Fail(result.Message);
    }
}
