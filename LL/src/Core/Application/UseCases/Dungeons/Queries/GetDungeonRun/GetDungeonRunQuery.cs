using Application.Interfaces.Services.LL.Dungeons;
using Application.MediatR.Markers;
using Application.UseCases.Dungeons.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Dungeons.Queries.GetDungeonRun;
public record GetDungeonRunQuery(Guid CharacterId) : IQuery<DungeonRunDto?>;
public class GetDungeonRunQueryHandler : IRequestHandler<GetDungeonRunQuery, DungeonRunDto?>
{
    private readonly IDungeonRunService _dungeonRunService;
    private readonly IMapper _mapper;

    public GetDungeonRunQueryHandler(IDungeonRunService dungeonRunService, IMapper mapper)
    {
        _dungeonRunService = dungeonRunService;
        _mapper = mapper;
    }

    public async Task<DungeonRunDto?> Handle(GetDungeonRunQuery request, CancellationToken cancellationToken)
    {
        var dungeon = await _dungeonRunService.GetDungeonRunAsync(request.CharacterId, cancellationToken);
        if (dungeon == null) { return null; }

        var result = _mapper.Map<DungeonRunDto>(dungeon);
        return result;
    }
}