using Application.Interfaces.Services.LL.CombatStyles;
using Application.MediatR.Markers;
using Application.UseCases.CombatStyles.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.CombatStyles.Commands.RankUpCombatStyleNode;

public sealed record RankUpCombatStyleNodeCommand(Guid CharacterId, string StyleId, string NodeId)
    : ICommand<Response<CombatStyleMutationResponseDto>>;

public sealed class RankUpCombatStyleNodeCommandHandler
    : IRequestHandler<RankUpCombatStyleNodeCommand, Response<CombatStyleMutationResponseDto>>
{
    private readonly ICombatStyleService _service;
    private readonly IMapper _mapper;

    public RankUpCombatStyleNodeCommandHandler(ICombatStyleService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    public async Task<Response<CombatStyleMutationResponseDto>> Handle(
        RankUpCombatStyleNodeCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _service.RankUpNodeAsync(
            request.CharacterId,
            request.StyleId,
            request.NodeId,
            cancellationToken);

        if (!result.Succeeded)
            return Response<CombatStyleMutationResponseDto>.Fail(result.Message);

        return Response<CombatStyleMutationResponseDto>.Success(
            _mapper.Map<CombatStyleMutationResponseDto>(result));
    }
}
