using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using AutoMapper;
using Domain.Models.Dungeons.Runs;
using MediatR;

namespace Application.UseCases.Dungeons.Queries.GetDungeonRun;
public record GetDungeonRunQuery(Guid CharacterId) : IQuery<DungeonRun?>;
public class GetDungeonRunQueryHandler : IRequestHandler<GetDungeonRunQuery, DungeonRun?>
{
    private readonly IDungeonRunService _dungeonRunService;
    private readonly IMapper _mapper;

    public GetDungeonRunQueryHandler(IDungeonRunService dungeonRunService, IMapper mapper)
    {
        _dungeonRunService = dungeonRunService;
        _mapper = mapper;
    }

    public async Task<DungeonRun?> Handle(GetDungeonRunQuery request, CancellationToken cancellationToken)
    {
        var dungeonRun = await _dungeonRunService.GetDungeonRunAsync(request.CharacterId, cancellationToken);

        return _mapper.Map<DungeonRun>(dungeonRun);
    }
}