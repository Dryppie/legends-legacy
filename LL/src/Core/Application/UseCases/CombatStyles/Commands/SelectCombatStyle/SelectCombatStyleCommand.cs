using Application.Interfaces.Services.LL.CombatStyles;
using Application.MediatR.Markers;
using Application.UseCases.CombatStyles.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.CombatStyles;
using MediatR;

namespace Application.UseCases.CombatStyles.Commands.SelectCombatStyle;

public sealed record SelectCombatStyleCommand(Guid CharacterId, CombatStyleSelectionDto Selection) : ICommand<Response<CombatStyleOverviewDto>>;

public sealed class SelectCombatStyleCommandHandler(ICombatStyleService service, IMapper mapper)
    : IRequestHandler<SelectCombatStyleCommand, Response<CombatStyleOverviewDto>>
{
    public async Task<Response<CombatStyleOverviewDto>> Handle(SelectCombatStyleCommand request, CancellationToken ct)
    {
        var result = await service.SelectAsync(request.CharacterId, mapper.Map<CombatStyleSelectionRequest>(request.Selection), ct);
        return result.Succeeded
            ? Response<CombatStyleOverviewDto>.Success(mapper.Map<CombatStyleOverviewDto>(
                await service.GetOverviewAsync(request.CharacterId, ct)))
            : Response<CombatStyleOverviewDto>.Fail(result.Message);
    }
}
