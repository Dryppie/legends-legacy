using Application.Interfaces.Services.LL.CombatStyles;
using Application.MediatR.Markers;
using Application.UseCases.CombatStyles.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.CombatStyles.Commands.ResetCombatStyleTree;

public sealed record ResetCombatStyleTreeCommand(Guid CharacterId, string StyleId)
    : ICommand<Response<CombatStyleMutationResponseDto>>;

public sealed class ResetCombatStyleTreeCommandHandler
    : IRequestHandler<ResetCombatStyleTreeCommand, Response<CombatStyleMutationResponseDto>>
{
    private readonly ICombatStyleService _service;
    private readonly IMapper _mapper;

    public ResetCombatStyleTreeCommandHandler(ICombatStyleService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    public async Task<Response<CombatStyleMutationResponseDto>> Handle(
        ResetCombatStyleTreeCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ResetSkillTreeAsync(
            request.CharacterId,
            request.StyleId,
            cancellationToken);

        if (!result.Succeeded)
            return Response<CombatStyleMutationResponseDto>.Fail(result.Message);

        return Response<CombatStyleMutationResponseDto>.Success(
            _mapper.Map<CombatStyleMutationResponseDto>(result));
    }
}
