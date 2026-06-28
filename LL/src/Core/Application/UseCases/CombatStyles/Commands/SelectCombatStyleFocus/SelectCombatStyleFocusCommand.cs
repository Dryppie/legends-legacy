using Application.Interfaces.Services.LL.CombatStyles;
using Application.MediatR.Markers;
using Application.UseCases.CombatStyles.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.CombatStyles.Commands.SelectCombatStyleFocus;

public sealed record SelectCombatStyleFocusCommand(Guid CharacterId, string StyleId, string FocusId)
    : ICommand<Response<CombatStyleDto>>;

public sealed class SelectCombatStyleFocusCommandHandler
    : IRequestHandler<SelectCombatStyleFocusCommand, Response<CombatStyleDto>>
{
    private readonly ICombatStyleService _service;
    private readonly IMapper _mapper;

    public SelectCombatStyleFocusCommandHandler(ICombatStyleService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    public async Task<Response<CombatStyleDto>> Handle(
        SelectCombatStyleFocusCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _service.SelectFocusAsync(
            request.CharacterId,
            request.StyleId,
            request.FocusId,
            cancellationToken);

        return result.Succeeded && result.Value is not null
            ? Response<CombatStyleDto>.Success(_mapper.Map<CombatStyleDto>(result.Value))
            : Response<CombatStyleDto>.Fail(result.Message);
    }
}
